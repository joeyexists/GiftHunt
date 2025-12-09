using UnityEngine;

namespace GiftHunt.Gifts
{
    internal class GiftData(Vector3? spawnPosition = null, string targetLevelId = null, long devTimeMs = 0, string seed = null)
    {
        public Vector3? SpawnPosition { get; } = spawnPosition;
        public string TargetLevelId { get; } = targetLevelId;
        public long DevTimeMs { get; } = devTimeMs;
        public string Seed { get; } = seed;

        public long PersonalBestMs { get; set; } = 0;
    }
}
