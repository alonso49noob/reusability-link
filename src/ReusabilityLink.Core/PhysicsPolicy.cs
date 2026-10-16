namespace ReusabilityLink.Core
{
    public enum LinkMode
    {
        /// <summary>Loaded and unpacked: full physics (aerodynamics, engines, collisions).</summary>
        Physics,
        /// <summary>Loaded but packed: exact orbit propagation, no physics.</summary>
        Rails,
        /// <summary>Needs physics but is beyond MaxPhysicsDistance, so it stays packed and the user is warned.</summary>
        OutOfRange,
    }

    /// <summary>Per-vessel memory of the policy. A freshly separated vessel starts in physics, as in stock KSP.</summary>
    public sealed class PolicyState
    {
        public LinkMode Mode = LinkMode.Physics;
        /// <summary>Continuous seconds spent in physics with q below RailsBelowDynamicPressure.</summary>
        public double CalmSeconds;
    }

    public struct PolicyInput
    {
        public double DynamicPressurePa;
        /// <summary>Highest q expected within LookaheadSeconds.</summary>
        public double PredictedDynamicPressurePa;
        public double StaticPressureKPa;
        public double SurfaceSpeed;
        /// <summary>Distance to the active vessel in metres.</summary>
        public double DistanceM;
        public bool EngineThrusting;
        public bool LandedOrSplashed;
    }

    /// <summary>Decides physics vs rails for an inactive tracked vessel (spec §3.2).</summary>
    public static class PhysicsPolicy
    {
        const double MinPredictionStep = 5;
        const int MaxPredictionSamples = 20;

        /// <summary>
        /// Game seconds to look ahead: LookaheadSeconds plus two physics frames, so that under rails
        /// warp a vessel cannot jump from vacuum into dense air between two decisions.
        /// </summary>
        public static double LookaheadHorizon(LinkSettings s, double gameSecondsPerTick) =>
            s.LookaheadSeconds + 2 * gameSecondsPerTick;

        /// <summary>Sampling step along the predicted path, keeping at most MaxPredictionSamples samples.</summary>
        public static double PredictionStep(double horizonSeconds) =>
            System.Math.Max(MinPredictionStep, horizonSeconds / MaxPredictionSamples);

        public static LinkMode Step(PolicyState state, PolicyInput input, double dt, LinkSettings s)
        {
            bool forced = input.DynamicPressurePa >= s.PhysicsOnDynamicPressure
                || input.PredictedDynamicPressurePa >= s.PhysicsOnDynamicPressure
                || input.StaticPressureKPa >= s.RailsMaxStaticPressureKPa
                || input.EngineThrusting
                || (input.LandedOrSplashed && input.SurfaceSpeed >= s.LandedSettleSpeed);

            bool wantsPhysics;
            if (forced)
            {
                wantsPhysics = true;
                state.CalmSeconds = 0;
            }
            else if (state.Mode == LinkMode.Physics)
            {
                if (input.DynamicPressurePa < s.RailsBelowDynamicPressure)
                {
                    state.CalmSeconds += dt;
                    wantsPhysics = state.CalmSeconds < s.RailsHoldSeconds;
                }
                else
                {
                    state.CalmSeconds = 0;
                    wantsPhysics = true;
                }
            }
            else
            {
                wantsPhysics = false;
            }

            if (!wantsPhysics)
            {
                state.Mode = LinkMode.Rails;
                state.CalmSeconds = 0;
            }
            else if (input.DistanceM > s.MaxPhysicsDistance)
            {
                state.Mode = LinkMode.OutOfRange;
                state.CalmSeconds = 0;
            }
            else
            {
                state.Mode = LinkMode.Physics;
            }
            return state.Mode;
        }
    }
}
