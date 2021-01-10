using StressLevelZero.Interaction;
using StressLevelZero.Props.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Log = MelonLoader.MelonLogger;

namespace WNP78.Grenades
{
    [Harmony.HarmonyPatch(typeof(HandWeaponSlotReciever), "MakeStatic")]
    static class Patches
    {
        /// <summary>
        /// This runs when the grenade is placed in a slot and notifies the grenade that it's in a slot. Because for some reason the onSlotInsert doesn't work?
        /// This is all so that it can remove itself from the slot when respawning
        /// </summary>
        /// <param name="__instance">The instance.</param>
        static void Postfix(HandWeaponSlotReciever __instance)
        {
            var host = __instance.m_WeaponHost;
            if (host != null)
            {
                var grenade = host.GetComponent<Grenade>();
                if (grenade != null)
                {
                    grenade.CurrentSlot = __instance;
                }
            }
        }
    }
}
