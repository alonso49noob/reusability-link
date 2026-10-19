using System;
using System.Collections.Generic;
using ReusabilityLink.Core;

namespace ReusabilityLink
{
    /// <summary>
    /// Every physics frame: decides physics vs rails for each inactive tracked vessel (PhysicsPolicy)
    /// and applies it through the vessel's own VesselRanges, which KSP evaluates in
    /// OrbitPhysicsManager (pack/unpack) and Vessel.Update (load/unload).
    /// </summary>
    public sealed class RangeController
    {
        readonly LinkSettings settings;
        readonly TrackingService tracking;
        readonly ThrottleMemory throttles;
        readonly Dictionary<Guid, PolicyState> states = new Dictionary<Guid, PolicyState>();
        double lastUT = double.NaN;
        double lookaheadSeconds;

        /// <summary>Fired with (vessel, previous mode, new mode).</summary>
        public event Action<Vessel, LinkMode, LinkMode> ModeChanged;

        public RangeController(LinkSettings settings, TrackingService tracking, ThrottleMemory throttles)
        {
            this.settings = settings;
            this.tracking = tracking;
            this.throttles = throttles;
        }

        public LinkMode ModeOf(Vessel v) =>
            v != null && states.TryGetValue(v.id, out var state) ? state.Mode : LinkMode.Physics;

        public void FixedTick(double dt)
        {
            var active = FlightGlobals.ActiveVessel;
            if (active == null) return;

            // Game time per tick is measured, not derived: it covers physics and rails warp alike.
            double ut = Planetarium.GetUniversalTime();
            double gameSecondsPerTick = double.IsNaN(lastUT) ? dt : Math.Max(0, ut - lastUT);
            lastUT = ut;
            lookaheadSeconds = PhysicsPolicy.LookaheadHorizon(settings, gameSecondsPerTick);

            var live = new HashSet<Guid>();
            foreach (var v in tracking.Tracked)
            {
                live.Add(v.id);
                if (!states.TryGetValue(v.id, out var state))
                    states[v.id] = state = new PolicyState();

                var before = state.Mode;
                LinkMode mode;
                if (v == active)
                {
                    state.Mode = mode = LinkMode.Physics;
                    state.CalmSeconds = 0;
                }
                else
                {
                    mode = PhysicsPolicy.Step(state, ReadInput(v, active), dt, settings);
                }

                Apply(v, RangePlanner.ForMode(mode, settings));
                if (mode != before)
                {
                    Log.Info($"{v.vesselName}: {before} → {mode} (q={AtmosphereSampler.CurrentQ(v):F0} Pa, d={Distance(v, active) / 1000:F0} km)");
                    ModeChanged?.Invoke(v, before, mode);
                }
            }

            if (states.Count > live.Count)
                foreach (var id in new List<Guid>(states.Keys))
                    if (!live.Contains(id)) states.Remove(id);
        }

        PolicyInput ReadInput(Vessel v, Vessel active)
        {
            return new PolicyInput
            {
                DynamicPressurePa = AtmosphereSampler.CurrentQ(v),
                PredictedDynamicPressurePa = AtmosphereSampler.PredictedMaxQ(v, lookaheadSeconds),
                StaticPressureKPa = AtmosphereSampler.StaticPressureKPa(v.mainBody, v.altitude),
                // A vessel left burning must stay unpacked even while packing zeroed its thrust.
                EngineThrusting = KspAdapters.EngineThrusting(v)
                    || (throttles.Held(v.id) > 0f && KspAdapters.HasIgnitedEngine(v)),
                LandedOrSplashed = v.LandedOrSplashed,
                SurfaceSpeed = v.srfSpeed,
                DistanceM = Distance(v, active),
            };
        }

        internal static double Distance(Vessel v, Vessel active) =>
            Vector3d.Distance(v.GetWorldPos3D(), active.GetWorldPos3D());

        static void Apply(Vessel v, RangeValues r)
        {
            var ranges = v.vesselRanges;
            if (ranges == null) return;
            Apply(ranges.prelaunch, r);
            Apply(ranges.landed, r);
            Apply(ranges.splashed, r);
            Apply(ranges.flying, r);
            Apply(ranges.subOrbital, r);
            Apply(ranges.orbit, r);
            Apply(ranges.escaping, r);
        }

        static void Apply(VesselRanges.Situation s, RangeValues r)
        {
            s.load = r.Load;
            s.unload = r.Unload;
            s.pack = r.Pack;
            s.unpack = r.Unpack;
        }
    }
}
