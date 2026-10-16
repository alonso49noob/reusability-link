using System;
using KSP.UI.Screens;
using ReusabilityLink.Core;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>
    /// The small corner window (spec §3.5): telemetry and mini navball of the other tracked vessel,
    /// warnings, a switch button, and manual track/untrack of the active vessel.
    /// </summary>
    public sealed class MonitorWindow : IDisposable
    {
        const float Width = 230f;
        const float NavballSize = 120f;
        const float DenseAirNoticeSeconds = 5f;

        readonly LinkSettings settings;
        readonly TrackingService tracking;
        readonly RangeController ranges;
        readonly SwitchController switcher;
        readonly int windowId = "ReusabilityLink.Monitor".GetHashCode();

        Rect rect = new Rect(Screen.width - Width - 20f, 120f, Width, 0f);
        bool visible = true;
        // Opened from the toolbar: also show the window when there is no linked vessel, for manual tracking.
        bool panelOpen;
        bool uiHidden;
        bool positionDirty;
        float denseAirUntil;
        ApplicationLauncherButton button;

        GUIStyle infoStyle, warnStyle, criticalStyle, noticeStyle;

        public MonitorWindow(LinkSettings settings, TrackingService tracking, RangeController ranges, SwitchController switcher)
        {
            this.settings = settings;
            this.tracking = tracking;
            this.ranges = ranges;
            this.switcher = switcher;

            if (SettingsLoader.TryLoadWindow(out var saved, out var savedVisible))
            {
                rect.x = Mathf.Clamp(saved.x, 0f, Screen.width - 50f);
                rect.y = Mathf.Clamp(saved.y, 0f, Screen.height - 50f);
                visible = savedVisible;
            }

            ranges.ModeChanged += OnModeChanged;
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            if (ApplicationLauncher.Ready) AddButton();
        }

        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                SettingsLoader.SaveWindow(rect, visible);
            }
        }

        public void Dispose()
        {
            ranges.ModeChanged -= OnModeChanged;
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
            if (button != null && ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(button);
            button = null;
        }

        public void Draw()
        {
            if (uiHidden || !visible || FlightGlobals.ActiveVessel == null) return;
            if (tracking.OtherTracked == null && !panelOpen) return;

            EnsureStyles();
            var previous = rect.position;
            rect = GUILayout.Window(windowId, rect, DrawContents, "Reusability Link", GUILayout.Width(Width));
            if (rect.position != previous) positionDirty = true;
            if (positionDirty && !Input.GetMouseButton(0))
            {
                positionDirty = false;
                SettingsLoader.SaveWindow(rect, visible);
            }
        }

        void DrawContents(int id)
        {
            var active = FlightGlobals.ActiveVessel;
            var other = tracking.OtherTracked;
            if (other != null)
            {
                var t = TelemetrySampler.Sample(other, active, ranges.ModeOf(other), settings);
                DrawTelemetry(t);
                if (GUILayout.Button($"Cambiar ({settings.SwitchKey})"))
                    switcher.SwitchToOther();
            }
            else
            {
                GUILayout.Label("Sin nave enlazada.", infoStyle);
            }

            bool activeTracked = tracking.IsTracked(active);
            if (GUILayout.Button(activeTracked ? "Dejar de seguir esta nave" : "Seguir esta nave"))
            {
                if (activeTracked) tracking.Untrack(active, "manual");
                else tracking.Track(active, manual: true);
            }
            GUI.DragWindow();
        }

        void DrawTelemetry(TelemetrySnapshot t)
        {
            GUILayout.Label(t.Name, infoStyle);
            GUILayout.Label($"VEL {t.SurfaceSpeed:F0} m/s", infoStyle);
            GUILayout.Label($"ALT {Distance(t.AltitudeASL)}   SUELO {Distance(t.HeightAboveTerrain)}", infoStyle);
            GUILayout.Label($"V.VERT {t.VerticalSpeed:+0;-0;0} m/s", infoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var navball = GUILayoutUtility.GetRect(NavballSize, NavballSize, GUILayout.Width(NavballSize), GUILayout.Height(NavballSize));
            if (Event.current.type == EventType.Repaint) MiniNavball.Draw(navball, t);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label($"RUMBO {t.Attitude.HeadingDeg:000}°", infoStyle);

            GUILayout.Label("ESTADO: " + StateText(t), infoStyle);
            GUILayout.Label($"DIST {t.DistanceM / 1000.0:F1} km", infoStyle);

            if (Time.time < denseAirUntil)
                GUILayout.Label("Entrando en aire denso", noticeStyle);
            if (t.Impact == ImpactLevel.Critical)
                GUILayout.Label($"Impacto estimado en < {settings.ImpactCriticalSeconds:F0} s", criticalStyle);
            else if (t.Impact == ImpactLevel.Warn)
                GUILayout.Label($"Impacto estimado en < {settings.ImpactWarnSeconds:F0} s", warnStyle);
            if (t.Mode == LinkMode.OutOfRange)
                GUILayout.Label("Fuera de rango de física — cambia a esta nave", criticalStyle);
            if (t.InAtmosphereAndFlying)
                GUILayout.Label("No salgas del vuelo: el booster puede perderse", warnStyle);
        }

        static string StateText(TelemetrySnapshot t)
        {
            if (t.LandedOrSplashed) return "ATERRIZADO";
            switch (t.Mode)
            {
                case LinkMode.Rails: return "RAÍLES";
                case LinkMode.OutOfRange: return "FUERA DE RANGO";
                default: return "FÍSICA";
            }
        }

        static string Distance(double metres) =>
            Math.Abs(metres) < 10000 ? $"{metres:F0} m" : $"{metres / 1000.0:F1} km";

        void OnModeChanged(Vessel v, LinkMode before, LinkMode now)
        {
            if (now == LinkMode.Physics && before != LinkMode.Physics && v != FlightGlobals.ActiveVessel)
                denseAirUntil = Time.time + DenseAirNoticeSeconds;
        }

        void OnHideUI() => uiHidden = true;
        void OnShowUI() => uiHidden = false;

        void AddButton()
        {
            if (button != null || ApplicationLauncher.Instance == null) return;
            button = ApplicationLauncher.Instance.AddModApplication(
                () => { panelOpen = true; Visible = true; },
                () => { panelOpen = false; Visible = false; },
                null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                MakeIcon());
            if (visible) button.SetTrue(false);
        }

        static Texture2D MakeIcon()
        {
            const int size = 38;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var clear = new Color(0, 0, 0, 0);
            var color = new Color(0.9f, 0.9f, 0.9f);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    // Two linked dots: the two vessels of the link.
                    bool dotA = (x - 11) * (x - 11) + (y - 11) * (y - 11) <= 36;
                    bool dotB = (x - 27) * (x - 27) + (y - 27) * (y - 27) <= 36;
                    bool link = Mathf.Abs(x - y) <= 1 && x > 11 && x < 27;
                    tex.SetPixel(x, y, dotA || dotB || link ? color : clear);
                }
            tex.Apply();
            return tex;
        }

        void EnsureStyles()
        {
            if (infoStyle != null) return;
            infoStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            noticeStyle = Colored(new Color(0.45f, 0.75f, 1f));
            warnStyle = Colored(new Color(1f, 0.85f, 0.2f));
            criticalStyle = Colored(new Color(1f, 0.3f, 0.3f));
        }

        GUIStyle Colored(Color c)
        {
            var style = new GUIStyle(infoStyle) { fontStyle = FontStyle.Bold };
            style.normal.textColor = c;
            return style;
        }
    }
}
