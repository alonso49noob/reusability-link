using ReusabilityLink.Core;

namespace ReusabilityLink
{
    /// <summary>Everything the monitor window shows about one vessel, read once per frame.</summary>
    public struct TelemetrySnapshot
    {
        public string Name;
        public double SurfaceSpeed;
        public double AltitudeASL;
        public double HeightAboveTerrain;
        public double VerticalSpeed;
        public double DistanceM;
        public double? SecondsToImpact;
        public ImpactLevel Impact;
        public Attitude Attitude;
        public NavballMarker Prograde;
        public NavballMarker Retrograde;
        public LinkMode Mode;
        public bool LandedOrSplashed;
        public bool InAtmosphereAndFlying;
    }

    public static class TelemetrySampler
    {
        public static TelemetrySnapshot Sample(Vessel v, Vessel active, LinkMode mode, LinkSettings s)
        {
            var b = v.mainBody;
            Vector3d pos = v.GetWorldPos3D();

            // Local frame computed here: KSP does not refresh vessel.up/north/east for packed vessels.
            Vector3d up = (pos - b.position).normalized;
            Vector3d north = Vector3d.Exclude(up, b.position + (Vector3d)b.transform.up * b.Radius - pos).normalized;
            Vector3d east = b.getRFrmVel(pos).normalized;
            if (east.sqrMagnitude < 0.5)
                east = Vector3d.Cross(up, north).normalized; // Unity is left-handed: up x north points east.

            Vec3 ToEnu(Vector3d w) => new Vec3(Vector3d.Dot(w, east), Vector3d.Dot(w, north), Vector3d.Dot(w, up));

            var reference = v.ReferenceTransform != null ? v.ReferenceTransform : v.transform;
            Vec3 nose = ToEnu(reference.up);
            Vec3 top = ToEnu(-reference.forward);
            Vector3d surfaceVelocity = v.srf_velocity;
            double verticalSpeed = Vector3d.Dot(surfaceVelocity, up);

            double terrain = b.TerrainAltitude(v.latitude, v.longitude, allowNegative: !b.ocean);
            double height = v.altitude - terrain;
            double? toImpact = v.LandedOrSplashed ? null : TelemetryMath.SecondsToImpact(height, verticalSpeed);

            var snapshot = new TelemetrySnapshot
            {
                Name = v.vesselName,
                SurfaceSpeed = surfaceVelocity.magnitude,
                AltitudeASL = v.altitude,
                HeightAboveTerrain = height,
                VerticalSpeed = verticalSpeed,
                DistanceM = active != null ? RangeController.Distance(v, active) : 0,
                SecondsToImpact = toImpact,
                Impact = TelemetryMath.ClassifyImpact(toImpact, s),
                Attitude = TelemetryMath.ComputeAttitude(nose, top),
                Mode = mode,
                LandedOrSplashed = v.LandedOrSplashed,
                InAtmosphereAndFlying = b.atmosphere && v.altitude < b.atmosphereDepth && !v.LandedOrSplashed,
            };
            if (surfaceVelocity.sqrMagnitude > 0.01)
            {
                Vec3 velocity = ToEnu(surfaceVelocity);
                snapshot.Prograde = TelemetryMath.ProjectMarker(velocity, nose, top);
                snapshot.Retrograde = TelemetryMath.ProjectMarker(-velocity, nose, top);
            }
            return snapshot;
        }
    }
}
