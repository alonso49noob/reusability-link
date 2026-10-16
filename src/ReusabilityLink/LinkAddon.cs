using HarmonyLib;
using ReusabilityLink.Core;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>Flight-scene entry point: owns settings and wires the services together.</summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LinkAddon : MonoBehaviour
    {
        const string HarmonyId = "reusabilitylink";

        public static LinkAddon Instance { get; private set; }

        public LinkSettings Settings { get; private set; }
        public TrackingService Tracking { get; private set; }
        public RangeController Ranges { get; private set; }
        public SwitchController Switcher { get; private set; }
        public MonitorWindow Monitor { get; private set; }

        Harmony harmony;
        WarpGuard warpGuard;

        void Awake()
        {
            Instance = this;
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(LinkAddon).Assembly);
        }

        void Start()
        {
            Settings = SettingsLoader.Load();
            Tracking = new TrackingService(Settings);
            Ranges = new RangeController(Settings, Tracking);
            warpGuard = new WarpGuard(Tracking, Ranges);
            Switcher = new SwitchController(Settings, Tracking);
            Monitor = new MonitorWindow(Settings, Tracking, Ranges, Switcher);
            Log.Info($"v1 cargado: MaxPhysicsDistance={Settings.MaxPhysicsDistance} SwitchKey={Settings.SwitchKey}");
        }

        void Update()
        {
            Switcher?.Update();
        }

        void OnGUI()
        {
            Monitor?.Draw();
        }

        void FixedUpdate()
        {
            if (Tracking == null || !FlightGlobals.ready) return;
            double dt = TimeWarp.fixedDeltaTime;
            Tracking.FixedTick(dt);
            Ranges.FixedTick(dt);
            warpGuard.Tick();
        }

        void OnDestroy()
        {
            Monitor?.Dispose();
            Tracking?.Dispose();
            harmony?.UnpatchAll(HarmonyId);
            if (Instance == this) Instance = null;
        }
    }
}
