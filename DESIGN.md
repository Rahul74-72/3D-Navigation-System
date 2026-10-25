# Design

## Experience
- Default 3D view is a perspective camera behind the person, including during the preview walk. The overhead map is an explicit optional view.
- Compact destination panel: query, building, floor, results, selection, preview start, Preview/Scan QR/Stop, AR/map toggle and status.
- Desktop uses a left panel; portrait uses the lower panel with the map above. Results scroll independently.
- Floor isolation applies in overhead map view. Follow view renders the whole building, including roofs and higher floors.
- Navigation green, measured-position blue, neutral architecture. Status always states Preview versus Live versus Checkpoint.
- Zoom, pan and Home remain explicit controls. Map geometry comes from the supplied GLB, not a decorative illustration.

## Interaction
Search accepts room names and the prefixes `take me to`, `navigate to`, `go to`, `find`. This is deterministic offline command matching, not general conversation. Building/floor selectors resolve ambiguity. A selection never starts camera access automatically.

Scan QR requests camera permission. A recognized code opens checkpoint confirmation. Stand on its surveyed standing mark, face its surveyed arrow, and enter phone height. These are necessary because decoding a QR ID alone does not determine its 3D pose. Confirming an unapproved marker is blocked.

Live map and AR share a single position and route. The AR guide is placed a short distance ahead of the user's current route, not used as a substitute for user tracking. A completed preview stops; live arrival requires the destination floor to be confirmed.

## Accessibility and safety
Do not claim stair-only routes are accessible. No lifts have been surveyed. Do not use this app for evacuation. Phone camera use must not distract from steps; hold handrails and stop to scan. Physical signage and staff take precedence. Touch size, clipping, safe areas, contrast and device-specific font scaling remain hardware acceptance items.
