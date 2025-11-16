using MelonLoader;
using UnityEngine;
using GiftHunt.Gifts;

namespace GiftHunt
{
    public static class Settings
    {
        public static MelonPreferences_Entry<bool> ModEnabledEntry { get; private set; }

        public static MelonPreferences_Entry<string> GiftSeedEntry { get; private set; }

        public static MelonPreferences_Entry<KeyCode> HideGiftKeyEntry { get; private set; }
        public static MelonPreferences_Entry<KeyCode> LoadGiftKeyEntry { get; private set; }

        public static void Initialize()
        {
            var category = MelonPreferences.CreateCategory(GiftHunt.ModInstance.Info.Name);

            ModEnabledEntry = category.CreateEntry("Enabled", true);

            GiftSeedEntry = category.CreateEntry("Gift Seed", "",
                description: "Paste a gift seed here to instantly load the corresponding level.");

            HideGiftKeyEntry = category.CreateEntry("Copy Gift Seed Hotkey", KeyCode.RightShift,
                description: "Generates a gift seed based on your current position and copies it to the clipboard.");
            LoadGiftKeyEntry = category.CreateEntry("Load Gift Seed Hotkey", KeyCode.Backslash,
                description: "Loads a gift seed from your clipboard.");

            ModEnabledEntry.OnEntryValueChanged.Subscribe((_, enable) =>
                GiftHunt.SetModActive(enable));

            GiftSeedEntry.OnEntryValueChanged.Subscribe((_, newSeed) => 
                GiftManager.OnGiftSeedEntryValueChanged(newSeed));
        }
    }
}
