using DG.Tweening;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UISettingPanel : MonoBehaviour
{
    public static bool IsPopupOpen { get; private set; }
    private static bool _timePausedByPopup;
    private static float _timeScaleBeforePopup = 1f;
#if ENABLE_INPUT_SYSTEM
    private static bool _inputModeChangedByPopup;
    private static InputSettings.UpdateMode _inputUpdateModeBeforePopup;
#endif

    [Header("Popup Controls")]
    [Tooltip("Chỉ GameObject của popup (panel nội dung). Phải là con của HUD hoặc object luôn active — không được trùng object chứa nút Setting.")]
    [SerializeField] private GameObject popupRoot;
    [Tooltip("Object HUD chứa nút Setting (luôn để active). Nếu để trống, script sẽ dùng chính GameObject gắn script này.")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backButton;
    [Tooltip("Tên scene sẽ load khi bấm Back. Để trống sẽ dùng scene trước theo Build Index.")]
    [SerializeField] private string backSceneName;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private float popupFadeDuration = 0.2f;
    [SerializeField] private float popupScaleFrom = 0.92f;

    [Header("Toggles")]
    [SerializeField] private Toggle toggleMusic;
    [SerializeField] private Toggle toggleSound;
    [SerializeField] private Toggle toggleControl;

    [Header("Visuals")]
    [SerializeField] private Image musicImage;
    [SerializeField] private Image soundImage;
    [SerializeField] private Image controlImage;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private TMP_Text soundLabel;
    [SerializeField] private TMP_Text controlLabel;
    [SerializeField] private float onX = 60f;
    [SerializeField] private float offX = -60f;
    [SerializeField] private float tweenDuration = 0.15f;
    private Animator[] _popupAnimators;
    private AnimatorUpdateMode[] _popupAnimatorModesBeforePause;

    private void Awake()
    {
        // Reset static state de tranh giu trang thai pause tu scene cu.
        _timePausedByPopup = false;
        _timeScaleBeforePopup = 1f;
#if ENABLE_INPUT_SYSTEM
        _inputModeChangedByPopup = false;
#endif

        if (hudRoot == null)
            hudRoot = gameObject;

        if (popupRoot == null)
        {
            Debug.LogError("[UISettingPanel] Chưa gán popupRoot. Gán đúng GameObject popup (không phải object chứa nút Setting).");
            return;
        }

        if (popupRoot == gameObject)
            Debug.LogError("[UISettingPanel] Script đang gắn trên cùng object với popupRoot. Hãy gắn script lên parent HUD (luôn active), chỉ gán popupRoot = child panel popup.");

        if (settingButton != null && popupRoot.transform.IsChildOf(settingButton.transform))
            Debug.LogError("[UISettingPanel] popupRoot đang nằm dưới settingButton — khi tắt popup sẽ tắt luôn nút. Hãy tách popup ra cùng cấp hoặc đặt script trên parent HUD luôn active.");

        if (popupCanvasGroup == null)
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        if (popupRect == null)
            popupRect = popupRoot.GetComponent<RectTransform>();
        _popupAnimators = popupRoot.GetComponentsInChildren<Animator>(true);
        _popupAnimatorModesBeforePause = new AnimatorUpdateMode[_popupAnimators.Length];

        ClosePopupImmediate();
    }

    private void Start()
    {
        LoadVisual();
        BindListeners();
    }

    private void OnDestroy()
    {
        UnbindListeners();
    }

    private void BindListeners()
    {
        if (settingButton != null)
            settingButton.onClick.AddListener(OpenPopup);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePopup);
        if (backButton != null)
            backButton.onClick.AddListener(GoToPreviousScene);

        toggleMusic.onValueChanged.AddListener(ToggleMusic);
        toggleSound.onValueChanged.AddListener(ToggleSound);
        toggleControl.onValueChanged.AddListener(ToggleControl);
    }

    private void UnbindListeners()
    {
        if (settingButton != null)
            settingButton.onClick.RemoveListener(OpenPopup);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(ClosePopup);
        if (backButton != null)
            backButton.onClick.RemoveListener(GoToPreviousScene);

        toggleMusic.onValueChanged.RemoveListener(ToggleMusic);
        toggleSound.onValueChanged.RemoveListener(ToggleSound);
        toggleControl.onValueChanged.RemoveListener(ToggleControl);
    }

    public void OpenPopup()
    {
        if (popupRoot == null)
            return;

        PauseGameForPopup();
        SetPopupAnimatorsUseUnscaled(true);
        IsPopupOpen = true;
        SetCursorForUI(true);
        popupRoot.SetActive(true);
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.DOKill();
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
            popupCanvasGroup.DOFade(1f, popupFadeDuration).SetUpdate(true);
        }

        if (popupRect != null)
        {
            popupRect.DOKill();
            popupRect.localScale = Vector3.one * popupScaleFrom;
            popupRect.DOScale(1f, popupFadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (popupCanvasGroup != null)
        {
            DOVirtual.DelayedCall(popupFadeDuration, () =>
            {
                if (popupCanvasGroup != null)
                {
                    popupCanvasGroup.interactable = true;
                    popupCanvasGroup.blocksRaycasts = true;
                }
            }).SetUpdate(true);
        }

        LoadVisual();
    }

    public void ClosePopup()
    {
        if (popupRoot == null)
            return;

        if (popupCanvasGroup == null)
        {
            popupRoot.SetActive(false);
            IsPopupOpen = false;
            SetPopupAnimatorsUseUnscaled(false);
            ResumeGameAfterPopup();
            SetCursorForUI(false);
            return;
        }

        popupCanvasGroup.DOKill();
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;
        popupCanvasGroup.DOFade(0f, popupFadeDuration).SetUpdate(true).OnComplete(() =>
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);
            IsPopupOpen = false;
            SetPopupAnimatorsUseUnscaled(false);
            ResumeGameAfterPopup();
            SetCursorForUI(false);
        });

        if (popupRect != null)
        {
            popupRect.DOKill();
            popupRect.DOScale(popupScaleFrom, popupFadeDuration).SetEase(Ease.InBack).SetUpdate(true);
        }
    }

    public void GoToPreviousScene()
    {
        IsPopupOpen = false;
        SetPopupAnimatorsUseUnscaled(false);
        ResumeGameAfterPopup();
        SetCursorForUI(false);

        if (!string.IsNullOrEmpty(backSceneName))
        {
            SceneManager.LoadScene(backSceneName);
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int previousIndex = currentIndex - 1;
        if (previousIndex < 0)
        {
            Debug.LogWarning("[UISettingPanel] Khong the quay ve scene truoc vi dang o scene dau tien trong Build Settings.");
            return;
        }
        SceneManager.LoadScene(previousIndex);
    }

    private void ClosePopupImmediate()
    {
        if (popupRoot == null)
            return;

        IsPopupOpen = false;
        SetPopupAnimatorsUseUnscaled(false);
        ResumeGameAfterPopup();
        SetCursorForUI(false);

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.DOKill();
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
        }

        if (popupRect != null)
        {
            popupRect.DOKill();
            popupRect.localScale = Vector3.one;
        }

        popupRoot.SetActive(false);
    }

    private void SetPopupAnimatorsUseUnscaled(bool useUnscaledTime)
    {
        if (_popupAnimators == null || _popupAnimatorModesBeforePause == null)
            return;

        for (int i = 0; i < _popupAnimators.Length; i++)
        {
            Animator animator = _popupAnimators[i];
            if (animator == null)
                continue;

            if (useUnscaledTime)
            {
                _popupAnimatorModesBeforePause[i] = animator.updateMode;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
            else
            {
                animator.updateMode = _popupAnimatorModesBeforePause[i];
            }
        }
    }

    private static void PauseGameForPopup()
    {
        if (_timePausedByPopup)
            return;

        _timeScaleBeforePopup = Time.timeScale;
        Time.timeScale = 0f;
        _timePausedByPopup = true;

#if ENABLE_INPUT_SYSTEM
        // Neu Input System dang o FixedUpdate thi khi timeScale=0 UI se khong nhan click.
        // Tam chuyen sang DynamicUpdate de UI van hoat dong.
        if (InputSystem.settings != null &&
            InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate)
        {
            _inputUpdateModeBeforePopup = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            _inputModeChangedByPopup = true;
        }
#endif
    }

    private static void ResumeGameAfterPopup()
    {
        if (!_timePausedByPopup)
            return;

        Time.timeScale = _timeScaleBeforePopup;
        _timePausedByPopup = false;

#if ENABLE_INPUT_SYSTEM
        if (_inputModeChangedByPopup && InputSystem.settings != null)
        {
            InputSystem.settings.updateMode = _inputUpdateModeBeforePopup;
            _inputModeChangedByPopup = false;
        }
#endif
    }

    private static void SetCursorForUI(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Confined;
    }

    private void LoadVisual()
    {
        toggleMusic.SetIsOnWithoutNotify(GameSettings.MusicEnabled);
        toggleSound.SetIsOnWithoutNotify(GameSettings.SoundEnabled);
        toggleControl.SetIsOnWithoutNotify(GameSettings.ControlEnabled);

        ApplyToggleVisual(musicImage, musicLabel, GameSettings.MusicEnabled, false);
        ApplyToggleVisual(soundImage, soundLabel, GameSettings.SoundEnabled, false);
        ApplyToggleVisual(controlImage, controlLabel, GameSettings.ControlEnabled, false);
    }

    public void ToggleMusic(bool enable)
    {
        GameSettings.MusicEnabled = enable;
        ApplyToggleVisual(musicImage, musicLabel, enable, true);
    }

    public void ToggleSound(bool enable)
    {
        GameSettings.SoundEnabled = enable;
        ApplyToggleVisual(soundImage, soundLabel, enable, true);
    }

    public void ToggleControl(bool enable)
    {
        ControlModePanel.ApplyControlState(enable);
        ApplyToggleVisual(controlImage, controlLabel, enable, true);
    }

    private void ApplyToggleVisual(Image targetImage, TMP_Text targetLabel, bool enable, bool animate)
    {
        if (targetImage != null)
        {
            targetImage.color = enable ? Color.white : Color.gray;

            float targetX = enable ? onX : offX;
            if (animate)
            {
                targetImage.rectTransform.DOKill();
                targetImage.rectTransform.DOAnchorPosX(targetX, tweenDuration).SetUpdate(true);
            }
            else
                targetImage.rectTransform.anchoredPosition = new Vector2(targetX, targetImage.rectTransform.anchoredPosition.y);
        }

        if (targetLabel != null)
            targetLabel.text = enable ? "ON" : "OFF";
    }
}
