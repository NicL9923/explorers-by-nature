# First milestone: the walk

Latest implementation and verification: [Fern Hollow](#fern-hollow-september-13-2026-utc). Earlier sections preserve the original milestone and historical measurements.

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

Validation for runtime implementation `9a3043d`, including the tracked standalone project definitions in `b09351a`:

| Check | Result | Evidence |
| --- | --- | --- |
| Shared authority | Passed | Conflicting placements/collections, invalid coordinates/types/IDs, unsupported roofs, malformed/oversized frames, join codes, exclusive save writer, corrupt/incomplete save rejection, cooldown persistence and restart |
| Synthetic concurrency | Passed | 20 simultaneous TCP clients polling the same world |
| Unity gameplay | Passed | Build, enter foundation without jumping, retain it across unchanged polls, plant/move flowers, collect milk/eggs, reload save, retain flowers on low graphics, validate cow scale and materials |
| Original terrain/scene checks | Passed | Three EditMode tests; terrain walk and quality-preserved wildlife PlayMode test |
| Cross-runtime rendered clients | Passed | Two rendered Linux Unity clients and the standalone .NET server agree at revision 16: 14 pieces, one milk, three eggs; includes snapshot-presence fix |
| Linux/Windows player and dedicated server packages | Built | Both desktop players and both self-contained dedicated servers built successfully; native Windows execution remains untested |
| Updated low/high rendered benchmark | Passed on available GPUs | Recorded below; actual minimum PC and a heavily populated ranch remain untested |

The gameplay test exposed and fixed an unchanged-poll bug: Unity's inline JSON null handling could turn an absent snapshot into an empty ranch. Both the worker revision and main-thread world replacement now require `hasState`. The foundation-entry check caught the visible consequence and verifies persistence across repeated polls.


Both updated reports identify Linux build `b09351a/sha256:74938f6a204b46f270f7129c1a56fe6c47ed2c116f0d1882465289e8e9aab541`. Later edits restore the authoring scene after builds, align HUD scaling between panels, and package the release. The benchmark reports identify the measured player before that HUD-only adjustment; gameplay and world geometry are unchanged.

| Run | Mean frame time | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| AMD integrated, Vulkan, Low, 1280×720 | 2.23 ms | 2.64 / 2.73 ms | 0 | 377 MB |
| RTX 5070 Ti, OpenGL, High, 1920×1080 | 0.72 ms | 0.97 / 1.13 ms | 0 | 408 MB |

Evidence: [low report](benchmarks/family-amd-integrated-low.json), [high report](benchmarks/family-nvidia-high.json), [ranch screenshot](benchmarks/family-ranch.png), [Unity gameplay results](validation/family-playmode.xml), [server results from clean committed source](validation/family-server-tests.txt), [matching multiplayer/save snapshot](validation/family-shared-ranch.json).

These remain accelerated, uncapped engine frame intervals on the same 32-thread, 32 GB desktop, with different graphics APIs and resolutions. They are not isolated GPU timings, a 20-player rendered-world load test, or proof of the intended two-core/8 GB floor. The automated ranch check exercises authority and rendering; the minigame's feel, family usability and native Windows behavior still require human playtesting.

Final Linux and Windows players were rebuilt at `2b36535` after the HUD alignment and build-script restoration changes. The final two-rendered-client test passed again against the standalone server at revision 16. The Windows build also verified that the authoring scene is restored byte-for-byte after stamping the player. No native Windows execution is claimed.

## Original art expansion (September 12, 2026 UTC)

Three Astra modelers created original Blender ranch, woodland and wildlife assets. The runtime preserves imported material slots, baked coat/wood maps, clipped foliage and distance meshes. Independent construction and interaction colliders retain the existing gameplay. The [art gallery](art-pass.md) distinguishes studio renders from player screenshots.

Validation: four EditMode checks cover terrain/scene assets and actual model imports, including triangle budgets, scale, dark eye pigment and flower placement origin. Two PlayMode checks cover ranch construction, collection, reload, movement and quality-preserved wildlife. The rendered two-client/dedicated-server check passed again at revision 16 with 14 pieces, one milk and three eggs. Linux and Windows players were built; Windows execution remains untested.

The art benchmark player identifies source `51cda7e/sha256:193b1255402c9eb5d5ab62020d1b6aecf360d42aa6b4e3a41dc891dd32efcbe5`. The subsequent flower-only export-origin correction does not alter the measured landscape route. Final release players include that correction; their runtime source stamps identify the release implementation commit. Raw reports retain the precise measured source identity.

| Run | Mean frame time | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| AMD integrated, Vulkan, Low, 1280×720 | 6.45 ms | 8.20 / 8.62 ms | 0 | 416 MB |
| RTX 5070 Ti, OpenGL, High, 1920×1080 | 1.07 ms | 1.39 / 1.52 ms | 0 | 533 MB |

Evidence: [AMD report](benchmarks/art-amd-integrated-low.json), [NVIDIA report](benchmarks/art-nvidia-high.json), [EditMode](validation/art-editmode.xml), [PlayMode](validation/art-playmode.xml), [matching multiplayer state](validation/art-shared-ranch.json).

These uncapped engine frame intervals cover the fixed landscape camera route on the available Ryzen 9/32 GB desktop. They are not GPU-only timings or proof of the two-core/8 GB minimum, a full ranch, or 20 rendered players. The new assets cost more than the old primitives; low quality retains the same animals and interactable objects. Skeletal animal animation, final landscape composition and native Windows playtesting remain outstanding.

Final Linux and Windows players were rebuilt from implementation commit `22c9115`. The final art-tour run passed against the standalone server and visually confirmed planted flowers at the requested position. Player screenshots above come from that final build. The later evidence/packaging commit changes no runtime assets or code.

## Landscape expansion (September 12, 2026 UTC)

Original procedural meadow/snow textures, denser fine grass, clustered forest placement, daisy drifts, more river stones, a cloud sky and a distant mountain mesh were added. The walkable height function and server ground data are unchanged. See the [player gallery](landscape-pass.md).

Four EditMode tests passed, including shader checks for the sky. Two PlayMode tests passed after the terrain/forest/sky integration; subsequent changes add snow texture blending, reposition decorative flowers and extend screenshot cameras. The final rendered two-client check passed at revision 16 with 14 pieces, one milk and three eggs. Linux built and ran; Windows built but native execution remains untested.

Measured Linux source: `20559ec/sha256:ec440d08fd69a7429646e30c768937547ef017227060f3976e148df4977a9326`. The final runtime landscape and extended screenshot tour are present in this player; later edits affect tests, evidence and packaging.

| Run | Mean | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| Vulkan / Low / 1280×720 | 6.23 ms | 8.45 / 9.01 ms | 0 | 477 MB |
| OpenGLCore / High / 1920×1080 | 1.14 ms | 1.53 / 1.68 ms | 0 | 589 MB |

Evidence: [AMD integrated](benchmarks/landscape-amd-integrated-low.json), [RTX 5070 Ti](benchmarks/landscape-nvidia-high.json), [EditMode](validation/landscape-editmode.xml), [PlayMode](validation/landscape-playmode.xml), [matching shared state](validation/landscape-shared-ranch.json). These are uncapped engine intervals on the Ryzen 9/32 GB desktop. They do not establish the intended two-core/8 GB floor or populated multiplayer performance.

## River and wildlife expansion (September 12, 2026 UTC)

Two Astra modelers authored mallards, beavers and red foxes. Five ducks, two beavers and two foxes now populate small habitats. The river follows the actual terrain waterline, with finer ripples, reeds and bank stones. Small embedded outcrops add mountain detail. See the [player and Blender gallery](river-wildlife-pass.md).

Six EditMode tests passed, covering the new model imports, shoreline intersection and habitat routes. Two PlayMode tests passed after final slope alignment and outcrop adjustments, including retention of all nine new animals across both quality levels and the existing ranch gameplay checks. Two rendered clients again matched the dedicated server and disk at revision 16, 14 pieces, one milk and three eggs. The extended tour verified all three new species in the player. Linux and Windows builds succeeded; native Windows execution remains untested.

Final measured Linux player: `d10261f/sha256:57868296788e35b6c591f62fb5a9defc7252b0066999858b192a785a008da4d2`. The final runtime code and models are included; later documentation/evidence edits do not affect the measured scene.

| Run | Mean | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| Vulkan / Low / 1280×720 | 6.24 ms | 8.47 / 9.12 ms | 0 | 483 MB |
| OpenGLCore / High / 1920×1080 | 1.15 ms | 1.56 / 1.72 ms | 0 | 592 MB |

Evidence: [AMD integrated](benchmarks/wildlife-amd-integrated-low.json), [RTX 5070 Ti](benchmarks/wildlife-nvidia-high.json), [EditMode](validation/wildlife-editmode.xml), [PlayMode](validation/wildlife-playmode.xml), [shared ranch snapshot](validation/wildlife-shared-ranch.json). Measurements are uncapped engine intervals on the Ryzen 9/32 GB desktop, not isolated GPU timings or proof of the two-core/8 GB minimum. Wildlife motion is local ambient scenery, not synchronized authoritative game state; articulated animal animation remains outstanding.


## Complete picnic and nature polish (September 12, 2026 UTC)

All eight requested areas are implemented: landscape composition, articulated animals, river/shore detail, placement and collection feedback, spatial nature audio, a complete picnic/reward loop, usable ranch furnishings, and gentle weather/time of day. See the [player gallery](picnic-polish.md).

The final Linux player identifies `ecf0d17/sha256:186608e7824ae166f0f4b529f9250469df12643fda0fb03d1970d101fd7c1339`. That digest includes the then-uncommitted implementation. The [per-file source manifest](validation/polish-source-files.json) records the final Assets, Packages, ProjectSettings, server and test files, checked unchanged after both final builds. Subsequent documentation, evidence and packaging changes do not alter those inputs.

| Check | Final result and evidence |
| --- | --- |
| Blender rigs | Seven animals, normalized weights, 10–18 bones, both LODs; [verification](../ArtSource/Animation/verification.json). Posed source renders and actual player poses inspected. |
| EditMode | [15 passed](validation/polish-editmode.xml): scene/terrain, shoreline/habitats, original art and skinned import, stance/swing checks. |
| PlayMode | [4 passed](validation/polish-playmode.xml), rerun after picnic overlay and bench camera continuity fixes; also covers ranch gameplay, supported furnishing/ghost height, shared light cap, rain/readable lighting, spatial audio and quality preservation. |
| Shared authority | [Passed](validation/polish-server-tests.txt): expedition order/proximity, old saves lacking the new field, reward persistence, all furnishing kinds, existing authority/restart/framing checks and 20 synthetic concurrent clients. |
| Rendered multiplayer | [Passed](validation/polish-multiplayer.txt): two Linux players and dedicated server save match at revision 25, 20 pieces, one milk, three eggs, expedition stage 3; [shared snapshot](validation/polish-shared-ranch.json). |
| Desktop builds | Final Linux and Windows x64 players built through Unity CLI with no shader/compiler errors. Linux exercised; native Windows execution remains untested. |
| Dedicated servers | [Both self-contained targets published](validation/polish-server-publish.txt) after regenerating the changed northern terrain table. |

Earlier integration checks caught unreadable meshes preventing pebble batching and a scale assertion accidentally measuring padded animation culling bounds. Both were corrected before the final tests. A read-only integration review also caught the picnic settings overlay and a bench stand-up camera snap; the final PlayMode run covers both fixes.

Performance measurements below use the final player on the fixed 60-second route, after warm-up, with no concurrent editor, build or Blender render. They are uncapped engine frame intervals on a Ryzen 9/32 GB desktop, not isolated GPU timings or proof of the two-core/8 GB floor. The rain run fixes full drizzle at evening. The route does not simulate a heavily furnished 20-player ranch. Audio emission is checked programmatically; sound balance, minigame feel and family comfort still need human playtesting.

| Run | Mean | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| [AMD integrated / Vulkan / Low / 1280×720 / clear](benchmarks/polish-amd-integrated-low.json) | 8.72 ms | 14.01 / 15.22 ms | 0 | 456 MB |
| [RTX 5070 Ti / OpenGL / High / 1920×1080 / clear](benchmarks/polish-nvidia-high.json) | 1.44 ms | 2.12 / 2.28 ms | 1 | 597 MB |
| [AMD integrated / Vulkan / Low / 1280×720 / rainy evening](benchmarks/polish-amd-integrated-rain.json) | 8.78 ms | 14.13 / 15.35 ms | 0 | 447 MB |

The high run contains one frame over 50 ms; the raw report preserves that hitch. No repeat run was used to replace it. These measurements support continued testing on the available GPUs, not a minimum-spec certification.


## Lush woodland and lighting (September 13, 2026 UTC)

Three Astra agents worked on original animal models, understory and mountain/water detail. Root integrated foliage transmission, sky and ambient lighting, high-quality grading and shadow-sampled woodland mist. The [player gallery](lush-lighting.md) shows the result.

Final Linux and Windows runtime stamp: `24a2538/sha256:2669aa7522191705bf3611cd5cf15622933d7eca12d776d9fb1664220dcef5ac`. This includes the then-uncommitted implementation. The [298-file source manifest](validation/lush-source-files.json) records the final runtime, assets, packages, settings, server and test files. It was verified unchanged after both builds and final rendered checks. Later docs and packaging edits do not change those inputs.

| Check | Result and evidence |
| --- | --- |
| Animal authoring | Seven original 2048px color and 1024px normal atlases; revised cow/fox/deer anatomy; preserved skinned LODs. [Mesh/atlas budgets](../ArtSource/Animation/detail-stats.json), [normalized weights](../ArtSource/Animation/verification.json). Blender previews and actual player models inspected. |
| EditMode | [15 passed](validation/lush-editmode.xml), including all seven atlas/normal-material bindings, skinning and mesh budgets, terrain/trail/shore/habitat checks. |
| PlayMode | [4 passed](validation/lush-playmode.xml), including valid double-sided bird normals, immediate low/high effect switching, base understory retention, weather and the existing ranch, save, bench and movement coverage. |
| Rendered multiplayer | [Passed](validation/lush-multiplayer.txt): two rendered Linux players and the dedicated save agree at revision 25, 20 pieces, one milk, three eggs and completed picnic reward. [Snapshot](validation/lush-shared-ranch.json). |
| Desktop builds | Linux and Windows x64 built through Unity CLI with the same source stamp. Linux ran; native Windows execution is still untested. |
| Dedicated servers | [Both targets published](validation/lush-server-publish.txt) with the final northern terrain export. Shared authority source is unchanged; prior authority and 20-client synthetic results remain applicable. |

Visual iteration caught undersampled distant mountains and shadows that were too dark. A shader review traced enormous evening glow artifacts to bird wings whose reversed triangles shared vertices and canceled their normals. Separate back-face vertices fixed the invalid lighting; a regression assertion checks unit normals. The final evening capture retains bloom and no longer shows those artifacts. No diagnostic no-bloom capture is presented as the finished game.

Low quality keeps the new animal art and base plant layer. High adds plants, post-processing, 4096px directional shadows and three depth-clipped local scattering volumes. Understory geometry uses at most 240 spatial batches. Animal near meshes stay under 30,000 triangles each; distant meshes use 1,600–4,400. The distant range has about 92,000 triangles. Those are design budgets; measured frame intervals follow below.

| Run | Mean | p95 / p99 | Frames over 50 ms | Unity allocated / final working set |
| --- | --- | --- | --- | --- |
| [AMD integrated / Vulkan / Low / 1280×720 / clear](benchmarks/lush-amd-integrated-low.json) | 6.90 ms | 9.35 / 9.89 ms | 0 | 436 / 690 MB |
| [RTX 5070 Ti / OpenGL / High / 1920×1080 / clear](benchmarks/lush-nvidia-high.json) | 2.01 ms | 3.13 / 3.37 ms | 0 | 456 / 994 MB |
| [AMD integrated / Vulkan / Low / 1280×720 / rainy evening](benchmarks/lush-amd-integrated-rain.json) | 7.04 ms | 9.62 / 10.11 ms | 0 | 436 / 676 MB |

Measurements use the final player on the fixed 60-second route after warm-up, without simultaneous Unity editor, builds or Blender renders. They are uncapped engine intervals on the Ryzen 9/32 GB desktop, not GPU-only timings or proof of the two-core/8 GB minimum. Memory readings are final snapshots, not peaks. New geometry and maps raise memory use compared with v0.6.0. Native Windows, the actual minimum PC and a populated 20-player rendered ranch remain unverified.


## Fern Hollow, September 13, 2026 UTC

A focused woodland scene now occupies about 100 metres of the existing trail. F8 visits it; F9 toggles the HUD. The grove uses recorded CC0 Poly Haven trees and scan props, with 108 adult trees and saplings, concentrated fern patches, mossy rocks and stumps. Terrain heights and shared ranch authority remain unchanged. The older roaming deer moved to the western meadow. A separate doe study uses quiet idle animation; its face and coat remain procedural.

[Scene notes and asset sources](fern-hollow.md). The photographs on source websites were references only. The gallery contains unedited captures from the Linux player.

| Check | Result and evidence |
| --- | --- |
| EditMode | [17 passed](validation/grove-editmode.xml), including dry terrain texture packing. |
| PlayMode | [6 passed](validation/grove-playmode.xml), including imported size/LOD/material checks, terrain heights and painting, grounded movement into the grove, quality switching, and existing wildlife/animation checks. |
| Desktop builds | [Linux and Windows succeeded](validation/grove-builds.json). Windows was cross-built, not executed on Windows. |
| Rendered multiplayer | [Passed](validation/grove-multiplayer.txt): two Linux clients and the dedicated save agree at revision 25, with 20 pieces and expedition stage 3. [Shared snapshot](validation/grove-shared-ranch.json). |
| Runtime source | [477 runtime, asset, configuration and test file hashes](validation/grove-source-files.json). Both desktop builds, captures, performance runs and rendered multiplayer checks carry the same embedded source stamp. |
| Integrated GPU | [Fern Hollow, Low, Vulkan, 1280×720](validation/grove-low.json): mean 20.61 ms, p95 28.30 ms, p99 28.84 ms, one frame over 50 ms; final process working set 698 MB. |
| Dedicated GPU | [Fern Hollow, High, OpenGL, 1920×1080](validation/grove-high.json): mean 5.34 ms, p95 7.55 ms, p99 7.73 ms, no frames over 50 ms; final process working set 944 MB. |

Both measurements used the same Linux player stamp, `1e60ded/sha256:6cf8e712059178760d7fc2810bbbe60c28b97159b7dc8746005ad84b278c2f76`, on the Ryzen 9 9950X3D with 32 GB RAM. The integrated device was RADV RAPHAEL_MENDOCINO; the dedicated device was an RTX 5070 Ti. Each run used five seconds of warm-up followed by sixty seconds along the grove. No editor build, Blender render or second benchmark ran concurrently. These are uncapped engine frame intervals, not GPU timer measurements. Final working set is not peak memory. The actual two-core, 8 GB minimum machine remains untested.

The visual inspection caught incorrect FBX units from incomplete importer metadata, ground images imported as cubemaps, invalid white pixels in the pine atlas, and terrain alpha being interpreted as mirror smoothness. The final import settings and texture packing address those defects. The tree trunk bake also preserves the source material’s blend between scanned roots and upper bark, removing stretched atlas artifacts. The grove still needs better close-range animal art and more varied large-scale ground detail. Passing the technical checks does not establish that the original visual ambition has been met.

An intermittent multiplayer smoke failure exposed timing races in the automation: it could move before a queued action captured its pose. The harness now waits for each accepted revision before moving on and preserves failure snapshots. The original failure’s exact trigger was not proven. Shared authority and connection code were not changed; the corrected harness passed against the final player.


## Woodland and alpine refinement, September 13, 2026 UTC

The grove now replaces up to 56 nearby prototype canopies at their existing planting positions while retaining their trunk collisions. Trail shoulders gain small rock groups, daylight shadows are softer, and the separate doe has revised ears, neck, muzzle and coat. A connected distant range and granite surface treatment improve the northern view without changing the shared heightfield. [Unedited player gallery](woodland-refinement.md).

The alpine geometry uses two renderers: 92,160 triangles in the distant range and 61,356 triangles on the northern terrain surface. Its granite photograph is the existing CC0 asset recorded in [asset provenance](assets.json). The doe stays at 30,000/6,000 triangles; a fresh Blender FBX import verified its rig, UVs and skin weights in [export evidence](../ArtSource/ReferenceDeer/export-check.json).

| Check | Result and evidence |
| --- | --- |
| EditMode | [18 passed](validation/woodland-editmode.xml), rerun after the final granite sampling change, including the serialized mountain material, shader compilation and texture linkage. |
| PlayMode | [6 passed](validation/woodland-playmode.xml), including retained transition-tree collisions, all grove LODs and materials, quality changes, terrain preservation and existing gameplay checks. This result is reused from before the final shader-only texture-blending change; its tested C# and model inputs did not change. |
| Desktop builds | [Linux and Windows succeeded](validation/woodland-builds.json) with the same embedded stamp. Linux ran on both GPUs; native Windows execution remains untested. |
| Rendered multiplayer | [Passed](validation/woodland-multiplayer.txt): two Linux clients and the dedicated save agree at revision 25, with 20 pieces, one milk and three eggs. [Snapshot](validation/woodland-shared-ranch.json). Authority, terrain export and server binaries are unchanged. |
| Runtime source | [486 file hashes](validation/woodland-source-files.json) record the runtime, assets, configuration and tests. Final builds, captures and performance runs share the stamp below. |

Runtime stamp: `fa134db/sha256:deea3c7eebcce85776b610d1f3c72da8df907f9f82e6a2614394f907963b92db`.

| Run | Mean | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| [AMD integrated / Vulkan / Low / 1280×720](validation/woodland-low.json) | 21.87 ms | 29.13 / 29.63 ms | 2 | 703 MB |
| [RTX 5070 Ti / OpenGL / High / 1920×1080](validation/woodland-high.json) | 6.20 ms | 8.06 / 8.23 ms | 0 | 1011 MB |

Both runs used the Ryzen 9 9950X3D and 32 GB RAM on the same 72-metre Fern Hollow route, with five seconds of warm-up and sixty seconds of measurements. No editor, build, Blender render or second benchmark ran concurrently. These are uncapped engine frame intervals, not GPU-only timings. Memory is a final snapshot, not a peak. The actual two-core, 8 GB minimum PC and a populated 20-player rendered ranch remain untested.

Player inspection rejected overly pale periodic mountain stripes, then excessive procedural fracture detail and obvious granite repetition. The final material uses darker photographed rock, restrained bump and blended texture scales. Both High and Low overlook captures were inspected. The nearby mountain silhouettes remain too rounded, the meadow still exposes earlier vegetation art, and the doe's face and fur need further work.


## Wind, fur and river motion, September 13, 2026 UTC

Trees, ferns and grass now share a moving wind field. Nearby deer, cows, foxes and rabbits have coat clumps pinned to their animated skin, with damped spring tips responding to wind and body movement. The cow's tail switch has longer hair. F7 visits the river; G tosses a pebble. [Controls, motion recordings and simulation limits](wind-water.md).

Both desktop builds carry runtime stamp `80141ea/sha256:61f172047ef8cc8db8fca03b60f02ad611b91f3ddd26bda8178767bb8007df32`. The evidence recorders require matching build, capture and benchmark identities before collecting their results.

| Check | Result and evidence |
| --- | --- |
| EditMode | [37 passed](validation/wind-water-editmode.xml), including wind continuity, vegetation shader passes, imported fur roots, spring stability, water propagation and advection, buoyancy, and river-grid continuity. |
| PlayMode | [8 passed](validation/wind-water-playmode.xml), including High/Low fur budgets, matching four-bone coat skinning, river interaction, grove preservation and existing ranch checks. |
| Desktop builds | [Linux and Windows succeeded](validation/wind-water-builds.json) with the same runtime stamp. Linux ran on both GPUs; native Windows execution remains untested. |
| Motion captures | [High and Low recordings verified](validation/wind-water-motion-builds.json): correct actual quality, 1,800/600 visible fur clumps, measurable spring displacement, a water impact, and 480 encoded frames at 24 FPS. Posters match the unedited source frames. |
| Rendered multiplayer | [Passed](validation/wind-water-multiplayer.txt): two rendered Linux clients and the dedicated save agree at revision 25, with 20 pieces, one milk and three eggs. Shared authority and server binaries are unchanged. |
| Runtime source | [520 file hashes](validation/wind-water-source-files.json) record the runtime, assets, settings and tests. No runtime inputs changed after final validation. |

The river simulates surface waves in a local 48 × 72 metre grid. Depth controls wave propagation; the current carries disturbances downstream. Pebbles and shallow-water footsteps disturb the surface, and floating branches respond to buoyancy and current. Moving the grid now preserves overlapping waves and their momentum. A boundary fix prevents the clamped grid from repeatedly recentering at the valley limits. Newly exposed water starts at rest. Distant water uses animated shading; this implementation does not simulate flooding, erosion or a full fluid volume.

Fur uses crossing ribbon clumps with simulated tips, distance limits and fewer clumps on Low. It does not simulate strand collisions. Water disturbances, driftwood and hair remain local visual effects rather than shared persistent objects.

The first player capture exposed a quality configuration defect: Low excluded the Standalone target, so Unity removed that quality level and remapped the remaining index. Requested graphics presets could therefore disagree with global quality settings in earlier builds. Both presets now remain available in desktop players. Earlier performance results should not be treated as clean comparisons of the intended Low and High settings.

| Route and device | Mean | p95 / p99 | Frames over 50 ms | Final working set |
| --- | --- | --- | --- | --- |
| [Fern Hollow, AMD integrated, Vulkan, Low, 1280×720](validation/wind-water-low.json) | 24.182 ms | 32.095 / 32.825 ms | 3 | 711 MB |
| [Fern Hollow, RTX 5070 Ti, OpenGL, High, 1920×1080](validation/wind-water-high.json) | 7.090 ms | 8.999 / 9.301 ms | 0 | 989 MB |
| [Riverbend, AMD integrated, Vulkan, Low, 1280×720](validation/wind-water-low-river.json) | 6.712 ms | 6.790 / 6.812 ms | 0 | 705 MB |
| [Riverbend, RTX 5070 Ti, OpenGL, High, 1920×1080](validation/wind-water-high-river.json) | 1.469 ms | 1.875 / 2.365 ms | 0 | 840 MB |

Each Riverbend run recorded 16 water impacts. All four runs used the same player on the Ryzen 9 9950X3D with 32 GB RAM. These are uncapped engine frame intervals, not GPU-only timings. Memory readings are final snapshots, not peaks. The fixed-rate motion recordings demonstrate movement and must not be used as performance measurements. The actual two-core, 8 GB minimum PC, native Windows execution and a populated 20-player rendered ranch remain unverified.


## Frontier tools and explorer choice, September 13

Four original western explorer models now support local selection and live multiplayer changes. A personal rideable horse, compass, axe, pickaxe and animated muzzleloader extend the ranch loop. Wood and stone are shared construction supplies with persistent regrowth timers; hunting is off by default and limited to designated deer. See [feature details and recordings](frontier.md) for controls, original assets and limitations.

Both desktop players carry runtime stamp `5deccf8/sha256:8e09a1835f84012620d14cf96ff97f8056893ae19e425e96afb77dc1d5ecfba6`.

| Check | Result and evidence |
| --- | --- |
| Editor checks | [37 EditMode tests passed](validation/frontier-editmode.xml). |
| Gameplay checks | [25 PlayMode tests passed](validation/frontier-playmode.xml), including import scale, wardrobe cleanup, tool rewards, reload motion, horse movement/dismount, seated interaction restrictions and exact shot-pose preservation across network dispatch. |
| Authority | [130 server assertions passed](validation/frontier-server-tests.txt), including migration, construction costs/refunds, regrowth, hunting bounds and pose/model validation. Dedicated servers republished for Linux and Windows. |
| Desktop builds and captures | [Matching successful builds and evidence](validation/frontier-builds.json). Low/High Linux gameplay captures gain eight wood, six stone and two venison, ride more than eight metres, and finish a reload. Four outfit previews checked at 1280×720 on both presets and 960×600 on Low. |
| Multiplayer | [Two rendered clients passed](validation/frontier-multiplayer.txt): distinct outfits, a live model change, and identical shared state/dedicated save at revision 25 with 20 pieces, one milk and three eggs. |
| Source identity | [606 source file hashes](validation/frontier-source-files.json). The final smoke-only change separates test players before checking visible avatars, since overlapping avatars are deliberately hidden. Existing gameplay/editor results remain applicable. |

After the desktop lockup, editor/build/capture work uses one project-wide lock, a three-core CPU quota, 8 GiB memory pressure threshold, 10 GiB hard limit, no additional swap, lower scheduling priority and a 25-minute timeout. Rendered checks use the AMD integrated GPU and a 30 FPS cap. No recorded OOM kill or explicit GPU fault established the original cause; these limits reduce validation load rather than prove the freeze fixed.

Reload recordings contain 240 frames at 24 simulation frames per second and are animation evidence, not performance benchmarks. Models use articulated rigid parts and have no authored distance LODs yet. The personal horse position resets on scene load; mounted visitors show their horse remotely. Native Windows execution, the proposed two-core/8 GB minimum PC and a populated 20-player rendered ranch remain unverified. Earlier NVIDIA evidence belongs to earlier releases; this release's rendered checks used AMD integrated graphics.
