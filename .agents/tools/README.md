# Project tools

- `unity.sh`: open the pinned editor, generate the prototype scene, run EditMode tests, or build Linux/Windows with a source-state stamp.
- `check-source.py`: compile source and test code against installed Unity assemblies; a limited check when editor activation is unavailable.
- `make-rocks.py`: use Blender headlessly to generate editable river-stone models and export Unity-ready FBX files.
- `benchmark.sh`: run the Linux player's fixed low/high camera benchmark, optionally selecting a Vulkan GPU by index and adding a third `rain`, `grove` or `river` argument for weather, woodland or interactive water.

- `server.sh`: test, publish self-contained Linux/Windows servers, or run the dedicated ranch authority.
- `make-ranch-art.py`: generate original cow and hen Blender sources and Unity FBX models.
- `ranch-smoke.py`: launch two rendered Linux players against an isolated published server and compare both snapshots with disk.
- `package-release.py VERSION`: package tested desktop/server builds with the playtest guide, source identity and SHA-256 checksums.
- `make-woodland-art.py`: generate original pine, aspen and daisy meshes with LODs, foliage textures, editable Blender sources and preview renders.
- `make-homestead-art.py`: generate the timber building kit, coop, original wood textures, LOD meshes and Blender previews.
- `make-wildlife-art.py`: generate sculpted deer/cottontail meshes, simplified LODs, baked original coat maps and Blender previews.
- `verify-release.py VERSION`: compare all five published release files with local sizes and SHA-256 digests.
- `make-river-wildlife.py`: generate original mallard and beaver Blender models, coat texture, LODs and studio previews.
- `make-fox-art.py`: generate the original red fox sculpt, baked coat, LODs and studio previews.

- `rig-animals.py`: skin all seven original animal LOD pairs from their Blender sources and re-export FBX armatures; run after regenerating animal art.
- `check-animal-rigs.py`: verify normalized skin weights and render articulated poses from the seven rigged Blender studios.

- `detail-animal-art.py`: bake original directional coat and feather color/normal atlases, preserve material slots, and save detailed animal studios before rigging.

- `source-manifest.py OUTPUT [--verify]`: record exact runtime/assets/tests for the validation ledger or check they remain unchanged.
- `make-reference-ground.py`: Convert the recorded CC0 Poly Haven fern, stump and moss-rock scans to meter-scale FBX props with three LODs.

- `fetch-reference-trees.py`: Download checksum-verified CC0 Poly Haven tree originals to ignored raw source storage.
- `import-reference-trees.py`: Export budgeted tree FBX LODs from authored Blender card geometry.
- `prepare-reference-tree-textures.py`: Pack source needle alpha with albedo and prepare Unity normal/color imports.
- `prepare-reference-ground-textures.py`: Restore hash-verified ground scan downloads and pack original PBR texture channels for Unity.

- `make-reference-deer.py`: rebuild the separate photo-referenced adult doe, 30k/6k LODs, compatible rig, 2k coat and inspection views.
- `preview-reference-trees.py`: Rebuild compact editable tree source and render an isolated silhouette/UV preview.

- `grove-capture.py`: capture six fixed first-person grove, doe and overlook views with isolated saves and preferences.

- `rebuild-reference-trees.py`: Rebake and export both authored trees with a valid temporary Blender color configuration.
- `bake-reference-trunks.py`: Flatten scanned-base and triplanar bark materials into the exported trunk UV0 atlas.

- `record-grove-validation.py --prefix NAME`: collect only matching build, benchmark, capture and multiplayer evidence for the Fern Hollow release.

- `nature-motion.py`: record isolated 20-second player clips and exact frame posters showing wind, spring fur, cow tail hair, and water interaction.
- `record-nature-validation.py --prefix NAME`: verify matching desktop stamps, both motion clips, actual quality budgets, and Riverbend benchmarks before collecting release evidence.
- `player-capture.py`: capture all four in-game explorer previews at a chosen resolution and quality with isolated preferences.

- `make-player-art.py`: generate four original frontier characters, fabric/leather maps, articulated FBX parts, editable Blender source and a studio contact sheet. Run with Python to apply the Fedora Blender color configuration workaround.
- `make-frontier-tools.py`: generate original muzzleloader, axe, pickaxe and gripping hands with named moving parts, textures, FBX exports and editable Blender studio.
- `check-frontier-art.py`: round-trip all nine frontier FBX assets and record file hashes, dimensions, triangle counts, UV coverage and required pivot checks.
- `make-horse-art.py`: build and render the original articulated saddle horse and packed Blender source.
- `frontier-capture.py`: exercise gathering, riding and hunting in the player, and record the full reload with matching gameplay proof.
- `bounded-run.sh COMMAND...`: serialize heavy validation and cap it at 3 CPU equivalents, 8 GiB memory pressure / 10 GiB hard limit, no swap, and 25 minutes; use for editor, builds, captures and rendering on the shared desktop.
- `record-frontier-validation.py --source-manifest PATH [--require-small-ui]`: verify frozen sources, matching desktop builds, tests, wardrobe/gameplay captures, encoded reloads, and multiplayer appearance/save evidence before collecting the frontier gallery and ledger artifacts.

- `western-hair-capture.py`: record isolated High horse mane/tail and explorer hair close-ups with measured guide motion, four posters and a verified ten-second video.
- `record-cinematic-validation.py`: collect only matching desktop builds, frozen sources, High/Low scenic and motion captures, western hair close-ups, and multiplayer evidence for the lighting release.
- `cinematic-checks.sh`: run the serial, resource-bounded cinematic tests, desktop builds and captures, then collect matching evidence; uses NVIDIA Vulkan device 0 for High and AMD device 1 for Low on NicolasDESKTOP.
