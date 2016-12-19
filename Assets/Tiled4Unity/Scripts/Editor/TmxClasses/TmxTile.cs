﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Tiled4Unity
{
    public partial class TmxTile : TmxHasProperties
    {
        public uint GlobalId { get; private set; }
        public uint LocalId { get; private set; }
        public Size TileSize { get; private set; }
        public Vector2 Offset { get; set; }
        public TmxImage TmxImage { get; private set; }
        public Vector2 LocationOnSource { get; private set; }
        public TmxProperties Properties { get; private set; }
        public TmxObjectGroup ObjectGroup { get; private set; }
        public TmxAnimation Animation { get; private set; }

        // Some tiles may be represented as a mesh for tile objects (a list is needed for animations)
        public List<TmxMesh> Meshes { get; set; }


        public TmxTile(uint globalId, uint localId, string tilesetName, TmxImage tmxImage)
        {
            this.GlobalId = globalId;
            this.LocalId = localId;
            this.TmxImage = tmxImage;
            this.Properties = new TmxProperties();
            this.ObjectGroup = new TmxObjectGroup();
            this.Animation = TmxAnimation.FromTileId(globalId);
            this.Meshes = new List<TmxMesh>();
        }

        public bool IsEmpty
        {
            get
            {
                return this.GlobalId == 0 && this.LocalId == 0;
            }
        }

        public void SetTileSize(int width, int height)
        {
            this.TileSize = new Size(width, height);
        }

        public void SetLocationOnSource(int x, int y)
        {
            this.LocationOnSource = new Vector2(x, y);
        }

        public override string ToString()
        {
            return String.Format("{{id = {0}, source({1})}}", this.GlobalId, this.LocationOnSource);
        }

    }
}
