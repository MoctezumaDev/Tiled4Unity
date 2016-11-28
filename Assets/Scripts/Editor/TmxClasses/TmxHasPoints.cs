using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled2Unity
{
    public interface TmxHasPoints
    {
        List<Vector2> Points { get; set; }
        bool ArePointsClosed();
    }
}
