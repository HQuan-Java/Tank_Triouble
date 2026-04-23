using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketReceiver : MonoBehaviour
{
    private static SocketReceiver _instance;
    private const int Port = 9999;

    private TcpListener server;
    private Thread thread;
    private volatile bool _running;

    // ── Pointer (tay phải người dùng) ────────────────────────────────
    public Vector2 pointerNorm { get; private set; } = new Vector2(0.5f, 0.5f);
    public bool pointerActive { get; private set; } = false;

    // ── Command (tay điều khiển) ─────────────────────────────────────
    public int moveDir { get; private set; } = 0;     // 1=forward, -1=backward, 0=stop
    public int turnDir { get; private set; } = 0;     // 1=left, -1=right, 0=straight
    public bool handShoot { get; private set; } = false;

    // Backward-compat
    public volatile string gesture = "STOP";

    // ── Pending data từ background thread ────────────────────────────
    private float _pendingPx = 0.5f;
    private float _pendingPy = 0.5f;
    private bool _pendingPa = false;
    private int _pendingMove = 0;
    private int _pendingTurn = 0;
    private bool _pendingShoot = false;
    private bool _hasNew = false;

    [Serializable]
    private struct HandPacket
    {
        public int move;
        public int turn;
        public int shoot;
        public float px;
        public float py;
        public int pa;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _running = true;
        thread = new Thread(ServerLoop)
        {
            IsBackground = true,
            Name = "SocketReceiver"
        };
        thread.Start();
    }

    void Update()
    {
        if (!_hasNew) return;
        _hasNew = false;

        pointerNorm = new Vector2(_pendingPx, _pendingPy);
        pointerActive = _pendingPa;
        moveDir = _pendingMove;
        turnDir = _pendingTurn;
        handShoot = _pendingShoot;

        // Cập nhật gesture string để các script cũ vẫn chạy được
        if (_pendingShoot)
            gesture = "SHOOT";
        else if (_pendingMove == 1)
            gesture = "FORWARD";
        else if (_pendingMove == -1)
            gesture = "BACKWARD";
        else if (_pendingTurn == 1)
            gesture = "LEFT";
        else if (_pendingTurn == -1)
            gesture = "RIGHT";
        else
            gesture = "STOP";
    }

    void ServerLoop()
    {
        while (_running)
        {
            try
            {
                server = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
                server.Start();
                Debug.Log("[SocketReceiver] TCP server ready on port 9999.");
                break;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.LogWarning("[SocketReceiver] Port 9999 đang bận, thử lại...");
                Thread.Sleep(200);
            }
            catch (Exception e)
            {
                Debug.LogError("[SocketReceiver] Không mở được port 9999: " + e.Message);
                return;
            }
        }

        while (_running)
        {
            TcpClient client = null;
            try
            {
                client = server.AcceptTcpClient();
                client.NoDelay = true;
                Debug.Log("[SocketReceiver] ✅ Python đã kết nối!");
                ReceiveLoop(client);
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception e)
            {
                if (_running)
                    Debug.LogWarning("[SocketReceiver] Lỗi accept client: " + e.Message);
            }
            finally
            {
                try { client?.Close(); } catch { }
            }

            // Reset khi mất kết nối
            ResetPendingState();
        }
    }

    void ReceiveLoop(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        StringBuilder sb = new StringBuilder();
        byte[] buf = new byte[512];

        while (_running && client.Connected)
        {
            int len;
            try
            {
                len = stream.Read(buf, 0, buf.Length);
            }
            catch
            {
                break;
            }

            if (len == 0)
                break;

            sb.Append(Encoding.UTF8.GetString(buf, 0, len));

            while (true)
            {
                string current = sb.ToString();
                int nl = current.IndexOf('\n');
                if (nl < 0) break;

                string line = current.Substring(0, nl).Trim();
                sb.Remove(0, nl + 1);

                if (string.IsNullOrEmpty(line))
                    continue;

                ParseIncomingLine(line);
            }
        }
    }

    void ParseIncomingLine(string line)
    {
        // 1) Nhận JSON packet
        if (line.StartsWith("{"))
        {
            try
            {
                HandPacket pkt = JsonUtility.FromJson<HandPacket>(line);

                _pendingMove = Mathf.Clamp(pkt.move, -1, 1);
                _pendingTurn = Mathf.Clamp(pkt.turn, -1, 1);
                _pendingShoot = pkt.shoot == 1;
                _pendingPx = Mathf.Clamp01(pkt.px);
                _pendingPy = Mathf.Clamp01(pkt.py);
                _pendingPa = pkt.pa == 1;
                _hasNew = true;

                Debug.Log($"[SocketReceiver] JSON => move={_pendingMove}, turn={_pendingTurn}, shoot={_pendingShoot}, px={_pendingPx:F2}, py={_pendingPy:F2}, pa={_pendingPa}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SocketReceiver] JSON parse lỗi: " + ex.Message + " | line=" + line);
            }

            return;
        }

        // 2) Nhận plain text gesture
        string g = line.Trim().ToUpperInvariant();

        // Reset trước
        _pendingMove = 0;
        _pendingTurn = 0;
        _pendingShoot = false;

        switch (g)
        {
            case "FORWARD":
                _pendingMove = 1;
                break;

            case "BACKWARD":
                _pendingMove = -1;
                break;

            case "LEFT":
                _pendingTurn = 1;
                break;

            case "RIGHT":
                _pendingTurn = -1;
                break;

            case "SHOOT":
                _pendingShoot = true;
                break;

            case "STOP":
            default:
                // giữ hết = 0 / false
                break;
        }

        // Vì Python hiện tại không gửi pointer nên giữ giá trị mặc định này
        _pendingPx = 0.5f;
        _pendingPy = 0.5f;
        _pendingPa = false;
        _hasNew = true;

        Debug.Log($"[SocketReceiver] TEXT => {g} | move={_pendingMove}, turn={_pendingTurn}, shoot={_pendingShoot}");
    }

    void ResetPendingState()
    {
        _pendingMove = 0;
        _pendingTurn = 0;
        _pendingShoot = false;
        _pendingPx = 0.5f;
        _pendingPy = 0.5f;
        _pendingPa = false;
        _hasNew = true;

        Debug.Log("[SocketReceiver] Kết nối Python đã ngắt. Reset input về STOP.");
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        _running = false;

        try { server?.Stop(); } catch { }
        try { thread?.Interrupt(); } catch { }

        try
        {
            if (thread != null && thread.IsAlive)
                thread.Join(300);
        }
        catch { }

        server = null;
        thread = null;
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}