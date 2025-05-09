using HarmonyLib;
using MelonLoader;
using System.Text;
using UnityEngine;

namespace GiftHunt
{
    internal static class GiftManager
    {
        private static readonly List<ActorData> giftActorPool = [];
        public static (Vector3? giftPosition, string levelId) 
            currentGiftSeed = (null, null);

        private static GameObject spawnedGift = null;

        private const float GiftBoxScaleFactor = 0.75f;
        private const float GiftAmbienceRadius = 15.9f;
        private const float DistanceFromSpawnThreshold = 6f;

        private static bool canCollectGift = true;
        private static DateTime lastSeedUpdateTime = DateTime.MinValue;

        // states from which a level can be loaded
        private static readonly HashSet<MainMenu.State> ValidStates =
        [
            MainMenu.State.None,
            MainMenu.State.Map,
            MainMenu.State.Location,
            MainMenu.State.Level,
            MainMenu.State.Mission,
            MainMenu.State.Staging,
            MainMenu.State.Pause
        ];

        public static void OnLevelLoadComplete()
        {
            if (UITextManager.TimeText.visible)
                UITextManager.TimeText.FadeOut();

            // destory existing gift hunt gifts to make "compatible" with super restart
            if (spawnedGift != null)
            {
                UnityEngine.Object.Destroy(spawnedGift);
            }

            if (CanSpawnGift())
            {
                canCollectGift = true;
                SpawnGift(currentGiftSeed.giftPosition.Value);
            }
        }

        public static void OnGiftSeedEntryValueChanged(string oldValue, string newValue)
        {
            if (UpdateGiftSeed(newValue) && CanLoadLevel())
            {
                LoadLevel(currentGiftSeed.levelId);
            }
        }

        public static void CacheGiftActors()
        {
            var allActors = Resources.FindObjectsOfTypeAll<ActorData>();

            giftActorPool.AddRange(allActors.Where(actor => actor != null &&
                (actor.name == "Actor_Yellow" || actor.name == "Actor_Red" || actor.name == "Actor_Violet"
                || actor.name == "Actor_Mikey" || actor.name == "Actor_Raz"))
            );
        }

        public static bool UpdateGiftSeed(string newSeed)
        {
            // 1-second cooldown so you cant spam load levels
            if ((DateTime.Now - lastSeedUpdateTime).TotalSeconds < 1)
                return false;

            if (string.IsNullOrEmpty(newSeed))
            {
                currentGiftSeed = (null, null);
                return false;
            }

            var decodedSeed = TryDecodeGiftSeed(newSeed.Trim());
            if (decodedSeed.giftPosition == null || decodedSeed.levelId == null)
            {
                MelonLogger.Msg($"Failed to load gift: invalid gift seed '{newSeed}'.");
                UITextManager.PopupText.DisplayMessage("Invalid gift seed.");
                return false;
            }

            currentGiftSeed = decodedSeed;
            lastSeedUpdateTime = DateTime.Now;

            return true;
        }

        private static bool CanSpawnGift()
        {
            if (currentGiftSeed.giftPosition == null || currentGiftSeed.levelId == null)
                return false;

            if (giftActorPool.Count == 0)
                return false;

            if (Core.GameInstance.GetCurrentLevel().levelID != currentGiftSeed.levelId)
                return false;

            if (LevelRush.IsLevelRush())
                return false;

            return true;
        }

        private static void SpawnGift(Vector3 position)
        {
            ActorData giftActor = giftActorPool[UnityEngine.Random.Range(0, giftActorPool.Count)];

            CardPickup gift = CardPickup.SpawnPickupCollectible(giftActor, position, Quaternion.Euler(0f, 344.686f, 0f));

            spawnedGift = gift.gameObject;

            AudioObjectAmbience ambience = gift.GetComponent<AudioObjectAmbience>();
            ambience.UpdateSFXRadiusOverride(GiftAmbienceRadius);

            // update the gift's ambience manually (scuffed)
            AmbienceUpdater updater = gift.gameObject.AddComponent<AmbienceUpdater>();
            updater.Initialize(ambience);

            gift.name = "GiftHunt_SpawnedGift";

            Vector3 giftScale = Vector3.one * GiftBoxScaleFactor;
            gift.giftBox.transform.parent.transform.localScale = giftScale;

            ParticleSystem particleSystem = gift.giftBox.GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null)
                particleSystem.transform.localScale = giftScale;
        }

        public static bool CanLoadLevel()
        {
            if (LevelRush.IsLevelRush())
                return false;

            if (MainMenu.Instance() is not MainMenu menu)
                return false;

            MainMenu.State menuState = menu.GetCurrentState();

            if (!ValidStates.Contains(menuState))
            {
                UITextManager.PopupText.DisplayMessage($"Cannot load level from menu '{menuState}'.", 1f);
                return false;
            }

            return true;
        }

        public static void LoadLevel(string levelId)
        {
            if (Core.GameInstance.GetCurrentLevel() is not LevelData currentLevel)
                return;
            if (Core.GameInstance.GetGameData() is not GameData gameData)
                return;
            if (gameData.GetLevelData(levelId) is not LevelData targetLevel)
                return;

            if (currentLevel.levelID == targetLevel.levelID)
            {
                // restart level
                Core.GameInstance.PlayLevel(targetLevel, fromArchive: true, fromRestart: true);
            }
            else
            {
                // load level normally
                Core.GameInstance.PlayLevel(targetLevel, fromArchive: true, fromRestart: false);
            }
        }

