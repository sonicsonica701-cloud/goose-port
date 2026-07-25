using UnityEditor;
using UnityEngine;
using System.IO;

public class AndroidBuilder
{
    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {
        string buildPath = "Build/UntitledGooseGame.apk";
        string buildDir = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        if (scenes.Count == 0)
        {
            Debug.LogWarning("No scenes in build settings, finding all scenes...");
            foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("PackageCache") && !path.Contains("Editor"))
                    scenes.Add(path);
            }
        }

        Debug.Log($"Building APK with {scenes.Count} scenes");
        foreach (var s in scenes)
            Debug.Log($"  Scene: {s}");

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        // Use SDK 30 which is available in the Docker image
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)30;
        // Must be positive integer for Gradle
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.bundleVersion = "1.1.4";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mobileport.untitledgoosegame");

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"Build succeeded! APK size: {new FileInfo(buildPath).Length / 1024 / 1024} MB");
        }
    }
}
