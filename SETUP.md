# Setup and Initialisation

## Editor
1. Keep the original project closed while opening its copy only if your machine lacks memory for two editors. Add `final/UnityProject` to Unity Hub.
2. Use Unity **6000.3.10f1**. Let Package Manager resolve the pinned manifest and lockfile.
3. Open `Assets/Campus/Scenes/Campus.unity`. The old SampleScene is not the build scene.
4. Press Play. Pick Building A, Floor 1, Classroom 4 and Preview. Room labels use the approved sequence rather than the old baked mesh signs.
5. `Campus > Prepare final scene` regenerates the application scene from the preserved preview. It preserves an existing campus.json but regenerates scene wiring; back up manual edits to the generated application scene first.

## Android
Install Android Build Support, Android SDK & NDK Tools and OpenJDK in Unity Hub for this exact editor. The project sets API 25 minimum, ARM64/IL2CPP, OpenGLES3, AR Optional and optional depth. Set Build Profiles to Android; inspect XR Plug-in Management/Android for ARCore and run its Project Validation rules. Review current store target API requirements separately before publication.

`Campus > Build Android APK` makes a development APK when the module is available. Connect an unlocked phone with USB debugging, accept its RSA prompt, install the APK and grant camera permission only when Scan QR requests it. Check denied permission and non-AR fallback too. Do not treat APK generation as evidence that real navigation is accurate.

## Checkpoints
Generated PNGs are in `qr/`. Print with a white border and place only after permission and survey. The JSON position describes the user's marked standing point; `facingYaw` describes the physical arrow. Update measured floor height/model scale first, then survey and set `surveyed: true` per checkpoint. Preserve stable IDs. Rebuild and rerun route tests after changes.

## MCP connection
The existing editor bridge was available on the original project during development. The final project intentionally does not copy its auth token or expose an editor-control server. MCP is optional development tooling, not an Android runtime requirement.

To operate this final project through MCP, install the trusted MCP Unity plugin into this project, start its local authenticated server, and use the plugin's client-configuration action for your coding client. Keep remote connections disabled. Reconnect/restart the client after changing projects. Confirm the reported Unity `Application.dataPath` ends with `final/UnityProject/Assets` before any edit. Do not assume the old bridge now controls the final project. No final-project MCP connection is claimed as verified.

## Dependencies
Unity packages are pinned in `Packages/manifest.json` and `packages-lock.json`. ZXing.Net 0.16.11's netstandard2.0 DLL is bundled under Assets/Plugins/ZXing and preserved for IL2CPP. No API keys or online backend setup is needed.
