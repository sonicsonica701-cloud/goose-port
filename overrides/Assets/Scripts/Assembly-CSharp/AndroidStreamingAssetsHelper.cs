using UnityEngine;
using System.IO;
using System.Threading;

/// <summary>
/// Synchronously copies StreamingAssets to persistentDataPath on Android BEFORE scenes load.
/// Uses RuntimeInitializeOnLoadMethod with BeforeSceneLoad to ensure files are ready
/// before LocalizationManager tries to read them.
/// </summary>
public static class AndroidStreamingAssetsHelper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CopyStreamingAssets()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string[] filesToCopy = new string[]
        {
            "StringTables/english.json",
            "StringTables/french.json",
            "StringTables/german.json",
            "StringTables/spanish.json",
            "StringTables/italian.json",
            "StringTables/japanese.json",
            "StringTables/korean.json",
            "StringTables/russian.json",
        };
        
        string destBase = Application.persistentDataPath + "/StreamingAssets";
        
        foreach (string relPath in filesToCopy)
        {
            string destPath = destBase + "/" + relPath;
            if (File.Exists(destPath))
                continue;
                
            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            
            string srcPath = Application.streamingAssetsPath + "/" + relPath;
            
            // Use Java/Android API to read from APK synchronously
            try
            {
                using (var www = new UnityEngine.Networking.UnityWebRequest(srcPath))
                {
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    var op = www.SendWebRequest();
                    // Block until complete (we're in BeforeSceneLoad so this is OK)
                    while (!op.isDone)
                        Thread.Sleep(1);
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(destPath, www.downloadHandler.data);
                        Debug.Log($"[AndroidSA] Copied: {relPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AndroidSA] Failed: {relPath} - {www.error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AndroidSA] Exception copying {relPath}: {ex.Message}");
            }
        }
        Debug.Log("[AndroidSA] StreamingAssets copy complete (sync)");
#endif
    }
}
