namespace ReusabilityLink
{
    /// <summary>Per-vessel persistent "tracked" flag and the vessel's original ranges for restoring them.</summary>
    public class LinkVesselModule : VesselModule
    {
        [KSPField(isPersistant = true)]
        public bool tracked;

        [KSPField(isPersistant = true)]
        public bool rangesSaved;

        VesselRanges originalRanges;

        public override Activation GetActivation() => Activation.FlightScene | Activation.LoadedOrUnloaded;

        public void SaveOriginalRanges()
        {
            if (rangesSaved || vessel == null || vessel.vesselRanges == null) return;
            originalRanges = new VesselRanges(vessel.vesselRanges);
            rangesSaved = true;
        }

        public void RestoreOriginalRanges()
        {
            if (vessel == null) return;
            // After a reload only the flag survives; stock defaults are what KSP itself would use.
            vessel.vesselRanges = originalRanges != null
                ? new VesselRanges(originalRanges)
                : new VesselRanges(PhysicsGlobals.Instance.VesselRangesDefault);
            originalRanges = null;
            rangesSaved = false;
        }
    }
}
