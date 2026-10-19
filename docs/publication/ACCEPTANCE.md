# Acceptance for the exact standalone ZIP

Extract the review ZIP into a new directory. Verify `SHA256SUMS` and the full
ZIP inventory before serving it with `python3 -m http.server`. Test once at
the server root and once under a subdirectory. Do not use a production build
or a developer build as substitute evidence.

Record independent sessions for desktop Chromium, desktop Firefox, and a
physical mobile browser. Mobile emulation is supplementary. Each session
needs its browser/version, OS/device, UTC start/end, screenshots or recordings,
console output, and a full request inventory including visibility changes
and reload. Only extracted static files may load; there must be no external
requests, service APIs or WebSockets.

For every required browser, exercise:

1. Fresh startup and tutorial, with no public-name field or social UI.
2. Pointer/touch steering; on keyboards, WASD, release/coasting, focus loss,
   opposing keys, and pointer takeover. Text entry must not steer the ship.
3. Docking, combat and rewards, ship upgrades, and settings/audio changes.
4. Reload persistence and pause/resume after switching tabs.
5. Save export/import, including manual clipboard fallback and cancellation.
6. Corrupt-save rejection without replacing valid progress; backup recovery
   after corrupting the primary save in a disposable test browser profile.
7. Physical-device rotation and fullscreen, including clipboard text entry.

The acceptance JSON stays outside Git. Its `artifact_files` must exactly
match `.artifacts/build.json`'s `files`. Its `sessions` contains entries with
`browser` equal to `chromium`, `firefox`, and `physical-mobile`; `platform`,
`started_utc`, `ended_utc`, `evidence`, `network_external_requests`,
`websockets`, and `scenarios`. The physical-mobile entry must include
`physical_device: true`. Scenario keys are listed in `tool/publication.py`.
Use `passed` only for interactions actually performed and evidenced. Failed
or unperformed scenarios block release packaging. A local review ZIP remains
available for completing acceptance without uploading anything publicly.
