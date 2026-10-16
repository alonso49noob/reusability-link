using System;
using System.Collections.Generic;
using ReusabilityLink.Core;

namespace ReusabilityLink
{
    /// <summary>Which vessels are tracked (spec §3.1): auto-track on separation, manual track, end of tracking.</summary>
    public sealed class TrackingService : IDisposable
    {
        readonly LinkSettings settings;
        readonly List<Vessel> tracked = new List<Vessel>();
        readonly Dictionary<Guid, LandingSettle> settles = new Dictionary<Guid, LandingSettle>();
        // New vessels get their VesselModules during creation, so tracking is applied on the next tick.
        readonly List<Vessel> pendingAutoTrack = new List<Vessel>();
        // Vessels the user tracked by hand: kept even while alone, until a pair forms. Session only.
        readonly HashSet<Guid> manualPins = new HashSet<Guid>();

        public event Action<Vessel> TrackingStarted;
        public event Action<Vessel> TrackingEnded;

        public TrackingService(LinkSettings settings)
        {
            this.settings = settings;
            GameEvents.onPartDeCoupleNewVesselComplete.Add(OnDecoupled);
            GameEvents.onVesselWillDestroy.Add(OnVesselWillDestroy);
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
            GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
            GameEvents.onFlightReady.Add(Rebuild);
            Rebuild();
        }

        public void Dispose()
        {
            GameEvents.onPartDeCoupleNewVesselComplete.Remove(OnDecoupled);
            GameEvents.onVesselWillDestroy.Remove(OnVesselWillDestroy);
            GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
            GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
            GameEvents.onFlightReady.Remove(Rebuild);
        }

        /// <summary>Live tracked vessels, including the active one when it is part of the link.</summary>
        public IReadOnlyList<Vessel> Tracked
        {
            get
            {
                Prune();
                return tracked;
            }
        }

        /// <summary>The first tracked vessel that is not the active one, or null.</summary>
        public Vessel OtherTracked
        {
            get
            {
                var active = FlightGlobals.ActiveVessel;
                foreach (var v in Tracked)
                    if (v != active) return v;
                return null;
            }
        }

        public bool IsTracked(Vessel v)
        {
            Prune();
            return v != null && tracked.Contains(v);
        }

        public bool Track(Vessel v, bool manual)
        {
            var module = KspAdapters.Module(v);
            if (module == null) return false;
            if (tracked.Contains(v)) return true;
            if (Tracked.Count >= settings.MaxTrackedVessels)
            {
                if (manual) ScreenMessages.PostScreenMessage("Reusability Link: límite de naves seguidas alcanzado", 4f);
                return false;
            }

            module.SaveOriginalRanges();
            module.tracked = true;
            tracked.Add(v);
            if (manual) manualPins.Add(v.id);
            settles[v.id] = new LandingSettle();
            Log.Info($"siguiendo {v.vesselName} ({(manual ? "manual" : "automático")})");
            TrackingStarted?.Invoke(v);
            return true;
        }

        public void Untrack(Vessel v, string reason)
        {
            if (!tracked.Remove(v)) return;
            settles.Remove(v.id);
            manualPins.Remove(v.id);
            var module = KspAdapters.Module(v);
            if (module != null)
            {
                module.RestoreOriginalRanges();
                module.tracked = false;
            }
            Log.Info($"deja de seguirse {v.vesselName}: {reason}");
            TrackingEnded?.Invoke(v);
        }

        public void FixedTick(double dt)
        {
            if (pendingAutoTrack.Count > 0)
            {
                var pending = pendingAutoTrack.ToArray();
                pendingAutoTrack.Clear();
                foreach (var v in pending) TrackPair(v);
            }

            foreach (var v in new List<Vessel>(Tracked))
            {
                if (!settles.TryGetValue(v.id, out var settle)) continue;
                if (settle.Update(v.LandedOrSplashed, v.srfSpeed, dt, settings))
                {
                    ScreenMessages.PostScreenMessage($"{v.vesselName} aterrizado — seguimiento terminado", 5f);
                    Untrack(v, "aterrizado");
                }
            }

            EnforcePairRule();
        }

