# Working on Explorers by Nature

Read README.md and docs/game-design.md before changing scope. Use docs/first-milestone.md for the current milestone and validation ledger.

- Preserve peaceful family play, comfortable first-person movement, and beautiful wilderness as priorities.
- The project uses Unity 6000.3.24f1 with URP 17.3.0. Editor activation, import, and Linux player validation now work. Check docs/first-milestone.md for exact tested states and outstanding platform/hardware checks.
- Windows and Linux are both targets. Integrated graphics support and the high-quality preset must be tested on actual hardware.
- Exact low-end CPU/GPU models remain unknown. Do not invent supported minimum specifications.
- Keep the first region small. Do not add continents, complex settlement systems, or survival mechanics without revisiting scope.
- Keep graphics quality independent of shared gameplay state. Player-built structures, planted flowers, and animals must not disappear because a decorative detail setting changes.
- Multiplayer uses the shared C# authority in `Assets/Shared`, embedded for solo and hosted by `Server` for dedicated play. Preserve the explicit `hasState` protocol flag; Unity inline JSON nulls are not reliable snapshot-presence signals. Run server tests and Unity ranch tests when changing authority or serialization.
- Commit .meta files alongside assets and pin editor/package versions. Keep generated editor data and builds out of Git.
- Record asset provenance and redistribution rights before adding third-party content to this public repository.
- Keep validation evidence tied to the exact code state. Never present unrun builds or performance targets as passed checks.
- Run checks appropriate to each change. Documentation-only changes need link and diff checks, not fabricated gameplay tests.
- Keep project documents current as working proposals become decisions.
- Use `.agents/tools/unity.sh` for setup, opening, tests, and builds. Scene generation also runs on the first interactive editor open. The source-only compiler check does not replace Unity validation.
- Use Blender CLI for scripted assets. `.agents/tools/make-rocks.py` regenerates the checked-in FBX stones and editable ArtSource/RiverStones.blend.
