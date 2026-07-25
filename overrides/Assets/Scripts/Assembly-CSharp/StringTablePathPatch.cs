using UnityEngine;
using System.IO;

/// <summary>
/// Patches string table loading to work on Android.
/// The game's Localisation system tries to load from StreamingAssets using File.ReadAllText,
/// which doesn't work on Android (jar:// paths). This redirects to persistentDataPath.
/// </summary>
public static class StringTablePathPatch
{
    /// <summary>
    /// Call this to get the correct path for a streaming asset on the current platform.
    /// </summary>
    public static string GetStreamingAssetPath(string relativePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // On Android, try persistentDataPath first (copied by AndroidStreamingAssetsHelper)
        string androidPath = Path.Combine(Application.persistentDataPath, "StreamingAssets", relativePath);
        if (File.Exists(androidPath))
            return androidPath;
        // Fall back to dataPath (some Unity versions extract StreamingAssets to data)
        string dataPath = Path.Combine(Application.dataPath, "StreamingAssets", relativePath);
        if (File.Exists(dataPath))
            return dataPath;
#endif
        // Default: standard StreamingAssets path
        return Path.Combine(Application.streamingAssetsPath, relativePath);
    }
    
    /// <summary>
    /// Reads a streaming asset file, handling Android's jar:// paths.
    /// </summary>
    public static string ReadStreamingAssetText(string relativePath)
    {
        string path = GetStreamingAssetPath(relativePath);
        if (File.Exists(path))
            return File.ReadAllText(path);
        
        Debug.LogError($"[StringTablePathPatch] Cannot read: {relativePath} (tried: {path})");
        return null;
    }
}
