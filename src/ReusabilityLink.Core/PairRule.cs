namespace ReusabilityLink.Core
{
    /// <summary>
    /// A link needs two vessels. A vessel left tracked on its own (its partner landed, was lost or
    /// docked) is released, unless the user pinned it by hand or releasing it could lose it.
    /// </summary>
    public static class PairRule
    {
        public static bool ShouldRelease(int trackedCount, bool isActive, bool isSafe, bool manuallyPinned) =>
            trackedCount == 1 && !manuallyPinned && (isActive || isSafe);

        /// <summary>True when stock KSP would keep the vessel even after unloading it.</summary>
        public static bool IsSafeAlone(FlightSituation situation, double periapsisM, double atmosphereDepthM)
        {
            switch (situation)
            {
                case FlightSituation.Landed:
                case FlightSituation.Splashed:
                case FlightSituation.Prelaunch:
                    return true;
                case FlightSituation.Orbiting:
                case FlightSituation.Escaping:
                    return periapsisM >= atmosphereDepthM;
                default:
                    return false;
            }
        }
    }
}
