using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Automatically fixes native plugin import settings before build.
/// Forces Unity to recognize .so files as native Android plugins.
/// Runs at InitializeOnLoad (earliest editor script hook).
/// </summary>
[InitializeOnLoad]
public class FixPluginImportSettings
{
    static FixPluginImportSettings()
    {
        FixAll();
    }

    [MenuItem("Build/Fix Plugin Import Settings")]
    public static void FixAll()
    {
        Debug.Log("[FixPluginImportSettings] Starting native plugin fix...");
        
        // Find all .so files in Assets
        string[] soFiles = Directory.GetFiles("Assets", "*.so", SearchOption.AllDirectories);
        Debug.Log($"[FixPluginImportSettings] Found {soFiles.Length} .so files");
        
        int fixed_count = 0;
        foreach (string soFile in soFiles)
        {
            string assetPath = soFile.Replace("\\", "/");
            Debug.Log($"[FixPluginImportSettings] Processing: {assetPath}");
            
            // Force reimport as PluginImporter
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[FixPluginImportSettings] Cannot get PluginImporter for: {assetPath} - forcing reimport");
                // Delete the .meta and reimport to let Unity auto-detect as native plugin
                string metaPath = assetPath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                }
                if (importer == null)
                {
                    Debug.LogError($"[FixPluginImportSettings] Still cannot get PluginImporter for: {assetPath}");
                    continue;
                }
            }

            bool changed = false;
            
            // Disable for all platforms first
            importer.SetCompatibleWithAnyPlatform(false);
            
            // Enable specifically for Android
            importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
            
            // Set CPU type for Android
            importer.SetPlatformData(BuildTarget.Android, "CPU", "ARMv7");
            
            // Disable for Editor (native .so won't run on editor anyway)
            importer.SetCompatibleWithEditor(false);
            
            changed = true;
            
            if (changed)
            {
                importer.SaveAndReimport();
                fixed_count++;
                Debug.Log($"[FixPluginImportSettings] FIXED: {assetPath} -> Android/ARMv7");
            }
        }
        
        if (fixed_count > 0)
            Debug.Log($"[FixPluginImportSettings] Fixed {fixed_count} native plugins for Android");
        else
            Debug.Log("[FixPluginImportSettings] No plugins needed fixing");
    }
}
