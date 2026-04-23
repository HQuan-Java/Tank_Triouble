using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
                if (instance == null)
                {
                    var go = new GameObject(nameof(GameManager));
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    [Header("End Game Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private string winPanelObjectName = "WinPanel";
    [SerializeField] private string losePanelObjectName = "LosePanel";

    private bool isGameEnded = false;
    public bool IsGameEnded => isGameEnded;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetForCurrentScene();
        Debug.Log($"[GameManager] Awake | isGameEnded={isGameEnded} | timeScale={Time.timeScale}");
    }

    private void Start()
    {
        Debug.Log($"[GameManager] Start | isGameEnded={isGameEnded}");
        HideEndPanels();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log($"[GameManager] OnDestroy | isGameEnded={isGameEnded}\nStack:\n{System.Environment.StackTrace}");
        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetForCurrentScene();
        Debug.Log($"[GameManager] Scene loaded: {scene.name} | mode={mode} | isGameEnded={isGameEnded}");
    }

    private void ResetForCurrentScene()
    {
        isGameEnded = false;
        Time.timeScale = 1f;
        RebindEndPanelsIfNeeded();
        HideEndPanels();
    }

    private void HideEndPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void RebindEndPanelsIfNeeded()
    {
        if (!IsSceneObjectValid(winPanel))
            winPanel = FindSceneObjectByName(winPanelObjectName, "win", "victory", "youwin");

        if (!IsSceneObjectValid(losePanel))
            losePanel = FindSceneObjectByName(losePanelObjectName, "lose", "fail", "defeat", "gameover");
    }

    private static bool IsSceneObjectValid(GameObject obj)
    {
        return obj != null && obj.scene.IsValid() && obj.scene.isLoaded;
    }

    private static GameObject FindSceneObjectByName(string exactName, params string[] fallbackKeywords)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    if (!string.IsNullOrEmpty(exactName) &&
                        string.Equals(t.name, exactName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return t.gameObject;
                    }
                }
            }
        }

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    string lowerName = t.name.ToLowerInvariant();
                    for (int i = 0; i < fallbackKeywords.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(fallbackKeywords[i]) && lowerName.Contains(fallbackKeywords[i]))
                            return t.gameObject;
                    }
                }
            }
        }

        return null;
    }

    public void GameWin()
    {
        if (isGameEnded) return;
        RebindEndPanelsIfNeeded();
        isGameEnded = true;

        Debug.Log("You win!", this);
        Debug.Log($"[GameManager] GameWin called | stack:\n{System.Environment.StackTrace}");
        if (winPanel != null)
        {
            StartCoroutine(FadeInPanel(winPanel));
        }
        else
        {
            Debug.LogWarning("[GameManager] GameWin: winPanel is NULL! Hãy gán Win Panel vào Inspector của GameManager.", this);
        }
    }

    public void GameOver()
    {
        if (isGameEnded) return;
        RebindEndPanelsIfNeeded();
        isGameEnded = true;

        Debug.Log("Game Over!", this);
        Debug.Log($"[GameManager] GameOver called | stack:\n{System.Environment.StackTrace}");
        if (losePanel != null)
        {
            StartCoroutine(FadeInPanel(losePanel));
        }
    }

    private IEnumerator FadeInPanel(GameObject panel)
    {
        panel.SetActive(true);
        
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        Cursor.visible = true;
    }

    [Button]
    public void ScreenShot()
    {
        ScreenCapture.CaptureScreenshot("Assets/Art/ScreenShot.png");
        Debug.Log("Screenshot Captured!");
    }
}
