using UnityEngine;
using System.IO;
using System.Threading;

/// <summary>
/// Provides Android-safe alternatives to Application.streamingAssetsPath.
/// On Android, streamingAssetsPath returns a jar:// URL which File.ReadAllText() cannot read.
/// This class returns persistentDataPath/StreamingAssets instead, where files are pre-copied.
/// 
/// The build workflow sed-replaces Application.streamingAssetsPath with 
/// LocalizationManagerPatch.GetStreamingPath() in the game's LocalizationManager.cs source.
/// </summary>
public static class LocalizationManagerPatch
{
    private static string s_cachedPath = null;
    private static bool s_copied = false;
    
    /// <summary>
    /// Returns a filesystem path to StreamingAssets that File.ReadAllText can actually read.
    /// On Android: persistentDataPath/StreamingAssets (pre-copied from APK)
    /// On other platforms: Application.streamingAssetsPath (works directly)
    /// </summary>
    public static string GetStreamingPath()
    {
        if (s_cachedPath != null)
            return s_cachedPath;
            
#if UNITY_ANDROID && !UNITY_EDITOR
        // Ensure files are copied first
        if (!s_copied)
        {
            CopyStreamingAssets();
            s_copied = true;
        }
        s_cachedPath = Path.Combine(Application.persistentDataPath, "StreamingAssets");
#else
        s_cachedPath = Application.streamingAssetsPath;
#endif
        return s_cachedPath;
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void EarlyInit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[LocalizationPatch] Early init - copying StringTables...");
        CopyStreamingAssets();
        s_copied = true;
        Debug.Log("[LocalizationPatch] Done. Path: " + GetStreamingPath());
#endif
    }
    
#if UNITY_ANDROID && !UNITY_EDITOR
    static void CopyStreamingAssets()
    {
        string destBase = Path.Combine(Application.persistentDataPath, "StreamingAssets");
        string destDir = Path.Combine(destBase, "StringTables");
        
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
        
        string[] languages = new string[] {
            "base.json", "english.json", "french.json", "german.json",
            "spanish.json", "italian.json", "japanese.json", "korean.json",
            "russian.json", "chinese.json", "brazilianportuguese.json",
            "czech.json", "dutch.json", "data.json"
        };
        
        foreach (string lang in languages)
        {
            string destPath = Path.Combine(destDir, lang);
            if (File.Exists(destPath)) continue;
            
            string srcUrl = Application.streamingAssetsPath + "/StringTables/" + lang;
            try
            {
                using (var www = new UnityEngine.Networking.UnityWebRequest(srcUrl))
                {
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    var op = www.SendWebRequest();
                    while (!op.isDone) Thread.Sleep(1);
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(destPath, www.downloadHandler.data);
                    }
                }
            }
            catch (System.Exception) { }
        }
    }
#endif
}
