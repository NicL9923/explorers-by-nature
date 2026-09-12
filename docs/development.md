# Development

## Installed baseline

Unity Hub 3.21.2 and Unity Editor 6000.3.24f1 were installed on the Fedora 44 KDE development desktop on September 11, 2026. Windows Mono and Linux dedicated-server build modules were installed alongside the Linux editor.

URP 17.3.0 and Unity Test Framework 1.6.0 are bundled with that editor and pinned in `Packages/manifest.json`. Commit the package lockfile after the first successful Unity import; the stale template lockfile was intentionally removed.

The editor currently exits with code 198 and "No valid Unity Editor license found." Sign in to Hub and activate an appropriate license. Hub is available from the desktop menu or `unityhub --ozone-platform=x11` on this Fedora machine. No account credentials should be passed to project scripts or committed.

## First import

Run `.agents/tools/unity.sh setup`. This imports the project, creates materials, configures the camera and two URP quality assets, and saves `Assets/Scenes/Pinewatch.unity` as the build scene. This command is repeatable but intentionally replaces that generated scene; do not rerun it over manual scene edits without committing or saving them elsewhere.

Opening the project interactively creates the prototype scene automatically if it does not yet exist. The bundled template's `SampleScene` is not the game. Runtime terrain generation means the full valley appears after pressing Play, not in edit mode.

After a successful import, commit Unity-generated metadata, serialized settings changes, the generated scene/materials, and the resolved package lockfile. Keep `Library`, logs, and builds ignored.

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

Mouse sensitivity, field of view, and graphics preference are stored locally. No gameplay progress is saved. The settings menu releases the cursor but does not pause ambient wildlife.

## Benchmark

Build Linux with `.agents/tools/unity.sh linux`, then run:

```bash
Builds/Linux/ExplorersByNature --benchmark --quality-low \
  --benchmark-output "$PWD/Logs/benchmarks" \
  -screen-width 1280 -screen-height 720 -screen-fullscreen 0 \
  -logFile "$PWD/Logs/player-low.log"

Builds/Linux/ExplorersByNature --benchmark --quality-high \
  --benchmark-output "$PWD/Logs/benchmarks" \
  -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 \
  -logFile "$PWD/Logs/player-high.log"
```

The player waits five seconds for warm-up, then follows a fixed 60-second camera route. It writes JSON and a screenshot and exits. A manually triggered benchmark returns to the settings menu instead. Output defaults to `Benchmarks` under Unity's persistent-data folder when no output argument is supplied.

Reports contain the build's Git revision and source digest, OS, CPU, GPU, graphics API/driver string, resolution, graphics preset, frame count, mean/p95/p99 frame times, frames over 50 ms, Unity allocated memory, and process working set at the end. Memory numbers are snapshots, not peak measurements. Watch system RAM and swapping separately on the low-end PC. The benchmark disables the application's frame cap; the compositor or driver may still impose one.

Low quality reduces shadows, texture resolution, terrain detail, and tree LOD distance, and hides only decorative grass. It does not change terrain collision or wildlife count. There is no integrated-GPU performance claim yet.

## Validation

`.agents/tools/unity.sh test` runs EditMode tests for a dry, traversable trail, valid terrain heights, river placement, and connected scene assets/shaders. Read both Unity's exit status and `Logs/editmode.xml`.

`python3 .agents/tools/check-source.py` is a limited fallback that compiles source and test code against installed Unity and template assemblies. It currently passes with no warnings or errors. It does not run tests, compile shaders, import assets, validate scenes, or produce a player. It also cannot prove that the template's cached assemblies exactly match every package ultimately resolved by Unity.

The validation ledger is in [first-milestone.md](first-milestone.md). Editor setup was attempted and is blocked by activation. Windows/Linux player builds, graphical inspection, manual movement, and hardware performance checks remain pending.

## Assets and boundaries

Ground textures are 1K CC0 images from Poly Haven. Sources, download URLs, and SHA-256 checksums are recorded in [assets.json](assets.json). Poly Haven permits redistribution of its CC0 assets; see its [asset license](https://polyhaven.com/license), checked September 11, 2026.

Tree/deer meshes and grass/water shaders are original prototype assets. They establish placement and behavior, not final realism. Unity's template settings came from the installed Universal 3D template.

This is a landscape experiment. There is no networking, construction, animal harvesting, settlement management, audio landscape, or saved world yet. The installed server module does not mean a server implementation exists.
