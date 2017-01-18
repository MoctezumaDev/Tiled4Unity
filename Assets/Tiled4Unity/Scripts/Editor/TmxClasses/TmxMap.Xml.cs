using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tiled4Unity
{
    // Partial class methods for creating TmxMap data from xml files/data
    partial class TmxMap
    {
        private string _projectPath;

        protected static string GetProjectPath(string tmxPath)
        {
            string projectPath = "";
            int index = tmxPath.LastIndexOf("/");
            if (index > 0)
            {
                projectPath = tmxPath.Substring(0, index + 1); 
            }
            return projectPath;
        }

        public static TmxMap LoadFromFile(string tmxPath)
        {
            string fullTmxPath = Path.GetFullPath(tmxPath);
            string projectPath = GetProjectPath(tmxPath);

            //using (ChDir chdir = new ChDir(fullTmxPath))
            {
                TmxMap tmxMap = new TmxMap();
                XDocument doc = tmxMap.LoadDocument(fullTmxPath);

                tmxMap.Name = Path.GetFileNameWithoutExtension(fullTmxPath);
                tmxMap.ParseMapXml(doc, projectPath);

                // We're done reading and parsing the tmx file
                Console.WriteLine("Map details: {0}", tmxMap.ToString());
                Console.WriteLine("Parsed: {0} ", fullTmxPath);

                tmxMap.IsLoaded = true;
                return tmxMap;
            }
            return null;
        }

        private XDocument LoadDocument(string xmlPath)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.ProhibitDtd = false;

            XDocument doc = null;
            Console.WriteLine("Opening {0} ...", xmlPath);
            try
            {
                XmlReader reader = XmlTextReader.Create(xmlPath, settings);
                doc = XDocument.Load(reader);
            }
            catch (FileNotFoundException fnf)
            {
                string msg = String.Format("File not found: {0}", fnf.FileName);
                throw new TmxException(msg, fnf);
            }
            catch (XmlException xml)
            {
                string msg = String.Format("Xml error in {0}\n  {1}", xmlPath, xml.Message);
                throw new TmxException(msg, xml);
            }
            return doc;
        }

        private void ParseMapXml(XDocument doc, string projectPath)
        {
            _projectPath = projectPath;

            Console.WriteLine("TMX Path = {0}", _projectPath);

            Console.WriteLine("Parsing map root ...");

            XElement map = doc.Element("map");
            try
            {
                this.Orientation = TmxHelper.GetAttributeAsEnum<MapOrientation>(map, "orientation");
                this.StaggerAxis = TmxHelper.GetAttributeAsEnum(map, "staggeraxis", MapStaggerAxis.y);
                this.StaggerIndex = TmxHelper.GetAttributeAsEnum(map, "staggerindex", MapStaggerIndex.Odd);
                this.HexSideLength = TmxHelper.GetAttributeAsInt(map, "hexsidelength", 0);
                this.DrawOrderHorizontal = TmxHelper.GetAttributeAsString(map, "renderorder", "right-down").Contains("right") ? 1 : -1;
                this.DrawOrderVertical = TmxHelper.GetAttributeAsString(map, "renderorder", "right-down").Contains("down") ? 1 : -1;
                this.Width = TmxHelper.GetAttributeAsInt(map, "width");
                this.Height = TmxHelper.GetAttributeAsInt(map, "height");
                this.TileWidth = TmxHelper.GetAttributeAsInt(map, "tilewidth");
                this.TileHeight = TmxHelper.GetAttributeAsInt(map, "tileheight");
                this.BackgroundColor = TmxHelper.GetAttributeAsColor(map, "backgroundcolor", new Color(128, 128, 128));
            }
            catch (Exception e)
            {
                TmxException.FromAttributeException(e, map);
            }

            // Collect our map properties
            this.Properties = TmxProperties.FromXml(map);

            ParseAllTilesets(doc);
            ParseAllLayers(doc);
            ParseAllObjectGroups(doc);

            // Once everything is loaded, take a moment to do additional plumbing
            ParseCompleted();
        }

        private void ParseAllTilesets(XDocument doc)
        {
            Console.WriteLine("Parsing tileset elements ...");
            var tilesets = from item in doc.Descendants("tileset")
                           select item;

            foreach (var ts in tilesets)
            {
                ParseSingleTileset(ts);
            }

            // Treat images in imagelayers as tileset with a single entry
            var imageLayers = from item in doc.Descendants("imagelayer") select item;
            foreach (var il in imageLayers)
            {
                ParseTilesetFromImageLayer(il);
            }
        }

        private void ParseSingleTileset(XElement elem)
        {
            // Parse the tileset data and populate the tiles from it
            uint firstId = TmxHelper.GetAttributeAsUInt(elem, "firstgid");

            // Does the element contain all tileset data or reference an external tileset?
            XAttribute attrSource = elem.Attribute("source");
            if (attrSource == null)
            {
                ParseInternalTileset(elem, firstId);
            }
            else
            {
                // Need to load the tileset data from an external file first
                // Then we'll parse it as if it's internal data
                ParseExternalTileset(attrSource.Value, firstId);
            }
        }

        // This method is called eventually for external tilesets too
        // Only the gid attribute has been consumed at this Vector2 for the tileset
        private void ParseInternalTileset(XElement elemTileset, uint firstId)
        {
            string tilesetName = TmxHelper.GetAttributeAsString(elemTileset, "name");

            Console.WriteLine("Parse internal tileset '{0}' (gid = {1}) ...", tilesetName, firstId);

            int tileWidth = TmxHelper.GetAttributeAsInt(elemTileset, "tilewidth");
            int tileHeight = TmxHelper.GetAttributeAsInt(elemTileset, "tileheight");
            int spacing = TmxHelper.GetAttributeAsInt(elemTileset, "spacing", 0);
            int margin = TmxHelper.GetAttributeAsInt(elemTileset, "margin", 0);

            Vector2 tileOffset = Vector2.zero;
            XElement xmlTileOffset = elemTileset.Element("tileoffset");
            if (xmlTileOffset != null)
            {
                tileOffset.x = TmxHelper.GetAttributeAsInt(xmlTileOffset, "x");
                tileOffset.y = TmxHelper.GetAttributeAsInt(xmlTileOffset, "y");
            }

            IList<TmxTile> tilesToAdd = new List<TmxTile>();

            // Tilesets may have an image for all tiles within it, or it may have an image per tile
            if (elemTileset.Element("image") != null)
            {
                TmxImage tmxImage = TmxImage.FromXml(elemTileset.Element("image"),_projectPath);

                // Create all the tiles
                // This is a bit complicated because of spacing and margin
                // (Margin is ignored from Width and Height)
                for (int end_y = margin + tileHeight; end_y <= tmxImage.Size.Height; end_y += spacing + tileHeight)
                {
                    for (int end_x = margin + tileWidth; end_x <= tmxImage.Size.Width; end_x += spacing + tileWidth)
                    {
                        uint localId = (uint) tilesToAdd.Count();
                        uint globalId = firstId + localId;
                        TmxTile tile = new TmxTile(globalId, localId, tilesetName, tmxImage);
                        tile.Offset = tileOffset;
                        tile.SetTileSize(tileWidth, tileHeight);
                        tile.SetLocationOnSource(end_x - tileWidth, end_y - tileHeight);
                        tilesToAdd.Add(tile);
                    }
                }
            }
            else
            {
                // Each tile will have it's own image
                foreach (var t in elemTileset.Elements("tile"))
                {
                    TmxImage tmxImage = TmxImage.FromXml(t.Element("image"),_projectPath);

                    uint localId = (uint)tilesToAdd.Count();

                    // Local Id can be overridden by the tile element
                    // This is because tiles can be removed from the tileset, so we won'd always have a zero-based index
                    localId = TmxHelper.GetAttributeAsUInt(t, "id", localId);

                    uint globalId = firstId + localId;
                    TmxTile tile = new TmxTile(globalId, localId, tilesetName, tmxImage);
                    tile.Offset = tileOffset;
                    tile.SetTileSize(tmxImage.Size.Width, tmxImage.Size.Height);
                    tile.SetLocationOnSource(0, 0);
                    tilesToAdd.Add(tile);
                }
            }

            StringBuilder builder = new StringBuilder();
            foreach (TmxTile tile in tilesToAdd)
            {
                builder.AppendFormat("{0}", tile.ToString());
                if (tile != tilesToAdd.Last()) builder.Append("\n");
                this.Tiles[tile.GlobalId] = tile;
            }
            Console.WriteLine("Added {0} tiles", tilesToAdd.Count);

            // Add any extra data to tiles
            foreach (var elemTile in elemTileset.Elements("tile"))
            {
                int localTileId = TmxHelper.GetAttributeAsInt(elemTile, "id");
                var tiles = from t in this.Tiles
                            where t.Value.GlobalId == localTileId + firstId
                            select t.Value;

                // Note that some old tile data may be sticking around
                if (tiles.Count() == 0)
                {
                    Console.WriteLine("Tile '{0}' in tileset '{1}' does not exist but there is tile data for it.\n{2}", localTileId, tilesetName, elemTile.ToString());
                }
                else
                {
                    tiles.First().ParseTileXml(elemTile, this, firstId);
                }
            }
        }

        private void ParseExternalTileset(string tsxPath, uint firstId)
        {
            string fullTsxPath = Path.GetFullPath(tsxPath);
            /*using (ChDir chdir = new ChDir(fullTsxPath))
            {
                XDocument tsx = LoadDocument(fullTsxPath);
                ParseInternalTileset(tsx.Root, firstId);
            }*/
        }

        private void ParseTilesetFromImageLayer(XElement elemImageLayer)
        {
            string tilesetName = TmxHelper.GetAttributeAsString(elemImageLayer, "name");

            XElement xmlImage = elemImageLayer.Element("image");
            if (xmlImage == null)
            {
                Console.WriteLine("Image Layer '{0}' has no image assigned.", tilesetName);
                return;
            }

            TmxImage tmxImage = TmxImage.FromXml(xmlImage, _projectPath);

            // The "firstId" is is always one more than all the tiles that we've already parsed (which may be zero)
            uint firstId = 1;
            if (this.Tiles.Count > 0)
            {
                firstId = this.Tiles.Max(t => t.Key) + 1;
            }
            
            uint localId = 1;
            uint globalId = firstId + localId;

            TmxTile tile = new TmxTile(globalId, localId, tilesetName, tmxImage);
            tile.SetTileSize(tmxImage.Size.Width, tmxImage.Size.Height);
            tile.SetLocationOnSource(0, 0);
            this.Tiles[tile.GlobalId] = tile;
        }

        private void ParseAllLayers(XDocument doc)
        {
            Console.WriteLine("Parsing layer elements ...");

            // Parse "layer"s and "imagelayer"s
            var layers = (from item in doc.Descendants()
                          where (item.Name == "layer" || item.Name == "imagelayer")
                          select item).ToList();

            foreach (var lay in layers)
            {
                TmxLayer tmxLayer = TmxLayer.FromXml(lay, this);

                // Layers may be ignored
                if (tmxLayer.Ignore == TmxLayer.IgnoreSettings.True)
                {
                    // We don't care about this layer
                    Console.WriteLine("Ignoring layer due to unity:ignore = True property: {0}", tmxLayer.Name);
                    continue;
                }

                this.Layers.Add(tmxLayer);
            }
        }

        private void ParseAllObjectGroups(XDocument doc)
        {
            Console.WriteLine("Parsing objectgroup elements ...");
            var groups = from item in doc.Root.Elements("objectgroup")
                         select item;

            foreach (var g in groups)
            {
                TmxObjectGroup tmxObjectGroup = TmxObjectGroup.FromXml(g, this);
                this.ObjectGroups.Add(tmxObjectGroup);
            }
        }

        private void ParseCompleted()
        {
            // Every "layer type" instance needs its sort ordering figured out
            var layers = new List<TmxLayerBase>();
            foreach(TmxLayerBase layer in Layers)
            {
                layers.Add(layer);
            }
            foreach(TmxLayerBase objectGroup in ObjectGroups)
            {
                layers.Add(objectGroup);
            }

            // We sort by the XmlElementIndex because the order in the XML file is the implicity ordering or how tiles and objects are rendered
            layers = layers.OrderBy(l => l.XmlElementIndex).ToList();

            for (int i = 0; i < layers.Count(); ++i)
            {
                TmxLayerBase layer = layers[i];
                layer.SortingLayerName = layer.Properties.GetPropertyValueAsString("unity:sortingLayerName", "");
                layer.SortingOrder = layer.Properties.GetPropertyValueAsInt("unity:sortingOrder", i);
            }
        }

    }
}
