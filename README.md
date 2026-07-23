# Untitled Goose Game — Android Port

> **Target:** ARMv7 32-bit (armv7a) Android APK  
> **Unity:** 2020.3.48f1 LTS (last version supporting ARMv7)  
> **Source:** AssetRipper export of Steam PC build v1.1.4  

## Build Pipeline

GitHub Actions CI builds an APK on every push to `main`.

### Repo Structure

| Path | Purpose |
|------|---------|
| `.github/workflows/build.yml` | CI workflow |
| `overrides/` | C# overrides layered on top of the exported project |
| `overrides/Assets/Scripts/Assembly-CSharp/` | Game logic overrides/stubs |
| `overrides/Assets/Plugins/Android/` | Android-specific native libs |

### Required Secrets

| Secret | Purpose |
|--------|---------|
| `UNITY_EMAIL` | Unity account email for license activation |
| `UNITY_PASSWORD` | Unity account password |
| `PROJECT_PAT` | GitHub PAT for downloading project tarball release asset |

### Project Tarball

The AssetRipper-exported project (~1.2 GB) is stored as a GitHub Release asset
(zstd-compressed tar). CI downloads and extracts it on each build.

## Test Device

- Samsung SM-S367VL (Galaxy J3 Orbit)
- Android 9 / API 28
- ARMv7 (32-bit) + NEON
- Mali-G71 GPU
- 1280×720 display
