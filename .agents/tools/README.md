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
