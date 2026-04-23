using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class IntroStoryManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI storyText;
    public TextMeshProUGUI chapterLabel;
    public TextMeshProUGUI yearBadge;
    public Image fadeOverlay;
    public Slider progressBar;
    public Button skipButton;
    public Button nextButton;
    public Button prevButton;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip[] chapterMusic;
    public AudioClip typingSound;
    public AudioClip chapterEndSound;

    [Header("Settings")]
    public float typingSpeed = 0.04f;
    public float fadeDuration = 0.8f;
    public string nextScene = "TestCombat";

    [Header("Story Data")]
    public StoryChapter[] chapters;

    // ── Internal flat line list ──────────────────────
    private struct FlatLine { public int chapterIdx; public StoryLine line; }
    private readonly List<FlatLine> _allLines = new();

    private int _currentIndex = -1;
    private int _activeChapterIdx = -1;

    private Coroutine _activeCoroutine;
    private bool _isTyping = false;
    private string _currentFullText = "";

    // ────────────────────────────────────────────────
    void Start()
    {
        for (int c = 0; c < chapters.Length; c++)
            foreach (var ln in chapters[c].lines)
                _allLines.Add(new FlatLine { chapterIdx = c, line = ln });

        skipButton.onClick.AddListener(OnSkip);
        nextButton.onClick.AddListener(OnNext);
        prevButton.onClick.AddListener(OnPrev);

        StartCoroutine(BeginIntro());
    }

    void Update()
    {
        // Any keyboard key (not mouse) while typing → show full text immediately
        bool anyKeyboardKey = Input.anyKeyDown
                              && !Input.GetMouseButtonDown(0)
                              && !Input.GetMouseButtonDown(1)
                              && !Input.GetMouseButtonDown(2);

        if (_isTyping && anyKeyboardKey)
        {
            CompleteCurrentLine();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))                                          OnSkip();
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightArrow))  OnNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow))                                       OnPrev();
    }

    // ── Button handlers ──────────────────────────────
    void OnNext()
    {
        if (_isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        int next = _currentIndex + 1;
        if (next >= _allLines.Count)
        {
            SetNavButtons(false);
            StopActiveCoroutine();
            StartCoroutine(EndSequence());
            return;
        }
        ShowLineAt(next);
    }

    void OnPrev()
    {
        if (_currentIndex <= 0) return;
        ShowLineAt(_currentIndex - 1);
    }

    void OnSkip()
    {
        SetNavButtons(false);
        StopActiveCoroutine();
        StartCoroutine(EndSequence());
    }

    // ── Navigation core ──────────────────────────────
    IEnumerator BeginIntro()
    {
        SetNavButtons(false);
        // Start fully black
        SetOverlayAlpha(1f);
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FadeOverlay(1f, 0f, fadeDuration));

        SetNavButtons(true);
        ShowLineAt(0);
    }

    void ShowLineAt(int index)
    {
        StopActiveCoroutine();
        _currentIndex = index;
        UpdateButtonStates();
        UpdateProgressBar();
        _activeCoroutine = StartCoroutine(ShowLineRoutine());
    }

    IEnumerator ShowLineRoutine()
    {
        var flat = _allLines[_currentIndex];

        // Chapter change — update music & label
        if (flat.chapterIdx != _activeChapterIdx)
        {
            _activeChapterIdx = flat.chapterIdx;
            var chapter = chapters[_activeChapterIdx];

            if (chapterMusic != null && _activeChapterIdx < chapterMusic.Length
                                     && chapterMusic[_activeChapterIdx] != null)
            {
                if (musicSource.isPlaying)
                    yield return StartCoroutine(FadeAudio(musicSource, musicSource.volume, 0f, 0.4f));
                musicSource.clip = chapterMusic[_activeChapterIdx];
                musicSource.Play();
                yield return StartCoroutine(FadeAudio(musicSource, 0f, chapter.musicVolume, 0.4f));
            }

            if (chapterLabel != null)
            {
                chapterLabel.text = chapter.chapterTitle;
                yield return StartCoroutine(FadeTextAlpha(chapterLabel, 0f, 1f, 0.5f));
            }
        }

        yield return StartCoroutine(DisplayLine(flat.line));
    }

    IEnumerator DisplayLine(StoryLine line)
    {
        yield return StartCoroutine(FadeTextAlpha(storyText, storyText.alpha, 0f, 0.25f));

        if (!string.IsNullOrEmpty(line.overrideYearBadge) && yearBadge != null)
            yearBadge.text = line.overrideYearBadge;

        _currentFullText = line.text;
        yield return StartCoroutine(TypeLine(line.text));
    }

    IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        storyText.text = "";
        yield return StartCoroutine(FadeTextAlpha(storyText, 0f, 1f, 0.2f));

        foreach (char c in line)
        {
            storyText.text += c;

            if (typingSound != null && c != ' ' && c != '\n' && !sfxSource.isPlaying)
            {
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.PlayOneShot(typingSound, 0.3f);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
    }

    // Instantly finish the current typing animation
    void CompleteCurrentLine()
    {
        StopActiveCoroutine();
        _isTyping = false;
        storyText.text = _currentFullText;
        storyText.alpha = 1f;
    }

    // ── End sequence ─────────────────────────────────
    IEnumerator EndSequence()
    {
        yield return StartCoroutine(FadeTextAlpha(storyText, storyText.alpha, 0f, 0.5f));
        yield return new WaitForSeconds(0.3f);

        storyText.text = "[ Sẵn sàng chiến đấu... ]";
        yield return StartCoroutine(FadeTextAlpha(storyText, 0f, 1f, 0.5f));
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(FadeOverlay(0f, 1f, 1.2f));

        if (musicSource.isPlaying)
            yield return StartCoroutine(FadeAudio(musicSource, musicSource.volume, 0f, 0.8f));

        SceneManager.LoadScene(nextScene);
    }

    // ── Helpers ──────────────────────────────────────
    void StopActiveCoroutine()
    {
        // StopAllCoroutines stops nested coroutines too (TypeLine, FadeTextAlpha, etc.)
        // preventing multiple coroutines from writing to storyText simultaneously.
        StopAllCoroutines();
        _activeCoroutine = null;
        _isTyping = false;
    }

    void SetNavButtons(bool active)
    {
        if (nextButton != null) nextButton.gameObject.SetActive(active);
        if (prevButton != null) prevButton.gameObject.SetActive(active);
    }

    void UpdateButtonStates()
    {
        if (prevButton != null)
            prevButton.interactable = _currentIndex > 0;

        if (nextButton != null)
            nextButton.interactable = true;
    }

    void UpdateProgressBar()
    {
        if (progressBar != null && _allLines.Count > 0)
            progressBar.value = (float)(_currentIndex + 1) / _allLines.Count;
    }

    void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color;
        c.a = alpha;
        fadeOverlay.color = c;
    }

    IEnumerator FadeOverlay(float from, float to, float duration = 0.5f)
    {
        if (fadeOverlay == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetOverlayAlpha(to);
    }

    IEnumerator FadeTextAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        if (tmp == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tmp.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        tmp.alpha = to;
    }

    IEnumerator FadeAudio(AudioSource source, float from, float to, float duration = 0.5f)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        source.volume = to;
    }
}
