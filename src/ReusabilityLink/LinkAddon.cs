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

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Settings = SettingsLoader.Load();
            Log.Info($"v1 cargado: MaxPhysicsDistance={Settings.MaxPhysicsDistance} SwitchKey={Settings.SwitchKey}");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
