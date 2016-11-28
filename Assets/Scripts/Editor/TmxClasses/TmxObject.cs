using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Tiled2Unity
{
    public abstract partial class TmxObject : TmxHasProperties
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool Visible { get; private set; }
        public Vector2 Position { get; private set; }
        public SizeF Size { get; private set; }
        public float Rotation { get; private set; }
        public TmxProperties Properties { get; private set; }
        public TmxObjectGroup ParentObjectGroup { get; private set; }

        public string GetNonEmptyName()
        {
            if (String.IsNullOrEmpty(this.Name))
                return InternalGetDefaultName();
            return this.Name;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} pos={2}, size={3} rot = {4}", GetType().Name, GetNonEmptyName(), this.Position, this.Size, this.Rotation);
        }

        public void BakeRotation()
        {
            // Rotate (0, 0)
            Vector2[] Vector2s = new Vector2[1] { Vector2.zero };
            TmxMath.RotatePoints(Vector2s, this);

            // Bake that rotation into our position, sanitizing the result
            float x = this.Position.x - Vector2s[0].x;
            float y = this.Position.y - Vector2s[0].y;
            this.Position = new Vector2(x, y);
            this.Position = TmxMath.Sanitize(this.Position);

            // Null out our rotation
            this.Rotation = 0;
        }

        static protected void CopyBaseProperties(TmxObject from, TmxObject to)
        {
            to.Name = from.Name;
            to.Type = from.Type;
            to.Visible = from.Visible;
            to.Position = from.Position;
            to.Size = from.Size;
            to.Rotation = from.Rotation;
            to.Properties = from.Properties;
            to.ParentObjectGroup = from.ParentObjectGroup;
        }

        public abstract Rect GetWorldBounds();
        protected abstract void InternalFromXml(XElement xml, TmxMap tmxMap);
        protected abstract string InternalGetDefaultName();
    }
}
