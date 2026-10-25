# Testing, QA and Debugging

## Recorded evidence
- Unity EditMode suite: **39 passed, 0 failed, 0 skipped**, see qa/editmode.xml.
- Route audit: **29/56 connected** from each room's matching building gate. All 28 Building A destinations connect; Building B ground office connects, other 27 do not. See qa/route-audit.csv.
- Local scene preparation compiled and executed successfully. See qa/prepare.log.
- All ordered room pairs audited: **820/3,080 connected**, 2,260 explicitly blocked. This is coverage evidence, not a passing whole-campus acceptance test. See qa/room-pairs.csv.
- Windows development build succeeded. Built-player smoke verified offline search/building/floor filters and arrival at first-floor Classroom 4, with zero captured runtime errors. See qa/runtime-smoke.txt and qa/player.log.
- Screenshots checked at 1440x900, 540x960 and 360x800. Pixel checks confirmed nonblank output and no sampled magenta error-shader pixels. See qa/desktop.png, portrait.png, small-phone.png and pixel-check.json. This is desktop rendering at phone-shaped resolutions, not an Android device test.
- Android build preflight: **BLOCKED** with `Install Android Build Support, SDK/NDK and OpenJDK through Unity Hub first.` See qa/build-android.log. No APK was produced.
- Physical AR/campus accuracy: **NOT RUN**, pending requirements. Never interpret these as passed.

## Automated coverage
Catalogue uniqueness/schema/finiteness, verified naming, case-insensitive search, unknown and oversized QR rejection, QR encode/decode round trip, phone-to-ground alignment, heading rotation, transform round trip, invalid phone height, tracking jumps, correction budget, recalibration, route length/lookahead, offline command matching, actual staircase intermediate heights, off-map failure.

## Required release matrix
| Test | Pass condition | Status |
| --- | --- | --- |
| Android compile + install | APK builds; installs and opens without fatal errors | Pending toolchain |
| POCO X6 Pro AR | camera, pose, map/AR switch, QR all work | Pending phone |
| Additional supported Android | same functional tests | Pending phone |
| Unsupported AR device | usable map and QR, no false live tracking | Pending phone |
| Permission denied/revoked | usable map, clear recoverable status | Pending phone |
| Real QR scanning | >=19/20 acquisitions at agreed lighting/distance | Pending survey |
| Scale and heading | measured walk matches model; orientation aligned | Pending survey |
| Position error | target <=1.5 m at 90% of 20+ measured checkpoints; report actual distribution | Pending site |
| Floor changes | all transitions confirmed by correct-floor QR; no wrong-floor arrival | Pending site |
| Tracking interruption | cover lens, low light, pocket, pause/resume; arrows stop until corrected | Pending phone |
| Walls and stairs | every accepted physical route walked; no clipping/unsafe links | Building B blocked |
| UI | no clipping at 360x800, 540x960 and landscape, including permission/calibration states | Desktop smoke + phone pending |
| Performance | target >=30 FPS after warm-up; measure p95 frame time, memory, thermal/battery over 20 min | Pending phone |
| Privacy/security | no keys/frames/history uploaded; inspect merged manifest; unknown QR rejected | Source tests + APK pending |
| Distribution | signed build, privacy info, license notices, rollback package | Pending owner |

Accuracy numbers above are acceptance targets, not measurements or promises. Record phone/OS, build hash, lighting, checkpoint, actual position, app position, error, floor and correction count in each trial. Do not improve metrics by excluding failures without reporting them.

## Debugging
- Pink geometry: check missing materials and shader inclusion, not just lighting.
- No route: inspect route-audit.csv, geometry, door clearance, baked NavMesh and same-floor sampling. Never draw a direct line as fallback.
- Camera black: inspect permission, AR support/services, loader, AR background renderer feature and graphics API.
- Position rotated: recheck checkpoint arrow yaw and phone heading confirmation. A QR ID contains no pose.
- Vertical drift: remeasure floor heights/phone height; rescan. Pressure sensing is not present.
- Editor cannot reach licensing: run Unity under the signed-in desktop account rather than a restricted background sandbox.
- Build fails: inspect qa build log, first compiler error and Android module availability. Do not suppress exceptions to report success.

## Reproduction
Use tools/Verify.ps1. Keep raw test XML, route CSV, build logs and screenshots. Re-run all route tests after model/catalogue edits; re-run device/campus tests after tracking, scale, SDK or marker changes.
