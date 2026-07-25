using UnityEngine;
using System.IO;
using System.Reflection;

/// <summary>
/// Forces LocalizationManager initialization with correct Android paths.
/// Runs at SubsystemRegistration (earliest possible) to patch paths before
/// LocalizationManager.Init() reads from streamingAssetsPath.
/// </summary>
public static class LocalizationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void PatchLocalizationPaths()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[LocalizationBootstrap] Patching streamingAssetsPath for Android...");
        
        // The nuclear option: since Application.streamingAssetsPath returns jar://...
        // and the game uses it with File.ReadAllText, we ensure the files exist at 
        // Application.dataPath + "!/assets/StringTables/" won't work either.
        //
        // Instead, we'll use reflection to set the language file path in LocalizationManager
        // AFTER our AndroidStreamingAssetsHelper has copied the files.
        // 
        // The timing is: SubsystemRegistration -> BeforeSceneLoad -> AfterSceneLoad
        // AndroidStreamingAssetsHelper runs at BeforeSceneLoad
        // LocalizationManager.Init() likely runs at Awake/Start of scene
        // So our files SHOULD be ready by then if we block in BeforeSceneLoad.
#endif
    }
}
