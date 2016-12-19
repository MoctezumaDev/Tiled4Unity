﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled4Unity
{
    public class TmxObjectPolygon : TmxObject, TmxHasPoints
    {
        public List<Vector2> Points { get; set; }

        public TmxObjectPolygon()
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
            var points = from pt in xml.Element("polygon").Attribute("points").Value.Split(' ')
                         let x = float.Parse(pt.Split(',')[0])
                         let y = float.Parse(pt.Split(',')[1])
                         select new Vector2(x, y);

            this.Points = points.ToList();

            // Test if polygons are counter clocksise
            // From: http://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
            float sum = 0.0f;
            for (int i = 1; i < this.Points.Count(); i++)
            {
                var p1 = this.Points[i - 1];
                var p2 = this.Points[i];

                float v = (p2.x - p1.x) * -(p2.y + p1.y);
                sum += v;
            }

            if (sum < 0)
            {
                // Winding of polygons is counter-clockwise. Reverse the list.
                this.Points.Reverse();
            }
        }

        protected override string InternalGetDefaultName()
        {
            return "PolygonObject";
        }


        public override string ToString()
        {
            StringBuilder pts = new StringBuilder();
            if (this.Points == null)
            {
                pts.Append("<empty>");
            }
            else
            {
                foreach (var p in this.Points)
                {
                    pts.AppendFormat("({0}, {1})", p.x, p.y);
                    if (p != this.Points.Last())
                    {
                        pts.AppendFormat(", ");
                    }
                }
            }

            return String.Format("{0} {1} {2} points=({3})", GetType().Name, GetNonEmptyName(), this.Position, pts.ToString());
        }

        public bool ArePointsClosed()
        {
            return true;
        }

        static public TmxObjectPolygon FromRectangle(TmxMap tmxMap, TmxObjectRectangle tmxRectangle)
        {
            TmxObjectPolygon tmxPolygon = new TmxObjectPolygon();
            TmxObject.CopyBaseProperties(tmxRectangle, tmxPolygon);

            tmxPolygon.Points = tmxRectangle.Points;

            return tmxPolygon;
        }

    }
}
