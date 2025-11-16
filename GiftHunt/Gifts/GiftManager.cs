using UnityEngine;
using GiftHunt.LevelUtils;
using GiftHunt.UI;

namespace GiftHunt.Gifts
{
    internal static class GiftManager
    {
        public static GiftData ActiveGiftData { get; private set; } = null;
        public static GameObject LastSpawnedGift { get; private set; } = null;

        public const float GiftBoxScaleFactor = 0.75f;
        private const float GiftAmbienceRadius = 15.9f;

        private static readonly List<ActorData> GiftActorPool = [];

        private static float lastSeedUpdateTime = -1f;

        public static void OnLevelLoadComplete()
        {
            if (!GiftHunt.IsModEnabled)
                return;

            FadeOutTimeFeedback();

            // destory last spawned gift to make "compatible" with super restart
            DestroyLastSpawnedGift();

            TrySpawnGift(ActiveGiftData);
        }

        public static void OnGiftSeedEntryValueChanged(string newSeed)
        {
            if (!GiftHunt.IsModEnabled)
                return;

            if (!TrySetActiveGiftData(newSeed, out var setGiftError))
            {
                if (!string.IsNullOrWhiteSpace(setGiftError))
                    PopupManager.InfoText?.DisplayMessage(setGiftError);
                return;
            }

            // active gift data set, try to load level
            if (!LevelLoader.TryLoadLevel(ActiveGiftData.TargetLevelId, out var loadError))
            {
                PopupManager.InfoText?.DisplayMessage(loadError ?? "Failed to load level.");
                return;
            }

            PopupManager.InfoText?.DisplayMessage("Gift Seed Loaded from Clipboard!");
        }

        public static void CacheGiftActors()
        {
            if (GiftActorPool.Count > 0) return;

            var validNames = new HashSet<string> { "Actor_Yellow", "Actor_Red", "Actor_Violet", "Actor_Mikey", "Actor_Raz" };

            var actors = Resources.FindObjectsOfTypeAll<ActorData>()
                .Where(actor => actor != null && validNames.Contains(actor.name));

            GiftActorPool.AddRange(actors);
        }

        public static bool TrySetActiveGiftData(string seed, out string error)
        {
            const float CooldownSeconds = 1.0f;
            error = null;

            if (LevelRush.IsLevelRush())
            {
                error = "Cannot load gift seed in level rush.";
                return false;
            }

            if (Time.unscaledTime - lastSeedUpdateTime < CooldownSeconds)
                return false; // prevent spam-loading levels

            seed = seed?.Trim();
            if (string.IsNullOrWhiteSpace(seed))
                return false; // ignore empty seeds

            if (!GiftSeedUtility.TryDecodeGiftSeed(seed, out var giftData))
            {
                error = "Invalid gift seed.";
                return false;
            }

            ActiveGiftData = giftData;
            lastSeedUpdateTime = Time.unscaledTime;
            return true;
        }

        private static bool TrySpawnGift(GiftData giftData)
        {
            if (!CanSpawnGift(giftData)) return false;

            var actor = GiftActorPool[UnityEngine.Random.Range(0, GiftActorPool.Count)];
            var gift = CardPickup.SpawnPickupCollectible(
                actor,
                giftData.SpawnPosition.Value,
                Quaternion.Euler(0f, 344.686f, 0f)
            );

            if (gift == null) return false;

            LastSpawnedGift = gift.gameObject;
            gift.name = "GiftHunt.SpawnedGift";

            if (gift.TryGetComponent(out AudioObjectAmbience ambience))
            {
                ambience.UpdateSFXRadiusOverride(GiftAmbienceRadius);

                // manually update the gift's ambience (scuffed)
                var updater = gift.gameObject.AddComponent<AmbienceUpdater>();
                updater.Initialize(ambience);
            }

            var giftScale = Vector3.one * GiftBoxScaleFactor;
            gift.gameObject.transform.localScale = giftScale;

            var particleSystem = gift.giftBox?.GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null)
                particleSystem.transform.localScale = giftScale;

            return true;
        }

        public static void DestroyLastSpawnedGift()
        {
            if (LastSpawnedGift == null) return;
            UnityEngine.Object.Destroy(LastSpawnedGift);
            LastSpawnedGift = null;
        }

        public static void OnGiftPickup()
        {
            AudioController.Play("ITEM_POPUP");
            DestroyLastSpawnedGift();

            long collectTimeMs = GiftHunt.Game.GetCurrentLevelTimerMicroseconds() / 1000;
            if (collectTimeMs <= 17) return; // ~1 frame

            long personalBestMs = ActiveGiftData?.PersonalBestMs ?? 0;
            if (personalBestMs == 0 || personalBestMs > collectTimeMs)
            {
                if (ActiveGiftData != null)
                    ActiveGiftData.PersonalBestMs = collectTimeMs;
            }

            long devTimeMs = ActiveGiftData?.DevTimeMs ?? 0;

            DisplayTimeFeedback(collectTimeMs, devTimeMs, personalBestMs);
        }

        private static bool CanSpawnGift(GiftData giftData)
        {
            // holy spaghetti
            if (giftData == null) return false;
            var currentLevel = GiftHunt.Game.GetCurrentLevel();
            return currentLevel != null &&
                   giftData.SpawnPosition.HasValue &&
                   !string.IsNullOrWhiteSpace(giftData.TargetLevelId) &&
                   currentLevel.levelID == giftData.TargetLevelId &&
                   GiftActorPool.Count > 0 &&
                   !LevelRush.IsLevelRush();
        }

        private static void DisplayTimeFeedback(long collectTimeMs, long devTimeMs, long personalBestMs)
        {
            PopupManager.TimeText?.DisplayMessagePersistent($"Gift found in {FormatTime(collectTimeMs)}");

            if (personalBestMs > 0)
            {
                var difference = collectTimeMs - personalBestMs;
                var isPB = difference <= 0;

                var differenceStr = $"{(isPB ? "-" : "+")}{FormatTime(Math.Abs(difference))}";

                // green if pb otherwise red
                var col = isPB ? new Color32(50, 255, 50, 255) : new Color32(255, 50, 50, 255);
                var outlineCol = isPB ? new Color32(0, 48, 0, 255) : new Color32(48, 0, 0, 255);

                PopupManager.DiffText?.SetColor(col, outlineCol);
                PopupManager.DiffText?.DisplayMessagePersistent(differenceStr);
            }

            if (devTimeMs <= 0 )
                PopupManager.DevTimeText?.DisplayMessagePersistent("No dev time recorded.");
            else
            {
                var beatDevTime = collectTimeMs <= devTimeMs || (personalBestMs <= devTimeMs && personalBestMs > 0);
                var devTimeMessage = beatDevTime
                    ? $"Dev time: {FormatTime(devTimeMs)}"
                    : "Try again for dev time!";

                PopupManager.DevTimeText?.DisplayMessagePersistent(devTimeMessage);
            }
        }

        private static void FadeOutTimeFeedback(float fadeDuration = 0.5f)
        {
            PopupManager.TimeText?.FadeOut(fadeDuration);
            PopupManager.DiffText?.FadeOut(fadeDuration);
            PopupManager.DevTimeText?.FadeOut(fadeDuration);
        }

        private static string FormatTime(long milliseconds)
        {
            long minutes = milliseconds / 60000;
            long seconds = (milliseconds % 60000) / 1000;
            long ms = milliseconds % 1000;

            return $"{minutes:D2}:{seconds:D2}.{ms:D3}";
        }
    }
}
