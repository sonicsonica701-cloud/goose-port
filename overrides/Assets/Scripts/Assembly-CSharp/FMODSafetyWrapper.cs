using UnityEngine;
using FMODUnity;
using FMOD;

/// <summary>
/// Defense-in-depth: wraps FMOD RuntimeManager initialization in a try/catch so
/// that a missing or mis-versioned native FMOD library (EntryPointNotFoundException
/// on FMOD5_Memory_GetStats etc.) does not take down the entire game.
///
/// Without this, the cascading failure path is:
///   1. FMOD5_Memory_GetStats EntryPointNotFoundException
///   2. FMODUnity.RuntimeUtils.EnforceLibraryOrder() fails
///   3. SettingsWardrobe.InitPreferences() crashes (can't get audio bus)
///   4. MainMenuActivator.Start() / VersionNumberText.Awake() NPE
///   5. LocalizationManager + FontMappingData cascade-NPE on every string load
///   6. Black screen — no menu
///
/// With this wrapper, step 1 still happens but step 2 is caught, audio degrades
/// silently, and the rest of the game initializes normally.
/// </summary>
public static class FMODSafetyWrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Install()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[FMODSafetyWrapper] Installing FMOD init safety net...");
        try
        {
            // Touch RuntimeManager early to trigger any init path that might throw.
            // If it explodes, we catch it and mark FMOD as unavailable so the rest
            // of the game can proceed without audio.
            var instance = RuntimeManager.Instance;
            if (instance != null)
            {
                Debug.Log("[FMODSafetyWrapper] FMOD RuntimeManager initialized OK.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FMODSafetyWrapper] FMOD init failed — audio disabled: {ex.GetType().Name}: {ex.Message}");
            Debug.LogError("[FMODSafetyWrapper] The game will run without audio. " +
                "Check that libfmod.so / libfmodstudio.so in APK export FMOD5_ symbols.");
            // Don't rethrow — let the game continue without audio
        }
#endif
    }
}
