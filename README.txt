BOSS RESET ON DEATH
===================

A lightweight Valheim 1.0 BepInEx plugin. Whenever any player dies, every currently loaded, living boss is despawned. Summoning the boss again creates a fresh instance at full health.

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

Install the mod on the server and every client. Valheim distributes ownership of networked creatures, so each peer removes the bosses it owns when a player-death event is replicated.

CONFIGURATION
-------------

BepInEx creates this file after the first launch:

    BepInEx\config\com.jerem.valheim.bossresetondeath.cfg

Available settings:

- Enabled: Enables or disables boss resets.
- ResetDelaySeconds: Waits briefly after player death before removing bosses, allowing other death-related mods to finish their work.

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

1. Summon a boss.
2. Damage the boss without killing it.
3. Let the player die.
4. Return to the boss location and verify that the old boss is gone.
5. Summon the boss again and verify that it starts at full health.
