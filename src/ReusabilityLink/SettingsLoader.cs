using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ReusabilityLink.Core;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>Reads Settings.cfg into LinkSettings and persists the monitor window state in PluginData.</summary>
    public static class SettingsLoader
    {
        const string NodeName = "REUSABILITY_LINK";
        const string WindowNodeName = "WINDOW";

        static string ModPath => Path.Combine(KSPUtil.ApplicationRootPath, "GameData/ReusabilityLink");
        static string SettingsPath => Path.Combine(ModPath, "Settings.cfg");
        static string WindowPath => Path.Combine(ModPath, "PluginData/Window.cfg");

        public static LinkSettings Load()
        {
            ConfigNode node = null;
            var nodes = GameDatabase.Instance != null ? GameDatabase.Instance.GetConfigNodes(NodeName) : null;
            if (nodes != null && nodes.Length > 0)
                node = nodes[0];
            else if (File.Exists(SettingsPath))
                node = ConfigNode.Load(SettingsPath)?.GetNode(NodeName);

            if (node == null)
            {
                Log.Warn("Settings.cfg no encontrado, se usan los valores por defecto");
                return new LinkSettings();
            }

            var pairs = new List<KeyValuePair<string, string>>();
            foreach (ConfigNode.Value value in node.values)
                pairs.Add(new KeyValuePair<string, string>(value.name, value.value));

            var warnings = new List<string>();
            var settings = LinkSettings.FromValues(pairs, warnings);
            foreach (var warning in warnings) Log.Warn("Settings.cfg: " + warning);
            return settings;
        }

        public static void SaveWindow(Rect r, bool visible)
        {
            var window = new ConfigNode(WindowNodeName);
            window.AddValue("x", r.x.ToString(CultureInfo.InvariantCulture));
            window.AddValue("y", r.y.ToString(CultureInfo.InvariantCulture));
            window.AddValue("visible", visible.ToString());
            var root = new ConfigNode();
            root.AddNode(window);
            Directory.CreateDirectory(Path.GetDirectoryName(WindowPath));
            root.Save(WindowPath);
        }

        public static bool TryLoadWindow(out Rect r, out bool visible)
        {
            r = default;
            visible = true;
            if (!File.Exists(WindowPath)) return false;
            var window = ConfigNode.Load(WindowPath)?.GetNode(WindowNodeName);
            if (window == null) return false;
            if (!float.TryParse(window.GetValue("x"), NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return false;
            if (!float.TryParse(window.GetValue("y"), NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) return false;
            bool.TryParse(window.GetValue("visible"), out visible);
            r = new Rect(x, y, 0, 0);
            return true;
        }
    }
}
