using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RLProbe
{
    // Throwaway measurement tool for phase 0 (see prototype/README.md). Not part of the mod.
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ProbeAddon : MonoBehaviour
    {
        const float TrackedLoad = 2_500_000f, TrackedUnload = 2_600_000f;
        const float PhysicsPack = 2_550_000f, PhysicsUnpack = 2_400_000f;
        const float RailsPack = 2_500f, RailsUnpack = 2_000f;

        Harmony harmony;
        bool visible = true;
        Rect window = new Rect(80, 80, 900, 200);
        float nextLog;
        Vessel pendingSwitch;

        readonly HashSet<Guid> logged = new HashSet<Guid>();
        readonly Dictionary<Guid, Dictionary<uint, Vector3>> snapshots = new Dictionary<Guid, Dictionary<uint, Vector3>>();
        readonly Dictionary<Guid, float> jitterCurrent = new Dictionary<Guid, float>();
        readonly Dictionary<Guid, float> jitterShown = new Dictionary<Guid, float>();

        void Awake()
        {
            harmony = new Harmony("rlprobe");
            harmony.PatchAll(typeof(ProbeAddon).Assembly);
            Debug.Log("[RLProbe] loaded, Alt+F8 toggles the window");
        }

        void OnDestroy()
        {
            harmony?.UnpatchAll("rlprobe");
            ProbeKillGuard.Protected.Clear();
        }

        void Update()
        {
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (alt && Input.GetKeyDown(KeyCode.F8)) visible = !visible;
            if (alt && Input.GetKeyDown(KeyCode.Y)) ScreenMessages.PostScreenMessage("[RLProbe] Alt+Y detectado", 2f);

            if (pendingSwitch != null)
            {
                var v = pendingSwitch;
                pendingSwitch = null;
                float t0 = Time.realtimeSinceStartup;
                bool ok = FlightGlobals.ForceSetActiveVessel(v);
                Debug.Log($"[RLProbe] switch to {v.vesselName} ok={ok} loaded={v.loaded} packed={v.packed} took={(Time.realtimeSinceStartup - t0) * 1000f:F0}ms");
            }
        }

        void FixedUpdate()
        {
            var active = FlightGlobals.ActiveVessel;
            if (active == null) return;

            foreach (var v in FlightGlobals.VesselsLoaded)
                if (v != active) UpdateJitter(v);

            if (Time.time < nextLog) return;
            nextLog = Time.time + 1f;
            foreach (var v in FlightGlobals.Vessels)
            {
                if (v == null || v == active || !logged.Contains(v.id)) continue;
                jitterShown[v.id] = jitterCurrent.TryGetValue(v.id, out var j) ? j : 0f;
                jitterCurrent[v.id] = 0f;
                Debug.Log($"[RLProbe] {v.vesselName} d={DistanceKm(v, active):F1}km sit={v.situation} loaded={v.loaded} packed={v.packed} p={PressureKPa(v):F2} q={DynamicPressure(v):F0} jitter={jitterShown[v.id]:F3}");
            }
        }

        void UpdateJitter(Vessel v)
        {
            if (v.packed || v.rootPart == null)
            {
                snapshots.Remove(v.id);
                return;
            }
            var root = v.rootPart.transform;
            if (!snapshots.TryGetValue(v.id, out var snap) || snap.Count != v.parts.Count)
            {
                snap = new Dictionary<uint, Vector3>();
                foreach (var p in v.parts) snap[p.flightID] = root.InverseTransformPoint(p.transform.position);
                snapshots[v.id] = snap;
                return;
            }
            float max = 0f;
            foreach (var p in v.parts)
            {
                if (!snap.TryGetValue(p.flightID, out var reference)) continue;
                max = Mathf.Max(max, (root.InverseTransformPoint(p.transform.position) - reference).magnitude);
            }
            jitterCurrent.TryGetValue(v.id, out var current);
            jitterCurrent[v.id] = Mathf.Max(current, max);
        }

        void OnGUI()
        {
            if (!visible) return;
            window = GUILayout.Window(GetInstanceID(), window, DrawWindow, "RLProbe — medición (Alt+F8)");
        }

        void DrawWindow(int id)
        {
            var active = FlightGlobals.ActiveVessel;
            GUILayout.Label("Activa: " + (active != null ? active.vesselName : "-"));
            if (active != null)
            {
                foreach (var v in FlightGlobals.VesselsLoaded)
                {
                    if (v == active) continue;
                    jitterShown.TryGetValue(v.id, out var jitter);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{v.vesselName} | {DistanceKm(v, active):F1} km | {v.situation} | packed={v.packed} | p={PressureKPa(v):F2} kPa | q={DynamicPressure(v):F0} Pa | jitter={jitter:F3} m", GUILayout.Width(520));
                    if (GUILayout.Button("Mantener cargada")) { SetLoad(v); logged.Add(v.id); }
                    if (GUILayout.Button("FÍSICA")) SetPack(v, PhysicsPack, PhysicsUnpack);
                    if (GUILayout.Button("RAÍLES")) SetPack(v, RailsPack, RailsUnpack);
                    bool guarded = ProbeKillGuard.Protected.Contains(v.id);
                    if (GUILayout.Toggle(guarded, "KillGuard") != guarded)
                    {
                        if (guarded) ProbeKillGuard.Protected.Remove(v.id); else ProbeKillGuard.Protected.Add(v.id);
                    }
                    if (GUILayout.Button("Cambiar a esta")) pendingSwitch = v;
                    GUILayout.EndHorizontal();
                }
            }
            GUI.DragWindow();
        }

        static IEnumerable<VesselRanges.Situation> AllSituations(VesselRanges r)
        {
            yield return r.prelaunch;
            yield return r.landed;
            yield return r.splashed;
            yield return r.flying;
            yield return r.subOrbital;
            yield return r.orbit;
            yield return r.escaping;
        }

        static void SetLoad(Vessel v)
        {
            foreach (var s in AllSituations(v.vesselRanges)) { s.load = TrackedLoad; s.unload = TrackedUnload; }
            Debug.Log($"[RLProbe] {v.vesselName}: keep loaded");
        }

        static void SetPack(Vessel v, float pack, float unpack)
        {
            foreach (var s in AllSituations(v.vesselRanges)) { s.pack = pack; s.unpack = unpack; }
            Debug.Log($"[RLProbe] {v.vesselName}: pack={pack} unpack={unpack}");
        }

        static double DistanceKm(Vessel v, Vessel active) => Vector3d.Distance(v.GetWorldPos3D(), active.GetWorldPos3D()) / 1000.0;

        static double PressureKPa(Vessel v) => v.mainBody.atmosphere ? v.mainBody.GetPressure(v.altitude) : 0.0;

        static double DynamicPressure(Vessel v)
        {
            if (!v.mainBody.atmosphere) return 0.0;
            double p = v.mainBody.GetPressure(v.altitude);
            double rho = v.mainBody.GetDensity(p, v.mainBody.GetTemperature(v.altitude));
            return 0.5 * rho * v.srfSpeed * v.srfSpeed;
        }
    }
}
