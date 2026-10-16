using System;
using System.Collections.Generic;
using HarmonyLib;

namespace RLProbe
{
    // Skips only the "on rails inside atmosphere" branch of Vessel.CheckKill for protected vessels:
    // that branch is guarded by !FlightGlobals.overrideOrbit, the terrain-crash branch is not.
    [HarmonyPatch(typeof(Vessel), nameof(Vessel.CheckKill))]
    public static class ProbeKillGuard
    {
        public static readonly HashSet<Guid> Protected = new HashSet<Guid>();

        // __state: whether this patch raised the flag; only then does it lower it again,
        // so it composes with other patches doing the same (Harmony runs finalizers in prefix order).
        static void Prefix(Vessel __instance, out bool __state)
        {
            __state = Protected.Contains(__instance.id) && !FlightGlobals.overrideOrbit;
            if (__state) FlightGlobals.overrideOrbit = true;
        }

        static void Finalizer(bool __state)
        {
            if (__state) FlightGlobals.overrideOrbit = false;
        }
    }
}
