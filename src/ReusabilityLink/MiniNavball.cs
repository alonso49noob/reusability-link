using System;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>
    /// Simplified attitude indicator: sky/ground split by a horizon offset by pitch and rotated by roll,
    /// a fixed nose cross, and prograde/retrograde markers. Textures are generated in code.
    /// </summary>
    public static class MiniNavball
    {
        static Texture2D sky, ground, line, prograde, retrograde;

        public static void Draw(Rect area, TelemetrySnapshot t)
        {
            EnsureTextures();
            float radius = area.width / 2f;
            var center = new Vector2(radius, radius);

            GUI.BeginGroup(area);
            GUI.DrawTexture(new Rect(0, 0, area.width, area.height), sky);

            // Pitching up moves the horizon down on screen; rolling right turns the world counter-clockwise.
            float horizonY = center.y + (float)Math.Sin(t.Attitude.PitchDeg * Math.PI / 180.0) * radius;
            var savedMatrix = GUI.matrix;
            // The pivot is in local GUI coordinates (Unity unclips it through the current group).
            GUIUtility.RotateAroundPivot(-(float)t.Attitude.RollDeg, center);
            float big = area.width * 3f;
            GUI.DrawTexture(new Rect(center.x - big / 2f, horizonY, big, big), ground);
            GUI.DrawTexture(new Rect(center.x - big / 2f, horizonY - 1f, big, 2f), line);
            GUI.matrix = savedMatrix;

            GUI.DrawTexture(new Rect(center.x - 10f, center.y - 1f, 20f, 2f), line);
            GUI.DrawTexture(new Rect(center.x - 1f, center.y - 10f, 2f, 20f), line);
            DrawMarker(t.Prograde, prograde, center, radius);
            DrawMarker(t.Retrograde, retrograde, center, radius);
            GUI.EndGroup();
        }

        static void DrawMarker(Core.NavballMarker marker, Texture2D texture, Vector2 center, float radius)
        {
            if (!marker.Visible) return;
            float x = center.x + (float)marker.X * radius;
            float y = center.y - (float)marker.Y * radius;
            GUI.DrawTexture(new Rect(x - texture.width / 2f, y - texture.height / 2f, texture.width, texture.height), texture);
        }

        static void EnsureTextures()
        {
            if (sky != null) return;
            sky = Solid(new Color(0.20f, 0.45f, 0.80f));
            ground = Solid(new Color(0.50f, 0.32f, 0.15f));
            line = Solid(Color.white);
            prograde = Ring(14, new Color(0.2f, 1f, 0.2f));
            retrograde = Cross(14, new Color(1f, 0.85f, 0.1f));
        }

        static Texture2D Solid(Color c)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        static Texture2D Ring(int size, Color c)
        {
            var tex = Blank(size);
            float r = size / 2f - 1f;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    if (Mathf.Abs(d - r + 1f) < 1.2f) tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        static Texture2D Cross(int size, Color c)
        {
            var tex = Blank(size);
            for (int i = 1; i < size - 1; i++)
            {
                tex.SetPixel(i, i, c);
                tex.SetPixel(i, size - 1 - i, c);
                tex.SetPixel(i + 1 < size ? i + 1 : i, i, c);
                tex.SetPixel(i + 1 < size ? i + 1 : i, size - 1 - i, c);
            }
            tex.Apply();
            return tex;
        }

        static Texture2D Blank(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var clear = new Color(0, 0, 0, 0);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, clear);
            return tex;
        }
    }
}
