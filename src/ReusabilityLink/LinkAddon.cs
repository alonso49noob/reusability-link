using ReusabilityLink.Core;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>Flight-scene entry point: owns settings and wires the services together.</summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LinkAddon : MonoBehaviour
    {
        public static LinkAddon Instance { get; private set; }

        public LinkSettings Settings { get; private set; }
        public TrackingService Tracking { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Settings = SettingsLoader.Load();
            Tracking = new TrackingService(Settings);
            Log.Info($"v1 cargado: MaxPhysicsDistance={Settings.MaxPhysicsDistance} SwitchKey={Settings.SwitchKey}");
        }

        void FixedUpdate()
        {
            if (Tracking == null || !FlightGlobals.ready) return;
            Tracking.FixedTick(TimeWarp.fixedDeltaTime);
        }

        void OnDestroy()
        {
            Tracking?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
