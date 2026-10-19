# Changelog

## 1.3.0

- Added an optional arena visual: a shield-generator dome scaled to `Arena.Radius`.
- Patched `ShieldGenerator` radius/fuel so the dome stays active and matches the arena size.
- Patched `WearNTear.ApplyDamage` so arena visuals are invulnerable during the fight.
- Added `Arena.VisualEnabled` config option.
- Destroyed the visual automatically when the boss despawns.

## 1.2.0

- Added a one-way boss arena system.
  - Records living players inside the arena radius when a boss spawns as **entrants**.
  - Entrants may leave and re-enter while alive.
  - Dead/resurrected players, late joiners, and anyone outside at spawn are teleported out if they try to enter.
  - The boss despawns only when all entrants have died.
- Integrated arena reset with replicated `ZRoutedRpc` death detection.
- Added `Arena.Enabled`, `Arena.Radius`, and `Arena.CheckIntervalSeconds` config options.

## 1.1.0

- Replaced the local `Player.OnDeath` Harmony patch with a server-side `ZRoutedRpc.HandleRoutedRPC` postfix.
  - This detects player deaths on dedicated servers and in multiplayer.
  - Each peer destroys the bosses it owns, respecting Valheim's distributed creature ownership.
- Added run-away / abandoned-boss despawn.
  - Measures distance from each boss to the nearest living player.
  - Despawns the boss if no player stays within `RunAway.Range` for `RunAway.DelaySeconds`.
  - Timer is stored on the boss ZDO so it survives ownership transfers.
- Added `RunAway.Enabled`, `RunAway.Range`, `RunAway.DelaySeconds`, and `RunAway.CheckIntervalSeconds` config options.
- Added `assembly_utils.dll` reference for `StringExtensionMethods.GetStableHashCode` and `Utils.DistanceXZ`.

## 1.0.0

- Initial release: despawns active bosses after a short delay when the local player dies.
