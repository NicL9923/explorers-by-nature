# First milestone: the walk

## Question

Can one small forest-to-overlook walk feel beautiful and comfortable on both a weak integrated GPU and a stronger dedicated GPU?

This is the first technical milestone, not the full family-playable game. It is complete only after actual Windows and Linux builds are exercised and results are recorded.

## Engine candidate

The prototype uses Unity 6000.3.24f1 and Universal Render Pipeline 17.3.0, with one rendering pipeline across quality levels. The editor and desktop support modules are installed and activated. The project imports, generates its scene, and runs as a standalone Linux player.

Unity describes URP as covering mobile through high-end PCs and exposes quality controls. Those capabilities justify evaluating it; they do not establish performance for this game.

- [URP introduction](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/urp-introduction.html), consulted September 11, 2026.
- [URP quality controls](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/quality/quality-settings-through-code.html), consulted September 11, 2026.
- [Unity platform requirements](https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html), consulted September 11, 2026. Check editor and player requirements separately, including Linux distribution, GPU, and driver support.
- [Godot renderer comparison](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html), consulted September 11, 2026. Godot is an alternative if the Unity evaluation fails; switching between its Compatibility and advanced renderers requires care.

## Build sequence

1. Identify an actual low-end test PC and a stronger PC. Record CPU, GPU, RAM, OS, drivers, and resolution. Confirm access to both Windows and Linux testing.
2. Install the selected Unity editor and required desktop build modules. Verify development on the available Linux environment before adopting the toolchain.
3. Create the URP project. Commit editor and package versions, project settings, and all asset metadata. Establish a repeatable command to build desktop players.
4. Build a small authored route through a meadow and forest to an overlook. Include moving foliage, water, distant terrain, and one placeholder wildlife actor so the scene represents the intended workload.
5. Add first-person movement and essential camera settings. Use forgiving terrain collision and avoid mandatory camera motion.
6. Create low and high quality presets. Measure a fixed camera route in standalone builds, then adjust assets and rendering budgets based on the results.

Use redistributable assets with recorded sources and licenses. Do not commit paid source assets to the public repository unless their license explicitly permits redistribution.

## Graphics rules

| Scalable detail | Must remain consistent |
| --- | --- |
| Decorative ground cover density | Player-planted flowers and other interactable objects |
| Shadow resolution and distance | Buildings, collision, and navigation |
| Texture resolution and render scale | Animal existence and gameplay behavior |
| Distant mesh complexity | Mountain silhouettes and major landmarks |
| Reflections and atmospheric effects | Player positions and shared world state |

Preserve scenery composition on low settings. Distant terrain can be simple while retaining the view. Avoid designing essential visuals around effects that the minimum hardware cannot run.

## Acceptance and validation ledger

Provisional performance goal: 720p at 30 FPS on the named low-end machine. This goal still needs confirmation. Choose a high-preset target once its test GPU is known.

Record measurements against the exact commit and build configuration. Use the same route and scene for comparisons. Measure frame-time distribution, visible hitches, and memory use after warm-up. Verify that performance does not depend on sustained swapping on the 8 GB machine.

| Check | State | Evidence |
| --- | --- | --- |
| Exact low-end hardware identified | Pending | No specific CPU/GPU supplied |
| Editor and packages pinned | Passed | Unity 6000.3.24f1, URP 17.3.0, committed package lockfile |
| Hub and editor installation | Passed | Hub 3.21.2, editor, Windows Mono, Linux server modules installed |
| Editor setup and scene generation | Passed | Official Unity CLI; scene, materials, and Blender FBX imported |
| Unity EditMode tests | Passed | Three tests: trail, heightmap/river, scene wiring and shaders |
| Unity PlayMode test | Passed | Terrain collision, six-meter walk, and wildlife retained at both quality levels |
| Windows standalone build | Passed, execution pending | Windows x64 Mono player cross-built through Unity CLI; no Windows PC exercised |
| Linux standalone build | Passed | Mono player built through Unity CLI and rendered on two GPUs |
| Low-preset performance | Partial | 720p benchmark passed on desktop AMD integrated GPU; weakest intended PC still unidentified |
| High-preset appearance and performance | Partial | 1080p RTX 5070 Ti benchmark and screenshot inspected; trees/deer/grass remain placeholder art |
| Camera comfort and enjoyable view | Pending | Requires family playtest |

