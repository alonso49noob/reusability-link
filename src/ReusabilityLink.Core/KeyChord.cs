using System;
using System.Text;

namespace ReusabilityLink.Core
{
    /// <summary>A key plus optional Alt/Ctrl/Shift modifiers, parsed from text like "Alt+B".</summary>
    public struct KeyChord
    {
        public bool Alt;
        public bool Ctrl;
        public bool Shift;
        /// <summary>Key name in upper case, e.g. "B" or "F5".</summary>
        public string Key;

        public static bool TryParse(string text, out KeyChord chord)
        {
            chord = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var tokens = text.Split('+');
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (token.Length == 0) return false;
                bool last = i == tokens.Length - 1;
                if (!last)
                {
                    if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase)) chord.Alt = true;
                    else if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) chord.Ctrl = true;
                    else if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase)) chord.Shift = true;
                    else return false;
                }
                else
                {
                    chord.Key = token.ToUpperInvariant();
                }
            }
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Ctrl) sb.Append("Ctrl+");
            if (Alt) sb.Append("Alt+");
            if (Shift) sb.Append("Shift+");
            sb.Append(Key);
            return sb.ToString();
        }
    }
}
