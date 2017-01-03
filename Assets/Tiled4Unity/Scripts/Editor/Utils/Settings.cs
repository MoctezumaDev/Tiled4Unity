using UnityEngine;
using System.Collections;

namespace Tiled4Unity
{
    static class Settings
    {
        public const string Version = "0.0.1";
        public const int Scale = 1;

        public static bool PreferConvexPolygons = false;
        public static bool DepthBufferEnabled = false;

        public static readonly float DefaultTexelBias = 2048;
        public static float TexelBias = DefaultTexelBias;
    }
}
