# Development

## Installed baseline

Unity Hub 3.21.2 and Unity Editor 6000.3.24f1 were installed on the Fedora 44 KDE development desktop on September 11, 2026. Windows Mono and Linux dedicated-server build modules were installed alongside the Linux editor.

URP 17.3.0 and Unity Test Framework 1.6.0 are bundled with that editor and pinned in `Packages/manifest.json`. The resolved package lockfile, imported asset metadata, generated scene, and materials are committed.

Unity Personal is activated on the development machine. Official Unity CLI 1.0.0-beta.8 is available at `~/.local/bin/unity`; the project wrapper uses its run, test, build, and open commands. On another machine, sign in and activate an appropriate license through Hub. No account credentials should be passed to project scripts or committed.

## First import

Run `.agents/tools/unity.sh setup`. This imports the project, creates materials, configures the camera and two URP quality assets, and saves `Assets/Scenes/Pinewatch.unity` as the build scene. This command is repeatable but intentionally replaces that generated scene; do not rerun it over manual scene edits without committing or saving them elsewhere.

Opening the project interactively creates the prototype scene automatically if it does not yet exist. The bundled template's `SampleScene` is not the game. Runtime terrain generation means the full valley appears after pressing Play, not in edit mode.

After future imports or upgrades, commit relevant Unity-generated metadata, serialized settings changes, and the package lockfile. Keep `Library`, logs, and builds ignored.

## Controls

| Action | Input |
| --- | --- |
| Walk | WASD or arrow keys |
| Look | Mouse |
| Walk faster | Left Shift |
| Settings and release mouse | Escape |
| Return to meadow | Home |
| Switch low/high graphics | F2 or settings |
| Run camera benchmark | F6 or settings |

Mouse sensitivity, field of view, and graphics preference are stored locally. Ranch progress is saved locally or by the dedicated server. The settings menu releases the cursor but does not pause ambient wildlife.

## Benchmark

Build Linux with `.agents/tools/unity.sh linux`, then run:

```bash
.agents/tools/benchmark.sh high
.agents/tools/benchmark.sh low

# Optional: choose a Vulkan device. Verify its index with vulkaninfo first.
.agents/tools/benchmark.sh low 1
```

The player waits five seconds for warm-up, then follows a fixed 60-second camera route. It writes JSON and a screenshot and exits. A manually triggered benchmark returns to the settings menu instead. Output defaults to `Benchmarks` under Unity's persistent-data folder when no output argument is supplied.

Reports contain the build's Git revision and source digest, OS, CPU, GPU, graphics API/driver string, resolution, graphics preset, frame count, mean/p95/p99 frame times, frames over 50 ms, Unity allocated memory, and process working set at the end. Memory numbers are snapshots, not peak measurements. Watch system RAM and swapping separately on the low-end PC. The benchmark disables the application's frame cap; the compositor or driver may still impose one.

Low quality reduces shadows, texture resolution, terrain detail, and tree LOD distance, and reduces decorative grass density. A lighter meadow layer remains visible on low graphics. It does not change terrain collision or wildlife count. The current integrated-GPU measurement uses the desktop’s AMD Radeon with a Ryzen 9 CPU and 32 GB RAM; it does not establish the proposed two-core, 8 GB minimum.

## Validation

`.agents/tools/unity.sh test` runs EditMode tests for a dry, traversable trail, valid terrain heights, river placement, and connected scene assets/shaders. Read both Unity's exit status and `Logs/test.xml`. `.agents/tools/unity.sh playtest` also runs the scene and verifies terrain collision, walking, and preservation of wildlife across quality settings. Its report is `Logs/playtest.xml`.

`python3 .agents/tools/check-source.py` is a limited fallback that compiles source and test code against installed Unity and project assemblies, falling back to template assemblies before first import. It does not run tests, compile shaders, import assets, validate scenes, or produce a player. It also cannot prove that the template's cached assemblies exactly match every package ultimately resolved by Unity.

The validation ledger is in [first-milestone.md](first-milestone.md). It records actual Unity tests, player builds, rendered benchmarks, and remaining checks. Family playtesting and validation on the weakest intended PC remain outstanding.

## Assets and boundaries

Ground textures are 1K CC0 images from Poly Haven. Sources, download URLs, and SHA-256 checksums are recorded in [assets.json](assets.json). Poly Haven permits redistribution of its CC0 assets; see its [asset license](https://polyhaven.com/license), checked September 11, 2026.

Blender 5.2.1 CLI generated three original river stones. Run `blender --background --factory-startup --python .agents/tools/make-rocks.py` to regenerate `Assets/Models/RiverStone*.fbx` and the editable `ArtSource/RiverStones.blend`. FBX export and Unity import were verified. The newer art generators use a temporary compatible OpenColorIO configuration for studio renders without changing the system installation.

Trees, deer, rabbits, ranch animals and buildings now use original Blender assets with distance meshes; see [the art pass](art-pass.md). Grass and water use original shaders. Unity's template settings came from the installed Universal 3D template.

The ranch implementation is described in [network-design.md](network-design.md), and player/server instructions are in [morning-playtest.md](morning-playtest.md).

`blender --background --factory-startup --python .agents/tools/make-ranch-art.py` regenerates original Clover and Hen FBX assets and editable Blender sources. The game remaps their named materials to URP at runtime. These assets and the procedural nature audio are original work.

Server commands:

```sh
.agents/tools/server.sh test
.agents/tools/server.sh publish
.agents/tools/server.sh run --data /tmp/my-test-ranch
python3 .agents/tools/ranch-smoke.py
```

The last command requires published Linux server and player builds and a graphical session. It opens two game windows, builds and harvests against an isolated server, compares both clients to its saved JSON, captures screenshots and cleans up the processes. `RanchPlayTests` uses isolated temporary saves and checks Unity rendering, placement, move, animal products and reload. The console server suite checks simultaneous commands, bounds, 20 clients, framing, authentication and restart.

Run `.agents/tools/unity.sh art` after changing foliage shader requirements to prepare the serialized shader variant. `python3 .agents/tools/ranch-smoke.py --art-tour` also captures close-up player screenshots.
