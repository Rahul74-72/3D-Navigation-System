# Project Instructions

- Work inside this final directory. Preserve the original `My project` and Blender source.
- Read README.md, PRD.md and TESTING.md before making changes.
- Treat floor heights, unsurveyed checkpoints and Building B room names as provisional.
- Do not synthesize a missing staircase connection to make a route test pass.
- Never represent preview motion as live user positioning. Do not bypass surveyed-checkpoint validation.
- No API keys, access tokens, signing keys, MCP bridge tokens or camera recordings in source control.
- Keep runtime/editor/tests in their existing assemblies. Use Unity APIs and ZXing, not a new hand-written QR decoder.
- Run EditMode tests, scene preparation/route audit, desktop smoke and Android/device acceptance as applicable. Update TESTING.md with actual results, not intended results.
- Do not claim universal AR, centimetre accuracy, zero bugs, or release completion without evidence.
- Do not modify Library or generated package caches. Regenerate NavMesh after geometry changes.
