# 45-Day Delivery Plan

Budget per working day: 30 min review/logging, 2 h implementation or modelling, 1 h testing, 30 min documentation/backup (4 h total). On field days use the testing block for supervised site work. Adjust hours to your availability; do not remove verification to meet a date.

| Days | Work | Exit evidence |
| --- | --- | --- |
| 1-3 | Install Android modules; phone/OS compatibility; permissions; verified room inventory | Device connected, build toolchain, survey checklist |
| 4-6 | Measure floor heights, model scale, office/gallery boundaries and stair geometry | Signed measurements + corrected Blender file |
| 7-9 | Repair Building B connections; review all four floors, doors, roofs | Reviewed GLB + complete intended route matrix |
| 10-12 | Map UI, floor/search review, destination coordinates and labels | Desktop + portrait interaction tests |
| 13-15 | Print/install approved QR standing marks and arrows | Versioned survey catalogue and marker photos |
| 16-18 | APK installation, camera QR scanning, permission-denial fallback | Device scan results |
| 19-21 | AR pose alignment, map/AR switching, track-loss recovery | Supported and unsupported phone tests |
| 22-24 | Inter-floor route walks, rescan transitions, arrival behavior | Per-stair/per-floor acceptance records |
| 25-27 | AR arrows and guide placement; comfort and obstruction review | On-device route videos + fixes |
| 28-30 | Offline commands and ambiguity; optional barometer feasibility only | Command tests; sensor decision documented |
| 31-33 | Accuracy runs in varied light/crowding; correction-spacing adjustment | Measured error distribution |
| 34-36 | Performance, memory, battery, thermal and UI accessibility | 20-min device profiles + regression tests |
| 37-39 | Resolve defects; rerun route/device/security tests | No unresolved release-blocking defects |
| 40-42 | Signed candidate APK, privacy/licenses, clean-machine setup | Candidate build + reproducible evidence |
| 43-45 | Supervisor/user acceptance, demonstration rehearsal, buffer and handover | Approved release or explicit remaining blockers |

BLE and cloud AI are not commitments inside this offline-first milestone. Add them only after the base positioning/navigation acceptance succeeds, with a revised budget.
