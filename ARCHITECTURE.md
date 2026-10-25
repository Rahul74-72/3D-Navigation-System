# Architecture

## Modules
| Module | Responsibility |
| --- | --- |
| CampusData | Versioned room/checkpoint catalogue, validation, offline search |
| CampusRouting | NavMesh sampling, complete-path requirement, route length/lookahead |
| PoseAlignment | Rigid yaw + translation alignment between AR session and model metres |
| CampusTracking | AR availability, camera permission, QR decode, tracking/correction state |
| CampusApp | Preview/live/checkpoint state, route ownership, user marker, guide, floor view |
| CampusUI | Responsive map/search/filter/scan/preview controls |
| CampusBuild | Reproducible scene setup, Android settings, route audit, QR generation, builds |
| CampusTests / CampusSmoke | Pure/core/integration tests and built-player smoke capture |

## Coordinate contract
Unity metres, Y up. Imported Blender `(x,y,z)` maps to Unity `(-x,z,-y)`. Model base levels are estimated 0, 3.3, 6.6, 9.9 m. Asset transform stays identity. Do not correct scale only in the AR camera: revise model, catalogue, bake and checkpoint survey together.

At calibration:
`R = yaw(checkpointArrow - phoneHeading)`
`offset = checkpointGround + phoneHeight * up - R * ARCameraPosition`
`mapFeet = R * ARCameraPosition + offset - phoneHeight * up`
`ARVirtualPoint = inverse(R) * (mapPoint - offset)`

ARCore already fuses visual and inertial measurements. Integrating raw IMU acceleration again would double-count motion and drift. QR decoding supplies an approved ID; manual standing-point/heading confirmation supplies the initial pose. This is not automatic QR pose estimation.

## Position confidence
Uncalibrated -> recognized checkpoint -> surveyed confirmation -> tracking -> correction required. A pose jump over 2 m between updates or over 35 m accumulated travel invalidates calibration. These are conservative configurable engineering thresholds, not measured error bounds. Tracking loss and pause invalidate alignment. A floor change near a model level requires another QR scan. No barometer or BLE readings are fabricated.

## Routing
Scene preparation closes both ground-office doors and rebuilds the NavMesh from the actual building meshes, excluding navigation annotations and labels. Imported colliders alone are insufficient because they omit corridor slabs and stair treads. The bake preserves the 0.18 m radius, 1.65 m height, 0.2 m climb and 0.04 m voxel size. No synthetic stair links are added. Start/end sample radii are bounded to avoid snapping through floors; only complete paths are accepted. Offline preview uses NavMeshAgent. Live movement is measured by AR, sampled against the walkable surface, and rerouted once per second. Missing inter-floor connectivity remains blocked and is listed in the route audit.

## Rendering
Map camera excludes AR layer 30. AR camera sees only layer 30 and camera background. The campus mesh is not rendered over the real building. Route and companion are transformed from map into AR coordinates. Switching to map keeps the AR pose provider active on supported devices.

## Deployment
Unity 6000.3.10f1, URP 17.3.0, AR Foundation/ARCore 6.3.5, glTFast 6.20.0, ZXing.Net 0.16.11. Android: AR Optional, ARM64, IL2CPP, OpenGLES3, minimum API 25 (the installed Unity minimum). Compatibility must still be proven per device. Windows preview is a development aid, not an Android AR test.
