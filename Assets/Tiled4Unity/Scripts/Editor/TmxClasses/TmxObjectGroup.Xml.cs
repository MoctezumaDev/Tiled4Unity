﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tiled4Unity
{
    public partial class TmxObjectGroup
    {
        public static TmxObjectGroup FromXml(XElement xml, TmxMap tmxMap)
        {
            Debug.Assert(xml.Name == "objectgroup");

            TmxObjectGroup tmxObjectGroup = new TmxObjectGroup();

            // Order within Xml file is import for layer types
            tmxObjectGroup.XmlElementIndex = xml.NodesBeforeSelf().Count();

            tmxObjectGroup.Name = TmxHelper.GetAttributeAsString(xml, "name", "");
            tmxObjectGroup.Visible = TmxHelper.GetAttributeAsInt(xml, "visible", 1) == 1;
            tmxObjectGroup.Color = TmxHelper.GetAttributeAsColor(xml, "color", new Color(128, 128, 128));
            tmxObjectGroup.Properties = TmxProperties.FromXml(xml);

            Vector2 offset = new Vector2(0, 0);
            offset.x = TmxHelper.GetAttributeAsFloat(xml, "offsetx", 0);
            offset.y = TmxHelper.GetAttributeAsFloat(xml, "offsety", 0);
            tmxObjectGroup.Offset = offset;

            // Get all the objects
            Console.WriteLine("Parsing objects in object group '{0}'", tmxObjectGroup.Name);
            var objects = from obj in xml.Elements("object")
                          select TmxObject.FromXml(obj, tmxObjectGroup, tmxMap);

            tmxObjectGroup.Objects = objects.ToList();

            // Are we using a unity:layer override?
            tmxObjectGroup.UnityLayerOverrideName = tmxObjectGroup.Properties.GetPropertyValueAsString("unity:layer", "");

            return tmxObjectGroup;
        }

    }
}
