# What You Need To Supply

## Before real-campus navigation
1. Install Android Build Support, Android SDK & NDK Tools, and OpenJDK through Unity Hub for **6000.3.10f1**. They were absent at the initial audit.
2. Supply your POCO X6 Pro Android version; enable USB debugging and connect a data cable. Verify Google Play Services for AR is available. Test at least one additional supported phone and one non-AR phone.
3. Measure floor-to-floor heights. The current 3.3 m is an estimate, not a measurement. Measure a known corridor length to check overall model scale.
4. Verify all room names and stair connections in both buildings. Confirm that Environment Lab is Floor 1 above ground and Library is Floor 2 above ground. Supply upper-floor remaining room names and Building B names.
5. Obtain permission for QR markers and on-site tests. Survey each standing point and arrow heading in model coordinates. Record height, orientation, lighting and actual floor. Use printed codes with a clear quiet border; test the chosen print size/distance.
6. Confirm every doorway/corridor/stair on the intended routes is physically open and safe. State whether accessible/lift routing is required; no lift is currently mapped.
7. Allow a supervised site walk to collect accuracy and failure-recovery results. A desktop emulator cannot establish indoor positioning accuracy.
8. For distribution, provide app identity, privacy/contact information and a signing-keystore owner. Never send signing passwords or private keys in chat.

## Not required now
No BLE beacons, barometer, Wi-Fi fingerprint survey, AI account or API key is needed for the offline-first milestone. BLE and pressure sensing remain optional later work.

## Compatibility
The map targets Android API 25+ ARM64, subject to real build/device testing. AR requires a device supported by Google ARCore and camera permission. Unsupported phones get map plus QR checkpoint updates, not continuous AR/IMU location. Phone-camera tracking needs visual features and adequate light; a pocketed or covered camera cannot reliably track the walk.

Sources: [ARCore device list](https://developers.google.com/ar/devices), [AR Optional setup](https://developers.google.com/ar/develop/unity-arf/enable-arcore), [AR Foundation installation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/project-setup/install-arfoundation.html).
