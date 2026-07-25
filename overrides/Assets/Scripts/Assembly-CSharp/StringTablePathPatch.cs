using UnityEngine;
using System.IO;

/// <summary>
/// Patches the path used by the game's LocalizationManager on Android.
/// The game tries File.ReadAllText(Application.streamingAssetsPath + "/StringTables/xxx.json")
/// which fails on Android because streamingAssetsPath is a jar:// URL.
/// 
/// This class provides the correct path on Android (persistentDataPath copy).
/// The game's LocalizationManager should be patched to call GetLanguageFilePath() instead.
/// 
/// Since we can't easily modify the compiled Assembly-CSharp, we use an alternative approach:
/// We create a symbolic shim that intercepts the file load.
/// </summary>
public static class StringTablePathPatch
{
    /// <summary>
    /// Returns the correct filesystem path for a StringTables file on the current platform.
    /// </summary>
    public static string GetLanguageFilePath(string filename)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // On Android, files were copied to persistentDataPath by AndroidStreamingAssetsHelper
        string path = Path.Combine(Application.persistentDataPath, "StreamingAssets", "StringTables", filename);
        if (File.Exists(path))
            return path;
#endif
        return Path.Combine(Application.streamingAssetsPath, "StringTables", filename);
    }
    
    /// <summary>
    /// Reads a language JSON file, handling Android's path differences.
    /// </summary>
    public static string ReadLanguageFile(string filename)
    {
        string path = GetLanguageFilePath(filename);
        if (File.Exists(path))
            return File.ReadAllText(path);
        return null;
    }
}
