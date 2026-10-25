# Campus Navigator: Product Requirements

Status: implementation and local verification, NOT approved for campus deployment.

## Product
An offline-first Android campus map with optional AR guidance. Browse two buildings, four floors, search destinations, preview a walking route, initialize position at a surveyed QR checkpoint, and track movement using ARCore where available. No accounts or cloud AI are required for this release.

## Confirmed requirements
- Building A ground: Office, Classrooms 1-3, Computer Lab, Language Lab, IoT Lab.
- First floor above ground: Environment Lab and Classrooms 4-9.
- Library is above Environment Lab on the second floor above ground.
- Remaining floor/Building B names are provisional, not surveyed facts.
- Floor height remains estimated at 3.3 m. Four floors means ground plus three.
- POCO X6 Pro is the first test device. Other Android models need a device matrix, not a claim of universal AR support.
- Offline destination commands first; no generative AI, microphone or account service in this version.

## Acceptance criteria
1. Map loads without missing materials; building and floor filters work in portrait and landscape.
2. Search returns an explicit empty state and disambiguates identical names by building/floor.
3. Preview is unmistakably labelled, never represented as measured user motion.
4. A destination requires a complete NavMesh path; incomplete paths are rejected.
5. Valid QR IDs come from the bundled catalogue. Unknown URLs, schemas and oversized payloads are rejected.
6. Real navigation cannot start from an unsurveyed checkpoint.
7. Tracking loss, application interruption, a large pose jump, travel correction budget, or an unconfirmed floor change removes AR guidance and requests correction.
8. AR is optional: unsupported devices retain map/QR checkpoint mode without pretending to have continuous location.
9. Camera frames stay on device. Camera permission denial must leave map browsing usable.
10. Android build, physical QR acquisition, scale/alignment, accuracy, stairs, performance, battery and recovery tests must pass before release.

## Explicit exclusions / deferred work
BLE correction is a later integration. Barometer floor inference is optional and not implemented: QR is authoritative. No independent IMU dead reckoning, background tracking, voice recognition, generative AI, evacuation routing or wheelchair-route certification. No inferred stair links may be added through absent geometry.

## Release blockers
Measured floor heights and checkpoint transforms; verified room/floor naming and accessible stair connections; campus marker permission; Android toolchain; physical device and campus acceptance tests. See TESTING.md and REQUIREMENTS.md.
