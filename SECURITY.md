# Authentication and Security

## Offline scope
No accounts, passwords or cloud authorization are needed to browse a public campus map. Runtime does not send camera frames or user positions to a server. It keeps no position history. QR decoding uses a strict ID allowlist; URLs are never launched. Malformed/unknown/schema-mismatched inputs are rejected. A QR ID is **not authentication** and does not prove the user is physically at the marker. Physical relocation/copying of a valid QR remains a threat; standing-point confirmation and trusted marker installation are required.

Camera access is initiated by Scan QR and stops with Stop. Tracking interruption invalidates alignment. No microphone, Bluetooth or pressure permissions are required in this milestone. Built development logs can reveal model position during tests; remove development logging and smoke mode from a production release, and inspect the merged Android manifest before publishing.

## Development
MCP is a powerful local editor-control service, not part of the mobile app. Keep it loopback-only and authenticated. Do not include bridge tokens, old auth tokens from chat, `.codex` configuration, keystores, Library or user settings in source archives. Rotate previously shared tokens using the plugin UI. The final project's runtime does not require MCP or AI Assistant packages.

## Future services
Only introduce accounts when a user-data requirement exists. Online AI would require backend-held credentials, authenticated short-lived sessions, HTTPS, explicit consent, request limits and destination allowlisting. No such service is claimed as implemented here.

## Release checks
Inspect APK permissions, dependency licenses, secret scan, privacy disclosure and signing configuration. Device camera-denial/revocation, malformed QR and loss-of-tracking tests are release gates.
