using StressLevelZero.Pool;
using StressLevelZero.Props.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grenades
{
    [Harmony.HarmonyPatch(typeof(PoolManager), "Spawn", new Type[] { typeof(string), typeof(Vector3), typeof(Quaternion), typeof(Il2CppSystem.Nullable<bool>) })]
    [Harmony.HarmonyPatch(typeof(PoolManager), "Spawn", new Type[] { typeof(string), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Il2CppSystem.Nullable<bool>) })]
    internal class SpawnGunFirePatch
    {
        public static void Postfix(GameObject __result)
        {
            // im shidsting
        }

        static bool _hasPatched = false;
        void Awake()
        {
            if (!_hasPatched)
            {
                _hasPatched = true;
                var inst = Harmony.HarmonyInstance.Create("Spacemap");
                inst.PatchAll(GetType().Assembly);
            }
        }
    }
}
