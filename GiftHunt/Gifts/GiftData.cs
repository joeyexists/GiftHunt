using UnityEngine;

namespace GiftHunt.Gifts
{
    internal class GiftData(Vector3? spawnPosition = null, string targetLevelId = null, long devTimeMs = 0)
    {
        public Vector3? SpawnPosition { get; } = spawnPosition;
        public string TargetLevelId { get; } = targetLevelId;
        public long DevTimeMs { get; } = devTimeMs;

        public long PersonalBestMs { get; set; } = 0;
    }
}
