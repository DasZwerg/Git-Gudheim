BOSS RESET ON DEATH
===================

A lightweight Valheim 1.0 BepInEx plugin. Resets active bosses in several situations:

- Player death: whenever any player dies, every currently loaded, living boss is despawned (unless the boss is in an active arena).
- Players run away: if no player stays within range of a boss for a configurable time, the boss despawns.
- Arena wipe: when a boss spawns, a one-way arena is created. Players inside at spawn can stay or leave, but dead players and latecomers cannot enter. Once all arena entrants have died, the boss despawns.

Summoning the boss again creates a fresh instance at full health.

REQUIREMENTS
------------

- Valheim 1.0 or later.
- BepInEx 5.
- For multiplayer: install on the server AND every client.

The arena visual uses the piece_shieldgenerator prefab from the Ashlands update. If you are running an older Valheim version without this prefab, the gameplay barrier still works but no dome appears.

INSTALLATION
------------

1. Close Valheim.
2. Double-click install-uninstall.bat.
3. Choose "Build and install."
4. Start Valheim through the ValPlusKevin Gale profile.

You can also install from a command prompt with:

    install-uninstall.bat install

UNINSTALLATION / RESTORE VANILLA BOSS BEHAVIOR
----------------------------------------------

1. Close Valheim.
2. Double-click install-uninstall.bat.
3. Choose "Uninstall and restore vanilla boss behavior."

You can also uninstall from a command prompt with:

    install-uninstall.bat uninstall

Uninstalling removes this plugin and its configuration only. It does not alter or remove any other mods.

MULTIPLAYER
-----------

Install the mod on the server and every client.

Death detection watches the network death RPC instead of patching Player.OnDeath, so it works on dedicated servers and in multiplayer. When a player dies, every peer destroys the bosses it owns.

Run-away and arena mechanics run on the server/host, because the server knows where every player is.

If at least one player stays within range of a boss, the run-away despawn will not trigger.

CONFIGURATION
-------------

BepInEx creates this file after the first launch:

    BepInEx\config\com.jerem.valheim.bossresetondeath.cfg

Available settings:

General
- Enabled: Enables or disables boss resets on player death.
- ResetDelaySeconds: Waits briefly after player death before removing bosses, allowing other death-related mods to finish their work.

RunAway
- Enabled: Enables or disables run-away/abandoned-boss despawns.
- Range: Maximum horizontal distance a player can be from a boss before it is considered abandoned.
- DelaySeconds: How long all players must stay away before the boss despawns.
- CheckIntervalSeconds: How often the server/host checks player distances to bosses.

Arena
- Enabled: Enables or disables the one-way boss arena.
- VisualEnabled: Spawn a shield-generator dome visual at the arena boundary.
- Radius: Radius of the arena around the boss spawn point.
- CheckIntervalSeconds: How often the server/host checks arena boundaries and entrant status.

ARENA BEHAVIOR
--------------

When a boss spawns, the server records every living player currently inside the arena radius as an ENTRANT. Entrants may leave and re-enter while alive. Anyone else - including players who die and respawn, players who log in late, or players who were simply outside at spawn - is teleported back outside the arena if they try to enter.

If Arena.VisualEnabled is true, a shield-generator dome visual is spawned at the arena center and scaled to the arena radius, so players can see the boundary. The visual is destroyed when the boss despawns.

If every entrant dies, the boss is despawned. If at least one entrant is still alive (even if they left the arena), the boss remains.

KNOWN LIMITATIONS
-----------------

- The arena visual requires the piece_shieldgenerator prefab. On older Valheim versions that lack it, the gameplay barrier still teleports players out, but there is no visible dome.
- Always test in a real Valheim single-player, listen-server, and dedicated-server environment before relying on the mod for progression. Compilation success does not guarantee runtime behavior matches every Valheim patch.

MANUAL BUILD
------------

From this directory, run:

    dotnet build -c Release

The compiled plugin will be created at:

    bin\Release\netstandard2.1\BossResetOnDeath.dll

To override the configured Valheim and BepInEx paths:

    dotnet build -c Release -p:GamePath="C:\path\to\Valheim" -p:BepInExPath="C:\path\to\BepInEx"

TESTING
-------

Death reset:
1. Summon a boss.
2. Damage the boss without killing it.
3. Let the player die.
4. Return to the boss location and verify that the old boss is gone.
5. Summon the boss again and verify that it starts at full health.

Run-away reset:
1. Summon a boss.
2. Run away until the boss is outside the configured RunAway Range.
3. Wait for the configured RunAway Delay.
4. Return to the boss location and verify that the old boss is gone.
5. Summon the boss again and verify that it starts at full health.
6. Verify that the boss does NOT despawn while another player remains in range.

Arena reset:
1. With multiple players near the altar, summon a boss.
2. Verify that players inside the arena radius at spawn can fight the boss.
3. Have one player leave the arena and verify they can re-enter while alive.
4. Have that player die (or log out) and verify they are teleported out if they try to re-enter.
5. Kill every player who was inside at spawn.
6. Verify that the boss despawns once all entrants are dead.
