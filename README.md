# Boss Reset On Death

A lightweight Valheim 1.0 BepInEx plugin. Resets active bosses in several situations:

- **Player death:** whenever any player dies, every currently loaded, living boss is despawned (unless the boss is in an active arena).
- **Players run away:** if no player stays within range of a boss for a configurable time, the boss despawns.
- **Arena wipe:** when a boss spawns, a one-way arena is created. Players inside at spawn can stay or leave, but dead players and latecomers cannot enter. Once all arena entrants have died, the boss despawns.

Summoning the boss again creates a fresh instance at full health.

## Requirements

- Valheim 1.0 or later.
- BepInEx 5.
- For multiplayer: install on the server **and** every client.

The arena visual uses the `piece_shieldgenerator` prefab from the Ashlands update. If you are running an older Valheim version without this prefab, the gameplay barrier still works but no dome appears.

## Multiplayer

Install the mod on the server and every client.

Death detection now watches the network death RPC (`ZRoutedRpc.HandleRoutedRPC`) instead of patching `Player.OnDeath`, so it works on dedicated servers and in multiplayer. When a player dies, every peer that receives the RPC destroys the bosses it currently owns. This handles Valheim's distributed creature ownership correctly.

Run-away and arena mechanics run on the server/host, because the server has authoritative knowledge of every player's position.

If at least one player stays within range of a boss, the run-away despawn will not trigger.

## Build

```bat
dotnet build -c Release
```

Override local paths when needed:

```bat
dotnet build -c Release -p:GamePath="C:\path\to\Valheim" -p:BepInExPath="C:\path\to\BepInEx"
```

## Install or uninstall

Close Valheim, then double-click `install-uninstall.bat` and choose an option. You can also run `install-uninstall.bat install` or `install-uninstall.bat uninstall`. Uninstalling removes this plugin and its configuration only; it does not disturb your other mods.

## Configuration

BepInEx creates `BepInEx/config/com.jerem.valheim.bossresetondeath.cfg` after the first launch.

### General

- `Enabled`: enables or disables boss resets on player death.
- `ResetDelaySeconds`: waits briefly after player death before removing bosses, allowing other death-related Harmony patches to finish.

### RunAway

- `Enabled`: enables or disables run-away/abandoned-boss despawns.
- `Range`: maximum horizontal distance a player can be from a boss before it is considered abandoned.
- `DelaySeconds`: how long all players must stay away before the boss despawns.
- `CheckIntervalSeconds`: how often the server/host checks player distances to bosses.

### Arena

- `Enabled`: enables or disables the one-way boss arena.
- `VisualEnabled`: spawn a shield-generator dome visual at the arena boundary.
- `Radius`: radius of the arena around the boss spawn point.
- `CheckIntervalSeconds`: how often the server/host checks arena boundaries and entrant status.

## Arena behavior

When a boss spawns, the server records every living player currently inside the arena radius as an **entrant**. Entrants may leave and re-enter while alive. Anyone else — including players who die and respawn, players who log in late, or players who were simply outside at spawn — is teleported back outside the arena if they try to enter.

If `Arena.VisualEnabled` is true, a shield-generator dome visual is spawned at the arena center and scaled to the arena radius, so players can see the boundary. The visual is destroyed when the boss despawns.

If every entrant dies, the boss is despawned. If at least one entrant is still alive (even if they left the arena), the boss remains.

## Known limitations

- The arena visual requires the `piece_shieldgenerator` prefab. On older Valheim versions that lack it, the gameplay barrier still teleports players out, but there is no visible dome.
- Always test in a real Valheim single-player, listen-server, and dedicated-server environment before relying on the mod for progression. Compilation success does not guarantee runtime behavior matches every Valheim patch.
