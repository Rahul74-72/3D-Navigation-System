# Implementation Checklist

| Requested area | Implementation / evidence | Remaining |
| --- | --- | --- |
| PRD / design / architecture | Documents in this folder | Campus owner approval |
| Database / API | Local catalogue and internal contracts | Survey/import of final room data |
| AGENTS / README | Included | Keep updated after changes |
| Project implementation | Separate final Unity project; original preserved | Release sign-off |
| Setup / initialisation | Reproducible scene and build tool | Install Android modules |
| Feature development | Search, floor selection, route preview, QR, AR integration | Hardware UX and positioning validation |
| Authentication / security | Offline/no account; local camera; strict QR allowlist | APK manifest and physical marker security audit |
| MCP connection | Existing original editor connected; final setup documented | Optional final-project bridge must be explicitly configured |
| Integrations | glTFast, AR Foundation, ARCore, ZXing | BLE/barometer deferred; no cloud AI by user choice |
| Testing / QA | Core tests, route matrix, compiled-player smoke | Android and campus tests |
| Debugging | Diagnostic logs and reproducible failures | Fix all release blockers before claiming complete |

## Geometry and routing
The supplied four-floor model is preserved. A complete path is required before Preview or AR directions are drawn. The gate-to-first-floor Classroom 4 route was verified in the earlier Unity preview and is covered again in the final project's tests. The current audit enumerates every room from the matching building's gate; consult qa/route-audit.csv rather than assuming the whole two-building campus is connected.

Building B needs a geometry/connectivity investigation and physical comparison. Do not silently clone a staircase, bridge walls or lower agent clearance just to turn red tests green. Any model correction must be reviewed, re-exported and rebaked. The NavMesh radius is 0.18 m and height 1.65 m for the small preview avatar; this is not a certified human/accessibility clearance model.

## Meaning of implementation versus completion
Passing source tests and a desktop preview does not certify Android camera operation, SLAM drift, absolute positioning, floor transitions, battery life or all physical routes. No telemetry or simulated pose is being substituted for those tests. The full project remains a development milestone until its hardware and survey gates are satisfied.
