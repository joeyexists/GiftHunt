using UnityEngine;

namespace GiftHunt.Gifts
{
    internal class AmbienceUpdater : MonoBehaviour
    {
        private AudioObjectAmbience ambience;

        public void Initialize(AudioObjectAmbience ambience) => 
            this.ambience = ambience;

        private void Update() => ambience?.OnUpdate(0f);
    }
}
