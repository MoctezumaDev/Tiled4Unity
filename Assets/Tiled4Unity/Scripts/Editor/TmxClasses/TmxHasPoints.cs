using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled4Unity
{
    public interface TmxHasPoints
    {
        List<Vector2> Points { get; set; }
        bool ArePointsClosed();
    }
}
