using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled2Unity
{
    public class TmxObjectTile : TmxObject
    {
        public TmxTile Tile { get; private set; }
        public bool FlippedHorizontal { get; private set; }
        public bool FlippedVertical { get; private set; }

        public string SortingLayerName { get; private set; }
        public int? SortingOrder { get; private set; }

        public TmxObjectTile()
        {
            this.SortingLayerName = null;
        }

        public override Rect GetWorldBounds()
        {
            Rect myBounds = new Rect(this.Position.x, this.Position.y - this.Size.Height, this.Size.Width, this.Size.Height);

            Rect groupBounds = this.Tile.ObjectGroup.GetWorldBounds(this.Position);
            if (groupBounds.width == 0 || groupBounds.height == 0)
            {
                return myBounds;
            }
            float xMin = Mathf.Min(myBounds.xMin, groupBounds.xMin);
            float xMax = Mathf.Max(myBounds.xMax, groupBounds.xMax);
            float yMin = Mathf.Min(myBounds.yMin, groupBounds.yMin);
            float yMax = Mathf.Max(myBounds.yMax, groupBounds.yMax);
            float width = xMax - xMin;
            float height = yMax - yMin;

            Rect combinedBounds = new Rect(xMin, yMin, width, height);
            return combinedBounds;
        }

        public override string ToString()
        {
            return String.Format("{{ TmxObjectTile: name={0}, pos={1}, tile={2} }}", GetNonEmptyName(), this.Position, this.Tile);
        }

        public SizeF GetTileObjectScale()
        {
            float scaleX = this.Size.Width / this.Tile.TileSize.Width;
            float scaleY = this.Size.Height / this.Tile.TileSize.Height;
            return new SizeF(scaleX, scaleY);
        }

        protected override void InternalFromXml(System.Xml.Linq.XElement xml, TmxMap tmxMap)
        {
            // Get the tile
            uint gid = TmxHelper.GetAttributeAsUInt(xml, "gid");
            this.FlippedHorizontal = TmxMath.IsTileFlippedHorizontally(gid);
            this.FlippedVertical = TmxMath.IsTileFlippedVertically(gid);
            uint rawTileId = TmxMath.GetTileIdWithoutFlags(gid);

            this.Tile = tmxMap.Tiles[rawTileId];

            // The tile needs to have a mesh on it.
            // Note: The tile may already be referenced by another TmxObjectTile instance, and as such will have its mesh data already made
            if (this.Tile.Meshes.Count() == 0)
            {
                this.Tile.Meshes = TmxMesh.FromTmxTile(this.Tile, tmxMap);
            }

            // Check properties for layer placement
            if (this.Properties.PropertyMap.ContainsKey("unity:sortingLayerName"))
            {
                this.SortingLayerName = this.Properties.GetPropertyValueAsString("unity:sortingLayerName");
            }
            if (this.Properties.PropertyMap.ContainsKey("unity:sortingOrder"))
            {
                this.SortingOrder = this.Properties.GetPropertyValueAsInt("unity:sortingOrder");
            }
        }

        protected override string InternalGetDefaultName()
        {
            return "TileObject";
        }

    }
}
