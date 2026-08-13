using UnityEditor;
using UnityEngine;
using System.IO;

public class AndroidBuilder
{
    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {
        // --- FIX: Unity 2018.4 batchmode does not read ANDROID_HOME/SDK_ROOT
        // env vars (that's 2019.1+); the toolchain paths must live in
        // EditorPrefs before BuildPlayer is called.
        var sdk = System.Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
               ?? System.Environment.GetEnvironmentVariable("ANDROID_HOME");
        var ndk = System.Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT")
               ?? System.Environment.GetEnvironmentVariable("ANDROID_NDK_HOME");
        var jdk = System.Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(sdk))
        {
            Debug.Log($"Setting AndroidSdkRoot -> {sdk}");
            UnityEditor.EditorPrefs.SetString("AndroidSdkRoot", sdk);
        }
        if (!string.IsNullOrEmpty(ndk))
        {
            Debug.Log($"Setting AndroidNdkRoot -> {ndk}");
            UnityEditor.EditorPrefs.SetString("AndroidNdkRoot", ndk);
        }
        if (!string.IsNullOrEmpty(jdk))
        {
            Debug.Log($"Setting JdkRoot -> {jdk}");
            UnityEditor.EditorPrefs.SetString("JdkRoot", jdk);
        }

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

        // --- Core settings ---
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)30;
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.bundleVersion = "1.1.4";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mobileport.untitledgoosegame");

        // --- FIX: Force landscape orientation ---
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // --- FIX: pin the graphics API to OpenGL ES 3 only. This project came
        // in with Unity's default "Auto Graphics API" list (Vulkan first, GLES3
        // fallback). The AssetRipper-exported shaders/post-processing were
        // authored against the PC (D3D/desktop GL) pipeline and were never
        // validated against this device's Vulkan driver; picking Vulkan on a
        // low-end/older ARMv7 GPU is a common source of corrupted/garbled
        // rendering on exactly this kind of export. Forcing GLES3-only removes
        // that variable.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[]
        {
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
        });

        // --- FIX: re-target textures for mobile GPU-native compression
        // instead of leaving them in whatever desktop-compressed format
        // AssetRipper exported them in (see MobileTextureCompressionFixer.cs).
        MobileTextureCompressionFixer.FixAll();

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
