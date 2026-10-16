namespace ReusabilityLink.Core
{
    /// <summary>
    /// Temporarily raises a shared boolean (FlightGlobals.overrideOrbit) so that several patches can
    /// stack: each one only lowers the flag again if it was the one that raised it.
    /// </summary>
    public static class FlagGuard
    {
        /// <returns>True when this call raised the flag and is responsible for lowering it.</returns>
        public static bool Enter(ref bool flag, bool protect)
        {
            if (!protect || flag) return false;
            flag = true;
            return true;
        }

        public static void Exit(ref bool flag, bool raised)
        {
            if (raised) flag = false;
        }
    }
}
