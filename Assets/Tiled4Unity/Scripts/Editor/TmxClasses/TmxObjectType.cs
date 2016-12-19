﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Tiled4Unity
{
    // Has data for a single object type
    public class TmxObjectType
    {
        public string Name { get; private set; }
        public Color Color { get; private set; }
        public Dictionary<string, TmxObjectTypeProperty> Properties { get; private set; }

        public TmxObjectType()
        {
            this.Name = "";
            this.Color = Color.gray;
            this.Properties = new Dictionary<string, TmxObjectTypeProperty>();
        }

        public static TmxObjectType FromXml(XElement xml)
        {
            TmxObjectType tmxObjectType = new TmxObjectType();

            tmxObjectType.Name = TmxHelper.GetAttributeAsString(xml, "name", "");
            tmxObjectType.Color = TmxHelper.GetAttributeAsColor(xml, "color", Color.gray);
            tmxObjectType.Properties = TmxObjectTypeProperty.FromObjectTypeXml(xml);

            return tmxObjectType;
        }
    }
}
