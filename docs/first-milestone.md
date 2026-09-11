# First milestone: the walk

## Question

Can one small forest-to-overlook walk feel beautiful and comfortable on both a weak integrated GPU and a stronger dedicated GPU?

This is the first technical milestone, not the full family-playable game. It is complete only after actual Windows and Linux builds are exercised and results are recorded.

## Engine candidate

Start evaluation with Unity 6 and Universal Render Pipeline, using one rendering pipeline across quality levels. Select and pin a supported editor release and compatible package versions when creating the project.

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
| Editor and packages pinned | Pending | No Unity project created |
| Windows standalone build | Pending | No build produced |
| Linux standalone build | Pending | No build produced |
| Low-preset performance | Pending | Requires hardware and scene |
| High-preset appearance and performance | Pending | Requires hardware and scene |
| Camera comfort and enjoyable view | Pending | Requires family playtest |

Do not report supported minimum specifications or a validated engine choice from editor screenshots or documentation alone.

## After the benchmark

Prove a small dedicated-server session with two clients and persistence before building out ranch features. Then add modular construction, flower placement, and animal interactions in small increments. Test conflicting player actions and save/restart behavior as those features arrive.
