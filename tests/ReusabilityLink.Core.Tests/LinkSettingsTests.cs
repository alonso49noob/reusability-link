using System.Collections.Generic;
using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class LinkSettingsTests
    {
        [Fact]
        public void Defaults_MatchSpec()
        {
            var s = new LinkSettings();
            Assert.Equal(2, s.MaxTrackedVessels);
            Assert.True(s.AutoTrack);
            Assert.True(s.AutoTrackRequireEngines);
            Assert.Equal(100, s.PhysicsOnDynamicPressure);
            Assert.Equal(50, s.RailsBelowDynamicPressure);
            Assert.Equal(5, s.RailsHoldSeconds);
            Assert.Equal(15, s.LookaheadSeconds);
            Assert.Equal(300000, s.MaxPhysicsDistance);
            Assert.Equal(2500, s.RailsMinDistance);
            Assert.Equal(1.0, s.RailsMaxStaticPressureKPa);
            Assert.Equal(0.5, s.LandedSettleSpeed);
            Assert.Equal(10, s.LandedSettleSeconds);
            Assert.Equal(60, s.ImpactWarnSeconds);
            Assert.Equal(20, s.ImpactCriticalSeconds);
            Assert.Equal("Alt+B", s.SwitchKey.ToString());
        }

        [Fact]
        public void FromValues_ParsesInvariantNumbersAndBools()
        {
            var w = new List<string>();
            var s = LinkSettings.FromValues(new Dictionary<string, string>
            {
                ["MaxPhysicsDistance"] = "250000.5",
                ["AutoTrack"] = "False",
                ["SwitchKey"] = "Ctrl+Shift+K",
            }, w);
            Assert.Equal(250000.5, s.MaxPhysicsDistance);
            Assert.False(s.AutoTrack);
            Assert.True(s.SwitchKey.Ctrl && s.SwitchKey.Shift && !s.SwitchKey.Alt);
            Assert.Equal("K", s.SwitchKey.Key);
            Assert.Empty(w);
        }

        [Fact]
        public void FromValues_KeysAreCaseInsensitive()
        {
            var w = new List<string>();
            var s = LinkSettings.FromValues(new Dictionary<string, string> { ["maxtrackedvessels"] = "3" }, w);
            Assert.Equal(3, s.MaxTrackedVessels);
            Assert.Empty(w);
        }

        [Fact]
        public void FromValues_InvalidNumber_UsesDefaultAndWarns()
        {
            var w = new List<string>();
            var s = LinkSettings.FromValues(new Dictionary<string, string>
            {
                ["LandedSettleSpeed"] = "0,5",
                ["MaxTrackedVessels"] = "dos",
            }, w);
            Assert.Equal(0.5, s.LandedSettleSpeed);
            Assert.Equal(2, s.MaxTrackedVessels);
            Assert.Equal(2, w.Count);
            Assert.Contains("LandedSettleSpeed", w[0]);
            Assert.Contains("MaxTrackedVessels", w[1]);
        }

        [Fact]
        public void FromValues_InvalidKeyChord_UsesDefaultAndWarns()
        {
            var w = new List<string>();
            var s = LinkSettings.FromValues(new Dictionary<string, string> { ["SwitchKey"] = "Alt+" }, w);
            Assert.Equal("Alt+B", s.SwitchKey.ToString());
            Assert.Single(w);
        }

        [Fact]
        public void FromValues_UnknownKey_IsIgnoredWithWarning()
        {
            var w = new List<string>();
            var s = LinkSettings.FromValues(new Dictionary<string, string> { ["Foo"] = "1" }, w);
            Assert.Equal(2, s.MaxTrackedVessels);
            Assert.Single(w);
            Assert.Contains("Foo", w[0]);
        }

        [Theory]
        [InlineData("Alt+B", true, false, false, "B")]
        [InlineData("b", false, false, false, "B")]
        [InlineData("ctrl+alt+F5", true, true, false, "F5")]
        public void KeyChord_Parses(string text, bool alt, bool ctrl, bool shift, string key)
        {
            Assert.True(KeyChord.TryParse(text, out var chord));
            Assert.Equal(alt, chord.Alt);
            Assert.Equal(ctrl, chord.Ctrl);
            Assert.Equal(shift, chord.Shift);
            Assert.Equal(key, chord.Key);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Alt+")]
        [InlineData("Alt+B+C")]
        [InlineData(null)]
        public void KeyChord_RejectsInvalid(string text)
        {
            Assert.False(KeyChord.TryParse(text, out _));
        }

        [Fact]
        public void KeyChord_ToString_OrdersModifiers()
        {
            KeyChord.TryParse("shift+ctrl+alt+x", out var chord);
            Assert.Equal("Ctrl+Alt+Shift+X", chord.ToString());
        }
    }
}
