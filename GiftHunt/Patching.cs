using UnityEngine;
using HarmonyLib;
using System.Reflection;
using GiftHunt.Gifts;

namespace GiftHunt
{
    internal static class Patching
    {
        public static bool HasAnyPatches => HarmonyLib.Harmony.HasAnyPatches(GiftHunt.Harmony.Id);

        // CardPickup.OnTriggerStay
        private static readonly MethodInfo OnTriggerStayMethod =
            AccessTools.Method(typeof(CardPickup), "OnTriggerStay");

        private static readonly HarmonyMethod OnTriggerStayPrefixPatch =
            new(AccessTools.Method(typeof(Patching), nameof(OnTriggerStayPrefix)));

        private static readonly FieldInfo PickupAbleField =
            typeof(CardPickup).GetField("_pickupAble", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void PatchGame()
        {
            if (HasAnyPatches) return;

            GiftHunt.Harmony.Patch(OnTriggerStayMethod, OnTriggerStayPrefixPatch);
        }

        public static void UnpatchGame()
        {
            if (!HasAnyPatches) return;

            GiftHunt.Harmony.Unpatch(OnTriggerStayMethod, OnTriggerStayPrefixPatch.method);
        }

        internal static bool OnTriggerStayPrefix(CardPickup __instance, Collider c)
        {
            if (GiftManager.LastSpawnedGift != null &&
                GiftManager.LastSpawnedGift.TryGetComponent<CardPickup>(out var cardPickup))
            {
                if (__instance == cardPickup)
                {
                    // custom gift collided with
                    var pickupAbleValue = false;
                    if (PickupAbleField != null)
                        pickupAbleValue = (bool)PickupAbleField.GetValue(__instance);

                    if (pickupAbleValue)
                    {
                        GiftManager.OnGiftPickup();
                        return false; // skip original method
                    }
                }
            }

            return true; // regular cardpickup, run original method
        }
    }
}
