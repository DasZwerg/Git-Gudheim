using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BossResetOnDeath
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class BossResetOnDeathPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jerem.valheim.bossresetondeath";
        public const string PluginName = "Boss Reset On Death";
        public const string PluginVersion = "1.3.0";

        private const string AbandonedKey = "BossResetAbandonedSince";
        private const string ArenaCenterKey = "BossArenaCenter";
        private const string ArenaRadiusKey = "BossArenaRadius";
        private const string ArenaEntrantsKey = "BossArenaEntrants";
        private const string ArenaVisualKey = "BossArenaVisualZDO";
        private const string ArenaVisualMarker = "BossArenaVisual";
        private const string SGCustomRadiusKey = "SG_Custom_Radius";
        private const string PlayerNameKey = "playerName";

        private static readonly int OnDeathHash = StringExtensionMethods.GetStableHashCode("OnDeath");
        private static readonly int PlayerPrefabHash = StringExtensionMethods.GetStableHashCode("Player");

        private static BossResetOnDeathPlugin _instance;
        private Harmony _harmony;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _resetDelay;

        private ConfigEntry<bool> _runAwayEnabled;
        private ConfigEntry<float> _runAwayRange;
        private ConfigEntry<float> _runAwayDelay;
        private ConfigEntry<float> _runAwayCheckInterval;

        private ConfigEntry<bool> _arenaEnabled;
        private ConfigEntry<bool> _arenaVisualEnabled;
        private ConfigEntry<float> _arenaRadius;
        private ConfigEntry<float> _arenaCheckInterval;

        private void Awake()
        {
            _instance = this;

            _enabled = Config.Bind("General", "Enabled", true, "Despawn all active bosses whenever a player dies.");
            _resetDelay = Config.Bind("General", "ResetDelaySeconds", 0.25f, "Delay before despawning bosses so other death patches can finish.");

            _runAwayEnabled = Config.Bind("RunAway", "Enabled", true, "Despawn bosses when no player has been near them for a while.");
            _runAwayRange = Config.Bind("RunAway", "Range", 96f, "Maximum horizontal distance a player can be from a boss before it is considered abandoned.");
            _runAwayDelay = Config.Bind("RunAway", "DelaySeconds", 90f, "How long all players must stay away before the boss despawns.");
            _runAwayCheckInterval = Config.Bind("RunAway", "CheckIntervalSeconds", 2f, "How often to check player distances to bosses.");

            _arenaEnabled = Config.Bind("Arena", "Enabled", true, "Create a one-way arena around a boss when it spawns. Entrants can leave, but dead players and latecomers cannot enter.");
            _arenaVisualEnabled = Config.Bind("Arena", "VisualEnabled", true, "Spawn a shield-generator dome visual for the arena boundary.");
            _arenaRadius = Config.Bind("Arena", "Radius", 48f, "Radius of the boss arena.");
            _arenaCheckInterval = Config.Bind("Arena", "CheckIntervalSeconds", 1f, "How often the server/host checks arena boundaries and entrant status.");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);

            StartCoroutine(RunAwayChecker());
            StartCoroutine(ArenaChecker());

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _instance = null;
            StopAllCoroutines();
        }

        internal static void QueueBossReset(string reason)
        {
            if (_instance == null || !_instance._enabled.Value)
            {
                return;
            }

            _instance.StartCoroutine(_instance.ResetBossesAfterDelay(reason));
        }

        private IEnumerator ResetBossesAfterDelay(string reason)
        {
            yield return new WaitForSeconds(_resetDelay.Value);

            int removed = DespawnOwnedBosses();
            if (removed > 0)
            {
                Logger.LogInfo($"Despawned {removed} active boss{(removed == 1 ? string.Empty : "es")} because {reason}.");
            }
        }

        private IEnumerator RunAwayChecker()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0.1f, _runAwayCheckInterval.Value));
                CheckRunAway();
            }
        }

        private IEnumerator ArenaChecker()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0.1f, _arenaCheckInterval.Value));
                CheckArenas();
            }
        }

        private void CheckRunAway()
        {
            if (!_runAwayEnabled.Value || ZNetScene.instance == null)
            {
                return;
            }

            // Only the server/host sees every connected player, so it is authoritative for abandonment.
            if (ZNet.instance != null && !ZNet.instance.IsServer())
            {
                return;
            }

            List<Player> players = Player.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                return;
            }

            List<Character> characters = Character.GetAllCharacters();
            for (int i = characters.Count - 1; i >= 0; --i)
            {
                Character boss = characters[i];
                if (boss == null || !boss.IsBoss() || boss.IsDead())
                {
                    continue;
                }

                ZNetView netView = boss.GetComponent<ZNetView>();
                if (netView == null || !netView.IsValid())
                {
                    continue;
                }

                float nearest = GetNearestPlayerDistance(boss.transform.position, players);
                ZDO zdo = netView.GetZDO();
                if (zdo == null)
                {
                    continue;
                }

                if (nearest > _runAwayRange.Value)
                {
                    float abandonedSince = zdo.GetFloat(AbandonedKey, -1f);
                    float now = Time.time;
                    if (abandonedSince < 0f)
                    {
                        zdo.Set(AbandonedKey, now);
                        continue;
                    }

                    if (now - abandonedSince >= _runAwayDelay.Value)
                    {
                        zdo.Set(AbandonedKey, -1f);
                        DestroyArenaVisual(zdo);
                        netView.ClaimOwnership();
                        ZNetScene.instance.Destroy(boss.gameObject);
                        Logger.LogInfo($"Despawned {boss.m_name} after all players stayed away for {_runAwayDelay.Value:F0}s.");
                    }
                }
                else
                {
                    zdo.Set(AbandonedKey, -1f);
                }
            }
        }

        private void CheckArenas()
        {
            if (!_arenaEnabled.Value || ZNetScene.instance == null)
            {
                return;
            }

            // Only the server/host is authoritative for arena boundaries and entrant lists.
            if (ZNet.instance != null && !ZNet.instance.IsServer())
            {
                return;
            }

            List<Player> players = Player.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                return;
            }

            List<Character> characters = Character.GetAllCharacters();
            for (int i = characters.Count - 1; i >= 0; --i)
            {
                Character boss = characters[i];
                if (boss == null || !boss.IsBoss() || boss.IsDead())
                {
                    continue;
                }

                ZNetView netView = boss.GetComponent<ZNetView>();
                if (netView == null || !netView.IsValid())
                {
                    continue;
                }

                ZDO zdo = netView.GetZDO();
                if (zdo == null)
                {
                    continue;
                }

                Vector3 center = zdo.GetVec3(ArenaCenterKey, Vector3.zero);
                float radius = zdo.GetFloat(ArenaRadiusKey, 0f);
                if (radius <= 0f)
                {
                    center = boss.transform.position;
                    radius = _arenaRadius.Value;
                    zdo.Set(ArenaCenterKey, center);
                    zdo.Set(ArenaRadiusKey, radius);
                    zdo.Set(ArenaEntrantsKey, BuildEntrantString(center, radius, players));
                    if (_arenaVisualEnabled.Value)
                    {
                        SpawnArenaVisual(zdo, center, radius);
                    }
                    Logger.LogInfo($"Created arena for {boss.m_name} at {center} with radius {radius}.");
                    continue;
                }

                HashSet<string> entrants = ParseEntrantString(zdo.GetString(ArenaEntrantsKey, ""));
                bool changed = false;

                for (int p = 0; p < players.Count; ++p)
                {
                    Player player = players[p];
                    if (player == null)
                    {
                        continue;
                    }

                    string name = GetPlayerName(player);
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(player.transform.position, center);
                    bool isEntrant = entrants.Contains(name);

                    if (distance <= radius)
                    {
                        if (!isEntrant)
                        {
                            PushPlayerOut(player, center, radius);
                        }
                        else if (player.IsDead())
                        {
                            entrants.Remove(name);
                            changed = true;
                        }
                    }
                    else if (isEntrant && player.IsDead())
                    {
                        entrants.Remove(name);
                        changed = true;
                    }
                }

                if (changed)
                {
                    zdo.Set(ArenaEntrantsKey, string.Join(",", entrants));
                }

                bool anyAlive = false;
                foreach (string entrant in entrants)
                {
                    Player p = FindPlayerByName(players, entrant);
                    if (p != null && !p.IsDead())
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive && entrants.Count > 0)
                {
                    zdo.Set(ArenaEntrantsKey, "");
                    DestroyArenaVisual(zdo);
                    netView.ClaimOwnership();
                    ZNetScene.instance.Destroy(boss.gameObject);
                    Logger.LogInfo($"All arena entrants died; despawned {boss.m_name}.");
                }
            }
        }

        private static void SpawnArenaVisual(ZDO bossZdo, Vector3 center, float radius)
        {
            if (ZNetScene.instance == null)
            {
                return;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab("piece_shieldgenerator");
            if (prefab == null)
            {
                _instance?.Logger.LogWarning("Could not find piece_shieldgenerator prefab for arena visual.");
                return;
            }

            GameObject visual = Object.Instantiate(prefab, center, Quaternion.identity);
            ZNetView visualNview = visual.GetComponent<ZNetView>();
            if (visualNview == null)
            {
                Object.Destroy(visual);
                return;
            }

            ZDO visualZdo = visualNview.GetZDO();
            if (visualZdo == null)
            {
                Object.Destroy(visual);
                return;
            }

            visualZdo.Set(ArenaVisualMarker, true);
            visualZdo.Set(SGCustomRadiusKey, radius);
            visualZdo.Set("fuel", 10f);
            visualZdo.Set("maxFuel", 10f);
            visualZdo.Set("health", 10000f);
            bossZdo.Set(ArenaVisualKey, visualZdo.m_uid);
        }

        private static void DestroyArenaVisual(ZDO bossZdo)
        {
            if (ZDOMan.instance == null)
            {
                return;
            }

            ZDOID visualId = bossZdo.GetZDOID(ArenaVisualKey);
            if (visualId.IsNone())
            {
                return;
            }

            ZDO visualZdo = ZDOMan.instance.GetZDO(visualId);
            if (visualZdo != null)
            {
                visualZdo.SetOwner(ZNet.GetUID());
                ZDOMan.instance.DestroyZDO(visualZdo);
            }

            bossZdo.Set(ArenaVisualKey, ZDOID.None);
        }

        private static string BuildEntrantString(Vector3 center, float radius, List<Player> players)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < players.Count; ++i)
            {
                Player player = players[i];
                if (player == null || player.IsDead())
                {
                    continue;
                }

                if (Vector3.Distance(player.transform.position, center) <= radius)
                {
                    string name = GetPlayerName(player);
                    if (!string.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
            }
            return string.Join(",", names);
        }

        private static HashSet<string> ParseEntrantString(string value)
        {
            HashSet<string> set = new HashSet<string>();
            if (string.IsNullOrEmpty(value))
            {
                return set;
            }

            string[] parts = value.Split(',');
            for (int i = 0; i < parts.Length; ++i)
            {
                string part = parts[i].Trim();
                if (!string.IsNullOrEmpty(part))
                {
                    set.Add(part);
                }
            }
            return set;
        }

        private static void PushPlayerOut(Player player, Vector3 center, float radius)
        {
            ZNetView nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            Vector3 toPlayer = player.transform.position - center;
            float distance = toPlayer.magnitude;
            if (distance < 0.001f)
            {
                toPlayer = Vector3.forward;
                distance = 1f;
            }

            Vector3 boundary = center + toPlayer.normalized * (radius + 2f);
            boundary.y = player.transform.position.y;

            long owner = nview.GetZDO().GetOwner();
            nview.InvokeRPC(owner, "TeleportTo", boundary, player.transform.rotation, false);
        }

        private static Player FindPlayerByName(List<Player> players, string name)
        {
            for (int i = 0; i < players.Count; ++i)
            {
                Player player = players[i];
                if (player != null && GetPlayerName(player) == name)
                {
                    return player;
                }
            }
            return null;
        }

        private static string GetPlayerName(Player player)
        {
            return player.GetComponent<ZNetView>()?.GetZDO()?.GetString(PlayerNameKey, player.m_name) ?? player.m_name;
        }

        private static float GetNearestPlayerDistance(Vector3 position, List<Player> players)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < players.Count; ++i)
            {
                Player player = players[i];
                if (player == null || player.IsDead())
                {
                    continue;
                }

                float distance = Utils.DistanceXZ(position, player.transform.position);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }
            return nearest;
        }

        private static int DespawnOwnedBosses()
        {
            if (ZNetScene.instance == null)
            {
                return 0;
            }

            List<Character> characters = Character.GetAllCharacters();
            int removed = 0;
            for (int i = characters.Count - 1; i >= 0; --i)
            {
                Character character = characters[i];
                if (character == null || !character.IsBoss() || character.IsDead())
                {
                    continue;
                }

                ZNetView netView = character.GetComponent<ZNetView>();
                if (netView == null || !netView.IsValid() || !netView.IsOwner())
                {
                    continue;
                }

                DestroyArenaVisual(netView.GetZDO());
                ZNetScene.instance.Destroy(character.gameObject);
                ++removed;
            }
            return removed;
        }

        private static bool HandleArenaDeath(string deadPlayerName)
        {
            if (_instance == null || !_instance._arenaEnabled.Value || ZNetScene.instance == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(deadPlayerName))
            {
                return false;
            }

            List<Character> characters = Character.GetAllCharacters();
            for (int i = 0; i < characters.Count; ++i)
            {
                Character boss = characters[i];
                if (boss == null || !boss.IsBoss() || boss.IsDead())
                {
                    continue;
                }

                ZNetView netView = boss.GetComponent<ZNetView>();
                if (netView == null || !netView.IsValid())
                {
                    continue;
                }

                ZDO zdo = netView.GetZDO();
                if (zdo == null)
                {
                    continue;
                }

                float radius = zdo.GetFloat(ArenaRadiusKey, 0f);
                if (radius <= 0f)
                {
                    continue;
                }

                HashSet<string> entrants = ParseEntrantString(zdo.GetString(ArenaEntrantsKey, ""));
                if (!entrants.Contains(deadPlayerName))
                {
                    continue;
                }

                entrants.Remove(deadPlayerName);
                zdo.Set(ArenaEntrantsKey, string.Join(",", entrants));

                bool anyAlive = false;
                List<Player> players = Player.GetAllPlayers();
                foreach (string entrant in entrants)
                {
                    Player p = FindPlayerByName(players, entrant);
                    if (p != null && !p.IsDead())
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive)
                {
                    zdo.Set(ArenaEntrantsKey, "");
                    DestroyArenaVisual(zdo);
                    netView.ClaimOwnership();
                    ZNetScene.instance.Destroy(boss.gameObject);
                    _instance?.Logger.LogInfo($"All arena entrants died; despawned {boss.m_name}.");
                }
                else
                {
                    _instance?.Logger.LogInfo($"Arena entrant {deadPlayerName} died; {entrants.Count} remain alive.");
                }

                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(ZRoutedRpc), "HandleRoutedRPC")]
        private static class RoutedRpcDeathPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ZRoutedRpc.RoutedRPCData data)
            {
                if (data == null || data.m_methodHash != OnDeathHash)
                {
                    return;
                }

                if (data.m_targetZDO.IsNone())
                {
                    return;
                }

                ZDO zdo = ZDOMan.instance?.GetZDO(data.m_targetZDO);
                if (zdo == null || zdo.GetPrefab() != PlayerPrefabHash)
                {
                    return;
                }

                string deadPlayerName = zdo.GetString(PlayerNameKey, "");
                if (HandleArenaDeath(deadPlayerName))
                {
                    return;
                }

                QueueBossReset("a player died");
            }
        }

        [HarmonyPatch(typeof(ShieldGenerator), "Start")]
        private static class ShieldGeneratorStartPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ShieldGenerator __instance)
            {
                ZNetView nview = __instance.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    return;
                }

                ZDO zdo = nview.GetZDO();
                if (zdo == null || !zdo.GetBool(ArenaVisualMarker, false))
                {
                    return;
                }

                float radius = zdo.GetFloat(SGCustomRadiusKey, 30f);
                __instance.m_minShieldRadius = radius;
                __instance.m_maxShieldRadius = radius;
            }
        }

        [HarmonyPatch(typeof(ShieldGenerator), "UpdateShield")]
        private static class ShieldGeneratorUpdateShieldPatch
        {
            [HarmonyPrefix]
            private static void Prefix(ShieldGenerator __instance)
            {
                ZNetView nview = __instance.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    return;
                }

                ZDO zdo = nview.GetZDO();
                if (zdo == null || !zdo.GetBool(ArenaVisualMarker, false))
                {
                    return;
                }

                float radius = zdo.GetFloat(SGCustomRadiusKey, 30f);
                __instance.m_minShieldRadius = radius;
                __instance.m_maxShieldRadius = radius;
            }
        }

        [HarmonyPatch(typeof(ShieldGenerator), "Update")]
        private static class ShieldGeneratorUpdatePatch
        {
            [HarmonyPostfix]
            private static void Postfix(ShieldGenerator __instance)
            {
                ZNetView nview = __instance.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    return;
                }

                ZDO zdo = nview.GetZDO();
                if (zdo == null || !zdo.GetBool(ArenaVisualMarker, false))
                {
                    return;
                }

                if (zdo.IsOwner() && zdo.GetFloat("fuel", 0f) < 9.5f)
                {
                    zdo.Set("fuel", 10f);
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
        private static class WearNTearApplyDamagePatch
        {
            [HarmonyPrefix]
            private static bool Prefix(WearNTear __instance)
            {
                ZNetView nview = __instance.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid())
                {
                    ZDO zdo = nview.GetZDO();
                    if (zdo != null && zdo.GetBool(ArenaVisualMarker, false))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
