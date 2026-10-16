using System.Collections.Generic;
using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class KeyConflictsTests
    {
        static KeyChord Chord(string text)
        {
            KeyChord.TryParse(text, out var chord);
            return chord;
        }

        static readonly KeyValuePair<string, string>[] StockBindings =
        {
            new KeyValuePair<string, string>("BRAKES", "B"),
            new KeyValuePair<string, string>("LANDING_GEAR", "G"),
            new KeyValuePair<string, string>("UNUSED", "None"),
        };

        [Fact]
        public void Find_SameKeyIgnoringModifiers_IsConflict()
        {
            // KSP's KeyBinding ignores modifiers, so Alt+B still fires BRAKES.
            Assert.Equal(new List<string> { "BRAKES" }, KeyConflicts.Find(Chord("Alt+B"), StockBindings));
        }

        [Fact]
        public void Find_FreeKey_HasNoConflict()
        {
            Assert.Empty(KeyConflicts.Find(Chord("Alt+Y"), StockBindings));
        }

        [Fact]
        public void Find_IsCaseInsensitive()
        {
            var bindings = new[] { new KeyValuePair<string, string>("X", "g") };
            Assert.Single(KeyConflicts.Find(Chord("Alt+G"), bindings));
        }
    }
}
