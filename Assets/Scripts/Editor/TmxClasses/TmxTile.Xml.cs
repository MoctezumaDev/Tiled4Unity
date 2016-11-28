using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tiled2Unity
{
    // partial class methods that build tile data from xml
    partial class TmxTile
    {
        public void ParseTileXml(XElement elem, TmxMap tmxMap, uint firstId)
        {
            Console.WriteLine("Parse tile data (gid = {0}, id {1}) ...", this.GlobalId, this.LocalId);

            this.Properties = TmxProperties.FromXml(elem);

            // Do we have an object group for this tile?
            XElement elemObjectGroup = elem.Element("objectgroup");
            if (elemObjectGroup != null)
            {
                this.ObjectGroup = TmxObjectGroup.FromXml(elemObjectGroup, tmxMap);
                FixTileColliderObjects(tmxMap);
            }

            // Is this an animated tile?
            XElement elemAnimation = elem.Element("animation");
            if (elemAnimation != null)
            {
                this.Animation = TmxAnimation.FromXml(elemAnimation, firstId);
            }
        }

        private void FixTileColliderObjects(TmxMap tmxMap)
        {
            // Objects inside of tiles are colliders that will be merged with the colliders on neighboring tiles.
            // In order to promote this merging we have to perform the following clean up operations ...
            // - All rectangles objects are made into polygon objects
            // - All polygon objects will have their rotations burned into the polygon points (and Rotation set to zero)
            // - All cooridinates will be "sanitized" to make up for floating Vector2 errors due to rotation and poor placement of colliders
            // (The sanitation will round all numbers to the nearest 1/256th)

            // Replace rectangles with polygons
            for (int i = 0; i < this.ObjectGroup.Objects.Count; i++)
            {
                TmxObject tmxObject = this.ObjectGroup.Objects[i];
                if (tmxObject is TmxObjectRectangle)
                {
                    TmxObjectPolygon tmxObjectPolygon = TmxObjectPolygon.FromRectangle(tmxMap, tmxObject as TmxObjectRectangle);
                    this.ObjectGroup.Objects[i] = tmxObjectPolygon;
                }
            }

            // Burn rotation into all polygon points, sanitizing the Vector2 locations as we go
            foreach (TmxObject tmxObject in this.ObjectGroup.Objects)
            {
                TmxHasPoints tmxHasPoints = tmxObject as TmxHasPoints;
                if (tmxHasPoints != null)
                {
                    Vector2[] Vector2s = tmxHasPoints.Points.ToArray();

                    // Rotate our points by the rotation and position in the object
                    TmxMath.RotatePoints(Vector2s, tmxObject);

                    // Sanitize our points to make up for floating Vector2 precision errors
                    for (int i = 0; i < Vector2s.Length; i++)
                    {
                        Vector2s[i] = TmxMath.Sanitize(Vector2s[i]);
                    }

                    // Set the points back into the object
                    tmxHasPoints.Points = Vector2s.ToList();

                    // Zero out our rotation
                    tmxObject.BakeRotation();
                }
            }
        }

    }
}
