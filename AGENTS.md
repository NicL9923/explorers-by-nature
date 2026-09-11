# Working on Explorers by Nature

Read README.md and docs/game-design.md before changing scope. Use docs/first-milestone.md for the current milestone and validation ledger.

- Preserve peaceful family play, comfortable first-person movement, and beautiful wilderness as priorities.
- Unity 6 with URP is provisional. There is no engine project yet; do not claim a playable build exists.
- Windows and Linux are both targets. Integrated graphics support and the high-quality preset must be tested on actual hardware.
- Exact low-end CPU/GPU models remain unknown. Do not invent supported minimum specifications.
- Keep the first region small. Do not add continents, complex settlement systems, or survival mechanics without revisiting scope.
- Keep graphics quality independent of shared gameplay state. Player-built structures, planted flowers, and animals must not disappear because a decorative detail setting changes.
- Multiplayer will use a dedicated server. Test the networking and persistence foundation before expanding ranch systems.
- Once a Unity project exists, commit .meta files alongside assets and pin editor/package versions. Keep generated editor data and builds out of Git.
- Record asset provenance and redistribution rights before adding third-party content to this public repository.
- Keep validation evidence tied to the exact code state. Never present unrun builds or performance targets as passed checks.
- Run checks appropriate to each change. Documentation-only changes need link and diff checks, not fabricated gameplay tests.
- Keep project documents current as working proposals become decisions.
