using System.Diagnostics;
using UnityEngine;

public static class ControlMode
{
    public enum Mode { Keyboard, PythonHand }

    public static Mode Current = Mode.Keyboard;
    public static bool IsPython => Current == Mode.PythonHand;

    // ── Python process (managed ở đây, không phụ thuộc vào scene) ──
    private static Process _process;

    /// Gọi sau khi launch Python để đăng ký process
    public static void SetProcess(Process p)
    {
        KillPython();
        _process = p;
    }

    public static void KillPython()
    {
        if (_process == null) return;
        try
        {
            if (!_process.HasExited)
                _process.Kill();
        }
        catch { }
        finally
        {
            _process.Dispose();
            _process = null;
            UnityEngine.Debug.Log("[ControlMode] Python process đã tắt.");
        }
    }

    // ── Đăng ký Application.quitting một lần duy nhất khi game start ──
    // Hoạt động bất kể scene nào, kể cả khi Stop Play mode trong Editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterQuitHandler()
    {
        // Luon reset ve Keyboard khi bat game.
        Current = Mode.Keyboard;
        PlayerPrefs.SetInt("ControlMode_v1", (int)Mode.Keyboard);
        PlayerPrefs.SetInt("Setting_ControlEnabled", 0);
        PlayerPrefs.Save();

        // Neu co tien trinh cu ton tai (Editor/domain reload off), tat di.
        KillPython();
        Application.quitting += KillPython;
    }
}
