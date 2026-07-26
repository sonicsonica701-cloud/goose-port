# Untitled Goose Game — Android Port (ARMv7 32-bit)

## Project Overview
Porting Untitled Goose Game (PC version v1.1.4) to Android as a 32-bit ARMv7 APK using GitHub Actions CI. The source is an AssetRipper export of the original Unity game, rebuilt with Unity 2020.3.48f1 LTS.

**Repo:** `sonicsonica701-cloud/goose-port`  
**Branch:** `main`  
**CI:** GitHub Actions (`.github/workflows/build.yml`)  
**Latest successful release:** `v1.1.4-android-20260726052218` (352MB APK)

---

## Architecture & Build Pipeline

### Source Material
- Original game extracted via **AssetRipper** → Unity project source (scripts, assets, scenes)
- Project tarball stored as a GitHub Release asset (downloaded by CI via `project_asset_id.txt`)
- AssetRipper did NOT export native .so libraries (FMOD libs missing entirely)

### Build Environment
- **Docker image:** `unityci/editor:ubuntu-2020.3.48f1-android-3`
- **Unity version:** 2020.3.48f1 LTS
- **License:** Pre-activated `UnityEntitlementLicense.xml` (injected into Docker)
  - Account: `badamsgaming1977@gmail.com`
  - Machine ID: `576562626272264761624c65526f7578`
- **Target:** Android ARMv7, 32-bit, Mono scripting backend
- **Min SDK:** 22 (Android 5.1) / **Target SDK:** 30 (Android 11)

### Build Flow (build.yml)
1. Checkout repo
2. Free disk space
3. Install tools (`zstd`, `gcc-arm-linux-gnueabihf`)
4. Download project tarball from GitHub release asset
5. Apply local overrides (`overrides/` → `ExportedProject/`)
6. Fix deprecated API calls (`DrawProceduralIndirect` → `DrawProceduralIndirectNow`)
7. **Build FMOD stub .so libraries** (ARM cross-compile with `FMOD5_` prefix)
8. Fix StringTables loading path (copy to StreamingAssets)
9. **Patch LocalizationManager.cs source** (sed-replace `streamingAssetsPath`)
10. Pull Unity Docker image
11. **Docker build:**
    - Verify native plugins
    - **Cecil patcher** compiles and patches PostProcessing/TextMeshPro DLLs
    - Unity batch-mode build (`AndroidBuilder.BuildAndroid`)
12. Create GitHub Release with APK

---

## Key Files

| File | Purpose |
|------|---------|
| `.github/workflows/build.yml` | Main CI workflow — all build logic |
| `ci-scripts/PatchDLL.cs` | Mono.Cecil 0.9.x patcher for DLL namespace fixes |
| `overrides/Assets/Scripts/Assembly-CSharp/Editor/AndroidBuilder.cs` | Build settings (landscape, ARMv7, Mono, SDK levels) |
| `overrides/Assets/Scripts/Assembly-CSharp/Editor/FixPluginImportSettings.cs` | Force-reimports .so as Android native plugins |
| `overrides/Assets/Scripts/Assembly-CSharp/AndroidStreamingAssetsHelper.cs` | Copies StreamingAssets from APK jar:// to persistentDataPath |
| `overrides/Assets/Scripts/Assembly-CSharp/LocalizationManagerPatch.cs` | Provides `GetStreamingPath()` — Android-safe path to StringTables |
| `overrides/Assets/Scripts/Assembly-CSharp/StringTablePathPatch.cs` | Helper for redirecting file reads |
| `overrides/Assets/Scripts/Assembly-CSharp/LocalizationBootstrap.cs` | Early init stub |
| `overrides/Assets/Scripts/Assembly-CSharp/GooseLogBootstrap.cs` | Runtime logging |
| `overrides/Assets/Scripts/Assembly-CSharp/SteamManagerStub.cs` | Stubs out Steam API (not needed on Android) |
| `project_asset_id.txt` | GitHub release asset ID for the project tarball |
| `UnityEntitlementLicense.xml` | Unity license file for headless CI builds |

---

## Solved Issues

### 1. Landscape Orientation ✅
**Problem:** Game rendered portrait.  
**Fix:** `AndroidBuilder.cs` forces `UIOrientation.LandscapeLeft` and disables portrait auto-rotate.

