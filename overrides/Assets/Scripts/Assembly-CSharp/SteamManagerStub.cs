using UnityEngine;

/// <summary>
/// No-op stub replacing Facepunch.Steamworks dependency for Android.
/// Preserves public API surface so existing scripts don't break.
/// </summary>
public static class SteamClient
{
    public static bool IsValid => false;
    public static bool IsLoggedOn => false;
    public static ulong SteamId => 0;
    public static string Name => "Player";

    public static void Init(uint appId, bool asyncCallbacks = true) { }
    public static void Shutdown() { }
    public static void RunCallbacks() { }
}

public static class SteamApps
{
    public static string GameLanguage => "english";
    public static bool IsSubscribed => true;
}
