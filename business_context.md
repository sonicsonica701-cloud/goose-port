# Untitled Goose Game — Android Port (Handoff Document)

## Status: Unity 2018.4 Build Ready — Trigger Workflow

**Last updated:** 2026-07-28  
**Repo:** `sonicsonica701-cloud/goose-port` (branch: `main`)  
**Target:** ARMv7 32-bit APK

---

## Critical Discovery & Fix

The game was built with **Unity 2018.4 LTS** (confirmed for ALL versions including v1.1.4). Previous builds used Unity 2020.3.48f1, causing:
- **Distorted visuals** — shader bytecode compiled by 2018.4 cannot run in 2020.3 runtime
- **No audio** — FMOD init affected by rendering pipeline issues
- **Missing localization** — Android streaming assets path issue (fixed separately)

**Fix applied:** Switched Docker image to `unityci/editor:ubuntu-2018.4.36f1-android-3` (verified exists on Docker Hub).

---

## How to Build

1. Go to repo → Actions → "Build Untitled Goose Game Android APK (ARMv7)"
2. Click "Run workflow" (manual dispatch only)
3. Wait ~30-60 minutes
4. APK will be uploaded as a GitHub Release

---

## Build Pipeline (`.github/workflows/build.yml`)

| Step | What it does |
|------|-------------|
| Free disk | Removes dotnet/android SDK to free ~30GB |
| Install NDK | r23c for FMOD stub compilation |
| Download tarball | Project from release asset (ID in `project_asset_id.txt`) |
| Apply overrides | Copies `overrides/` into `ExportedProject/` |
| Build FMOD stubs | NDK-compiled ARMv7 .so with FMOD5_ ABI |
| Setup .meta files | Plugin import settings for .so files |
| Localization patch | sed replaces streamingAssetsPath in source |
| Pull Docker image | `unityci/editor:ubuntu-2018.4.36f1-android-3` |
| Build APK | Unity batchmode via AndroidBuilder.BuildAndroid |
| Create Release | Tags and uploads APK |

### Secrets Required
- `GITHUB_TOKEN` — Auto-provided, downloads release asset
- Unity license: Pre-activated `UnityEntitlementLicense.xml` checked into repo
  - Account: `badamsgaming1977@gmail.com`
  - Machine ID: `576562626272264761624c65526f7578`

---

## Override Files

| File | Purpose |
|------|---------|
| `Editor/AndroidBuilder.cs` | Forces ARMv7, Mono2x, landscape, GLES3, SDK 22-28, ASTC textures |
| `Editor/FixPluginImportSettings.cs` | Forces FMOD .so as Android/ARMv7 plugins via PluginImporter API |
| `Editor/MobileTextureCompressionFixer.cs` | Re-targets all textures to ASTC_RGB_6x6 for Android |
| `LocalizationManagerPatch.cs` | Copies StringTable from jar:// to persistentDataPath at boot |
| `AndroidStreamingAssetsHelper.cs` | Generic streaming assets copy helper |
| `PostProcessMobileGuard.cs` | Disables compute-heavy post-FX on mobile (DoF, MSVO AO, SSR, SMAA) |
| `GooseLogBootstrap.cs` | Runtime logging to device for debugging |
| `SteamManagerStub.cs` | No-op stub replacing Steamworks dependency |
| `FMODSafetyWrapper.cs` | Defense wrapper for FMOD native calls |
| `Plugins/Android/AndroidManifest.xml` | Android manifest |

---

## What Was Removed (2018.4 switch)

- **Mono.Cecil DLL patcher** — Was patching PostProcessing namespace. Not needed with matching engine.
- **DrawProceduralIndirectNow rename** — That API doesn't exist in 2018.4; original DrawProceduralIndirect is correct.
- **Docker PatchDLL.cs mount** — Removed from docker run command.

---

## After Successful Build

1. Install APK on device
2. Check `goose_log.txt` at `/storage/emulated/0/Android/data/com.mobileport.untitledgoosegame/files/`
3. Verify: visuals correct, audio playing, text visible
4. If base works → port newer v1.1.4 content (co-op, new assets)

---

## Constraints

- APK must be ARMv7 32-bit
- Unity 2018.4 LTS — do NOT upgrade engine
- Same repo/workflow
- AssetRipper export is from v1.1.4 (same 2018.4 engine)
