using System;
using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    // All vectors are in a right-handed local ENU frame: x = east, y = north, z = up.
    public class TelemetryMathTests
    {
        static readonly LinkSettings S = new LinkSettings();
        static readonly double R = Math.Sqrt(0.5);

        [Fact]
        public void DynamicPressure_Basic() => Assert.Equal(1800, TelemetryMath.DynamicPressure(0.0009, 2000), 6);

        [Fact]
        public void SecondsToImpact_Falling() => Assert.Equal(50, TelemetryMath.SecondsToImpact(5000, -100).Value, 6);

        [Fact]
        public void SecondsToImpact_Climbing_IsNull() => Assert.Null(TelemetryMath.SecondsToImpact(5000, 10));

        [Fact]
        public void SecondsToImpact_OnGround_IsNull() => Assert.Null(TelemetryMath.SecondsToImpact(0, -5));

        [Theory]
        [InlineData(70.0, ImpactLevel.None)]
        [InlineData(59.0, ImpactLevel.Warn)]
        [InlineData(19.0, ImpactLevel.Critical)]
        public void ClassifyImpact_Thresholds(double seconds, ImpactLevel expected)
        {
            Assert.Equal(expected, TelemetryMath.ClassifyImpact(seconds, S));
        }

        [Fact]
        public void ClassifyImpact_Null_IsNone() => Assert.Equal(ImpactLevel.None, TelemetryMath.ClassifyImpact(null, S));

        [Fact]
        public void Attitude_LevelNorth()
        {
            var a = TelemetryMath.ComputeAttitude(new Vec3(0, 1, 0), new Vec3(0, 0, 1));
            Assert.Equal(0, a.PitchDeg, 6);
            Assert.Equal(0, a.HeadingDeg, 6);
            Assert.Equal(0, a.RollDeg, 6);
        }

        [Fact]
        public void Attitude_LevelEast()
        {
            var a = TelemetryMath.ComputeAttitude(new Vec3(1, 0, 0), new Vec3(0, 0, 1));
            Assert.Equal(90, a.HeadingDeg, 6);
        }

        [Fact]
        public void Attitude_LevelWest_HeadingIsPositive()
        {
            var a = TelemetryMath.ComputeAttitude(new Vec3(-1, 0, 0), new Vec3(0, 0, 1));
            Assert.Equal(270, a.HeadingDeg, 6);
        }

        [Fact]
        public void Attitude_Pitch45North()
        {
            var a = TelemetryMath.ComputeAttitude(new Vec3(0, R, R), new Vec3(0, -R, R));
            Assert.Equal(45, a.PitchDeg, 6);
            Assert.Equal(0, a.HeadingDeg, 6);
            Assert.Equal(0, a.RollDeg, 6);
        }

        [Fact]
        public void Attitude_RolledRight90()
        {
            var a = TelemetryMath.ComputeAttitude(new Vec3(0, 1, 0), new Vec3(1, 0, 0));
            Assert.Equal(90, a.RollDeg, 6);
        }

        [Fact]
        public void Attitude_Vertical_HeadingFromTop()
        {
            var a = TelemetryMath.ComputeAttitude(new Vec3(0, 0, 1), new Vec3(0, 1, 0));
            Assert.Equal(90, a.PitchDeg, 6);
            Assert.Equal(180, a.HeadingDeg, 6);
            Assert.Equal(0, a.RollDeg, 6);
        }

        [Fact]
        public void Marker_AlongNose_IsCenter()
        {
            var m = TelemetryMath.ProjectMarker(new Vec3(0, 1, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1));
            Assert.True(m.Visible);
            Assert.Equal(0, m.X, 6);
            Assert.Equal(0, m.Y, 6);
        }

        [Fact]
        public void Marker_RightOfNose()
        {
            var m = TelemetryMath.ProjectMarker(new Vec3(R, R, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1));
            Assert.True(m.Visible);
            Assert.Equal(R, m.X, 6);
            Assert.Equal(0, m.Y, 6);
        }

        [Fact]
        public void Marker_Behind_IsNotVisible()
        {
            var m = TelemetryMath.ProjectMarker(new Vec3(0, -1, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1));
            Assert.False(m.Visible);
        }

        [Fact]
        public void Marker_UnnormalizedDirection_IsNormalized()
        {
            var m = TelemetryMath.ProjectMarker(new Vec3(0, 0, 250), new Vec3(0, 1, 0), new Vec3(0, 0, 1));
            Assert.Equal(1, m.Y, 6);
        }

        [Fact]
        public void Vec3_CrossIsRightHanded()
        {
            var z = Vec3.Cross(new Vec3(1, 0, 0), new Vec3(0, 1, 0));
            Assert.Equal((0.0, 0.0, 1.0), (z.X, z.Y, z.Z));
        }
    }
}
