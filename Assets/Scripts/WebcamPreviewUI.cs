using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nhận JPEG frame từ Python (port 9998) và hiển thị lên RawImage góc màn hình.
/// QUAN TRỌNG: Gắn script này lên một GameObject LUÔN ACTIVE (ví dụ HUD root).
/// visualRoot là con chứa phần visual — script sẽ bật/tắt visualRoot thay vì tự tắt.
/// </summary>
public class WebcamPreviewUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("GameObject chứa toàn bộ phần visual preview (RawImage, background…). Phải là con luôn có parent active — script sẽ bật/tắt object này.")]
    [SerializeField] private GameObject visualRoot;
    [Tooltip("RawImage sẽ hiển thị camera preview")]
    [SerializeField] private RawImage previewImage;

    [Header("Connection")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int    framePort = 9998;
    [Tooltip("Số giây chờ giữa các lần thử kết nối lại")]
    [SerializeField] private float  reconnectDelay = 1.5f;
    [Header("Display")]
    [Tooltip("Nếu bật, khung preview luôn hiển thị. Nếu tắt, chỉ hiển thị khi Python mode ON.")]
    [SerializeField] private bool alwaysShowPreview = true;

    // Thread-safe frame buffer
    private volatile byte[] _pendingFrame;
    private Thread           _thread;
    private volatile bool    _running;
    private TcpClient        _client;

    private Texture2D _tex;

    // ── Unity lifecycle ──────────────────────────────────────────────

    void Awake()
    {
        if (previewImage == null)
            previewImage = GetComponentInChildren<RawImage>(true);

        _tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        _tex.filterMode = FilterMode.Bilinear;

        if (previewImage != null)
            previewImage.texture = _tex;

        // Ẩn visual ngay khi khởi động (script vẫn active để Update chạy được)
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    void Start()
    {
        _running = true;
        _thread  = new Thread(ReceiveLoop) { IsBackground = true, Name = "WebcamPreview" };
        _thread.Start();
    }

    void Update()
    {
        // Có thể luôn hiển thị khung cam, hoặc chỉ hiện khi Python mode ON.
        bool wantActive = alwaysShowPreview || ControlMode.IsPython;
        if (visualRoot != null && visualRoot.activeSelf != wantActive)
            visualRoot.SetActive(wantActive);

        // Áp frame mới nhất lên texture (chỉ chạy trên main thread)
        byte[] frame = _pendingFrame;
        if (frame == null) return;

        _pendingFrame = null;
        _tex.LoadImage(frame);   // LoadImage tự resize texture theo ảnh
    }

    void OnDestroy()
    {
        _running = false;
        try { _client?.Close(); } catch { }
        try { _thread?.Interrupt(); } catch { }
        try
        {
            if (_thread != null && _thread.IsAlive)
                _thread.Join(300);
        }
        catch { }
    }

    // ── Background receive thread ────────────────────────────────────

    void ReceiveLoop()
    {
        while (_running)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(host, framePort);
                _client.NoDelay = true;

                Debug.Log("[WebcamPreview] Kết nối tới Python frame server OK.");

                var stream  = _client.GetStream();
                var lenBuf  = new byte[4];

                while (_running)
                {
                    ReadExact(stream, lenBuf, 4);
                    int len = (lenBuf[0] << 24) | (lenBuf[1] << 16) | (lenBuf[2] << 8) | lenBuf[3];

                    if (len <= 0 || len > 2_000_000)   // sanity check (max ~2MB)
                        throw new Exception($"Frame size không hợp lệ: {len}");

                    var data = new byte[len];
                    ReadExact(stream, data, len);
                    _pendingFrame = data;   // atomic write
                }
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_running)
                    Debug.LogWarning($"[WebcamPreview] Mất kết nối: {ex.Message}. Thử lại sau {reconnectDelay}s…");
            }
            finally
            {
                try { _client?.Close(); } catch { }
                _client = null;
            }

            if (_running)
            {
                try
                {
                    Thread.Sleep((int)(reconnectDelay * 1000));
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }
    }

    static void ReadExact(NetworkStream stream, byte[] buf, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buf, offset, count - offset);
            if (read == 0) throw new Exception("Connection closed by peer.");
            offset += read;
        }
    }
}