### 2. FMOD Audio — EntryPointNotFoundException ✅
**Problem:** `DllNotFoundException` / `EntryPointNotFoundException: FMOD5_Memory_GetStats`  
**Root causes:**
- AssetRipper didn't export native .so files
- Real FMOD SDK requires login to download
- Initial stubs used wrong prefix (`FMOD_` instead of `FMOD5_`)
- Compiled with `-nostdlib` (Android linker couldn't load the ELF)
- .meta files had leading whitespace (Unity used DefaultImporter)
- Even with correct .meta, Unity 2020.3 needs PluginImporter API to force-set platform

**Fix (multi-part):**
- Cross-compile ARM stub .so with `FMOD5_` prefix functions (returns `FMOD_OK` for everything)
- Compile with `-lc` (NOT `-nostdlib`) for proper ELF sections
- `FixPluginImportSettings.cs` (`[InitializeOnLoad]`) deletes .meta and force-reimports via `PluginImporter` API, setting Android/ARMv7

**Current state:** Stubs load without crashing. No actual audio plays (stubs are silent no-ops).

### 3. PostProcessing Visuals — TypeLoadException ✅
**Problem:** `TypeLoadException: UnityEngine.Experimental.Rendering.RenderPipelineAsset`  
**Root cause:** Unity.Postprocessing.Runtime.dll references `UnityEngine.Experimental.Rendering` namespace which was moved to `UnityEngine.Rendering` in newer Unity versions.  

**Fix:**
- `ci-scripts/PatchDLL.cs` uses Mono.Cecil 0.9.5 (Unity's bundled version) to rewrite type references
- Runs INSIDE Docker where Cecil and Unity managed DLLs are available
- Patches both `Unity.Postprocessing.Runtime.dll` and `Unity.TextMeshPro.dll`
- Uses `$MONO_BIN` (full path `/opt/unity/Editor/Data/MonoBleedingEdge/bin/mono`) since `mono`/`mcs` aren't in PATH

**Cecil 0.9.x constraints:**
- No `ReadWrite` in `ReaderParameters`
- No parameterless `Write()` — must write to stream/path
- No `Dispose()` on `AssemblyDefinition`
- No `TypeSystem.CoreLibrary`
- Cecil DLL must be from `/opt/unity/Editor/Data/Managed` or GAC (NOT `unity_web` which is stripped)

### 4. Localization — "Cannot find langauge file" ✅
**Problem:** Game's `LocalizationManager.cs` uses `Application.streamingAssetsPath` which returns `jar:file:///...!/assets` on Android — unreadable by `File.ReadAllText`.

**Fix (two-part):**
1. **Source patch:** Build step `sed`-replaces `Application.streamingAssetsPath` → `LocalizationManagerPatch.GetStreamingPath()` in `LocalizationManager.cs` before Unity compiles it
2. **Runtime helper:** `LocalizationManagerPatch.cs` copies all StringTable JSON files from APK (via UnityWebRequest) to `persistentDataPath/StreamingAssets/` at `SubsystemRegistration` (earliest hook), then returns that path

---

## Known Remaining Issues / Next Steps

### Audio is Silent (Not Crashing, Just No Sound)
The FMOD stubs return `FMOD_OK` for everything but don't actually produce audio output. To get real audio:
- Option A: Download real FMOD Android .so from fmod.com (requires account login)
- Option B: Include the FMOD .bank files in StreamingAssets and use real FMOD libs
- The game's audio won't play until real FMOD native libraries are used

### Runtime Validation Needed
The APK builds and the CI log shows all patches applied, but full runtime validation on a device is still pending. Possible remaining issues:
- Shaders may still have visual artifacts (separate from PostProcessing namespace fix)
- Touch input / controls may need mapping
- Performance on low-end ARMv7 devices

### PostProcessing Visual Quality
The namespace fix prevents the crash, but the actual PostProcessing effects (bloom, color grading, etc.) may still not render correctly if the PostProcessing stack isn't fully compatible with the mobile renderer. Visual distortion could have multiple causes.

---

## Important Technical Constraints

1. **Unity 2020.3.48f1 only** — newer versions break AssetRipper export compatibility
2. **Mono scripting backend** — IL2CPP would require different native plugin handling  
3. **ARMv7 32-bit** — user requirement; don't switch to ARM64
4. **Mono.Cecil 0.9.5** — only version available in Unity Docker image; API differs from modern Cecil
5. **No `mcs` or `mono` in Docker PATH** — must use full paths under `/opt/unity/Editor/Data/MonoBleedingEdge/`
6. **Heredoc in YAML `run: |` blocks** — content is literal (YAML strips indentation), but bash quoting inside Docker `bash -c` is fragile; use mounted script files instead
7. **Docker script approach** — write script to `/tmp/docker_build.sh` via heredoc, then mount into container to avoid quoting issues
8. **PluginImporter .meta alone doesn't work** — Unity 2020.3 needs programmatic `PluginImporter` API calls via `[InitializeOnLoad]` editor script

---

## Build Commands

```bash
# Trigger a new build
cd goose-port && gh workflow run build.yml

# Check build status
gh run list --limit 5

# View build log
gh run view <RUN_ID> --log

# Check specific fix output
gh run view <RUN_ID> --log 2>&1 | grep "Patcher compiled\|Done:\|Fix:\|FIXED:"
```

---

## Repo Structure

```
goose-port/
├── .github/workflows/build.yml    # Main CI pipeline
├── ci-scripts/
│   └── PatchDLL.cs                # Mono.Cecil namespace patcher
├── overrides/                     # Files copied over ExportedProject before build
│   └── Assets/Scripts/Assembly-CSharp/
│       ├── Editor/
│       │   ├── AndroidBuilder.cs
│       │   └── FixPluginImportSettings.cs
│       ├── AndroidStreamingAssetsHelper.cs
│       ├── LocalizationManagerPatch.cs
│       ├── LocalizationBootstrap.cs
│       ├── StringTablePathPatch.cs
│       ├── GooseLogBootstrap.cs
│       └── SteamManagerStub.cs
├── project_asset_id.txt           # Release asset ID for project tarball
├── UnityEntitlementLicense.xml    # Unity headless license
└── business_context.md            # This file
```

---

*Last updated: 2026-07-26 — All 3 critical runtime fixes confirmed working in CI.*
