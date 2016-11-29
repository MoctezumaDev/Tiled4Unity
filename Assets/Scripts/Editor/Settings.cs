using UnityEngine;
using System.Collections;

namespace Tiled2Unity
{
    static class Settings
    {
        public const string Version = "0.0.1";
        public const int Scale = 1;

        public static bool PreferConvexPolygons = false;
        public static bool DepthBufferEnabled = false;

        public static readonly float DefaultTexelBias = 8192.0f;
        public static float TexelBias = DefaultTexelBias;
    }
}
