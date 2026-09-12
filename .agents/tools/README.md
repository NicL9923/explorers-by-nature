# Project tools

- `unity.sh`: open the pinned editor, generate the prototype scene, run EditMode tests, or build Linux/Windows with a source-state stamp.
- `check-source.py`: compile source and test code against installed Unity assemblies; a limited check when editor activation is unavailable.
- `make-rocks.py`: use Blender headlessly to generate editable river-stone models and export Unity-ready FBX files.
- `benchmark.sh`: run the Linux player's fixed low/high camera benchmark, optionally selecting a Vulkan GPU by index.

- `server.sh`: test, publish self-contained Linux/Windows servers, or run the dedicated ranch authority.
- `make-ranch-art.py`: generate original cow and hen Blender sources and Unity FBX models.
- `ranch-smoke.py`: launch two rendered Linux players against an isolated published server and compare both snapshots with disk.
- `package-release.py VERSION`: package tested desktop/server builds with the playtest guide, source identity and SHA-256 checksums.
- `make-woodland-art.py`: generate original pine, aspen and daisy meshes with LODs, foliage textures, editable Blender sources and preview renders.
- `make-homestead-art.py`: generate the timber building kit, coop, original wood textures, LOD meshes and Blender previews.
- `make-wildlife-art.py`: generate sculpted deer/cottontail meshes, simplified LODs, baked original coat maps and Blender previews.
- `verify-release.py VERSION`: compare all five published release files with local sizes and SHA-256 digests.
