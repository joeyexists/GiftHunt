using MelonLoader;
using UnityEngine;
using UniverseLib.Input;

[assembly: MelonInfo(typeof(GiftHunt.Core), "GiftHunt", "1.0.0-rc.4", "joeyexists", null)]
[assembly: MelonGame("Little Flag Software, LLC", "Neon White")]
[assembly: MelonColor(204, 255, 138, 25)] 

namespace GiftHunt
{
    public class Core : MelonMod
    {
        internal static Game GameInstance { get; private set; }

        public override void OnLateInitializeMelon()
        {
            Settings.Register();

            GameInstance = Singleton<Game>.Instance;
            GameInstance.OnLevelLoadComplete += GiftManager.OnLevelLoadComplete;

            var harmony = new HarmonyLib.Harmony("com.joeyexists.gifthunt");
            harmony.PatchAll();

            GiftManager.CacheGiftActors();
            GiftManager.UpdateGiftSeed(Settings.giftSeedEntry.Value);

            UITextManager.Initialize();
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
                if (GiftManager.UpdateGiftSeed(GUIUtility.systemCopyBuffer) 
                    && GiftManager.CanLoadLevel())
                {
                    GiftManager.LoadLevel(GiftManager.currentGiftSeed.levelId);
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

                giftSeedEntry.OnEntryValueChanged.Subscribe(GiftManager.OnGiftSeedEntryValueChanged);
            }
        }
    }
}