# A morning in Pinewatch

Start `ExplorersByNature` on Linux or `ExplorersByNature.exe` on Windows. Your solo ranch opens automatically and saves each completed change.

1. Walk west from the trail to Clover and the hens. Aim at Clover and press **E**, then press **Space** three times as the marker crosses the green band. Aim at a hen/nesting box and press **E** to gather eggs.
2. Press **B** and look at nearby ground. **1** floor, **2** wall, **3** doorway, **4** roof, **5** fence, **6** flowers. **R** rotates; left click places. Walls and roofs attach to a foundation's three-metre cell.
3. Aim at a piece and press **M**, then click a new location to move it. Right click removes a piece. Remove a foundation's walls and roof first. Pieces are free; experiment freely.
4. Follow the trail north to Pinewatch Overlook. The aspen grove lies northwest of the ranch; Riverbend is east and north. **Tab** opens your discovery journal and multiplayer connection screen.
5. **Escape** opens camera, audio and graphics settings. **F2** changes graphics quality. **Home** returns to the starting meadow. Nothing starves, dies, or punishes an absence.

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

In the game, **Tab → Private server**: enter a name, the server machine's address, port, and code. All players share construction, flowers, pantry and animal readiness. The server stays alive when the owner leaves the game. The server machine must stay on; use the included systemd example on Linux for automatic restarts.

Use a trusted LAN or an encrypted private network. This prototype's TCP connection is not encrypted; do not forward it directly onto the public Internet. A firewall may need a rule limited to your private network. No firewall rules are changed by the game or scripts.

## Saves and recovery

Solo saves are under Unity's persistent data folder: on Linux, `~/.config/unity3d/Explorers by Nature/Explorers by Nature/Ranches/Pinewatch/ranch.json`; on Windows, `%USERPROFILE%\AppData\LocalLow\Explorers by Nature\Explorers by Nature\Ranches\Pinewatch\ranch.json`. Dedicated saves use `--data`.

Every committed change saves atomically; `ranch.json.bak` contains the preceding successful version. Stop the game/server before copying or restoring files. Two processes cannot open the same ranch for writing. The local and dedicated ranches are separate; copy a stopped ranch's JSON into the dedicated data folder to migrate it.

This is the first family-playable prototype. One valley, one-storey modular building, shared ranch activities and private server play are implemented. Wildlife animation and trees remain prototype art. Continents, historical expeditions, a settlement network and a deeper economy remain deferred. Windows is cross-built here; native Windows play still needs your test. The actual two-core/8 GB minimum machine remains unverified.
