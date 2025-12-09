using UnityEngine;
using GiftHunt.LevelUtils;

namespace GiftHunt.Gifts
{
    internal static class GiftSeedUtility
    {
        const float MaxDevTimeMs = 600_000f; // 10 minutes
        const float DistanceFromSpawnThreshold = 6f;

        public static bool TryCreateGiftSeed(out string seed, out string error)
        {
            seed = null;
            error = null;

            if (LevelRush.IsLevelRush())
            {
                error = "Cannot generate gift seed in level rush.";
                return false;
            }

            var level = GiftHunt.Game.GetCurrentLevel();
            if (level == null)
            {
                error = "Current level is null.";
                return false;
            }

            var levelName = LevelNameMapper.GetDisplayName(level.levelID);
            if (string.IsNullOrWhiteSpace(levelName))
            {
                error = "Invalid level.";
                return false;
            }

            if (RM.drifter == null)
            {
                error = "Player not found.";
                return false;
            }

            if(!TryGetSpawnPosition(out var spawnPos))
            {
                error = "Player spawn not found.";
                return false;
            }

            var playerPos = RM.playerPosition;
            float distFromSpawn = Vector3.Distance(playerPos, spawnPos);

            if (distFromSpawn <= DistanceFromSpawnThreshold)
            {
                error = "Too close to spawn.";
                return false;
            }

            var posEncoded = Base64Utils.PackPosition(playerPos);
            if (string.IsNullOrWhiteSpace(posEncoded))
            {
                error = "Invalid position.";
                return false;
            }

            long devTimeMs = CalculateDevTimeMs();

            seed = $"{levelName}:{posEncoded}";
            if (devTimeMs > 0)
                seed += $":{Base64Utils.PackUInt((uint)devTimeMs)}";

            return true;
        }

        public static string UpdateGiftSeedWithNewDevTime(string seed, long devTimeMs)
        {
            string[] parts = seed.Split(':');
            if (parts.Length != 3 && string.IsNullOrWhiteSpace(parts[2]))
                return null; // seed has no dev time

            devTimeMs = (long)Mathf.Clamp(devTimeMs, 0, MaxDevTimeMs);
            var devTimeEncoded = Base64Utils.PackUInt((uint)devTimeMs);

            parts[2] = devTimeEncoded;

            return string.Join(":", parts);
        }

        public static bool TryDecodeGiftSeed(string seed, out GiftData giftData)
        {
            giftData = null;

            if (string.IsNullOrWhiteSpace(seed))
                return false;

            seed = seed.Trim();
            string[] parts = seed.Split(':');

            // account for seeds with no dev time recorded
            if (parts.Length != 2 && parts.Length != 3) 
                return false;
            
            string levelId = LevelNameMapper.GetLevelId(parts[0]);
            if (levelId == null) return false;

            var position = Base64Utils.UnpackPosition(parts[1]);
            if (!position.HasValue) return false;

            long devTimeMs = 0;
            if (parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]))
            {
                devTimeMs = Base64Utils.UnpackUInt(parts[2]);

                if (devTimeMs < 0 || devTimeMs > MaxDevTimeMs)
                    devTimeMs = 0;
            }

