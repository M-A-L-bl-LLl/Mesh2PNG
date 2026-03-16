using UnityEditor;
using UnityEngine;

namespace Tools.Mesh2PNG
{
    internal static class Mesh2PNGIcon
    {
        private const string PackageIconPath = "Packages/com.mesh2png/Editor/Icons/Mesh2PNG.png";

        private static Texture2D _cache;

        internal static Texture2D Get()
        {
            if (_cache != null) return _cache;

            _cache = AssetDatabase.LoadAssetAtPath<Texture2D>(PackageIconPath);
            if (_cache == null)
                _cache = Generate();

            return _cache;
        }

        // Generates a simple 32x32 icon: dark rounded rect + white frame + blue mesh triangle.
        private static Texture2D Generate()
        {
            const int S    = 32;
            var bgColor     = new Color(0.18f, 0.22f, 0.32f, 1f);
            var frameColor  = new Color(1f,    1f,    1f,    0.85f);
            var meshColor   = new Color(0.35f, 0.72f, 1.00f, 1f);

            var pixels = new Color[S * S];

            // Rounded rect background (skip 3px corners)
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var corner = (x < 3 || x >= S - 3) && (y < 3 || y >= S - 3);
                pixels[y * S + x] = corner ? Color.clear : bgColor;
            }

            // White inner border (1px)
            for (var y = 3; y < S - 3; y++)
            for (var x = 3; x < S - 3; x++)
            {
                if (x == 3 || x == S - 4 || y == 3 || y == S - 4)
                    pixels[y * S + x] = frameColor;
            }

            // Wireframe triangle: apex at top-center, base at bottom
            DrawLine(pixels, S,  16, S - 8,  6,  8, meshColor); // left edge
            DrawLine(pixels, S,  16, S - 8, 26,  8, meshColor); // right edge
            DrawLine(pixels, S,   6,     8, 26,  8, meshColor); // base

            // Horizontal midline (inner detail)
            DrawLine(pixels, S, 10, 17, 22, 17, new Color(0.35f, 0.72f, 1f, 0.5f));

            var tex = new Texture2D(S, S, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void DrawLine(Color[] pixels, int S, int x0, int y0, int x1, int y1, Color c)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < S && y0 >= 0 && y0 < S)
                    pixels[y0 * S + x0] = c;

                if (x0 == x1 && y0 == y1) break;

                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }
    }
}
