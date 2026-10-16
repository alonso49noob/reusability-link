using System;

namespace ReusabilityLink.Core
{
    public enum ImpactLevel { None, Warn, Critical }

    public struct Attitude
    {
        public double PitchDeg;
        /// <summary>0..360, clockwise from north.</summary>
        public double HeadingDeg;
        /// <summary>Positive when rolled right (top tilted toward the vessel's right side).</summary>
        public double RollDeg;
    }

    /// <summary>Position of a direction on the 2D navball: unit-disc coordinates, X right, Y up.</summary>
    public struct NavballMarker
    {
        public double X;
        public double Y;
        /// <summary>False when the direction points behind the nose (back hemisphere).</summary>
        public bool Visible;
    }

    /// <summary>Numbers shown in the monitor window (spec §3.5). Vectors are in a right-handed ENU frame.</summary>
    public static class TelemetryMath
    {
        const double RadToDeg = 180.0 / Math.PI;
        static readonly double VerticalHorizontalLimit = Math.Sin(0.5 / RadToDeg);
        static readonly Vec3 Up = new Vec3(0, 0, 1);

        public static double DynamicPressure(double densityKgM3, double speedMs) => 0.5 * densityKgM3 * speedMs * speedMs;

        /// <summary>Constant-velocity estimate; null when not descending or already on the ground.</summary>
        public static double? SecondsToImpact(double heightAboveTerrainM, double verticalSpeedMs)
        {
            if (verticalSpeedMs >= 0 || heightAboveTerrainM <= 0) return null;
            return heightAboveTerrainM / -verticalSpeedMs;
        }

        public static ImpactLevel ClassifyImpact(double? seconds, LinkSettings s)
        {
            if (seconds == null) return ImpactLevel.None;
            if (seconds.Value < s.ImpactCriticalSeconds) return ImpactLevel.Critical;
            if (seconds.Value < s.ImpactWarnSeconds) return ImpactLevel.Warn;
            return ImpactLevel.None;
        }

        public static Attitude ComputeAttitude(Vec3 noseEnu, Vec3 topEnu)
        {
            var nose = noseEnu.Normalized();
            var top = topEnu.Normalized();
            double pitch = Math.Asin(Math.Max(-1.0, Math.Min(1.0, nose.Z))) * RadToDeg;

            // Pointing straight up or down the nose has no horizontal heading; use the side the
            // navball shows at its bottom instead (-top when pitched up, top when pitched down).
            var headingSource = nose;
            if (Math.Sqrt(nose.X * nose.X + nose.Y * nose.Y) < VerticalHorizontalLimit)
                headingSource = pitch > 0 ? -top : top;
            double heading = Math.Atan2(headingSource.X, headingSource.Y) * RadToDeg;
            if (heading < 0) heading += 360.0;

            double roll = 0;
            var reference = Up - nose * Vec3.Dot(Up, nose);
            if (reference.Length >= 1e-6)
                roll = Math.Atan2(Vec3.Dot(Vec3.Cross(reference, top), nose), Vec3.Dot(reference, top)) * RadToDeg;

            return new Attitude { PitchDeg = pitch, HeadingDeg = heading, RollDeg = roll };
        }

        public static NavballMarker ProjectMarker(Vec3 directionEnu, Vec3 noseEnu, Vec3 topEnu)
        {
            var d = directionEnu.Normalized();
            var nose = noseEnu.Normalized();
            var top = topEnu.Normalized();
            var right = Vec3.Cross(nose, top);
            return new NavballMarker
            {
                X = Vec3.Dot(d, right),
                Y = Vec3.Dot(d, top),
                Visible = Vec3.Dot(d, nose) >= 0,
            };
        }
    }
}
