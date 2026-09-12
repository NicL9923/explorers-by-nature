# Project tools

- `unity.sh`: open the pinned editor, generate the prototype scene, run EditMode tests, or build Linux/Windows with a source-state stamp.
- `check-source.py`: compile source and test code against installed Unity assemblies; a limited check when editor activation is unavailable.
- `make-rocks.py`: use Blender headlessly to generate editable river-stone models and export Unity-ready FBX files.
- `benchmark.sh`: run the Linux player's fixed low/high camera benchmark, optionally selecting a Vulkan GPU by index.

- `server.sh`: test, publish self-contained Linux/Windows servers, or run the dedicated ranch authority.
- `make-ranch-art.py`: generate original cow and hen Blender sources and Unity FBX models.
- `ranch-smoke.py`: launch two rendered Linux players against an isolated published server and compare both snapshots with disk.