        public static string TryGenerateGiftSeed()
        {
            if (GameObject.Find("Player") is not GameObject player)
            {
                MelonLogger.Msg("Gift seed generation failed: player not found.");
                return null;
            }

            if (Core.GameInstance.GetCurrentLevel() is not LevelData currentLevel)
            {
                MelonLogger.Msg("Gift seed generation failed: current level is null.");
                return null;
            }

            if (GameObject.Find("Teleport_START") is not GameObject playerSpawn)
            {
                MelonLogger.Msg("Gift seed generation failed: player spawn is null.");
                return null;
            }

            Vector3 giftPosition = player.transform.position;
            Vector3 spawnPosition = playerSpawn.transform.position;

            float giftDistanceFromSpawn = Vector3.Distance(giftPosition, spawnPosition);
            if (giftDistanceFromSpawn <= DistanceFromSpawnThreshold)
            {
                MelonLogger.Msg($"Gift seed generation failed: gift position is too close to player spawn.");
                UITextManager.PopupText.DisplayMessage("Too close to spawn!");
                return null;
            }

            string levelName = LevelNameMapper.GetDisplayName(currentLevel.levelID);
            if (string.IsNullOrEmpty(levelName))
            {
                MelonLogger.Msg($"Gift seed generation failed: invalid level '{currentLevel.levelID}'.");
                UITextManager.PopupText.DisplayMessage("Invalid level.");
                return null;
            }

            // each coordinate is allocated 18 bits, 2^18 -> range of [0, 262,143]
            // coordinates multiplied by 10 to preserve 1 decimal place -> range of [0, 26,214.3]
            // shift by 13,107.15 for balanced position range -> [-13,107.15, 13,107.15]
            int x = (int)Math.Round((giftPosition.x + 13107.15f) * 10f);
            int y = (int)Math.Round((giftPosition.y + 13107.15f) * 10f);
            int z = (int)Math.Round((giftPosition.z + 13107.15f) * 10f);

            // check if coordinates are within range
            if (x < 0 || x > 262143 || y < 0 || y > 262143 || z < 0 || z > 262143)
            {
                UITextManager.PopupText.DisplayMessage("Invalid position.");
                return null;
            }

            // convert to uint first to guarantee unsigned 32-bit values
            ulong packed = ((ulong)(uint)x << 36) | ((ulong)(uint)y << 18) | (ulong)(uint)z;

            System.Random rng = new();
            ulong randomBits = ((ulong)rng.Next(0, 1024)) << 54; // randomly fill first 10 bits
            packed |= randomBits; // combine position bits and random bits

            byte[] bytes = BitConverter.GetBytes(packed);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            string positionEncoded = Convert.ToBase64String(bytes);

            string giftSeed = $"{levelName}:{positionEncoded}";

            MelonLogger.Msg($"Generated gift seed for level '{levelName}' at position {giftPosition}: \n\"{giftSeed}\"");

            return giftSeed;
        }

        public static (Vector3? giftPosition, string levelId) TryDecodeGiftSeed(string giftSeed)
        {
            try
            {
                string[] parts = giftSeed.Split(':');
                
                if (parts.Length != 2)
                    return (null, null);

                string levelId = LevelNameMapper.GetLevelId(parts[0]);

                if (levelId == null)
                    return (null, null);

                byte[] bytes = Convert.FromBase64String(parts[1]);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                ulong packed = BitConverter.ToUInt64(bytes, 0) & 0x003FFFFFFFFFFFFF;

                int x = (int)((packed >> 36) & 0x3FFFF);
                int y = (int)((packed >> 18) & 0x3FFFF);
                int z = (int)(packed & 0x3FFFF);

                Vector3 seedPosition = new(
                    (x / 10f) - 13107.15f,
                    (y / 10f) - 13107.15f,
                    (z / 10f) - 13107.15f
                );

                //MelonLogger.Msg($"Position at ({seedPosition.x}, {seedPosition.y}, {seedPosition.z})");

                string decodedPositionString = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                string[] positionParts = decodedPositionString.Split(',');

                return (seedPosition, levelId);
            }
            catch
            {
                return (null, null);
            }
        }

        public class AmbienceUpdater : MonoBehaviour
        {
            private AudioObjectAmbience ambience;

            public void Initialize(AudioObjectAmbience ambience)
            {
                this.ambience = ambience;
            }
            private void Update()
            {
                ambience?.OnUpdate(0f);
            }
        }

        /* this patch would stop the gift counter from incrementing, but i decided not to include it
           because it would also affect regular gifts */
        /*
        [HarmonyPatch(typeof(GameDataManager), "OnGiftCollect")]
        public static class OnGiftCollectPatch
        {
            [HarmonyPrefix]
            static bool Prefix()
            {
                return false; // skip gift collection logic
            }
        }
        */

        [HarmonyPatch(typeof(MechController), "DoCardPickup")]
        public static class OnGiftPickupPatch
        {
            [HarmonyPrefix]
            static bool Prefix(PlayerCardData card)
            {
                if (canCollectGift && card != null && card.consumableType == PlayerCardData.ConsumableType.GiftCollectible)
                {
                    canCollectGift = false; // stops this from running twice
                    long giftTimeMicroseconds = Core.GameInstance.GetCurrentLevelTimerMicroseconds();
                    if (giftTimeMicroseconds > 16666) // 1 frame 
                    {
                        string timerText = NeonLite.Helpers.FormatTime(giftTimeMicroseconds / 1000, null, '.');
                        UITextManager.TimeText.DisplayTime(timerText);
                    }
                }

                return true; // continue with original method
            }
        }
    }
}
