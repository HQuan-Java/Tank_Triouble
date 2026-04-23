using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Toggle điều khiển mode Keyboard/PythonHand.
/// ON  = Python hand control
/// OFF = Keyboard control
/// </summary>
public class ControlModePanel : MonoBehaviour
{
    [Header("References (tự điền bởi Setup tool)")]
    public Toggle          controlToggle;
    public TextMeshProUGUI modeLabel;
    public Image           toggleBg;

    static readonly Color ColorKeyboard = new Color(0.18f, 0.44f, 0.90f);
    static readonly Color ColorPython   = new Color(0.15f, 0.75f, 0.38f);

    const string PREFS_KEY = "ControlMode_v1";

    void Awake()
    {
        // Theo yeu cau: moi lan bat game, control luon mac dinh OFF (Keyboard).
        ControlMode.Current = ControlMode.Mode.Keyboard;
        PlayerPrefs.SetInt(PREFS_KEY, (int)ControlMode.Current);
        GameSettings.ControlEnabled = false;
        PlayerPrefs.Save();

        if (controlToggle != null)
        {
            controlToggle.SetIsOnWithoutNotify(ControlMode.IsPython);
            controlToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    void Start()
    {
        if (ControlMode.IsPython)
            LaunchPython();
        Refresh();
    }

    void OnDestroy()
    {
        if (controlToggle != null)
            controlToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    public static void ApplyControlState(bool enablePython)
    {
        ControlMode.Current = enablePython ? ControlMode.Mode.PythonHand : ControlMode.Mode.Keyboard;
        PlayerPrefs.SetInt(PREFS_KEY, (int)ControlMode.Current);
        PlayerPrefs.Save();
        GameSettings.ControlEnabled = enablePython;

        if (enablePython)
            LaunchPython();
        else
            ControlMode.KillPython();
    }

    void OnToggleValueChanged(bool enablePython)
    {
        ApplyControlState(enablePython);
        Refresh();
    }

    void Refresh()
    {
        if (ControlMode.IsPython)
        {
            if (modeLabel != null) modeLabel.text = "✋  ĐIỀU KHIỂN TAY";
            if (toggleBg != null) toggleBg.color = ColorPython;
        }
        else
        {
            if (modeLabel != null) modeLabel.text = "⌨  BÀN PHÍM";
            if (toggleBg != null) toggleBg.color = ColorKeyboard;
        }
    }

    static void LaunchPython()
    {
        // Tìm script trong StreamingAssets hoặc thư mục Python bên cạnh build
        string[] candidates = {
            Path.Combine(Application.streamingAssetsPath, "Python", "hand_control_socket.py"),
            Path.Combine(Application.dataPath, "..", "Python", "hand_control_socket.py"),
        };

        string scriptPath = null;
        foreach (var c in candidates)
            if (File.Exists(c)) { scriptPath = c; break; }

        if (scriptPath == null)
        {
            UnityEngine.Debug.LogError("[ControlMode] Đã dò các đường dẫn script:");
            foreach (var c in candidates)
                UnityEngine.Debug.LogError("[ControlMode]  - " + c);
            UnityEngine.Debug.LogError("[ControlMode] Không tìm thấy hand_control_socket.py");
            return;
        }

        string scriptDir = Path.GetDirectoryName(scriptPath) ?? string.Empty;
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string rootPythonVenv = Path.Combine(projectRoot, "Python", ".venv", "Scripts", "python.exe");
        string scriptLocalVenv = Path.Combine(scriptDir, ".venv", "Scripts", "python.exe");
        string scriptParentVenv = Path.GetFullPath(Path.Combine(scriptDir, "..", ".venv", "Scripts", "python.exe"));
        string buildSiblingVenv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".venv", "Scripts", "python.exe"));

        var launchers = new List<string>();
        void AddIfExists(string path)
        {
            if (!File.Exists(path)) return;
            if (launchers.Contains(path)) return;
            launchers.Add(path);
        }

        // Ưu tiên .venv chuẩn của project để mọi máy dùng cùng runtime.
        AddIfExists(rootPythonVenv);
        AddIfExists(scriptLocalVenv);
        AddIfExists(scriptParentVenv);
        AddIfExists(buildSiblingVenv);

        UnityEngine.Debug.Log("[ControlMode] Python launcher candidates:");
        foreach (var launcherPath in launchers)
            UnityEngine.Debug.Log("[ControlMode]  - " + launcherPath);

        launchers.Add("python");
        launchers.Add("py");
        launchers.Add("python3");

        foreach (var exe in launchers)
        {
            try
            {
                bool isPyLauncher = string.Equals(exe, "py", StringComparison.OrdinalIgnoreCase);
                string args = isPyLauncher
                    ? $"-3 -u \"{scriptPath}\""
                    : $"-u \"{scriptPath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName               = exe,
                    Arguments              = args,  // -u = unbuffered output
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                };
                var proc = Process.Start(psi);
                if (proc == null) continue;

                // Một số máy có python nhưng thiếu package, process sẽ thoát ngay.
                // Chờ ngắn để phát hiện lỗi sớm rồi thử launcher khác.
                if (proc.WaitForExit(600))
                {
                    string err = proc.StandardError.ReadToEnd();
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.Dispose();

                    UnityEngine.Debug.LogWarning(
                        $"[ControlMode] '{exe}' chạy nhưng thoát sớm. stdout={output} | stderr={err}");
                    continue;
                }

                // Pipe stdout/stderr → Unity Console để debug
                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        UnityEngine.Debug.Log("[Python] " + e.Data);
                };
                proc.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        UnityEngine.Debug.LogWarning("[Python ERR] " + e.Data);
                };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                ControlMode.SetProcess(proc);
                UnityEngine.Debug.Log($"[ControlMode] Python PID={proc.Id} | exe={exe} | script={scriptPath}");
                return;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[ControlMode] Thử '{exe}' thất bại: {ex.Message}");
            }
        }

        UnityEngine.Debug.LogError("[ControlMode] Không launch được Python. Hãy chạy script setup ở thư mục Python để tạo .venv chuẩn.");
    }
}
