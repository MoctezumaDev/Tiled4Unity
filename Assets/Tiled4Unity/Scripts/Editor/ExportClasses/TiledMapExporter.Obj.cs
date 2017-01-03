using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Tiled4Unity
{
    // Partial class that concentrates on creating the Wavefront Mesh (.obj) string
    partial class TiledMapExporter
    {

        public struct FaceVertices
        {
            public Vector2[] Vertices { get; set; }
            public float Depth_z { get; set; }

            public Vector3 V0
            {
                get { return new Vector3(Vertices[0].x, Vertices[0].y, this.Depth_z); }
            }

            public Vector3 V1
            {
                get { return new Vector3(Vertices[1].x, Vertices[1].y, this.Depth_z); }
            }

            public Vector3 V2
            {
                get { return new Vector3(Vertices[2].x, Vertices[2].y, this.Depth_z); }
            }

            public Vector3 V3
            {
                get { return new Vector3(Vertices[3].x, Vertices[3].y, this.Depth_z); }
            }
        }

        // Creates the text for a Wavefront OBJ file for the TmxMap
        private StringWriter BuildObjString()
        {
            HashIndexOf<Vector3> vertexDatabase = new HashIndexOf<Vector3>();
            HashIndexOf<Vector2> uvDatabase = new HashIndexOf<Vector2>();

            float mapLogicalHeight = this.tmxMap.MapSizeInPixels().Height;

            // Go through every face of every mesh of every visible layer and collect vertex and texture coordinate indices as you go
            int groupCount = 0;
            StringBuilder faceBuilder = new StringBuilder();
            foreach (var layer in this.tmxMap.Layers)
            {
                if (layer.Visible != true)
                    continue;

                if (layer.Ignore == TmxLayer.IgnoreSettings.Visual)
                    continue;

                // We're going to use this layer
                ++groupCount;

                // Enumerate over the tiles in the direction given by the draw order of the map
                var verticalRange = (this.tmxMap.DrawOrderVertical == 1) ? Enumerable.Range(0, layer.Height) : Enumerable.Range(0, layer.Height).Reverse();
                var horizontalRange = (this.tmxMap.DrawOrderHorizontal == 1) ? Enumerable.Range(0, layer.Width) : Enumerable.Range(0, layer.Width).Reverse();

                foreach (TmxMesh mesh in layer.Meshes)
                {
                    Console.WriteLine("Writing '{0}' mesh group", mesh.UniqueMeshName);
                    faceBuilder.AppendFormat("\ng {0}\n", mesh.UniqueMeshName);

                    foreach (int y in verticalRange)
                    {
                        foreach (int x in horizontalRange)
                        {
                            int tileIndex = layer.GetTileIndex(x, y);
                            uint tileId = mesh.GetTileIdAt(tileIndex);

                            // Skip blank tiles
                            if (tileId == 0)
                                continue;

                            TmxTile tile = this.tmxMap.Tiles[TmxMath.GetTileIdWithoutFlags(tileId)];
                            
                            // What are the vertex and texture coorindates of this face on the mesh?
                            var position = this.tmxMap.GetMapPositionAt(x, y);
                            var vertices = CalculateFaceVertices(position, tile.TileSize, this.tmxMap.TileHeight, tile.Offset);

                            // If we're using depth shaders then we'll need to set a depth value of this face
                            float depth_z = 0.0f;
                            if (Settings.DepthBufferEnabled)
                            {
                                depth_z = position.y / mapLogicalHeight * -1.0f;
                            }

                            FaceVertices faceVertices = new FaceVertices { Vertices = vertices, Depth_z = depth_z };

                            // Is the tile being flipped or rotated (needed for texture cooridinates)
                            bool flipDiagonal = TmxMath.IsTileFlippedDiagonally(tileId);
                            bool flipHorizontal = TmxMath.IsTileFlippedHorizontally(tileId);
                            bool flipVertical = TmxMath.IsTileFlippedVertically(tileId);
                            var uvs = CalculateFaceTextureCoordinates(tile, flipDiagonal, flipHorizontal, flipVertical);

                            // Adds vertices and uvs to the database as we build the face strings
                            string v0 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V0) + 1, uvDatabase.Add(uvs[0]) + 1);
                            string v1 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V1) + 1, uvDatabase.Add(uvs[1]) + 1);
                            string v2 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V2) + 1, uvDatabase.Add(uvs[2]) + 1);
                            string v3 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V3) + 1, uvDatabase.Add(uvs[3]) + 1);
                            faceBuilder.AppendFormat("f {0} {1} {2} {3}\n", v0, v1, v2, v3);
                        }
                    }
                }
            }

            // Now go through any tile objects we may have and write them out as face groups as well
            foreach (var tmxMesh in this.tmxMap.GetUniqueListOfVisibleObjectTileMeshes())
            {
                // We're going to use this tile object
                groupCount++;

                Console.WriteLine("Writing '{0}' tile group", tmxMesh.UniqueMeshName);
                faceBuilder.AppendFormat("\ng {0}\n", tmxMesh.UniqueMeshName);

                // Get the single tile associated with this mesh
                TmxTile tmxTile = this.tmxMap.Tiles[tmxMesh.TileIds[0]];

                var vertices = CalculateFaceVertices_TileObject(tmxTile.TileSize, tmxTile.Offset);
                var uvs = CalculateFaceTextureCoordinates(tmxTile, false, false, false);

                // TileObjects have zero depth on their vertices. Their GameObject parent will set depth.
                FaceVertices faceVertices = new FaceVertices { Vertices = vertices, Depth_z = 0.0f };

                // Adds vertices and uvs to the database as we build the face strings
                string v0 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V0) + 1, uvDatabase.Add(uvs[0]) + 1);
                string v1 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V1) + 1, uvDatabase.Add(uvs[1]) + 1);
                string v2 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V2) + 1, uvDatabase.Add(uvs[2]) + 1);
                string v3 = String.Format("{0}/{1}/1", vertexDatabase.Add(faceVertices.V3) + 1, uvDatabase.Add(uvs[3]) + 1);
                faceBuilder.AppendFormat("f {0} {1} {2} {3}\n", v0, v1, v2, v3);
            }

            // All of our faces have been built and vertex and uv databases have been filled.
            // Start building out the obj file
            StringWriter objWriter = new StringWriter();
            objWriter.WriteLine("# Wavefront OBJ file automatically generated by Tiled4Unity");
            objWriter.WriteLine();

            Console.WriteLine("Writing face vertices");
            objWriter.WriteLine("# Vertices (Count = {0})", vertexDatabase.List.Count());
            foreach (var v in vertexDatabase.List)
            {
                objWriter.WriteLine("v {0} {1} {2}", v.x, v.y, v.z);
            }
            objWriter.WriteLine();

            Console.WriteLine("Writing face uv coordinates");
            objWriter.WriteLine("# Texture cooridinates (Count = {0})", uvDatabase.List.Count());
            foreach (var uv in uvDatabase.List)
            {
                objWriter.WriteLine("vt {0} {1}", uv.x, uv.y);
            }
            objWriter.WriteLine();

            // Write the one indexed normal
            objWriter.WriteLine("# Normal");
            objWriter.WriteLine("vn 0 0 -1");
            objWriter.WriteLine();

            // Now we can copy over the string used to build the databases
            objWriter.WriteLine("# Groups (Count = {0})", groupCount);
            objWriter.WriteLine(faceBuilder.ToString());

            return objWriter;
        }

        private Vector2[] CalculateFaceVertices(Vector2 mapLocation, Size tileSize, int mapTileHeight, Vector2 offset)
        {
            // Location on map is complicated by tiles that are 'higher' than the tile size given for the overall map
            mapLocation += new Vector2(0, -tileSize.Width + mapTileHeight);

            Vector2 pt0 = mapLocation;
            Vector2 pt1 = mapLocation + new Vector2(tileSize.Width, 0);
            Vector2 pt2 = mapLocation + new Vector2(tileSize.Width,tileSize.Height);
            Vector2 pt3 = mapLocation + new Vector2(0, tileSize.Height);

            // Apply the tile offset

            pt0 = TmxMath.AddPoints(pt0, offset);
            pt1 = TmxMath.AddPoints(pt1, offset);
            pt2 = TmxMath.AddPoints(pt2, offset);
            pt3 = TmxMath.AddPoints(pt3, offset);

            // We need to use ccw winding for Wavefront objects
            Vector2[] vertices  = new Vector2[4];
            vertices[3] = VectorToObjVertex(pt0);
            vertices[2] = VectorToObjVertex(pt1);
            vertices[1] = VectorToObjVertex(pt2);
            vertices[0] = VectorToObjVertex(pt3);
            return vertices;
        }

        private Vector2[] CalculateFaceVertices_TileObject(Size tileSize, Vector2 offset)
        {
            // Tile Object vertices are not concerned about where they are placed in the world
            Vector2 origin = Vector2.zero;

            Vector2 pt0 = origin;
            Vector2 pt1 = origin + new Vector2(tileSize.Width, 0);
            Vector2 pt2 = origin + new Vector2(tileSize.Width, tileSize.Height);
            Vector2 pt3 = origin + new Vector2(0, tileSize.Height);

            // Apply the tile offset

            pt0 = TmxMath.AddPoints(pt0, offset);
            pt1 = TmxMath.AddPoints(pt1, offset);
            pt2 = TmxMath.AddPoints(pt2, offset);
            pt3 = TmxMath.AddPoints(pt3, offset);

            // We need to use ccw winding for Wavefront objects
            Vector2[] vertices = new Vector2[4];
            vertices[3] = VectorToObjVertex(pt0);
            vertices[2] = VectorToObjVertex(pt1);
            vertices[1] = VectorToObjVertex(pt2);
            vertices[0] = VectorToObjVertex(pt3);
            return vertices;
        }

        private Vector2[] CalculateFaceTextureCoordinates(TmxTile tmxTile, bool flipDiagonal, bool flipHorizontal, bool flipVertical)
        {
            Vector2 imageLocation = tmxTile.LocationOnSource;
            Vector2 tileSize = new Vector2(tmxTile.TileSize.Width,tmxTile.TileSize.Height);
            Vector2 imageSize = new Vector2(tmxTile.TmxImage.Size.Width, tmxTile.TmxImage.Size.Height);

            Vector2[] points = new Vector2[4];
            points[0] = imageLocation;
            points[1] = imageLocation + new Vector2(tileSize.x, 0);
            points[2] = imageLocation + tileSize;
            points[3] = imageLocation + new Vector2(0, tileSize.y);

            Vector2 center = new Vector2(tileSize.x * 0.5f, tileSize.y * 0.5f);
            center.x += imageLocation.x;
            center.y += imageLocation.y;

            TmxMath.TransformPoints_DiagFirst(points, center, flipDiagonal, flipHorizontal, flipVertical);
            //TmxMath.TransformPoints(points, center, flipDiagonal, flipHorizontal, flipVertical);

            Vector2[] coordinates = new Vector2[4];
            coordinates[3] = PointToTextureCoordinate(points[0], imageSize);
            coordinates[2] = PointToTextureCoordinate(points[1], imageSize);
            coordinates[1] = PointToTextureCoordinate(points[2], imageSize);
            coordinates[0] = PointToTextureCoordinate(points[3], imageSize);

            // Apply a small bias to the "inner" edges of the texels
            // This keeps us from seeing seams
            //const float bias = 1.0f / 8192.0f;
            //const float bias = 1.0f / 4096.0f;
            //const float bias = 1.0f / 2048.0f;
            if (Settings.TexelBias > 0)
            {
                float bias = 1.0f / Settings.TexelBias;

                Vector2[] multiply = new Vector2[4];
                multiply[0] = new Vector2(1, 1);
                multiply[1] = new Vector2(-1, 1);
                multiply[2] = new Vector2(-1, -1);
                multiply[3] = new Vector2(1, -1);

                // This nudge has to be transformed too
                TmxMath.TransformPoints_DiagFirst(multiply, Vector2.zero, flipDiagonal, flipHorizontal, flipVertical);

                coordinates[0] = TmxMath.AddPoints(coordinates[0], TmxMath.ScalePoints(multiply[0], bias));
                coordinates[1] = TmxMath.AddPoints(coordinates[1], TmxMath.ScalePoints(multiply[1], bias));
                coordinates[2] = TmxMath.AddPoints(coordinates[2], TmxMath.ScalePoints(multiply[2], bias));
                coordinates[3] = TmxMath.AddPoints(coordinates[3], TmxMath.ScalePoints(multiply[3], bias));
            }

            return coordinates;
        }
    }
}
