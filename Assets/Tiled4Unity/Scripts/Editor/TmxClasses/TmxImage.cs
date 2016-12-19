using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled4Unity
{
    public partial class TmxImage
    {
        public string AbsolutePath { get; private set; }
        public Size Size { get; private set; }
        public String TransparentColor { get; set; }

#if !TILED_2_UNITY_LITE
        public Texture ImageBitmap { get; private set; }
#endif
    }
}
