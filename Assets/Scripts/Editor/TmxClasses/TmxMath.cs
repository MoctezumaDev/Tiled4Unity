using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

// Helper utitlities for performing math within a Tiled context
namespace Tiled2Unity
{
    public class TmxMath
    {
        static public readonly uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
        static public readonly uint FLIPPED_VERTICALLY_FLAG = 0x40000000;
        static public readonly uint FLIPPED_DIAGONALLY_FLAG = 0x20000000;

        static public uint GetTileIdWithoutFlags(uint tileId)
        {
            return tileId & ~(FLIPPED_HORIZONTALLY_FLAG | FLIPPED_VERTICALLY_FLAG | FLIPPED_DIAGONALLY_FLAG);
        }

        static public bool IsTileFlippedDiagonally(uint tileId)
        {
            return (tileId & FLIPPED_DIAGONALLY_FLAG) != 0;
        }

        static public bool IsTileFlippedHorizontally(uint tileId)
        {
            return (tileId & FLIPPED_HORIZONTALLY_FLAG) != 0;
        }

        static public bool IsTileFlippedVertically(uint tileId)
        {
            return (tileId & FLIPPED_VERTICALLY_FLAG) != 0;
        }

        static public void RotatePoints(Vector2[] points, TmxObject tmxObject)
        {
            TranslatePoints(points, -tmxObject.Position.x, -tmxObject.Position.y);

            TmxRotationMatrix rotate = new TmxRotationMatrix(-tmxObject.Rotation);
            rotate.TransformPoints(points);

            TranslatePoints(points, tmxObject.Position.x, tmxObject.Position.y);
        }

        static public void TransformPoints(Vector2[] points, Vector2 origin, bool diagonal, bool horizontal, bool vertical)
        {
            // Put the points into origin/local space
            TranslatePoints(points, -origin.x, -origin.y);

            TmxRotationMatrix rotate = new TmxRotationMatrix();

            // Apply the flips/rotations (order matters)
            if (horizontal)
            {
                TmxRotationMatrix h = new TmxRotationMatrix(-1, 0, 0, 1);
                rotate = TmxRotationMatrix.Multiply(h, rotate);
            }
            if (vertical)
            {
                TmxRotationMatrix v = new TmxRotationMatrix(1, 0, 0, -1);
                rotate = TmxRotationMatrix.Multiply(v, rotate);
            }
            if (diagonal)
            {
                TmxRotationMatrix d = new TmxRotationMatrix(0, 1, 1, 0);
                rotate = TmxRotationMatrix.Multiply(d, rotate);
            }

            // Apply the combined flip/rotate transformation
            rotate.TransformPoints(points);

            // Put points back into world space
            TranslatePoints(points, origin.x, origin.y);
        }

        // Hack function to do diaonal flip first in transformations
        static public void TransformPoints_DiagFirst(Vector2[] points, Vector2 origin, bool diagonal, bool horizontal, bool vertical)
        {
            // Put the points into origin/local space
            TranslatePoints(points, -origin.x, -origin.y);

            TmxRotationMatrix rotate = new TmxRotationMatrix();

            // Apply the flips/rotations (order matters)
            if (diagonal)
            {
                TmxRotationMatrix d = new TmxRotationMatrix(0, 1, 1, 0);
                rotate = TmxRotationMatrix.Multiply(d, rotate);
            }
            if (horizontal)
            {
                TmxRotationMatrix h = new TmxRotationMatrix(-1, 0, 0, 1);
                rotate = TmxRotationMatrix.Multiply(h, rotate);
            }
            if (vertical)
            {
                TmxRotationMatrix v = new TmxRotationMatrix(1, 0, 0, -1);
                rotate = TmxRotationMatrix.Multiply(v, rotate);
            }

            // Apply the combined flip/rotate transformation
            rotate.TransformPoints(points);

            // Put points back into world space
            TranslatePoints(points, origin.x, origin.y);
        }

        static public void TranslatePoints(Vector2[] points, float tx, float ty)
        {
            TranslatePoints(points, new Vector2(tx, ty));
        }

        static public void TranslatePoints(Vector2[] points, Vector2 translate)
        {
            for (int p = 0; p < points.Length; ++p)
            {
                points[p] = points[p] + translate;
            }
        }

        static public bool DoStaggerX(TmxMap tmxMap, int x)
        {
            int staggerX = (tmxMap.StaggerAxis == TmxMap.MapStaggerAxis.x) ? 1 : 0;
            int staggerEven = (tmxMap.StaggerIndex == TmxMap.MapStaggerIndex.Even) ? 1 : 0;

            return staggerX != 0 && ((x & 1) ^ staggerEven) != 0;
        }

        static public bool DoStaggerY(TmxMap tmxMap, int y)
        {
            int staggerX = (tmxMap.StaggerAxis == TmxMap.MapStaggerAxis.x) ? 1 : 0;
            int staggerEven = (tmxMap.StaggerIndex == TmxMap.MapStaggerIndex.Even) ? 1 : 0;

            return staggerX == 0 && ((y & 1) ^ staggerEven) != 0;
        }

