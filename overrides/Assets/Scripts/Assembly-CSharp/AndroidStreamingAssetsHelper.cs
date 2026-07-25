using UnityEngine;
using System.IO;

/// <summary>
/// Helper that copies StreamingAssets files to persistentDataPath on Android at startup.
/// On Android, Application.streamingAssetsPath points to a jar:// URL which can't be read with File.IO.
/// This copies essential files to Application.persistentDataPath where they're accessible.
/// </summary>
public class AndroidStreamingAssetsHelper : MonoBehaviour
{
    private static bool s_initialized = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (s_initialized) return;
        s_initialized = true;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        // Create a temporary GameObject to run coroutines
        var go = new GameObject("AndroidStreamingAssetsHelper");
        go.AddComponent<AndroidStreamingAssetsHelper>();
        DontDestroyOnLoad(go);
#endif
    }
    
    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(CopyStreamingAssets());
#endif
    }
    
    System.Collections.IEnumerator CopyStreamingAssets()
    {
        string[] filesToCopy = new string[]
        {
            "StringTables/english.json",
            "StringTables/french.json",
            "StringTables/german.json",
            "StringTables/spanish.json",
            "StringTables/italian.json",
            "StringTables/japanese.json",
            "StringTables/korean.json",
            "StringTables/chinese_simplified.json",
            "StringTables/chinese_traditional.json",
            "StringTables/portuguese.json",
            "StringTables/russian.json",
        };
        
        string destBase = Application.persistentDataPath + "/StreamingAssets";
        
        foreach (string relPath in filesToCopy)
        {
            string srcPath = Application.streamingAssetsPath + "/" + relPath;
            string destPath = destBase + "/" + relPath;
            string destDir = Path.GetDirectoryName(destPath);
            
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            
            if (File.Exists(destPath))
                continue; // Already copied
            
            // On Android, streamingAssetsPath is a jar URL, use UnityWebRequest
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(srcPath))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(destPath, www.downloadHandler.data);
                    Debug.Log($"[AndroidSA] Copied: {relPath}");
                }
                else
                {
                    Debug.LogWarning($"[AndroidSA] Failed to copy {relPath}: {www.error}");
                }
            }
        }
        
        Debug.Log("[AndroidSA] StreamingAssets copy complete");
    }
}
