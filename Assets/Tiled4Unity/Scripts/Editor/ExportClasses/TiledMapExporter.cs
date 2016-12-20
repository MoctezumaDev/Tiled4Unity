using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Tiled4Unity
{
    public partial class TiledMapExporter
    {
        private TmxMap tmxMap = null;

        public TiledMapExporter(TmxMap tmxMap)
        {
            this.tmxMap = tmxMap;
        }

        public void Export(string exportToTiled4UnityPath)
        {
            if (String.IsNullOrEmpty(exportToTiled4UnityPath))
            {
                throw new TmxException("Unity project export path is invalid or not set.");
            }

            // Create an Xml file to be imported by a Unity project
            // The unity project will have code that turns the Xml into Unity objects and prefabs
            string fileToSave = this.tmxMap.GetExportedFilename();
            Console.WriteLine("Compiling tiled4unity file: "+ fileToSave);

            // Need an element for embedded file data that will be imported into Unity
            // These are models and textures
            List<XElement> importFiles = CreateImportFilesElements(exportToTiled4UnityPath);
            List<XElement> assignMaterials = CreateAssignMaterialsElements();

            Console.WriteLine("Gathering prefab data ...");
            XElement prefab = CreatePrefabElement();

            // Create the Xml root and populate it
            Console.WriteLine("Writing as Xml ...");

            XElement root = new XElement("Tiled4Unity", new XAttribute("version", Settings.Version));
            root.Add(assignMaterials);
            root.Add(prefab);
            root.Add(importFiles);

            // Create the XDocument to save
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XComment("Tiled4Unity generated xml data"),
                new XComment("Do not modify by hand"),
                new XComment(String.Format("Last exported: {0}", DateTime.Now)),
                root);

            // Build the export directory
            string exportDir = Path.Combine(exportToTiled4UnityPath, "Imported");

            if (!Directory.Exists(exportDir))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("Could not export '{0}'\n", fileToSave);
                builder.AppendFormat("Tiled4Unity.unitypackage is not installed in unity project: {0}\n", exportToTiled4UnityPath);
                builder.AppendFormat("Select \"Help -> Import Unity Package to Project\" and re-export");
                Console.WriteLine(builder.ToString()); //Error
                return;
            }

            // Detect which version of Tiled4Unity is in our project
            // ...\Tiled4Unity\Tiled4Unity.export.txt
            //TODO: update version checking @Leon
            string unityProjectVersionTXT = Path.Combine(exportToTiled4UnityPath, "Tiled4Unity.export.txt");
            if (!File.Exists(unityProjectVersionTXT))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("Could not export '{0}'\n", fileToSave);
                builder.AppendFormat("Tiled4Unity.unitypackage is not properly installed in unity project: {0}\n", exportToTiled4UnityPath);
                builder.AppendFormat("Missing file: {0}\n", unityProjectVersionTXT);
                builder.AppendFormat("Select \"Help -> Import Unity Package to Project\" and re-export");
                Console.WriteLine(builder.ToString()); //Error
                return;
            }

            // Open the unity-side script file and check its version number
            string text = File.ReadAllText(unityProjectVersionTXT);
            if (!String.IsNullOrEmpty(text))
            {
                string pattern = @"^\[Tiled4Unity Version (?<version>.*)?\]";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(text);
                Group group = match.Groups["version"];
                if (group.Success)
                {
                    if (Settings.Version != group.ToString())
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendFormat("Export/Import Version mismatch\n");
                        builder.AppendFormat("  Tiled4Unity version   : {0}\n", Settings.Version);
                        builder.AppendFormat("  Unity Project version : {0}\n", group.ToString());
                        builder.AppendFormat("  (Did you forget to update Tiled4Unity scipts in your Unity project?)");
                        Console.WriteLine(builder.ToString());
                    }
                }
            }

            // Save the file (which is importing it into Unity)
            string pathToSave = Path.Combine(exportDir, fileToSave);
            Console.WriteLine("Exporting to: {0}", pathToSave);

            //TODO: not save the xml at all
            doc.Save(pathToSave);
            Console.WriteLine("Succesfully exported: {0}\n  Vertex Scale = {1}\n  Object Type Xml = {2}",
                pathToSave,
                Settings.Scale,
                "<none>");

            using (ImportTiled4Unity t2uImporter = new ImportTiled4Unity(pathToSave))
            {
                if (t2uImporter.IsTiled4UnityFile())
                {
                    // Start the import process. This will trigger textures and meshes to be imported as well.
                    t2uImporter.ImportBegin(doc);
                    AssetDatabase.Refresh();
                }
            }
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
            scaled.x *= Settings.Scale;
            scaled.y *= Settings.Scale;

            // Have to watch for negative zero, ffs
            return new Vector2(scaled.x, scaled.y == 0 ? 0 : -scaled.y);
        }

        public static Vector2 VectorToObjVertex(Vector2 pt)
        {
            // Note, we negate the x and y due to Wavefront's coordinate system
            // Applying scaling
            Vector2 scaled = pt;
            scaled.x *= Settings.Scale;
            scaled.y *= Settings.Scale;

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