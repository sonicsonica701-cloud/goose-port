using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Forces an Android-specific texture compression override (ASTC 6x6) on every
/// texture that doesn't already have one.
///
/// Context: this project is an AssetRipper export of the Steam/PC build.
/// AssetRipper re-serializes texture *assets* but does not re-author an
/// Android platform override in their .meta/importer settings - so, absent
/// this fix, textures import for Android using Unity's default platform
/// settings, which typically falls back to point-sampled/uncompressed or an
/// unintended format rather than a GPU-native mobile compressed format. Mobile
/// GPUs (Mali/Adreno on this device tier) do not support the desktop
/// BC1-BC7/DXT formats the PC textures may have been authored with, so a
/// texture that isn't explicitly re-targeted at import time can come through
/// with corrupted/blocky "garbled" pixel data - matching the reported
/// distorted-visuals symptom. ASTC is supported on effectively all GLES
/// 3.1+/Vulkan Android GPUs from this era and gives much better quality per
/// bit than ETC2, so it's the safer default; drop to ETC2 6:1 if a specific
/// target device turns out not to support ASTC.
/// </summary>
public static class MobileTextureCompressionFixer
{
    public static void FixAll()
    {
        Debug.Log("[MobileTextureCompressionFixer] Scanning textures for Android compression override...");

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int fixedCount = 0, skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            var platformSettings = importer.GetPlatformTextureSettings("Android");

            if (platformSettings.overridden &&
                platformSettings.format != TextureImporterFormat.DXT1 &&
                platformSettings.format != TextureImporterFormat.DXT5 &&
                platformSettings.format != TextureImporterFormat.BC7)
            {
                // Already has a sane, non-desktop override - leave it alone.
                skipped++;
                continue;
            }

            platformSettings.overridden = true;
            platformSettings.format = TextureImporterFormat.ASTC_RGB_6x6;
            platformSettings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit;
            platformSettings.maxTextureSize = platformSettings.overridden && platformSettings.maxTextureSize > 0
                ? platformSettings.maxTextureSize
                : 2048;

            importer.SetPlatformTextureSettings(platformSettings);
            importer.SaveAndReimport();
            fixedCount++;
        }

        Debug.Log($"[MobileTextureCompressionFixer] Done. Re-targeted {fixedCount} texture(s) to ASTC_6x6 for Android, left {skipped} with an existing non-desktop override.");
    }
}
