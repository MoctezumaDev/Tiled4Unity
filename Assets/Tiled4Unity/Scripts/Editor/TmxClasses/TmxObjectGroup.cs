using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled4Unity
{
    public partial class TmxObjectGroup : TmxLayerBase
    {
        public string Name { get; private set; }
        public bool Visible { get; private set; }
        public List<TmxObject> Objects { get; private set; }
        public Color Color { get; private set; }
        public Vector2 Offset { get; private set; }

        public TmxObjectGroup()
        {
            this.Objects = new List<TmxObject>();
        }

        public Rect GetWorldBounds(Vector2 translation)
        {
            Rect bounds = new Rect();
            foreach (var obj in this.Objects)
            {
                Rect objBounds = obj.GetWorldBounds();
                objBounds.position += translation;

                float maxX = Mathf.Max(bounds.max.x,objBounds.max.x);

                float xMin = Mathf.Min(bounds.xMin, objBounds.xMin);
                float xMax = Mathf.Max(bounds.xMax, objBounds.xMax);
                float yMin = Mathf.Min(bounds.yMin, objBounds.yMin);
                float yMax = Mathf.Max(bounds.yMax, objBounds.yMax);
                float width = xMax - xMin;
                float height = yMax - yMin;

                bounds = new Rect(xMin, yMin, width, height);
            }
            return bounds;
        }

        public Rect GetWorldBounds()
        {
            return GetWorldBounds(new Vector2(0, 0));
        }

        public override string ToString()
        {
            return String.Format("{{ ObjectGroup name={0}, numObjects={1} }}", this.Name, this.Objects.Count());
        }

    }
}
