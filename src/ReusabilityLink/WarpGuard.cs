using ReusabilityLink.Core;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>
    /// Rails warp packs every vessel, which would drop an inactive tracked vessel's aerodynamics.
    /// While one of them needs physics, only physics warp is allowed (spec §3.7).
    /// </summary>
    public sealed class WarpGuard
    {
        const float MessageIntervalSeconds = 5f;

        readonly TrackingService tracking;
        readonly RangeController ranges;
        float nextMessageTime;

        public WarpGuard(TrackingService tracking, RangeController ranges)
        {
            this.tracking = tracking;
            this.ranges = ranges;
        }

        public void Tick()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH || TimeWarp.CurrentRateIndex == 0) return;

            var active = FlightGlobals.ActiveVessel;
            foreach (var v in tracking.Tracked)
            {
                if (v == active || ranges.ModeOf(v) != LinkMode.Physics) continue;
                TimeWarp.SetRate(0, true);
                if (Time.realtimeSinceStartup >= nextMessageTime)
                {
                    ScreenMessages.PostScreenMessage("Warp limitado: el booster necesita física", 4f);
                    nextMessageTime = Time.realtimeSinceStartup + MessageIntervalSeconds;
                }
                return;
            }
        }
    }
}
