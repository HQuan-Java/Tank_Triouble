using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Script riêng cho nút "Connect Hand Control".
/// Gắn lên chính Button hoặc object bất kỳ, rồi gọi OnClickConnect từ OnClick().
/// </summary>
public class HandControlConnectButton : MonoBehaviour
{
    [Header("UI (optional)")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string idleText = "Connect Hand Control";
    [SerializeField] private string connectingText = "Dang ket noi...";
    [SerializeField] private string connectedText = "Da ket noi";

    [Header("Behavior")]
    [SerializeField] private bool disableButtonAfterConnected = true;
    [SerializeField] private Button playButton;
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool requireConnectedBeforePlay = true;

    private bool _isWaitingConnection;
    private bool _connected;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        SetStatus(idleText);

        if (playButton != null)
            playButton.interactable = !requireConnectedBeforePlay;
    }

    void Update()
    {
        if (!_isWaitingConnection || _connected)
            return;

        SocketReceiver receiver = FindAnyObjectByType<SocketReceiver>();
        bool ready = receiver != null && receiver.gesture != null;
        if (!ready) return;

        _connected = true;
        _isWaitingConnection = false;
        SetStatus(connectedText);

        if (button != null && disableButtonAfterConnected)
            button.interactable = false;

        if (playButton != null)
            playButton.interactable = true;
    }

    public void OnClickConnect()
    {
        if (_connected) return;

        _isWaitingConnection = true;
        SetStatus(connectingText);

        // Bật Python hand control và launch python process.
        ControlModePanel.ApplyControlState(true);
    }

    public void OnClickPlay()
    {
        if (requireConnectedBeforePlay && !_connected)
        {
            SetStatus("Hay connect hand control truoc.");
            return;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(current + 1);
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
