# Game design

## Intended experience

Walk around with the family, build something, care for animals, or head out on a spontaneous adventure to find a gorgeous view. Straightforward, enjoyable processes should make ordinary play relaxing.

Stardew Valley, Minecraft, and cozy simulation games are points of reference for the activities and mood. The camera is first person and the environment is 3D, with natural-looking scenery.

## Agreed direction

### Audience and pace

Cozy adults and kids playing together. Peaceful ranch play remains the default. The family can opt into non-graphic hunting; players and farm animals cannot be harmed. There is no definite ending; tending the homestead and exploring provide continuing reasons to return.

### Homestead

Start with pioneer-era tools and materials. Use a simple modular building system for cabins, ranch structures, and fences. Players can plant decorative flowers, gather milk from cows, and collect chicken eggs through short manual interactions or minigames.

Axes and pickaxes gather wood and stone from replenishing timber and rock sites. Shared supplies pay for construction; moving is free and dismantling refunds materials. Existing ranches receive starter supplies.

Begin with a small set of enjoyable activities. Breeding, elaborate supply chains, and settlement management are outside the first playable scope.

Players must be able to leave for an expedition without worrying about neglected animals. Pausing relevant production or using forgiving capped accumulation are acceptable approaches; the exact implementation is undecided.

### Explorer equipment and horses

Choose a pioneer or cowboy outfit with a preview and a choice saved on the local PC. Other players see changes without reconnecting. A compass shows cardinal directions and the home bearing.

A personal saddle horse supports walking and cantering, with mounted appearances visible in multiplayer. It returns to its ranch starting position when the scene loads.

A muzzleloader can fire harmlessly with hunting off. Reloading takes eight seconds and has visible hand, hammer and ramrod motion. Shared hunting is opt-in, with designated meadow deer, venison rewards and timed recovery. There is no player or farm-animal damage.

### Exploration

The eventual world should have semi-recognizable continents in roughly Earth-like positions and relative sizes, with varied geography and biomes. Begin with one small region rather than implementing continents immediately.

Wilderness can be simple initially. Views and wildlife are core attractions. Route choices, rivers, landmarks, and navigation are possible sources of interaction; detailed supply management is not required.

Historical expedition routes, such as Lewis and Clark's, can be loosely mapped into the world with optional, concise discoveries. Historical text must be sourced and must acknowledge existing inhabitants and the people who enabled the expeditions. Historical content comes after the exploration prototype.

Exploration may lead to new land and building locations. Moving home, creating an outpost, and owning multiple homesteads remain possible, but their rules are not settled.

### Multiplayer

Private servers, with an eventual target of roughly 5–20 players. The server must be available without the owner connected. Start validation with a small family group; 20-player capacity is an aspiration until measured.

Ownership, editing permissions, and individual versus shared resources remain open. A shared homestead is the working prototype assumption.

### Hardware and graphics

Windows and Linux PCs. The provisional weakest machine has a two-core CPU, 8 GB system RAM, and integrated graphics. Exact CPU and GPU models are unknown, so this is not a supported minimum specification.

Visual quality is the priority for the current pass. Push lighting, materials, vegetation, water and hair on High, then optimize from measured results. Preserve Low as a fallback without using the provisional minimum PC to cap the High preset. Keep agent validation workloads resource-bounded after the desktop lockup.

## Working proposals to test

- Nearby scenic outings take approximately 10–20 minutes round trip, without a timer or obligation to finish.
- Exploration can unlock flower varieties to plant at home; this reward has not been finalized.
- Milk and eggs accumulate to a small cap without animal deterioration while players are away.
- No hunger, combat, animal death, crop failure, or absence penalties in the initial experience.
- Additional properties reuse the building system before introducing any management mechanics.
- Low settings provisionally target 720p at 30 FPS. The family has not yet confirmed this target on a named machine.

## First family-playable scope

- One meadow, forest, river, and mountain overlook region.
- Comfortable walking with adjustable sensitivity and field of view; optional head bob disabled by default.
- A few wildlife species with simple ambient behavior.
- Snapping foundations, walls, doors, roofs, and fences, with forgiving removal and repositioning.
- A small selection of decorative flowers.
- Cows, chickens, milking, and egg collection.
- Shared interactions and construction saved by a private dedicated server.

## Deferred

Continental world generation, elaborate settlement networks, historical route content, deep ranch simulation, modern machinery, combat, public servers, and commercial launch systems.

## Next decisions

Resolve the minimum test hardware and graphics feasibility through the landscape benchmark. Then settle building permissions, inventory rules, and production behavior before implementing multiplayer ranch interactions. Do not treat proposals in this document as already validated mechanics.
