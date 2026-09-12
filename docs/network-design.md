# Shared ranch authority

The Unity client renders and requests changes. `Assets/Shared/Ranch.cs` owns construction validation, production, inventory, revisions, and durable commits. Both the embedded solo server and standalone .NET server call the same interface. `Apply(request, visitor, utc)` returns a rejection or commits the change; `Snapshot` returns an independent copy.

We considered three interfaces:

| Option | Caller responsibilities | Benefit and cost |
| --- | --- | --- |
| Unity Netcode RPCs and headless Unity | Spawned network objects, ownership, transport, scene lifecycle | Established replication stack; adds engine lifecycle and prefab integration to every ranch feature |
| Shared C# authority over bounded request/reply TCP, selected | Send an action and display committed snapshots | One module hides validation, production and disk commits from both callers; easy restart/concurrency tests; custom transport needs explicit framing and disconnect checks |
| Local save plus peer synchronization | Every client reconciles edits and saves | Small initial solo interface, but conflict resolution and owner departure leak into every caller |

Dependencies are local-substitutable disk storage and an owned remote server. The serialization adapter differs between Unity JsonUtility and .NET System.Text.Json; domain rules remain identical. The terrain adapter reads a metre-resolution height sample generated from Unity's valley, so remote validation does not trust client-supplied ground heights. The server project links shared sources directly instead of committing generated DLLs.

Server connections receive identities. Commands run under one authority lock. Replies contain visitors plus a world snapshot only when its revision changes. Client polling runs at approximately 6 Hz, with interpolated avatars. An explicit `hasState` flag distinguishes an unchanged reply from a real snapshot; Unity inline JSON objects do not safely preserve null as a presence signal. Network frames, connections, request rates, client queues and piece counts are bounded. A blocked socket holds no world lock. Save replacement finishes before a successful reply. A lock file prevents simultaneous writers; `.bak` retains the preceding save. A damaged save fails visibly instead of generating a new ranch.

Solo embeds the same TCP authority bound to loopback. A dedicated server runs independently of any player process. No edits are queued across a disconnect; the screen reports reconnection and refreshes from the server.

Prototype limits: movement is client-reported; peaceful wildlife is decorative and simulated independently on each client. All invited players may edit any piece. Shared codes travel over plaintext TCP, so remote use is for a trusted LAN or an encrypted private network, not a publicly exposed Internet port. The synthetic 20-client check establishes protocol concurrency, not 20 rendered clients or WAN performance.
