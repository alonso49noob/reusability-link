using HarmonyLib;
using ReusabilityLink.Core;

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
        // __state: whether this patch raised the flag. Other mods may patch CheckKill the same way.
        static void Prefix(Vessel __instance, out bool __state)
        {
            __state = FlagGuard.Enter(ref FlightGlobals.overrideOrbit, KillGuard.IsProtected(__instance));
        }

        static void Finalizer(bool __state)
        {
            FlagGuard.Exit(ref FlightGlobals.overrideOrbit, __state);
        }
    }
}
