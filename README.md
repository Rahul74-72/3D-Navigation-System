# Campus Navigator

**Status: working development project, not a release-certified Android navigation app.** Read TESTING.md for the evidence and remaining gates. This folder does not overwrite the earlier Unity or Blender project.

## Open
In Unity Hub, add `UnityProject` under this folder using **Unity 6000.3.10f1**. Open `Assets/Campus/Scenes/Campus.unity` and press Play. A Windows development preview is built into `Builds/Windows` when the build succeeds.

The built preview is `Builds/Windows/CampusNavigator.exe`. Keep its companion folders and DLLs together. This is a Windows preview, not the Android APK.

## How it works
1. Search a room or enter `take me to IoT Lab`. Select the building/floor and a result.
2. Choose a preview starting checkpoint and press **Preview**. The perspective camera follows behind the person, including on stairs. **Map** switches to the optional overhead view; **Follow** returns behind the person. This is a simulation, not phone tracking.
3. On a phone, **Scan QR** initializes an approved checkpoint. Stand on its surveyed mark, face the arrow and confirm phone height.
4. On supported devices, ARCore measures motion using camera features plus IMU. The app converts that pose into campus coordinates and replans the route while you walk.
5. Switch between the 3D map and AR camera. AR shows the route, directional arrows and a guide ahead of your position.
6. Lost tracking, large motion discontinuities, correction distance or a floor transition requests another QR scan. Unsupported phones retain map/QR checkpoint mode without automatic walking updates.

**The supplied markers are provisional and deliberately cannot authorize live navigation yet.** Measure and approve them before changing `surveyed` in the catalogue. QR decoding by itself does not recover a 3D position/orientation.

## Included
- Original campus GLB, four-floor preview, procedural human guide, preserved Blender source.
- Two-building room catalogue, floor/building search, offline command prefixes.
- NavMesh-based route validation, preview walking, explicit disconnected-route errors.
- AR Foundation + ARCore integration, local QR decoder, camera-permission flow and fallback.
- Local tests, route-audit CSV, generated QR PNGs, reproducible setup/build commands.

## Known limits
- Building A routes connect; 27 Building B routes from its gate fail the current mesh audit. The app blocks them rather than cutting through geometry.
- Floor heights are estimated. Building B and upper-floor labels need verification.
- Android SDK/NDK/JDK support, an APK build, actual camera/AR behavior and campus accuracy tests are not yet verified here.
- Offline commands are deterministic, not a generative AI assistant. BLE and barometer support are deferred.
- No promise of universal AR support, emergency navigation, accessible routes or zero defects.

## Documents
| File | Purpose |
| --- | --- |
| REQUIREMENTS.md | What you need to provide/install |
| PRD.md | Scope and release acceptance |
| DESIGN.md | Interface and calibration flow |
| ARCHITECTURE.md | Components and coordinate/state contracts |
| DATABASE.md | Local JSON schema and survey updates |
| API.md | Internal API, deferred integrations |
| SECURITY.md | Camera privacy, QR trust, authentication decisions |
| SETUP.md | Setup, initialization, builds and MCP |
| IMPLEMENTATION.md | Feature checklist and remaining work |
| TESTING.md | QA evidence, device/campus protocol and debugging |
| PLAN.md | 45-day project plan |
| AGENTS.md | Safe maintenance instructions |

## Reproduce
From this folder: `./tools/Verify.ps1 -Action Verify`, `-Action Windows`, or `-Action Android`. Android requires its Unity Hub modules first. Logs and results go to `qa`. Do not distribute Library, Temp, keystores or tokens.

## Repository contents
Git contains source, tests, required Unity assets, the editable Blender source, QR codes and documentation. Builds, downloaded dependency archives, generated QA logs/screenshots, Unity caches, local MCP settings and duplicate model copies are excluded. Build the Windows preview locally using the command above; it is not downloaded with a clone. Generated QA evidence described in TESTING.md is regenerated locally.
