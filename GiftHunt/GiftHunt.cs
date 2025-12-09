using MelonLoader;
using UnityEngine;
using UniverseLib.Input;
using GiftHunt.Gifts;
using GiftHunt.UI;

[assembly: MelonInfo(typeof(GiftHunt.GiftHunt), "GiftHunt", "1.2.0", "joeyexists", null)]
[assembly: MelonGame("Little Flag Software, LLC", "Neon White")]
[assembly: MelonColor(204, 255, 138, 25)]

namespace GiftHunt
{
    public class GiftHunt : MelonMod
    {
        public static bool IsModEnabled { get; private set; }
        internal static Game Game => Singleton<Game>.Instance;
        internal static new HarmonyLib.Harmony Harmony { get; private set; }
        internal static GiftHunt ModInstance { get; private set; }

        public override void OnLateInitializeMelon()
        {
            ModInstance = this;
            Harmony = new HarmonyLib.Harmony($"joeyexists.GiftHunt");
            Settings.Initialize();
            Game.OnInitializationComplete += InitializeMod;
        }

        private static void InitializeMod()
        {
            GiftManager.CacheGiftActors();
            SetModActive(Settings.ModEnabledEntry.Value);
        }

        internal static void SetModActive(bool enable)
        {
            if (IsModEnabled == enable) 
                return;

            if (enable)
            {
                PopupManager.Initialize();
                Patching.PatchGame();
                Game.OnLevelLoadComplete += GiftManager.OnLevelLoadComplete;

                var numPatches = Harmony.GetPatchedMethods().Count();
                MelonLogger.Msg($"Enabled (v{ModInstance.Info.Version}) - " +
                    $"Ran {numPatches} patch{(numPatches == 1 ? "" : "es")}.");
            }
            else
            {
                PopupManager.Deinitialize();
                Game.OnLevelLoadComplete -= GiftManager.OnLevelLoadComplete;
                GiftManager.DestroyLastSpawnedGift();
                Patching.UnpatchGame();
                
                MelonLogger.Msg("Disabled.");
            }

            IsModEnabled = enable;
        }

        public override void OnUpdate()
        {
            if (!IsModEnabled) 
                return;

            if (InputManager.GetKeyDown(Settings.HideGiftKeyEntry.Value))
            {
                if (GiftSeedUtility.TryCreateGiftSeed(out var seed, out var error))
                {
                    GUIUtility.systemCopyBuffer = seed;
                    PopupManager.InfoText?.DisplayMessage("Gift Seed Copied to Clipboard!");
                    MelonLogger.Msg($"Generated gift seed: {seed}");
                    GiftManager.LastCreatedGiftSeed = seed;
                }
                else
                {
                    PopupManager.InfoText?.DisplayMessage(error ?? "Failed to create gift seed.");
                }

                return;
            }

            if (InputManager.GetKeyDown(Settings.LoadGiftKeyEntry.Value))
            {
                GiftManager.OnGiftSeedEntryValueChanged(GUIUtility.systemCopyBuffer);
                return;
            }

            if (InputManager.GetKeyDown(Settings.ClearGiftKeyEntry.Value))
            {
                GiftManager.ClearActiveGiftData();
            }
        }
    }
}