#if !UNITY_WEBPLAYER
// Note: This parital class is not compiled in for WebPlayer builds.
// The Unity Webplayer is deprecated. If you *must* use it then make sure Tiled4Unity assets are imported via another build target first.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using UnityEditor;
using UnityEngine;


namespace Tiled4Unity
{
    // Concentrates on the Xml file being imported
    partial class ImportTiled4Unity
    {
        // Called when the import process has completed and we have a prefab ready to go
        public void ImportFinished(string prefabPath)
        {
            // String the prefab extension
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            // Get at the import behavour tied to this prefab and remove it from the scene
            string xmlAssetPath = GetXmlImportAssetPath(prefabName);
            ImportXMLHelper importBehaviour = ImportXMLHelper.ImportPath(xmlAssetPath);
            ImportProgressBar.HideProgressBar();
            ImportXMLHelper.RemovePath(importBehaviour.ImportName);
        }

        public List<Material> CreateMaterialsFromInternalTextures(List<TmxImage> images)
        {
            List<Material> materials = new List<Material>();
            foreach (var image in images)
            {
                bool isInternal = File.Exists(Path.ChangeExtension(image.AbsolutePath, "meta"));
                if (isInternal)
                {
                    //TODO: recover internal textures
                }
                else
                {
                    // The path to the texture will be WRT to the Unity project root
                    string imageAssetPath = image.AbsolutePath;
                    imageAssetPath = imageAssetPath.TrimStart('\\');
                    imageAssetPath = imageAssetPath.TrimStart('/');

                    string materialPath = GetMaterialAssetPath(imageAssetPath);

                    // Transparent color key?
                    string transparentColor = image.TransparentColor;
                    bool depthBufferEnabled = Settings.DepthBufferEnabled;

                    // Create our material
                    Material material = CreateMaterial(transparentColor, depthBufferEnabled);

                    // Assign to it the texture that is already internal to our Unity project
                    Texture2D texture2d = AssetDatabase.LoadAssetAtPath(imageAssetPath, typeof(Texture2D)) as Texture2D;
                    material.SetTexture("_MainTex", texture2d);

                    // Write the material to our asset database
                    ImportUtils.ReadyToWrite(materialPath);
                    Material newMaterial = ImportUtils.CreateOrReplaceAsset(material, materialPath);
                    materials.Add(material);
                }
            }
            return materials;
        }

        private Material CreateMaterial(string htmlColor, bool usesDepthShader)
        {
            // Determine which shader we sould be using
            string shaderName = "Tiled4Unity/";

            // Are we using depth shaders?
            if (usesDepthShader)
            {
                shaderName += "Depth";
            }
            else
            {
                shaderName += "Default";
            }

            // Are we using color key shaders?
            Color? keyColor = null;
            if (!String.IsNullOrEmpty(htmlColor))
            {
                shaderName += " Color Key";

                byte r = byte.Parse(htmlColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(htmlColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(htmlColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                keyColor = new Color32(r, g, b, 255);
            }

            Material material = new Material(Shader.Find(shaderName));

            if (keyColor.HasValue)
            {
                material.SetColor("_AlphaColorKey", keyColor.Value);
            }

            return material;
        }

        public ModelImporter CreateMesh(TmxObj mesh)
        {
            // We're going to create/write a file that contains our mesh data as a Wavefront Obj file
            // The actual mesh will be imported from this Obj file
            string data = mesh.data;

            // The data is in base64 format. We need it as a raw string.
            string raw = ImportUtils.Base64ToString(data);

            // Save and import the asset
            string pathToMesh = GetMeshAssetPath(mesh.fileName);
            ImportUtils.ReadyToWrite(pathToMesh);
            File.WriteAllText(pathToMesh, raw, Encoding.UTF8);
            AssetDatabase.ImportAsset(pathToMesh, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter importer = AssetImporter.GetAtPath(pathToMesh) as ModelImporter;
            return importer;
        }
    }
}
#endif