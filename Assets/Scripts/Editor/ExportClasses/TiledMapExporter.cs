﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace Tiled2Unity
{
    public partial class TiledMapExporter
    {
        private TmxMap tmxMap = null;

        public TiledMapExporter(TmxMap tmxMap)
        {
            this.tmxMap = tmxMap;
        }

        public void Export(string exportToTiled2UnityPath)
        {
            if (String.IsNullOrEmpty(exportToTiled2UnityPath))
            {
                throw new TmxException("Unity project export path is invalid or not set.");
            }

            // Create an Xml file to be imported by a Unity project
            // The unity project will have code that turns the Xml into Unity objects and prefabs
            string fileToSave = this.tmxMap.GetExportedFilename();
            Console.WriteLine("Compiling tiled2unity file: "+ fileToSave);

            // Need an element for embedded file data that will be imported into Unity
            // These are models and textures
            List<XElement> importFiles = CreateImportFilesElements(exportToTiled2UnityPath);
            List<XElement> assignMaterials = CreateAssignMaterialsElements();

            Console.WriteLine("Gathering prefab data ...");
            XElement prefab = CreatePrefabElement(); //TODO: Create prefabs

            // Create the Xml root and populate it
            Console.WriteLine("Writing as Xml ...");

            XElement root = new XElement("Tiled2Unity", new XAttribute("version", Tiled2Unity.Settings.Version));
            root.Add(assignMaterials);
            root.Add(prefab); //TODO: Create prefabs
            root.Add(importFiles);

            // Create the XDocument to save
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XComment("Tiled2Unity generated xml data"),
                new XComment("Do not modify by hand"),
                new XComment(String.Format("Last exported: {0}", DateTime.Now)),
                root);

            // Build the export directory
            string exportDir = Path.Combine(exportToTiled2UnityPath, "Imported");

            if (!Directory.Exists(exportDir))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("Could not export '{0}'\n", fileToSave);
                builder.AppendFormat("Tiled2Unity.unitypackage is not installed in unity project: {0}\n", exportToTiled2UnityPath);
                builder.AppendFormat("Select \"Help -> Import Unity Package to Project\" and re-export");
                Console.WriteLine(builder.ToString()); //Error
                return;
            }

            // Detect which version of Tiled2Unity is in our project
            // ...\Tiled2Unity\Tiled2Unity.export.txt
            string unityProjectVersionTXT = Path.Combine(exportToTiled2UnityPath, "Tiled2Unity.export.txt");
            if (!File.Exists(unityProjectVersionTXT))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("Could not export '{0}'\n", fileToSave);
                builder.AppendFormat("Tiled2Unity.unitypackage is not properly installed in unity project: {0}\n", exportToTiled2UnityPath);
                builder.AppendFormat("Missing file: {0}\n", unityProjectVersionTXT);
                builder.AppendFormat("Select \"Help -> Import Unity Package to Project\" and re-export");
                Console.WriteLine(builder.ToString()); //Error
                return;
            }

            // Open the unity-side script file and check its version number
            string text = File.ReadAllText(unityProjectVersionTXT);
            if (!String.IsNullOrEmpty(text))
            {
                string pattern = @"^\[Tiled2Unity Version (?<version>.*)?\]";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(text);
                Group group = match.Groups["version"];
                if (group.Success)
                {
                    if (Tiled2Unity.Settings.Version != group.ToString())
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendFormat("Export/Import Version mismatch\n");
                        builder.AppendFormat("  Tiled2Unity version   : {0}\n", Tiled2Unity.Settings.Version);
                        builder.AppendFormat("  Unity Project version : {0}\n", group.ToString());
                        builder.AppendFormat("  (Did you forget to update Tiled2Unity scipts in your Unity project?)");
                        Console.WriteLine(builder.ToString());
                    }
                }
            }

            // Save the file (which is importing it into Unity)
            string pathToSave = Path.Combine(exportDir, fileToSave);
            Console.WriteLine("Exporting to: {0}", pathToSave);
            doc.Save(pathToSave);
            Console.WriteLine("Succesfully exported: {0}\n  Vertex Scale = {1}\n  Object Type Xml = {2}",
                pathToSave,
                Tiled2Unity.Settings.Scale,
                "<none>");
        }

        public static Vector2 VectorToUnityVector_NoScale(Vector2 pt)
        {
            // Unity's coordinate sytem has y-up positive, y-down negative
            // Have to watch for negative zero, ffs
            return new Vector2(pt.x, pt.y == 0 ? 0 : -pt.y);
        }

        public static Vector2 VectorToUnityVector(float x, float y)
        {
            return VectorToUnityVector(new Vector2(x, y));
        }

        //TODO refactor the name of this method
        public static Vector2 VectorToUnityVector(Vector2 pt)
        {
            // Unity's coordinate sytem has y-up positive, y-down negative
            // Apply scaling
            Vector2 scaled = pt;
            scaled.x *= Tiled2Unity.Settings.Scale;
            scaled.y *= Tiled2Unity.Settings.Scale;

            // Have to watch for negative zero, ffs
            return new Vector2(scaled.x, scaled.y == 0 ? 0 : -scaled.y);
        }

        public static Vector2 VectorToObjVertex(Vector2 pt)
        {
            // Note, we negate the x and y due to Wavefront's coordinate system
            // Applying scaling
            Vector2 scaled = pt;
            scaled.x *= Tiled2Unity.Settings.Scale;
            scaled.y *= Tiled2Unity.Settings.Scale;

            // Watch for negative zero, ffs
            return new Vector2(scaled.x == 0 ? 0 : -scaled.x, scaled.y == 0 ? 0 : -scaled.y);
        }

        public static Vector2 PointToTextureCoordinate(Vector2 pt, Size imageSize)
        {
            float tx = pt.x / (float)imageSize.Width;
            float ty = pt.y / (float)imageSize.Height;
            return new Vector2(tx, 1.0f - ty);
        }

        private string StringToBase64String(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        private string FileToBase64String(string path)
        {
            return Convert.ToBase64String(File.ReadAllBytes(path));
        }

        //private string FileToCompressedBase64String(string path)
        //{
        //    using (FileStream originalStream = File.OpenRead(path))
        //    using (MemoryStream byteStream = new MemoryStream())
        //    using (GZipStream gzipStream = new GZipStream(byteStream, CompressionMode.Compress))
        //    {
        //        originalStream.CopyTo(gzipStream);
        //        byte[] compressedBytes = byteStream.ToArray();
        //        return Convert.ToBase64String(compressedBytes);
        //    }
        //
        //    // Without compression (testing shows it ~300% larger)
        //    //return Convert.ToBase64String(File.ReadAllBytes(path));
        //}

    } // end class
} // end namepsace