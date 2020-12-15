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
