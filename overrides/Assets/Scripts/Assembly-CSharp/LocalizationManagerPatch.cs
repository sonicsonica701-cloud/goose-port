using UnityEngine;

/// <summary>
/// Provides an Android-safe alternative to Application.streamingAssetsPath.
/// On Android, streamingAssetsPath returns a jar:// URL which File.ReadAllText()
/// cannot read. This returns persistentDataPath/StreamingAssets instead, where
/// AndroidStreamingAssetsHelper has already copied the files.
///
/// The build workflow sed-replaces Application.streamingAssetsPath with
/// LocalizationManagerPatch.GetStreamingPath() in the game's LocalizationManager.cs
/// source.
///
/// NOTE: this class previously duplicated AndroidStreamingAssetsHelper's copy
/// logic and ran it at RuntimeInitializeLoadType.SubsystemRegistration - the
/// earliest possible engine bootstrap phase, before Unity's player loop exists
/// to drive the UnityWebRequest to completion. That could stall app startup
/// indefinitely, which is the most likely cause of the menu never appearing
/// (black screen with only a static arrow/cursor icon that doesn't depend on
/// the stalled localization system). The copy now lives in exactly one place
/// (AndroidStreamingAssetsHelper, at BeforeSceneLoad with a bounded wait) and
/// this class just resolves the resulting path.
/// </summary>
public static class LocalizationManagerPatch
{
    public static string GetStreamingPath()
    {
        return AndroidStreamingAssetsHelper.EnsureCopied();
    }
}
