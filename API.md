# API and Contracts

## Current release
No HTTP API, cloud account, external AI calls or API key is needed. Offline commands are local search. This avoids adding an unnecessary server/authentication dependency.

| Contract | Input | Result |
| --- | --- | --- |
| CampusData.Search | query, floor, building | zero or more rooms; caller must select ambiguity |
| CampusData.Decode | QR string <=128 chars | known schema-1 checkpoint or null |
| CampusRouting.TryRoute | start and end in model metres | complete NavMeshPath or explicit error |
| PoseAlignment.Calibrate | surveyed checkpoint, AR camera pose/yaw, phone height | session-to-model transform |
| PoseAlignment.Update | AR camera position | mapped feet position or invalidation |
| CampusApp.StartScan | user command | camera permission/availability flow |
| CampusApp.SetCheckpoint | approved checkpoint, AR support flag | Live or Checkpoint mode |

## Future online AI contract, not implemented
Add a separately authenticated HTTPS backend, never a provider key inside Unity. Send text and an allowlisted catalogue of IDs, not camera frames or continuous user coordinates. Validate any suggested destination against local IDs and let the deterministic path planner own routing. Require rate limits, body-size limits, timeout/retry policy, consent and deletion policy. Use standards-based user authentication only if accounts are actually required.

## Optional sensors
BLE and barometer adapters must provide timestamp, device calibration and uncertainty. They must not directly teleport the user across walls or authorize a floor change without confidence checks. Both integrations are deferred pending hardware/requirements.
