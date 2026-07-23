using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// Captures Application.logMessageReceived to a text file on the device.
/// Pull via: /storage/emulated/0/Android/data/com.mobileport.untitledgoosegame/files/goose_log.txt
/// </summary>
public class GooseLogBootstrap : MonoBehaviour
{
    static GooseLogBootstrap _instance;
    static StringBuilder _buffer = new StringBuilder();
    static string _logPath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (_instance != null) return;
        var go = new GameObject("GooseLogBootstrap");
        _instance = go.AddComponent<GooseLogBootstrap>();
        DontDestroyOnLoad(go);

        _logPath = Path.Combine(Application.persistentDataPath, "goose_log.txt");

        // Device info header
        _buffer.AppendLine($"=== Goose Log ===");
        _buffer.AppendLine($"Time: {System.DateTime.Now}");
        _buffer.AppendLine($"Device: {SystemInfo.deviceModel}");
        _buffer.AppendLine($"OS: {SystemInfo.operatingSystem}");
        _buffer.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)");
        _buffer.AppendLine($"RAM: {SystemInfo.systemMemorySize} MB");
        _buffer.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
        _buffer.AppendLine($"Screen: {Screen.width}x{Screen.height} @ {Screen.dpi} dpi");
        _buffer.AppendLine($"Unity: {Application.unityVersion}");
        _buffer.AppendLine($"App: {Application.identifier} v{Application.version}");
        _buffer.AppendLine("==================");
        Flush();

        Application.logMessageReceived += OnLog;
    }

    static void OnLog(string msg, string stackTrace, LogType type)
    {
        var ts = System.DateTime.Now.ToString("HH:mm:ss.fff");
        _buffer.AppendLine($"[{ts}] [{type}] {msg}");
        if (type == LogType.Exception || type == LogType.Error)
            _buffer.AppendLine(stackTrace);
        Flush();
    }

    static void Flush()
    {
        try
        {
            File.AppendAllText(_logPath, _buffer.ToString());
            _buffer.Clear();
        }
        catch { }
    }
}
