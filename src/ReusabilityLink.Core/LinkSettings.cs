using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReusabilityLink.Core
{
    /// <summary>User-tunable values from Settings.cfg (spec §3.6) plus fixed range constants (spec §3.2).</summary>
    public sealed class LinkSettings
    {
        public const float TrackedLoadDistance = 2_500_000f;
        public const float TrackedUnloadDistance = 2_600_000f;
        public const float PhysicsPackDistance = 2_550_000f;
        public const float PhysicsUnpackDistance = 2_400_000f;

        public int MaxTrackedVessels { get; set; } = 2;
        public bool AutoTrack { get; set; } = true;
        public bool AutoTrackRequireEngines { get; set; } = true;
        public double PhysicsOnDynamicPressure { get; set; } = 100;
        public double RailsBelowDynamicPressure { get; set; } = 50;
        public double RailsHoldSeconds { get; set; } = 5;
        public double LookaheadSeconds { get; set; } = 15;
        public double MaxPhysicsDistance { get; set; } = 300000;
        public double RailsMinDistance { get; set; } = 2500;
        public double RailsMaxStaticPressureKPa { get; set; } = 1.0;
        public double LandedSettleSpeed { get; set; } = 0.5;
        public double LandedSettleSeconds { get; set; } = 10;
        public double ImpactWarnSeconds { get; set; } = 60;
        public double ImpactCriticalSeconds { get; set; } = 20;
        public KeyChord SwitchKey { get; set; } = DefaultSwitchKey();

        static KeyChord DefaultSwitchKey()
        {
            KeyChord.TryParse("Alt+Y", out var chord);
            return chord;
        }

        /// <summary>
        /// Builds settings from key/value pairs. Keys are case-insensitive; numbers use the invariant
        /// culture. Invalid values keep the default and unknown keys are ignored, each adding a warning.
        /// </summary>
        public static LinkSettings FromValues(IEnumerable<KeyValuePair<string, string>> values, IList<string> warnings)
        {
            var s = new LinkSettings();
            var setters = new Dictionary<string, Func<string, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(MaxTrackedVessels)] = v => TryInt(v, x => s.MaxTrackedVessels = x),
                [nameof(AutoTrack)] = v => TryBool(v, x => s.AutoTrack = x),
                [nameof(AutoTrackRequireEngines)] = v => TryBool(v, x => s.AutoTrackRequireEngines = x),
                [nameof(PhysicsOnDynamicPressure)] = v => TryDouble(v, x => s.PhysicsOnDynamicPressure = x),
                [nameof(RailsBelowDynamicPressure)] = v => TryDouble(v, x => s.RailsBelowDynamicPressure = x),
                [nameof(RailsHoldSeconds)] = v => TryDouble(v, x => s.RailsHoldSeconds = x),
                [nameof(LookaheadSeconds)] = v => TryDouble(v, x => s.LookaheadSeconds = x),
                [nameof(MaxPhysicsDistance)] = v => TryDouble(v, x => s.MaxPhysicsDistance = x),
                [nameof(RailsMinDistance)] = v => TryDouble(v, x => s.RailsMinDistance = x),
                [nameof(RailsMaxStaticPressureKPa)] = v => TryDouble(v, x => s.RailsMaxStaticPressureKPa = x),
                [nameof(LandedSettleSpeed)] = v => TryDouble(v, x => s.LandedSettleSpeed = x),
                [nameof(LandedSettleSeconds)] = v => TryDouble(v, x => s.LandedSettleSeconds = x),
                [nameof(ImpactWarnSeconds)] = v => TryDouble(v, x => s.ImpactWarnSeconds = x),
                [nameof(ImpactCriticalSeconds)] = v => TryDouble(v, x => s.ImpactCriticalSeconds = x),
                [nameof(SwitchKey)] = v =>
                {
                    if (!KeyChord.TryParse(v, out var chord)) return false;
                    s.SwitchKey = chord;
                    return true;
                },
            };

            var defaults = new LinkSettings();
            foreach (var pair in values)
            {
                if (!setters.TryGetValue(pair.Key, out var set))
                {
                    warnings.Add($"{pair.Key}: clave desconocida, se ignora");
                    continue;
                }
                if (!set(pair.Value))
                    warnings.Add($"{pair.Key}: valor inválido '{pair.Value}', se usa {defaults.DefaultText(pair.Key)}");
            }
            return s;
        }

        string DefaultText(string key)
        {
            var property = typeof(LinkSettings).GetProperty(key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            object value = property?.GetValue(this);
            return value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();
        }

        static bool TryDouble(string text, Action<double> set)
        {
            // A comma is never a valid decimal separator here: "0,5" must not silently become 5.
            if (text == null || text.Contains(",")) return false;
            if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return false;
            set(x);
            return true;
        }

        static bool TryInt(string text, Action<int> set)
        {
            if (!int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) return false;
            set(x);
            return true;
        }

        static bool TryBool(string text, Action<bool> set)
        {
            if (!bool.TryParse(text?.Trim(), out var x)) return false;
            set(x);
            return true;
        }
    }
}