        static public Vector2 TileCornerInGridCoordinates(TmxMap tmxMap, int x, int y)
        {
            // Support different map display types (orthographic, isometric, etc..)
            // Note: simulates "tileToScreenCoords" function from Tiled source
            if (tmxMap.Orientation == TmxMap.MapOrientation.Isometric)
            {
                Vector2 Vector2 = Vector2.zero;

                int origin_x = tmxMap.Height * tmxMap.TileWidth / 2;
                float final_x = (x - y) * tmxMap.TileWidth / 2 + origin_x;
                float final_y = (x + y) * tmxMap.TileHeight / 2;

                Vector2 point = new Vector2(final_x, final_y);

                return point;
            }
            else if (tmxMap.Orientation == TmxMap.MapOrientation.Staggered || tmxMap.Orientation == TmxMap.MapOrientation.Hexagonal)
            {
                Vector2 point = Vector2.zero;

                int tileWidth = tmxMap.TileWidth & ~1;
                int tileHeight = tmxMap.TileHeight & ~1;

                int sideLengthX = tmxMap.StaggerAxis == TmxMap.MapStaggerAxis.x ? tmxMap.HexSideLength : 0;
                int sideLengthY = tmxMap.StaggerAxis == TmxMap.MapStaggerAxis.y ? tmxMap.HexSideLength : 0;

                int sideOffsetX = (tileWidth - sideLengthX) / 2;
                int sideOffsetY = (tileHeight - sideLengthY) / 2;

                int columnWidth = sideOffsetX + sideLengthX;
                int rowHeight = sideOffsetY + sideLengthY;

                if (tmxMap.StaggerAxis == TmxMap.MapStaggerAxis.x)
                {
                    point.y = y * (tileHeight + sideLengthY);
                    if (TmxMath.DoStaggerX(tmxMap, x))
                    {
                        point.y += rowHeight;
                    }

                    point.x = x * columnWidth;
                }
                else
                {
                    point.x = x * (tileWidth + sideLengthX);
                    if (TmxMath.DoStaggerY(tmxMap, y))
                    {
                        point.x += columnWidth;
                    }

                    point.y = y * rowHeight;
                }

                point.x += tileWidth / 2;

                return point;
            }

            // Default orthographic orientation
            return new Vector2(x * tmxMap.TileWidth, y * tmxMap.TileHeight);
        }

        static public Vector2 TileCornerInScreenCoordinates(TmxMap tmxMap, int x, int y)
        {
            Vector2 point = TileCornerInGridCoordinates(tmxMap, x, y);

            if (tmxMap.Orientation != TmxMap.MapOrientation.Orthogonal)
            {
                Vector2 offset = new Vector2(-tmxMap.TileWidth / 2, 0);
                point = point + offset;
            }

            return point;
        }

        static public Vector2 ObjectVector2ToMapSpace(TmxMap tmxMap, float x, float y)
        {
            return ObjectVector2ToMapSpace(tmxMap, new Vector2(x, y));
        }

        static public Vector2 ObjectVector2ToMapSpace(TmxMap tmxMap, Vector2 pt)
        {
            if (tmxMap.Orientation == TmxMap.MapOrientation.Isometric)
            {
                Vector2 xf = Vector2.zero;

                float origin_x = tmxMap.Height * tmxMap.TileWidth * 0.5f;
                float tile_y = pt.y / tmxMap.TileHeight;
                float tile_x = pt.x / tmxMap.TileHeight;

                xf.x = (tile_x - tile_y) * tmxMap.TileWidth * 0.5f + origin_x;
                xf.y = (tile_x + tile_y) * tmxMap.TileHeight * 0.5f;
                return xf;
            }

            // Other maps types don't transform object points
            return pt;
        }

        public static Vector2 AddPoints(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }

        public static Vector2 ScalePoints(Vector2 p, float s)
        {
            return new Vector2(p.x * s, p.y * s);
        }

        public static List<Vector2> GetPointsInMapSpace(TmxMap tmxMap, TmxHasPoints objectWithPoints)
        {
            Vector2 local = TmxMath.ObjectVector2ToMapSpace(tmxMap, 0, 0);
            local.x = -local.x;
            local.y = -local.y;

            List<Vector2> xfPoints = objectWithPoints.Points.Select(pt => TmxMath.ObjectVector2ToMapSpace(tmxMap, pt)).ToList();
            xfPoints = xfPoints.Select(pt => TmxMath.AddPoints(pt, local)).ToList();
            return xfPoints;
        }

        // We don't want ugly floating Vector2 issues. Take for granted that sanitized values can be rounded to nearest 1/256th of value
        public static float Sanitize(float v)
        {
            return (float)Math.Round(v * 256) / 256.0f;
        }

        public static Vector2 Sanitize(Vector2 pt)
        {
            return new Vector2(Sanitize(pt.x), Sanitize(pt.y));
        }
    }
}
