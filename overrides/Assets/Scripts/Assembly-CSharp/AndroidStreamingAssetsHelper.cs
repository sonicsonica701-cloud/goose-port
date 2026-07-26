using UnityEngine;
using System.IO;
using System.Threading;

/// <summary>
/// Single canonical copy of StreamingAssets -> persistentDataPath on Android.
///
/// This used to be duplicated across three separate scripts
/// (AndroidStreamingAssetsHelper, LocalizationManagerPatch, LocalizationBootstrap),
/// each with a different file list and a different lifecycle hook
/// (SubsystemRegistration vs BeforeSceneLoad). That duplication was the likely
/// cause of the "menu never renders / black screen with just a cursor" bug:
///
///   - LocalizationManagerPatch previously ran its blocking UnityWebRequest copy
///     at RuntimeInitializeLoadType.SubsystemRegistration - the *earliest*
///     native engine bootstrap phase, before Unity's player loop is pumping
///     coroutines/web requests. Spin-waiting on `op.isDone` there can stall far
///     longer than expected (in the worst case indefinitely, since nothing is
///     driving the request to completion yet), delaying or blocking the first
///     scene (the menu) from ever finishing initialization.
///   - Meanwhile this script did the *same* copy again at BeforeSceneLoad, but
///     with a shorter file list that omitted "base.json"/"data.json" - the
///     files the real LocalizationManager needs first to build its string
///     table index. If those were missing/late, UI text (and anything that
///     depends on the localization system finishing before building menu
///     widgets) can silently fail to populate, leaving only elements that
///     don't depend on localized strings (e.g. a raw pointer/arrow sprite)
///     visible on an otherwise black screen.
///
/// Fix: exactly one implementation, running only at BeforeSceneLoad (after the
/// player loop exists), with a bounded wait per file so a failed/slow request
/// degrades to a loud warning instead of an indefinite hang.
/// </summary>
public static class AndroidStreamingAssetsHelper
{
    // Full set the game's LocalizationManager can request - matches what was
    // previously only copied by LocalizationManagerPatch's now-removed
    // SubsystemRegistration copy. "base.json"/"data.json" are the core string
    // tables read before a language file is even selected, so they must be
    // present before the menu scene starts building its UI.
    static readonly string[] FilesToCopy = new string[]
    {
        "StringTables/base.json",
        "StringTables/data.json",
        "StringTables/english.json",
        "StringTables/french.json",
        "StringTables/german.json",
        "StringTables/spanish.json",
        "StringTables/italian.json",
        "StringTables/japanese.json",
        "StringTables/korean.json",
        "StringTables/russian.json",
        "StringTables/chinese.json",
        "StringTables/brazilianportuguese.json",
        "StringTables/czech.json",
        "StringTables/dutch.json",
    };

    const int MaxWaitMs = 8000; // bounded wait per file instead of an infinite spin
    const int PollMs = 4;

    static bool s_copied;
    static string s_cachedPath;

    /// <summary>
    /// Filesystem path (readable via File.ReadAllText) containing the copied
    /// StreamingAssets. Safe to call multiple times/from multiple scripts -
    /// only copies once.
    /// </summary>
    public static string EnsureCopied()
    {
        if (s_cachedPath != null)
            return s_cachedPath;

#if UNITY_ANDROID && !UNITY_EDITOR
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureCopied();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static void CopyStreamingAssets()
    {
        string destBase = Path.Combine(Application.persistentDataPath, "StreamingAssets");

        foreach (string relPath in FilesToCopy)
        {
            string destPath = Path.Combine(destBase, relPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(destPath))
                continue;

            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string srcPath = Application.streamingAssetsPath + "/" + relPath;

            try
            {
                using (var www = new UnityEngine.Networking.UnityWebRequest(srcPath))
                {
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    var op = www.SendWebRequest();

                    int waited = 0;
                    while (!op.isDone && waited < MaxWaitMs)
                    {
                        Thread.Sleep(PollMs);
                        waited += PollMs;
                    }

                    if (!op.isDone)
                    {
                        Debug.LogError($"[AndroidSA] Timed out after {MaxWaitMs}ms copying {relPath} - " +
                            "this file will be missing on device. If this happens for every file, " +
                            "the request is likely never being pumped (check for another blocking " +
                            "copy running earlier than BeforeSceneLoad).");
                        continue;
                    }

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
        Debug.Log("[AndroidSA] StreamingAssets copy complete.");
    }
#endif
}
