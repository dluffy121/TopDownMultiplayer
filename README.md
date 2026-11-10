# TopDown Multiplayer

A top-down multiplayer arena shooter built in Unity to explore networking with Photon Fusion 2: two network topologies, host migration, reconnects, lag-compensated projectiles and networked object pooling.

**Unity 6000.3.9f1** · URP · Photon Fusion 2.0.12 · Cinemachine

## Running it

1. Open the project in Unity 6000.3.9f1.
2. Fusion Hub opens on first import (or use **Tools → Fusion → Fusion Hub**). Create a Fusion 2 app in the [Photon dashboard](https://dashboard.photonengine.com) and paste its App ID there. No App ID is included in this repo.
3. Open `Assets/Scenes/Init.unity` and press Play. Multi-peer mode is enabled, so several players can run inside one Editor.

## Technical approach

### Two topologies
The lobby runs in Shared mode and game sessions run in Host/Client mode. The game-state flow (Main Menu → Lobby → Game) starts the right runner for each stage, and gameplay code such as player spawning, authority checks and the host / master-client indicator adapts to the active mode.

### Composable network input
Fusion uses a single input callback per runner, which tends to grow into one large input handler. Instead, a central manager collects input from every component that implements `INetworkInputListener` (player movement, each weapon) into one shared input struct. Fire presses are latched between ticks so quick taps aren't dropped.

### Three projectile models
- **Standalone:** one networked object per bullet, the simplest approach.
- **Hitscan:** a lag-compensated raycast at the moment of firing. Results go into a networked buffer that clients read to spawn the visuals.
- **Kinematic:** only the fire tick, origin and velocity are networked. The position is calculated each tick, and hits are found with lag-compensated raycasts between ticks. Clients interpolate the visuals between snapshots.

Projectile types are ScriptableObjects, so adding one is a data change rather than new network code.

### Host migration and reconnects
Each player connects with a random token. When the host leaves, the runner restarts with Fusion's migration token, networked objects are rebuilt from the last snapshot, and other systems are notified through an `IHostMigrationListener` interface. Snapshots are refreshed as players join. A returning player is matched back to their character by token, and a disconnected player's character stays in the game for a short grace period.

### Authority and RPCs
Firing runs only on the state authority, rate-limited with tick timers. Weapon pickups are requested by RPC and guarded by a networked flag, so only one player gets each pickup. Damage is applied by the state authority, and proxies forward their hits to it. Health, player colour and the host indicator update through `[Networked]` properties with change callbacks.

### Networked object pooling
A custom `INetworkObjectProvider` keeps a pool of network objects per prefab, with an ignore list and an optional cap on pool size.

### Multi-peer testing
Several peers can run in one Editor session. Projectile visuals are registered with each runner's visibility so every peer only renders its own, and physics scenes are stepped manually when running in multi-peer mode.

## Project structure

All scripts are under `Assets/Scripts`.

| Folder | Contents |
|---|---|
| `Network` | Runner management, input collection, host migration, session tokens |
| `Game` | Game-state flow and the player |
| `Projectiles`, `Weapons`, `Hit` | Projectile models, weapons and hit processing |
| `Spawners`, `Pickup` | Player, projectile and pickup spawning |
| `ObjectPooling` | Network object pool |
| `UI`, `Controller`, `Utility` | Menus, overhead health, camera and helpers |

## Scope

Characters are placeholder capsules. The project focuses on the networking layer rather than art or content.

## Credits

- [Photon Fusion 2](https://www.photonengine.com/fusion) SDK by Exit Games, included under its own license
