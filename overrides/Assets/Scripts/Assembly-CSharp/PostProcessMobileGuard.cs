using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Disables post-processing effects on Android that are the likely source of
/// the "distorted/garbled visuals" symptom on mobile.
///
/// Context: the CI build (see .github/workflows/build.yml, "Fix deprecated API
/// calls" step) blind-sed-replaces Graphics.DrawProceduralIndirect(...) with
/// Graphics.DrawProceduralIndirectNow(...) in DepthOfField.cs to fix a compile
/// error against the Unity version used here. That rename swaps a
/// command-buffer-queued procedural draw for an immediate one without
/// verifying the call site's arguments/overload still match, and Depth of
/// Field's bokeh pass is compute-buffer driven - a feature category that
/// AssetRipper-exported compute shaders and older mobile GPU drivers
/// frequently fail to reproduce correctly (channel-swapped/garbled output,
/// not a crash). We could not inspect DepthOfField.cs directly here (it ships
/// inside the private ~1.2GB AssetRipper project tarball, not this repo), so
/// rather than trust an unverified blind patch of a compute draw call, we
/// remove the failure mode entirely for mobile: turn off DepthOfField (and
/// other compute/SSAO-heavy effects unlikely to be GPU/driver-portable on the
/// ARMv7 device this targets) at runtime, after the exported PostProcessProfile
/// assets load, before the first frame renders.
///
/// ADDITIONALLY: device logs show per-frame render errors
///   "Hidden/PostProcessing/SubpixelMorphologicalAntialiasing: invalid pass
///    index 5/6 in DrawMesh"
/// The AssetRipper-exported SMAA shader is missing passes that the
/// PostProcessing stack expects, so SMAA fails every frame and can corrupt
/// the final image. We therefore also force antialiasingMode = None on every
/// PostProcessLayer on Android.
///
/// This trades some visual fidelity (no bokeh blur, no multi-scale ambient
/// occlusion, no SMAA) for correct, non-corrupted output - the standard
/// tradeoff made when porting PC post-processing stacks to mobile GPUs.
/// </summary>
public class PostProcessMobileGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SceneManager.sceneLoaded += (scene, mode) => GuardScene();
        GuardScene();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static void GuardScene()
    {
        int touched = 0;

        // 1) Disable compute-driven profile effects (DoF, MSVO AO, SSR)
        var volumes = Object.FindObjectsOfType<PostProcessVolume>(true);
        foreach (var volume in volumes)
        {
            var profile = volume.profile;
            if (profile == null)
                continue;

            if (profile.TryGetSettings(out DepthOfField dof) && dof.enabled.value)
            {
                dof.enabled.value = false;
                touched++;
            }

            if (profile.TryGetSettings(out AmbientOcclusion ao) &&
                ao.enabled.value && ao.mode.value == AmbientOcclusionMode.MultiScaleVolumetricObscurance)
            {
                // MSVO is compute-shader driven; same GPU/driver portability risk as DoF.
                ao.enabled.value = false;
                touched++;
            }

            if (profile.TryGetSettings(out ScreenSpaceReflections ssr) && ssr.enabled.value)
            {
                ssr.enabled.value = false;
                touched++;
            }
        }

        // 2) Kill SMAA/TAA on every PostProcessLayer - the exported SMAA shader
        //    is missing passes ("invalid pass index 5/6 in DrawMesh" every frame).
        var layers = Object.FindObjectsOfType<PostProcessLayer>(true);
        foreach (var layer in layers)
        {
            if (layer.antialiasingMode != PostProcessLayer.Antialiasing.None)
            {
                Debug.Log($"[PostProcessMobileGuard] PostProcessLayer '{layer.gameObject.name}': " +
                          $"antialiasing {layer.antialiasingMode} -> None (broken SMAA shader passes on Android).");
                layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                touched++;
            }
        }

        if (touched > 0)
            Debug.Log($"[PostProcessMobileGuard] Disabled {touched} mobile-incompatible post-process feature(s).");
    }
#endif
}
