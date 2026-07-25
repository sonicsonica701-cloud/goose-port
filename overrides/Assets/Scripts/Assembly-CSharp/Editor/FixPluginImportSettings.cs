using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Automatically fixes native plugin import settings before build.
/// Ensures FMOD .so files are included for Android ARMv7.
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
        // Find all .so files and enable them for Android
        string[] soFiles = Directory.GetFiles("Assets", "*.so", SearchOption.AllDirectories);
        int fixed_count = 0;
        
        foreach (string soFile in soFiles)
        {
            string assetPath = soFile.Replace("\\", "/");
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null) continue;

            // Check if it's an armeabi-v7a lib
            bool isArm = assetPath.Contains("armeabi-v7a") || assetPath.Contains("android");
            
            if (isArm || assetPath.ToLower().Contains("fmod"))
            {
                bool changed = false;
                
                if (!importer.GetCompatibleWithPlatform(BuildTarget.Android))
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
                    changed = true;
                }
                
                // Set CPU type for Android
                if (importer.GetPlatformData(BuildTarget.Android, "CPU") != "ARMv7")
                {
                    importer.SetPlatformData(BuildTarget.Android, "CPU", "ARMv7");
                    changed = true;
                }
                
                if (changed)
                {
                    importer.SaveAndReimport();
                    fixed_count++;
                    Debug.Log($"[FixPluginImportSettings] Enabled Android/ARMv7 for: {assetPath}");
                }
            }
        }
        
        // Also look for FMOD bank files in StreamingAssets
        string fmodBanksDir = "Assets/StreamingAssets";
        if (!Directory.Exists(fmodBanksDir))
        {
            // Try to find FMOD banks elsewhere and copy them
            string[] bankFiles = Directory.GetFiles("Assets", "*.bank", SearchOption.AllDirectories);
            if (bankFiles.Length > 0)
            {
                Directory.CreateDirectory(fmodBanksDir);
                foreach (string bank in bankFiles)
                {
                    string dest = Path.Combine(fmodBanksDir, Path.GetFileName(bank));
                    if (!File.Exists(dest))
                    {
                        File.Copy(bank, dest);
                        Debug.Log($"[FixPluginImportSettings] Copied FMOD bank to StreamingAssets: {Path.GetFileName(bank)}");
                    }
                }
            }
        }
        
        if (fixed_count > 0)
            Debug.Log($"[FixPluginImportSettings] Fixed {fixed_count} native plugins for Android");
    }
}
