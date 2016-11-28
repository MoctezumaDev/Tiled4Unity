using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled2Unity
{
    public class TmxObjectPolyline : TmxObject, TmxHasPoints
    {
        public List<Vector2> Points { get; set; }

        public TmxObjectPolyline()
        {
            this.Points = new List<Vector2>();
        }

        public override Rect GetWorldBounds()
        {
            float xmin = float.MaxValue;
            float xmax = float.MinValue;
            float ymin = float.MaxValue;
            float ymax = float.MinValue;

            foreach (var p in this.Points)
            {
                xmin = Math.Min(xmin, p.x);
                xmax = Math.Max(xmax, p.x);
                ymin = Math.Min(ymin, p.y);
                ymax = Math.Max(ymax, p.y);
            }

            Rect bounds = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
            bounds.position += this.Position;
            return bounds;
        }

        protected override void InternalFromXml(System.Xml.Linq.XElement xml, TmxMap tmxMap)
        {
            Debug.Assert(xml.Name == "object");
            Debug.Assert(xml.Element("polyline") != null);

            var points = from pt in xml.Element("polyline").Attribute("points").Value.Split(' ')
                         let x = float.Parse(pt.Split(',')[0])
                         let y = float.Parse(pt.Split(',')[1])
                         select new Vector2(x, y);

            this.Points = points.ToList();
        }

        protected override string InternalGetDefaultName()
        {
            return "PolylineObject";
        }

        public bool ArePointsClosed()
        {
            // Lines are open
            return false;
        }
    }
}
