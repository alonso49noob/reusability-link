namespace ReusabilityLink.Core
{
    public enum FlightSituation { Prelaunch, Landed, Splashed, Flying, SubOrbital, Orbiting, Escaping, Docked }

    /// <summary>What the auto-track rule needs to know about a separation event.</summary>
    public struct SeparationInfo
    {
        public bool FromActiveVessel;
        public bool HasCommandModule;
        public bool HasEngine;
        public bool ActiveIsTracked;
        public FlightSituation ActiveSituation;
        /// <summary>Periapsis altitude above sea level of the active vessel.</summary>
        public double ActivePeriapsisM;
        /// <summary>Atmosphere depth of the body; 0 for airless bodies.</summary>
        public double AtmosphereDepthM;
        public int TrackedCount;
    }

    public enum AutoTrackResult { Track, Disabled, NotFromActive, NoCommand, NoEngine, NotAscending, LimitReached }

    /// <summary>Spec §3.1: auto-track only first stages with a control core, separated during ascent.</summary>
    public static class AutoTrackFilter
    {
        public static AutoTrackResult Evaluate(SeparationInfo info, LinkSettings s)
        {
            if (!s.AutoTrack) return AutoTrackResult.Disabled;
            if (!info.FromActiveVessel) return AutoTrackResult.NotFromActive;
            if (!info.HasCommandModule) return AutoTrackResult.NoCommand;
            if (s.AutoTrackRequireEngines && !info.HasEngine) return AutoTrackResult.NoEngine;
            if (!IsAscending(info)) return AutoTrackResult.NotAscending;

            int trackedAfter = info.TrackedCount + (info.ActiveIsTracked ? 0 : 1) + 1;
            if (trackedAfter > s.MaxTrackedVessels) return AutoTrackResult.LimitReached;
            return AutoTrackResult.Track;
        }

        static bool IsAscending(SeparationInfo info)
        {
            switch (info.ActiveSituation)
            {
                case FlightSituation.Flying:
                case FlightSituation.SubOrbital:
                    return true;
                case FlightSituation.Orbiting:
                    return info.ActivePeriapsisM < info.AtmosphereDepthM;
                default:
                    return false;
            }
        }
    }
}
