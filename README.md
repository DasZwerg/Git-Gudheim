# Boss Reset On Death

A lightweight Valheim 1.0 BepInEx plugin. Whenever any player dies, every currently loaded, living boss is despawned. Summoning the boss again creates a fresh instance at full health.

## Multiplayer

Install the mod on the server and every client. Valheim distributes ownership of networked creatures, so each peer removes bosses it owns when the replicated player-death event runs.

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

- `Enabled`: enables or disables boss resets.
- `ResetDelaySeconds`: waits briefly after player death before removing bosses, allowing other death-related Harmony patches to finish.
