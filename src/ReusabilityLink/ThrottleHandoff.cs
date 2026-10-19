using System;
using ReusabilityLink.Core;

namespace ReusabilityLink
{
    /// <summary>
    /// Gives each linked vessel its own throttle across switches (see ThrottleMemory). KSP zeroes
    /// ctrlState when a vessel is packed (Vessel.GoOnRails → Neutralize), and packs every vessel when
    /// switching to a packed one, so the held throttle is re-applied while the vessel is unpacked.
    /// </summary>
    public sealed class ThrottleHandoff : IDisposable
    {
        readonly TrackingService tracking;

        public ThrottleMemory Memory { get; } = new ThrottleMemory();

        public ThrottleHandoff(TrackingService tracking)
        {
            this.tracking = tracking;
            GameEvents.onVesselSwitching.Add(OnVesselSwitching);
            tracking.TrackingEnded += OnTrackingEnded;
        }

        public void Dispose()
        {
            GameEvents.onVesselSwitching.Remove(OnVesselSwitching);
            tracking.TrackingEnded -= OnTrackingEnded;
        }

        public void FixedTick()
        {
            var active = FlightGlobals.ActiveVessel;
            foreach (var v in tracking.Tracked)
            {
                if (v == active || v.packed || v.ctrlState == null) continue;
                float throttle = Memory.Held(v.id);
                if (throttle > 0f) v.ctrlState.mainThrottle = throttle;
            }
        }

        void OnVesselSwitching(Vessel from, Vessel to)
        {
            if (from == null || to == null || FlightInputHandler.state == null) return;
            if (!tracking.IsTracked(from) && !tracking.IsTracked(to)) return;

            // The global throttle still holds the value the player left `from` with.
            Memory.Leaving(from.id, FlightInputHandler.state.mainThrottle);
            float arriving = Memory.Arriving(to.id);
            FlightInputHandler.state.mainThrottle = arriving;
            Log.Info($"acelerador: {from.vesselName} sigue a {Memory.Held(from.id):P0}, {to.vesselName} recupera {arriving:P0}");
        }

        void OnTrackingEnded(Vessel v)
        {
            if ((object)v != null) Memory.Forget(v.id);
        }
    }
}