        /// <summary>
        /// Runs every tick so a vessel left alone (partner landed, lost, docked, or a leftover from an
        /// earlier flight after loading) never blocks the next link.
        /// </summary>
        void EnforcePairRule()
        {
            var live = Tracked;
            if (live.Count != 1)
            {
                if (live.Count > 1) manualPins.Clear();
                return;
            }

            var v = live[0];
            double atmosphereDepth = v.mainBody.atmosphere ? v.mainBody.atmosphereDepth : 0;
            bool safe = PairRule.IsSafeAlone(KspAdapters.ToSituation(v.situation), v.orbit.PeA, atmosphereDepth);
            if (PairRule.ShouldRelease(1, v == FlightGlobals.ActiveVessel, safe, manualPins.Contains(v.id)))
                Untrack(v, "sin pareja");
        }

        void TrackPair(Vessel candidate)
        {
            var active = FlightGlobals.ActiveVessel;
            if (candidate == null || active == null) return;
            if (!Track(active, manual: false)) return;
            if (Track(candidate, manual: false))
                ScreenMessages.PostScreenMessage("Reusability Link: siguiendo " + candidate.vesselName, 4f);
        }

        void OnDecoupled(Vessel original, Vessel created)
        {
            var active = FlightGlobals.ActiveVessel;
            if (active == null || original == null || created == null) return;

            var candidate = created != active ? created : original;
            var other = candidate == created ? original : created;
            var activeModule = KspAdapters.Module(active);
            var info = new SeparationInfo
            {
                FromActiveVessel = other == active,
                HasCommandModule = KspAdapters.HasCommand(candidate),
                HasEngine = KspAdapters.HasEngine(candidate),
                ActiveIsTracked = activeModule != null && activeModule.tracked,
                ActiveSituation = KspAdapters.ToSituation(active.situation),
                ActivePeriapsisM = active.orbit.PeA,
                AtmosphereDepthM = active.mainBody.atmosphere ? active.mainBody.atmosphereDepth : 0,
                TrackedCount = Tracked.Count,
            };

            var result = AutoTrackFilter.Evaluate(info, settings);
            switch (result)
            {
                case AutoTrackResult.Track:
                    pendingAutoTrack.Add(candidate);
                    break;
                case AutoTrackResult.LimitReached:
                    ScreenMessages.PostScreenMessage("Reusability Link: límite de naves seguidas alcanzado", 4f);
                    Log.Info($"no se sigue {candidate.vesselName}: {result}");
                    break;
                default:
                    Log.Info($"no se sigue {candidate.vesselName}: {result}");
                    break;
            }
        }

        void OnVesselWillDestroy(Vessel v)
        {
            pendingAutoTrack.Remove(v);
            if (tracked.Contains(v)) Untrack(v, "destruida");
        }

        void OnVesselRecovered(ProtoVessel pv, bool quick)
        {
            if (pv == null) return;
            var v = tracked.Find(t => t != null && t.id == pv.vesselID);
            if (v != null) Untrack(v, "recuperada");
        }

        void OnVesselPartCountChanged(Vessel v)
        {
            if (tracked.Contains(v) && IsGone(v)) Untrack(v, "fusionada o destruida");
        }

        static bool IsGone(Vessel v) =>
            v == null || v.state == Vessel.State.DEAD || !FlightGlobals.Vessels.Contains(v);

        void Prune()
        {
            for (int i = tracked.Count - 1; i >= 0; i--)
            {
                var v = tracked[i];
                if (!IsGone(v)) continue;
                tracked.RemoveAt(i);
                if ((object)v != null) settles.Remove(v.id);
            }
        }

        void Rebuild()
        {
            foreach (var v in FlightGlobals.Vessels)
            {
                var module = KspAdapters.Module(v);
                if (module == null || !module.tracked || tracked.Contains(v)) continue;
                tracked.Add(v);
                settles[v.id] = new LandingSettle();
            }
            if (tracked.Count > 0) Log.Info($"naves seguidas tras cargar: {tracked.Count}");
        }
    }
}
