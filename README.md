# Explorers by Nature

A peaceful, first-person family homesteading game where a walk can turn into an adventure.

Build a pioneer homestead together, plant flowers, gather milk and eggs, watch wildlife, and explore until you find a mountain view worth stopping for.

## Project status

Pinewatch now includes modular homesteads, decorative flowers, cow milking, egg gathering, a shared pantry, saved worlds, and private dedicated-server multiplayer. Explore the meadow, aspen grove, river and mountain overlook with deer, rabbits and birds. Original Blender models now furnish the ranch, trees and wildlife, with simpler meshes for distant views. See the [art gallery](docs/art-pass.md).

Read the [morning playtest guide](docs/morning-playtest.md) for controls, multiplayer setup and saves.

The project targets Unity **6000.3.24f1** with Universal Render Pipeline **17.3.0**. Minimum hardware remains unverified; see the [validation ledger](docs/first-milestone.md).

[Download the Linux or Windows prototype](https://github.com/NicL9923/explorers-by-nature/releases/tag/v0.4.0-landscape.1). Linux was exercised on NVIDIA and AMD integrated graphics; the Windows build has not been run on Windows yet.

[View the landscape update](docs/landscape-pass.md).

## Run and build

Install the pinned editor through Unity Hub, sign in, and activate an appropriate Unity license. Install the [official Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli) for the project scripts. On Linux, from the repository root:

```bash
.agents/tools/unity.sh open
```

Press Play in the checked-in `Assets/Scenes/Pinewatch.unity` scene. The scene is also generated automatically on the first interactive editor open. The terrain and scenery are created when play starts, so edit mode initially shows the camera and scene configuration.

```bash
.agents/tools/unity.sh test
.agents/tools/unity.sh playtest
.agents/tools/unity.sh linux
.agents/tools/unity.sh windows
```

Logs go to `Logs/`; standalone builds go to `Builds/Linux/` and `Builds/Windows/`. Both directories are ignored by Git. Override `UNITY_EDITOR` if the executable is installed elsewhere. On Windows, open the folder through Hub and use **Explorers > Create prototype scene**, then Unity's Build Profiles window.

See [development and validation](docs/development.md) for controls, benchmark commands, and current limits.

## Prototype source

- A seeded valley with an arrival meadow, a forest trail, a winding river, and Pinewatch Overlook.
- First-person walking, adjustable mouse sensitivity and field of view, no head bob, and a return-to-meadow action.
- Pine trees with distance-based detail, wind-driven grass, original roaming deer models, and original Blender-made river stones.
- Low/high graphics presets that retain wildlife and terrain across settings.
- A repeatable camera benchmark with frame-time statistics, hardware information, memory measurements, and a screenshot.

- Snapped foundations, walls, open doorways, roofs and fences, with move/remove tools and flower planting.
- Original Blender cow and hen models; gentle milking activity, egg gathering and persistent shared products.
- Solo and dedicated multiplayer share authoritative rules and atomic saves; reconnects refresh the ranch.
- Broadleaf trees, rabbits, flying birds, original ambient audio and a discovery journal.

Trees and wildlife are original prototype assets, not finished realistic art. Continents, historical routes, deeper economy and settlement management remain deferred.

## Start here

- [Game design](docs/game-design.md): agreed direction, scope, and unresolved decisions.
- [First milestone](docs/first-milestone.md): benchmark scene, validation, and what follows.
- [Development rules](AGENTS.md): instructions for contributors and coding agents.

## Core direction

- Cozy play for adults and children together, with no combat or survival pressure initially.
- First-person 3D on Windows and Linux PCs.
- Beautiful, natural-looking wilderness, wildlife, and scenic discoveries.
- Simple modular ranch construction, decorative flowers, cows, and chickens.
- Private multiplayer with a dedicated server available without the owner online.
- An eventual world with loosely recognizable Earth continents and biomes, plus optional bite-size history along expedition routes.

This starts as a family project. Commercial release is not a current requirement.