            giftData = new GiftData(position, levelId, devTimeMs, seed);
            return true;
        }

        private static bool TryGetSpawnPosition(out Vector3 spawnPos)
        {
            spawnPos = Vector3.zero;
            var teleports = UnityEngine.Object.FindObjectsOfType<PlayerTeleport>();
            foreach (var teleport in teleports)
            {
                if (teleport.id == "START")
                {
                    spawnPos = teleport.transform.position;
                    return true;
                }
            }
            return false;
        }

        private static long CalculateDevTimeMs()
        {
            const float FrameDurationMs = 1000f / 60f;
            const float GiftRadius = 3f * GiftManager.GiftBoxScaleFactor;
            const float LateralThreshold = 18.75f;

            var timerMs = GiftHunt.Game.GetCurrentLevelTimerMicroseconds() / 1000f;
            var velocity = RM.drifter.Motor.BaseVelocity;
            var lateral = new Vector2(velocity.x, velocity.z).magnitude;

            if (lateral < LateralThreshold)
                return (long)Mathf.Clamp(timerMs, 0, MaxDevTimeMs);

            // estimate time saved by hitting the edge of the gift rather than its center
            var offsetMs = GiftRadius / lateral * 1000f;

            //round to nearest frame
            offsetMs = Mathf.Floor(offsetMs / FrameDurationMs) * FrameDurationMs;

            return (long)Mathf.Clamp(timerMs - offsetMs, 0, MaxDevTimeMs);
        }

        private static class Base64Utils
        {
            private static readonly System.Random rng = new();
            private const float CoordinateOffset = 13107.15f;

            public static string PackPosition(Vector3 position)
            {
                // each coordinate is allocated 18 bits, 2^18 -> range of [0, 262,143]
                // coordinates multiplied by 10 to preserve 1 decimal place -> range of [0, 26,214.3]
                // offset by 13,107.15 for balanced position range -> [-13,107.15, 13,107.15]
                int x = (int)Mathf.Round((position.x + CoordinateOffset) * 10f);
                int y = (int)Mathf.Round((position.y + CoordinateOffset) * 10f);
                int z = (int)Mathf.Round((position.z + CoordinateOffset) * 10f);

                // check if coordinates are within range
                if (x < 0 || x > 262143 || y < 0 || y > 262143 || z < 0 || z > 262143)
                    return null;

                // convert to uint first to guarantee unsigned 32-bit values
                ulong packed = (ulong)(uint)x << 36 | (ulong)(uint)y << 18 | (uint)z;

                ulong randomBits = (ulong)rng.Next(0, 1024) << 54; // randomly fill first 10 bits
                packed |= randomBits; // combine position bits and random bits

                byte[] bytes = BitConverter.GetBytes(packed);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                return Convert.ToBase64String(bytes).TrimEnd('=');
            }

            public static Vector3? UnpackPosition(string base64)
            {
                int padding = 4 - (base64.Length % 4);
                if (padding < 4) base64 = base64 + new string('=', padding);

                byte[] bytes;
                try { bytes = Convert.FromBase64String(base64); }
                catch { return null; } // invalid base64

                if (bytes.Length < 8) return null; // too short to unpack

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                ulong packed = BitConverter.ToUInt64(bytes, 0) & 0x003FFFFFFFFFFFFF;

                int x = (int)(packed >> 36 & 0x3FFFF);
                int y = (int)(packed >> 18 & 0x3FFFF);
                int z = (int)(packed & 0x3FFFF);

                return new Vector3(
                    x / 10f - CoordinateOffset,
                    y / 10f - CoordinateOffset,
                    z / 10f - CoordinateOffset
                );
            }

            public static string PackUInt(uint value)
            {
                int bytesNeeded = 1;
                if (value > 0xFF) bytesNeeded = 2;
                if (value > 0xFFFF) bytesNeeded = 3;
                if (value > 0xFFFFFF) bytesNeeded = 4;

                byte[] bytes = new byte[bytesNeeded];
                for (int i = 0; i < bytesNeeded; i++)
                {
                    bytes[i] = (byte)(value >> 8 * (bytesNeeded - i - 1));
                }

                return Convert.ToBase64String(bytes).TrimEnd('=');
            }

            public static uint UnpackUInt(string base64)
            {
                int padding = 4 - (base64.Length % 4);
                if (padding < 4) base64 = base64 + new string('=', padding);

                byte[] bytes;
                try { bytes = Convert.FromBase64String(base64); }
                catch { return 0; } // invalid base64

                uint value = 0;
                for (int i = 0; i < bytes.Length; i++)
                    value = value << 8 | bytes[i];

                return value;
            }
        }
    }
}