Do not report supported minimum specifications or a validated engine choice from editor screenshots or documentation alone.

The current implementation and commands are described in [development.md](development.md). The benchmark's 60-second camera pass is deliberately accelerated; it is not the intended duration of a player's expedition.

### Recorded benchmark evidence

The validated implementation and imported assets are committed in `01a1c56`. Final checks passed on that runtime code: three EditMode tests and one PlayMode test. Later ledger or Git-attribute edits do not change the player code.

Captured September 11, 2026 local time, September 12 UTC. Both reports identify the same Linux player input state: base commit `4637398`, source SHA-256 `65c25f32369d9c659bdd0ffe63e7c2f5c1d62853724710bd8ffd2020e1529503`. This stamp includes uncommitted build inputs; the reports preceded the commit that publishes this ledger.

| Run | Mean frame time | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| AMD integrated, Vulkan, Low, 1280×720 | 2.08 ms | 2.35 / 2.41 ms | 0 | 338 MB |
| RTX 5070 Ti, OpenGL, High, 1920×1080 | 0.67 ms | 0.87 / 0.98 ms | 0 | 340 MB |

Raw reports and screenshots: [AMD report](benchmarks/linux-amd-integrated-low.json), [AMD screenshot](benchmarks/linux-amd-integrated-low.png), [NVIDIA report](benchmarks/linux-nvidia-high.json), [NVIDIA screenshot](benchmarks/linux-nvidia-high.png).

These are engine-reported frame intervals in a sparse, uncapped blockout. They are not GPU profiler timings, display refresh rates, or evidence for a finished game's performance. The runs use different GPUs, resolutions, and graphics APIs, so they do not isolate the cost of changing presets. Both used the same Ryzen 9 9950X3D desktop with 32 GB RAM. Process memory is an end-of-run snapshot, not a peak measurement. Neither benchmark substitutes for testing an actual two-core, 8 GB machine or a populated multiplayer ranch.

## After the benchmark

Prove a small dedicated-server session with two clients and persistence before building out ranch features. Then add modular construction, flower placement, and animal interactions in small increments. Test conflicting player actions and save/restart behavior as those features arrive.

## Family-playable expansion (September 12, 2026 UTC)

The ranch implementation adds shared modular construction, flowers, cow milking, eggs, pantry counts, private server play and atomic world saves. The original landscape measurements above remain historical evidence for the earlier blockout.

Current validation before final packaging:

| Check | Result | Evidence |
| --- | --- | --- |
| Shared authority | Passed | Conflicting placements/collections, invalid coordinates/types/IDs, unsupported roofs, malformed/oversized frames, join codes, exclusive save writer, corrupt/incomplete save rejection, cooldown persistence and restart |
| Synthetic concurrency | Passed | 20 simultaneous TCP clients polling the same world |
| Unity gameplay | Passed | Build, enter foundation without jumping, retain it across unchanged polls, plant/move flowers, collect milk/eggs, reload save, retain flowers on low graphics, validate cow scale and materials |
| Original terrain/scene checks | Passed | Three EditMode tests; terrain walk and quality-preserved wildlife PlayMode test |
| Cross-runtime rendered clients | Initial pass; final capture pending | Two Linux Unity clients matched the .NET server's 14-piece ranch and pantry; final rebuild includes explicit snapshot-presence fix |
| Linux/Windows player and dedicated server packages | Final packaging pending | Native Windows execution remains untested |
| Updated low/high rendered benchmark | Pending | New grass, broadleaf trees, wildlife and ranch runtime change the workload |

The gameplay test exposed and fixed an unchanged-poll bug: Unity's inline JSON null handling could turn an absent snapshot into an empty ranch. Both the worker revision and main-thread world replacement now require `hasState`. The foundation-entry check caught the visible consequence and verifies persistence across repeated polls.
