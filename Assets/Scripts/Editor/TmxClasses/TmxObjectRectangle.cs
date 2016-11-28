using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled2Unity
{
    public class TmxObjectRectangle : TmxObjectPolygon
    {
        protected override void InternalFromXml(System.Xml.Linq.XElement xml, TmxMap tmxMap)
        {
            this.Points = new List<Vector2>();
            this.Points.Add(new Vector2(0, 0));
            this.Points.Add(new Vector2(this.Size.Width, 0));
            this.Points.Add(new Vector2(this.Size.Width, this.Size.Height));
            this.Points.Add(new Vector2(0, this.Size.Height));

            if (this.Size.Width == 0 || this.Size.Height == 0)
            {
                Console.WriteLine("Warning: Rectangle has zero width or height in object group\n{0}", xml.Parent.ToString());
            }
        }

        protected override string InternalGetDefaultName()
        {
            return "RectangleObject";
        }

    }
}
