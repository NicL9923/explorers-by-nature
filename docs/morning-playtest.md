# Lighting and nature

Choose **High** in Escape for the new clouds, richer riverbanks, reflected scenery in the water, mountain faces and denser moving hair. **F7** visits the river and **F8** enters Fern Hollow. Evening in Escape lowers the sun for warm light over the river. Low remains available as a fallback.

# Frontier playtest

**F4** opens the explorer selector. Browse four outfits, inspect the rotating model, then choose **Use this explorer**. Your choice is remembered on this PC and shown to other players.

**Q** cycles empty hands, axe, pickaxe and muzzleloader. Look at timber or stone near the ranch and **left-click** to gather. Wood and stone pay for building; moving is free and removing returns materials. Existing saves get starter stock.

With the muzzleloader, **left-click** fires, **right-click** aims and **R** performs the eight-second reload. Shots are harmless by default. Open **Tab** and enable ranch hunting to hunt designated deer in the western meadow for venison. This setting is shared. Players, horses and farm animals cannot be damaged.

Walk to the saddled horse near the ranch and press **H** to mount. **WASD** rides, **Shift** canters, and **H** dismounts on open ground. The compass at the top shows heading and the home direction.

# A morning in Pinewatch

Start `ExplorersByNature` on Linux or `ExplorersByNature.exe` on Windows. Your solo ranch opens automatically and saves each completed change. Use **High** graphics to see the new woodland mist, leaf lighting and filmic grading. **Low** keeps the new plants and detailed animals with fewer effects.

**F7** visits the river. **G** tosses a pebble near the water; watch the ripple spread and the driftwood follow the current. You can wade without drowning. In **Escape** settings, switch wind between calm, breeze and gusty. Look closely at the cow’s tail switch or the doe’s coat to see spring-driven hair.

**F8** visits Fern Hollow, the focused woodland scene north of the ranch. Walk along the trail, use **F9** to hide or restore the HUD, and **Home** to return to the meadow. Escape still opens settings in photo mode.

1. Walk west from the trail to Clover and the hens. Aim at Clover and press **E**, then press **Space** three times as the marker crosses the green band. Aim at a hen/nesting box and press **E** to gather eggs.
2. Press **B** and look at nearby ground. **1** floor, **2** wall, **3** doorway, **4** roof, **5** fence, **6** flowers. **[ / ]** browse all pieces, including benches, lanterns, troughs, flower boxes and campfires. **R** rotates; left click places. Green previews are valid; red previews explain what needs changing. Walls and roofs attach to a foundation's three-metre cell.
3. Aim at a piece and press **M**, then click a new location to move it. Right click removes a piece. Remove a foundation's walls and roof first. Placement spends shared wood and stone; dismantling refunds them.
4. Press **E** at the picnic basket beside the arrival trail to pack. Follow the trail north to Pinewatch Overlook, press **E** at the blanket, and enjoy five quiet seconds. Return to the basket and press **E** to unlock alpine flowers for the whole ranch. The aspen grove lies northwest of the ranch; Riverbend is east and north. **Tab** opens your discovery journal and multiplayer connection screen.
5. **Escape** opens camera, audio and graphics settings. Choose changing weather, clear skies or drizzle, and a gentle day cycle, daylight or evening. **F2** changes graphics quality. **Home** returns to the starting meadow. Nothing starves, dies, or punishes an absence.

Press **E** at a bench to sit and look around. **E**, **Space**, or a movement key stands up. Lanterns and campfires cast warm light in the evening; fire is harmless.

Clover is ready again after two minutes; the nesting box after 90 seconds. Production caps at one collection while away. Milk and eggs go into a shared pantry. These are simple ranch activities; there is no selling/crafting economy yet.

## Playing together

Run the matching dedicated server from the release's server archive. It contains its own .NET runtime.

Linux example, from the extracted server folder:

```sh
./Explorers.Server --listen 0.0.0.0 --port 7777 --code 'your-family-join-code' --data "$HOME/.local/share/explorers-family-ranch"
```

Windows PowerShell:

```powershell
.\Explorers.Server.exe --listen 0.0.0.0 --port 7777 --code 'your-family-join-code' --data "$env:LOCALAPPDATA\ExplorersFamilyRanch"
```

In the game, **Tab → Private server**: enter a name, the server machine's address, port, and code. Update both players and server to this release. Existing saves keep their buildings and pantry; the new picnic starts unpacked. All players share construction, flowers, pantry, animal readiness and picnic progress. The server stays alive when the owner leaves the game. The server machine must stay on; use the included systemd example on Linux for automatic restarts.

Use a trusted LAN or an encrypted private network. This prototype's TCP connection is not encrypted; do not forward it directly onto the public Internet. A firewall may need a rule limited to your private network. No firewall rules are changed by the game or scripts.

## Saves and recovery

Solo saves are under Unity's persistent data folder: on Linux, `~/.config/unity3d/Explorers by Nature/Explorers by Nature/Ranches/Pinewatch/ranch.json`; on Windows, `%USERPROFILE%\AppData\LocalLow\Explorers by Nature\Explorers by Nature\Ranches\Pinewatch\ranch.json`. Dedicated saves use `--data`.

Every committed change saves atomically; `ranch.json.bak` contains the preceding successful version. Stop the game/server before copying or restoring files. Two processes cannot open the same ranch for writing. The local and dedicated ranches are separate; copy a stopped ranch's JSON into the dedicated data folder to migrate it.

This is the first family-playable prototype. One valley, one-storey modular building, shared ranch activities and private server play are implemented. Animals have procedural skeletal animation; scenery and motion remain prototype art. Continents, historical expeditions, a settlement network and a deeper economy remain deferred. Windows is cross-built here; native Windows play still needs your test. The actual two-core/8 GB minimum machine remains unverified.
