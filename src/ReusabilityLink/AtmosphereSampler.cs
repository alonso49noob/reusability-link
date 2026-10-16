using System;
using ReusabilityLink.Core;

namespace ReusabilityLink
{
    /// <summary>
    /// Atmospheric values computed from the body itself, so they are valid for packed vessels too
    /// (KSP does not keep a packed vessel's own pressure/density fields up to date).
    /// </summary>
    internal static class AtmosphereSampler
    {
        public static double StaticPressureKPa(CelestialBody b, double alt) =>
            b.atmosphere && alt < b.atmosphereDepth ? b.GetPressure(Math.Max(alt, 0)) : 0;

        public static double Density(CelestialBody b, double alt)
        {
            if (!b.atmosphere || alt >= b.atmosphereDepth) return 0;
            alt = Math.Max(alt, 0);
            return b.GetDensity(b.GetPressure(alt), b.GetTemperature(alt));
        }

        public static double CurrentQ(Vessel v) =>
            TelemetryMath.DynamicPressure(Density(v.mainBody, v.altitude), v.srfSpeed);

        /// <summary>Highest q along the drag-free orbit over the next <paramref name="seconds"/>.</summary>
        public static double PredictedMaxQ(Vessel v, double seconds)
        {
            double step = PhysicsPolicy.PredictionStep(seconds);
            var b = v.mainBody;
            if (!b.atmosphere || v.LandedOrSplashed || v.orbit == null) return 0;

            double ut = Planetarium.GetUniversalTime();
            double max = 0;
            for (double t = step; t <= seconds + 1e-6; t += step)
            {
                Vector3d pos = v.orbit.getPositionAtUT(ut + t);
                double alt = b.GetAltitude(pos);
                Vector3d surfaceVelocity = v.orbit.getOrbitalVelocityAtUT(ut + t).xzy - b.getRFrmVel(pos);
                max = Math.Max(max, TelemetryMath.DynamicPressure(Density(b, alt), surfaceVelocity.magnitude));
            }
            return max;
        }
    }
}
