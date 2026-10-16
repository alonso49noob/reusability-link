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

        static void Prefix(Vessel __instance, out bool __state)
        {
            __state = FlightGlobals.overrideOrbit;
            if (Protected.Contains(__instance.id))
                FlightGlobals.overrideOrbit = true;
        }

        static void Finalizer(bool __state)
        {
            FlightGlobals.overrideOrbit = __state;
        }
    }
}
