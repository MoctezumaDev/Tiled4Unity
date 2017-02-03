using System;
using UnityEngine;

namespace Tiled4Unity
{
    public partial class TmxImage
    {
        public string AbsolutePath { get; private set; }
        public Size Size { get; private set; }
        public String TransparentColor { get; set; }
        public Texture ImageBitmap { get; private set; }
    }
}
