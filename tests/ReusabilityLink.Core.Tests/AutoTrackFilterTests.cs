using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class AutoTrackFilterTests
    {
        static readonly LinkSettings S = new LinkSettings();

        static SeparationInfo Booster() => new SeparationInfo
        {
            FromActiveVessel = true,
            HasCommandModule = true,
            HasEngine = true,
            ActiveIsTracked = false,
            ActiveSituation = FlightSituation.SubOrbital,
            ActivePeriapsisM = -500000,
            AtmosphereDepthM = 70000,
            TrackedCount = 0,
        };

        [Fact]
        public void Evaluate_TypicalBooster_Tracks()
        {
            Assert.Equal(AutoTrackResult.Track, AutoTrackFilter.Evaluate(Booster(), S));
        }

        [Fact]
        public void Evaluate_FlyingSituation_Tracks()
        {
            var info = Booster();
            info.ActiveSituation = FlightSituation.Flying;
            Assert.Equal(AutoTrackResult.Track, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_AutoTrackDisabled_ReturnsDisabled()
        {
            Assert.Equal(AutoTrackResult.Disabled, AutoTrackFilter.Evaluate(Booster(), new LinkSettings { AutoTrack = false }));
        }

        [Fact]
        public void Evaluate_NotFromActive_IsRejected()
        {
            var info = Booster();
            info.FromActiveVessel = false;
            Assert.Equal(AutoTrackResult.NotFromActive, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_NoCommand_IsRejected()
        {
            var info = Booster();
            info.HasCommandModule = false;
            Assert.Equal(AutoTrackResult.NoCommand, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_NoEngine_IsRejected_UnlessDisabledInSettings()
        {
            var info = Booster();
            info.HasEngine = false;
            Assert.Equal(AutoTrackResult.NoEngine, AutoTrackFilter.Evaluate(info, S));
            Assert.Equal(AutoTrackResult.Track, AutoTrackFilter.Evaluate(info, new LinkSettings { AutoTrackRequireEngines = false }));
        }

        [Fact]
        public void Evaluate_SatelliteInStableOrbit_NotAscending()
        {
            var info = Booster();
            info.ActiveSituation = FlightSituation.Orbiting;
            info.ActivePeriapsisM = 100000;
            Assert.Equal(AutoTrackResult.NotAscending, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_OrbitingButPeInsideAtmosphere_Tracks()
        {
            var info = Booster();
            info.ActiveSituation = FlightSituation.Orbiting;
            info.ActivePeriapsisM = 40000;
            Assert.Equal(AutoTrackResult.Track, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_AirlessBody_UsesSurfaceAsLimit()
        {
            var info = Booster();
            info.AtmosphereDepthM = 0;
            info.ActiveSituation = FlightSituation.Orbiting;
            info.ActivePeriapsisM = -1000;
            Assert.Equal(AutoTrackResult.Track, AutoTrackFilter.Evaluate(info, S));
            info.ActivePeriapsisM = 5000;
            Assert.Equal(AutoTrackResult.NotAscending, AutoTrackFilter.Evaluate(info, S));
        }

        [Theory]
        [InlineData(FlightSituation.Prelaunch)]
        [InlineData(FlightSituation.Landed)]
        [InlineData(FlightSituation.Splashed)]
        [InlineData(FlightSituation.Escaping)]
        [InlineData(FlightSituation.Docked)]
        public void Evaluate_OtherSituations_NotAscending(FlightSituation situation)
        {
            var info = Booster();
            info.ActiveSituation = situation;
            Assert.Equal(AutoTrackResult.NotAscending, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_SecondBoosterWithTwoTracked_LimitReached()
        {
            var info = Booster();
            info.TrackedCount = 2;
            info.ActiveIsTracked = true;
            Assert.Equal(AutoTrackResult.LimitReached, AutoTrackFilter.Evaluate(info, S));
        }

        [Fact]
        public void Evaluate_HigherLimit_AllowsThirdVessel()
        {
            var info = Booster();
            info.TrackedCount = 2;
            info.ActiveIsTracked = true;
            Assert.Equal(AutoTrackResult.Track, AutoTrackFilter.Evaluate(info, new LinkSettings { MaxTrackedVessels = 3 }));
        }
    }
}
