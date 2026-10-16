using System;
using System.Collections.Generic;

namespace ReusabilityLink.Core
{
    /// <summary>Finds stock key bindings that share the switch key (KSP bindings ignore modifiers).</summary>
    public static class KeyConflicts
    {
        /// <param name="bindings">Binding name → key name, as KSP's KeyCode names.</param>
        public static List<string> Find(KeyChord chord, IEnumerable<KeyValuePair<string, string>> bindings)
        {
            var conflicts = new List<string>();
            foreach (var binding in bindings)
                if (string.Equals(binding.Value, chord.Key, StringComparison.OrdinalIgnoreCase))
                    conflicts.Add(binding.Key);
            return conflicts;
        }
    }
}
