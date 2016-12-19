using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Tiled4Unity
{
    public class TmxObjectEllipse : TmxObject
    {
        public bool IsCircle()
        {
            return (this.Size.Width == this.Size.Height);
        }

        public float Radius
        {
            get
            {
                Debug.Assert(IsCircle());
                return this.Size.Width * 0.5f;
            }
        }

        public override Rect GetWorldBounds()
        {
            return new Rect(this.Position.x, this.Position.y, this.Size.Width, this.Size.Height);
        }

        protected override void InternalFromXml(System.Xml.Linq.XElement xml, TmxMap tmxMap)
        {
            // No extra data for ellipses
        }

        protected override string InternalGetDefaultName()
        {
            if (IsCircle())
                return "CircleObject";
            return "EllipseObject";
        }

    }
}
