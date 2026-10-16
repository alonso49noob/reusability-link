using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class PairRuleTests
    {
        [Fact]
        public void ShouldRelease_LoneActiveVessel_AfterBoosterLanded()
        {
            Assert.True(PairRule.ShouldRelease(trackedCount: 1, isActive: true, isSafe: false, manuallyPinned: false));
        }

        [Fact]
        public void ShouldRelease_LoneInactiveVesselInStableOrbit()
        {
            Assert.True(PairRule.ShouldRelease(1, isActive: false, isSafe: true, manuallyPinned: false));
        }

        [Fact]
        public void ShouldRelease_LoneInactiveVesselStillFalling_IsKept()
        {
            Assert.False(PairRule.ShouldRelease(1, isActive: false, isSafe: false, manuallyPinned: false));
        }

        [Fact]
        public void ShouldRelease_ManuallyPinned_IsKept()
        {
            Assert.False(PairRule.ShouldRelease(1, isActive: true, isSafe: true, manuallyPinned: true));
        }

        [Fact]
        public void ShouldRelease_Pair_IsKept()
        {
            Assert.False(PairRule.ShouldRelease(2, isActive: true, isSafe: true, manuallyPinned: false));
        }

        [Theory]
        [InlineData(FlightSituation.Landed, -1000.0, true)]
        [InlineData(FlightSituation.Splashed, -1000.0, true)]
        [InlineData(FlightSituation.Prelaunch, -1000.0, true)]
        [InlineData(FlightSituation.Orbiting, 80000.0, true)]
        [InlineData(FlightSituation.Escaping, 80000.0, true)]
        [InlineData(FlightSituation.Orbiting, 40000.0, false)]
        [InlineData(FlightSituation.SubOrbital, 80000.0, false)]
        [InlineData(FlightSituation.Flying, 80000.0, false)]
        public void IsSafeAlone_OnKerbin(FlightSituation situation, double periapsis, bool expected)
        {
            Assert.Equal(expected, PairRule.IsSafeAlone(situation, periapsis, atmosphereDepthM: 70000));
        }

        [Fact]
        public void IsSafeAlone_AirlessBody_OrbitAboveSurface()
        {
            Assert.True(PairRule.IsSafeAlone(FlightSituation.Orbiting, 5000, atmosphereDepthM: 0));
            Assert.False(PairRule.IsSafeAlone(FlightSituation.Orbiting, -100, atmosphereDepthM: 0));
        }
    }
}
