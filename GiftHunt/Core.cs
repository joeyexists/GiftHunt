using MelonLoader;
using UnityEngine;
using UniverseLib.Input;
using HarmonyLib;

[assembly: MelonInfo(typeof(GiftHunt.Core), "GiftHunt", "1.0.0-rc.5", "joeyexists", null)]
[assembly: MelonGame("Little Flag Software, LLC", "Neon White")]
[assembly: MelonColor(204, 255, 138, 25)] 

namespace GiftHunt
{
    public class Core : MelonMod
    {
        internal static Game GameInstance { get; private set; }
        internal static new HarmonyLib.Harmony HarmonyInstance { get; private set; }

        public override void OnLateInitializeMelon()
        {
            Settings.Register();

            GameInstance = Singleton<Game>.Instance;
            HarmonyInstance = new HarmonyLib.Harmony("joeyexists.GiftHunt");

            GameInstance.OnLevelLoadComplete += GiftManager.OnLevelLoadComplete;

            GiftManager.UpdateGiftSpawn(Settings.giftSeedEntry.Value);

            UITextManager.Initialize();

            GameInstance.OnInitializationComplete += () =>
            {
                GiftManager.CacheGiftActors();
                DoPatches();
            };
        }

        public override void OnUpdate()
        {
            if (InputManager.GetKeyDown(Settings.hideGiftKeyEntry.Value))
            {
                string giftSeed = GiftManager.TryGenerateGiftSeed();
                if (!string.IsNullOrEmpty(giftSeed))
                {
                    GUIUtility.systemCopyBuffer = giftSeed;
                    UITextManager.PopupText.DisplayMessage("Gift Seed Copied to Clipboard!");
                }
            }

            if (InputManager.GetKeyDown(Settings.loadGiftKeyEntry.Value))
            {
                if (GiftManager.UpdateGiftSpawn(GUIUtility.systemCopyBuffer))
                {
                    if (GiftManager.TryLoadLevel(GiftManager.activeGiftSpawn.targetLevelId))
                        UITextManager.PopupText.DisplayMessage("Gift Seed Loaded from Clipboard!");
                }
            }
        }

        public static class Settings
        {
            public static MelonPreferences_Entry<string> giftSeedEntry;
            public static MelonPreferences_Entry<KeyCode> hideGiftKeyEntry;
            public static MelonPreferences_Entry<KeyCode> loadGiftKeyEntry;

            public static void Register()
            {
                var category = MelonPreferences.CreateCategory("Gift Hunt");

                giftSeedEntry = category.CreateEntry("Gift Seed", "",
                    description: "Loads level automatically.");
                hideGiftKeyEntry = category.CreateEntry("Copy Gift Seed Hotkey", KeyCode.RightShift,
                    description: "Generates a gift seed based on your current position and copies it to the clipboard.");
                loadGiftKeyEntry = category.CreateEntry("Load Gift Seed Hotkey", KeyCode.Backslash,
                    description: "Loads a gift seed from your clipboard.");

                giftSeedEntry.OnEntryValueChanged.Subscribe((_, newGiftSeed) => GiftManager.OnGiftSeedEntryValueChanged(newGiftSeed));
            }
        }

        private static void DoPatches()
        {
            var MechController_DoCardPickup_Original = AccessTools.Method(
                typeof(MechController), "DoCardPickup");
            var MechController_DoCardPickup_Prefix = AccessTools.Method(
                typeof(GiftManager), "MechController_DoCardPickup_Prefix");

            var MenuScreenOptionsPanel_ApplyChanges_Original = AccessTools.Method(
                typeof(MenuScreenOptionsPanel), "ApplyChanges");
            var MenuScreenOptionsPanel_ApplyChanges_Postfix = AccessTools.Method(
                typeof(UITextManager), "MenuScreenOptionsPanel_ApplyChanges_Postfix");

            HarmonyInstance.Patch(
                MechController_DoCardPickup_Original,
                prefix: new HarmonyMethod(MechController_DoCardPickup_Prefix));

            HarmonyInstance.Patch(
                MenuScreenOptionsPanel_ApplyChanges_Original,
                postfix: new HarmonyMethod(MenuScreenOptionsPanel_ApplyChanges_Postfix));
        }
    }
}