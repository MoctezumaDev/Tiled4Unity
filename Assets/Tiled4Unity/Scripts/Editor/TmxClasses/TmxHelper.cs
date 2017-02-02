using System;
using UnityEngine;
using System.IO;
using System.Text;
using System.Xml.Linq;
using UnityEditor;


namespace Tiled4Unity
{
    public class TmxHelper
    {
        public static string GetAttributeAsString(XElement elem, string attrName)
        {
            return elem.Attribute(attrName).Value;
        }

        public static string GetAttributeAsString(XElement elem, string attrName, string defaultValue)
        {
            XAttribute attr = elem.Attribute(attrName);
            if (attr == null)
            {
                return defaultValue;
            }
            return GetAttributeAsString(elem, attrName);
        }

        public static int GetAttributeAsInt(XElement elem, string attrName)
        {
            return Convert.ToInt32(elem.Attribute(attrName).Value);
        }

        public static int GetAttributeAsInt(XElement elem, string attrName, int defaultValue)
        {
            XAttribute attr = elem.Attribute(attrName);
            if (attr == null)
            {
                return defaultValue;
            }
            return GetAttributeAsInt(elem, attrName);
        }

        public static uint GetAttributeAsUInt(XElement elem, string attrName)
        {
            return Convert.ToUInt32(elem.Attribute(attrName).Value);
        }

        public static uint GetAttributeAsUInt(XElement elem, string attrName, uint defaultValue)
        {
            XAttribute attr = elem.Attribute(attrName);
            if (attr == null)
            {
                return defaultValue;
            }
            return GetAttributeAsUInt(elem, attrName);
        }

        public static float GetAttributeAsFloat(XElement elem, string attrName)
        {
            return Convert.ToSingle(elem.Attribute(attrName).Value);
        }

        public static float GetAttributeAsFloat(XElement elem, string attrName, float defaultValue)
        {
            XAttribute attr = elem.Attribute(attrName);
            if (attr == null)
            {
                return defaultValue;
            }
            return GetAttributeAsFloat(elem, attrName);
        }

        public static string GetAttributeAsFullPath(XElement elem, string attrName)
        {
            return Path.GetFullPath(elem.Attribute(attrName).Value);
        }

        public static Color GetAttributeAsColor(XElement elem, string attrName)
        {
            string colorString = elem.Attribute(attrName).Value;
            Color color = TmxHelper.ColorFromHtml(colorString);
            return color;
        }

        public static Color GetAttributeAsColor(XElement elem, string attrName, Color defaultValue)
        {
            XAttribute attr = elem.Attribute(attrName);
            if (attr == null)
            {
                return defaultValue;
            }
            return GetAttributeAsColor(elem, attrName);
        }

        public static T GetStringAsEnum<T>(string enumString)
        {
            enumString = enumString.Replace("-", "_");

            T value = default(T);
            try
            {
                value = (T)Enum.Parse(typeof(T), enumString, true);
            }
            catch
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Could not convert '{0}' to enum of type '{1}'\n", enumString, typeof(T).ToString());
                msg.AppendFormat("Choices are:\n");

                foreach (T t in Enum.GetValues(typeof(T)))
                {
                    msg.AppendFormat("  {0}\n", t.ToString());
                }
                TmxException.ThrowFormat(msg.ToString());
            }

            return value;
        }

        public static T GetAttributeAsEnum<T>(XElement elem, string attrName)
        {
            string enumString = elem.Attribute(attrName).Value.Replace("-", "_");
            return GetStringAsEnum<T>(enumString);
        }

        public static T GetAttributeAsEnum<T>(XElement elem, string attrName, T defaultValue)
        {
            XAttribute attr = elem.Attribute(attrName);
            if (attr == null)
            {
                return defaultValue;
            }
            return GetAttributeAsEnum<T>(elem, attrName);
        }

        public static TmxProperties GetPropertiesWithTypeDefaults(TmxHasProperties hasProperties, TmxObjectTypes objectTypes)
        {
            TmxProperties tmxProperties = new TmxProperties();

            // Fill in all the default properties first
            // (Note: At the moment, only TmxObject has default properties it inherits from TmxObjectType)
            string objectTypeName = null;
            if (hasProperties is TmxObject)
            {
                TmxObject tmxObject = hasProperties as TmxObject;
                objectTypeName = tmxObject.Type;
            }

            // If an object type has been found then copy over all the default values for properties
            TmxObjectType tmxObjectType = objectTypes.GetValueOrNull(objectTypeName);
            if (tmxObjectType != null)
            {
                foreach (TmxObjectTypeProperty tmxTypeProp in tmxObjectType.Properties.Values)
                {
                    tmxProperties.PropertyMap[tmxTypeProp.Name] = new TmxProperty() { Name = tmxTypeProp.Name, Type = tmxTypeProp.Type, Value = tmxTypeProp.Default };
                }
            }

            // Now add all the object properties (which may override some of the default properties)
            foreach (TmxProperty tmxProp in hasProperties.Properties.PropertyMap.Values)
            {
                tmxProperties.PropertyMap[tmxProp.Name] = tmxProp;
            }

            return tmxProperties;
        }

        public static Color ColorFromHtml(string html)
        {
            Color htmlColor;
            if(ColorUtility.TryParseHtmlString(html,out htmlColor))
            {
                return htmlColor;
            }
            return Color.magenta;
        }

        // Prefer 32bpp bitmaps as they are at least 2x faster at Graphics.DrawImage functions
        // Note that 32bppPArgb is not properly supported on Mac builds.
        public static Texture CreateBitmap32bpp(int width, int height)
        {
            Texture texture = new Texture2D(width, height, TextureFormat.ARGB32,false);

            return texture;
        }

        public static string NormalizePath(string path)
        {
            string absolutepath = Path.GetFullPath(path);
            path = "Assets" + absolutepath.Substring(Application.dataPath.Length);
            return path;
        }

        public static Texture FromFileBitmap32bpp(string file)
        {
            string path = NormalizePath(file);
            //TODO: Create a texture
            //Bitmap bitmapRaw = (Bitmap)Bitmap.FromFile(file);
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            Texture texture = asset as Texture2D;
            //return CreateBitmap32bpp(1, 1);
            return texture;
        }

    }
}
