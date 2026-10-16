using HarmonyLib;

namespace ReusabilityLink
{
    /// <summary>
    /// Keeps KSP from destroying tracked vessels that are packed inside the atmosphere (spec §3.3).
    /// Vessel.CheckKill only takes that branch when FlightGlobals.overrideOrbit is false; the
    /// terrain-crash branch ignores the flag, so crashes through the ground still happen.
    /// </summary>
    public static class KillGuard
    {
        public static bool IsProtected(Vessel v)
        {
            var module = KspAdapters.Module(v);
            return module != null && module.tracked;
        }
    }

    [HarmonyPatch(typeof(Vessel), nameof(Vessel.CheckKill))]
    internal static class KillGuardPatch
    {
        static void Prefix(Vessel __instance, out bool __state)
        {
            __state = FlightGlobals.overrideOrbit;
            if (KillGuard.IsProtected(__instance))
                FlightGlobals.overrideOrbit = true;
        }

        static void Finalizer(bool __state)
        {
            FlightGlobals.overrideOrbit = __state;
        }
    }
}
