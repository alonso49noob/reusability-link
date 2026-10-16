namespace ReusabilityLink.Core
{
    /// <summary>Distances (m) written into every situation of a tracked vessel's VesselRanges.</summary>
    public struct RangeValues
    {
        public float Load;
        public float Unload;
        public float Pack;
        public float Unpack;
    }

    /// <summary>
    /// Translates a LinkMode into KSP vessel ranges (spec §3.2). Tracked vessels are never unloaded;
    /// physics keeps pack/unpack beyond any reachable distance, rails packs beyond RailsMinDistance.
    /// </summary>
    public static class RangePlanner
    {
        public static RangeValues ForMode(LinkMode mode, LinkSettings s)
        {
            var r = new RangeValues
            {
                Load = LinkSettings.TrackedLoadDistance,
                Unload = LinkSettings.TrackedUnloadDistance,
            };
            if (mode == LinkMode.Physics)
            {
                r.Pack = LinkSettings.PhysicsPackDistance;
                r.Unpack = LinkSettings.PhysicsUnpackDistance;
            }
            else
            {
                r.Pack = (float)s.RailsMinDistance;
                r.Unpack = (float)s.RailsMinDistance - 500f;
            }
            return r;
        }
    }
}
