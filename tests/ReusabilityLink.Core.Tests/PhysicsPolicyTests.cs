using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class PhysicsPolicyTests
    {
        static readonly LinkSettings S = new LinkSettings();

        static PolicyInput In(double q = 0, double predicted = 0, double pressureKPa = 0, bool engine = false,
            bool landed = false, double speed = 0, double distance = 10000)
        {
            return new PolicyInput
            {
                DynamicPressurePa = q,
                PredictedDynamicPressurePa = predicted,
                StaticPressureKPa = pressureKPa,
                EngineThrusting = engine,
                LandedOrSplashed = landed,
                SurfaceSpeed = speed,
                DistanceM = distance,
            };
        }

        static PolicyState From(LinkMode mode) => new PolicyState { Mode = mode };

        [Fact]
        public void NewState_StartsInPhysics()
        {
            Assert.Equal(LinkMode.Physics, new PolicyState().Mode);
        }

        [Fact]
        public void Step_HighQ_FromRails_GoesPhysics()
        {
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(From(LinkMode.Rails), In(q: 150), 0.02, S));
        }

        [Fact]
        public void Step_PredictedQ_GoesPhysicsEarly()
        {
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(From(LinkMode.Rails), In(q: 10, predicted: 120), 0.02, S));
        }

        [Fact]
        public void Step_StaticPressureAbove1kPa_KeepsPhysics()
        {
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(From(LinkMode.Physics), In(q: 10, pressureKPa: 1.2), 10, S));
        }

        [Fact]
        public void Step_StaticPressureAbove1kPa_FromRails_GoesPhysics()
        {
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(From(LinkMode.Rails), In(q: 10, pressureKPa: 1.2), 0.02, S));
        }

        [Fact]
        public void Step_EngineThrusting_KeepsPhysics()
        {
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(From(LinkMode.Physics), In(engine: true), 10, S));
        }

        [Fact]
        public void Step_LandedMoving_KeepsPhysics()
        {
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(From(LinkMode.Physics), In(landed: true, speed: 2), 10, S));
        }

        [Fact]
        public void Step_LandedSettledOnAirless_GoesRails()
        {
            var state = From(LinkMode.Physics);
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(state, In(landed: true, speed: 0.1), 4, S));
            Assert.Equal(LinkMode.Rails, PhysicsPolicy.Step(state, In(landed: true, speed: 0.1), 1.5, S));
        }

        [Fact]
        public void Step_CalmFor5Seconds_GoesRails()
        {
            var state = From(LinkMode.Physics);
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(state, In(q: 40), 4, S));
            Assert.Equal(LinkMode.Rails, PhysicsPolicy.Step(state, In(q: 40), 1.5, S));
        }

        [Fact]
        public void Step_QBetweenThresholds_ResetsCalm()
        {
            var state = From(LinkMode.Physics);
            PhysicsPolicy.Step(state, In(q: 40), 4, S);
            PhysicsPolicy.Step(state, In(q: 70), 1, S);
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(state, In(q: 40), 4, S));
        }

        [Fact]
        public void Step_FromRails_QBelowOnThreshold_StaysRails()
        {
            Assert.Equal(LinkMode.Rails, PhysicsPolicy.Step(From(LinkMode.Rails), In(q: 80), 0.02, S));
        }

        [Fact]
        public void Step_NeedsPhysicsButFar_IsOutOfRange()
        {
            var state = From(LinkMode.Rails);
            Assert.Equal(LinkMode.OutOfRange, PhysicsPolicy.Step(state, In(q: 150, distance: 300001), 0.02, S));
            Assert.Equal(LinkMode.Physics, PhysicsPolicy.Step(state, In(q: 150, distance: 299000), 0.02, S));
        }

        [Fact]
        public void Step_OutOfRange_Calm_GoesRails()
        {
            Assert.Equal(LinkMode.Rails, PhysicsPolicy.Step(From(LinkMode.OutOfRange), In(q: 10), 0.02, S));
        }

        [Fact]
        public void Step_OutOfRange_DoesNotAccumulateCalm()
        {
            var state = From(LinkMode.Physics);
            PhysicsPolicy.Step(state, In(q: 40), 4, S);
            PhysicsPolicy.Step(state, In(q: 150, distance: 400000), 1, S);
            Assert.Equal(0, state.CalmSeconds);
        }
    }
}
