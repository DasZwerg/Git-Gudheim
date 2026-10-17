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
        public const string PluginVersion = "1.0.0";

        private static BossResetOnDeathPlugin _instance;
        private Harmony _harmony;
        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _resetDelay;

        private void Awake()
        {
            _instance = this;
            _enabled = Config.Bind("General", "Enabled", true, "Despawn all active bosses whenever a player dies.");
            _resetDelay = Config.Bind("General", "ResetDelaySeconds", 0.25f, "Delay before despawning bosses so other death patches can finish.");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _instance = null;
        }

        internal static void QueueBossReset()
        {
            if (_instance == null || !_instance._enabled.Value)
            {
                return;
            }

            _instance.StartCoroutine(_instance.ResetBossesAfterDelay());
        }

        private IEnumerator ResetBossesAfterDelay()
        {
            yield return new WaitForSeconds(_resetDelay.Value);

            if (ZNetScene.instance == null)
            {
                yield break;
            }

            List<Character> characters = Character.GetAllCharacters();
            int removed = 0;
            for (int index = characters.Count - 1; index >= 0; --index)
            {
                Character character = characters[index];
                if (character == null || !character.IsBoss() || character.IsDead())
                {
                    continue;
                }

                ZNetView netView = character.GetComponent<ZNetView>();
                if (netView == null || !netView.IsValid() || !netView.IsOwner())
                {
                    continue;
                }

                ZNetScene.instance.Destroy(character.gameObject);
                ++removed;
            }

            if (removed > 0)
            {
                Logger.LogInfo($"Despawned {removed} active boss{(removed == 1 ? string.Empty : "es")} after a player death.");
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class PlayerOnDeathPatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                QueueBossReset();
            }
        }
    }
}
