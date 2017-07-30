using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;

using UnityEditor;
using UnityEngine;

namespace Tiled4Unity
{
    partial class ImportTiled4Unity : IDisposable
    {
        private string fullPathToFile = "";
        private string pathToTiled4UnityRoot = "";
        private string assetPathToTiled4UnityRoot = "";

        public ImportTiled4Unity(string file)
        {
            this.fullPathToFile = Path.GetFullPath(file);

            // Discover the root of the Tiled4Unity scripts and assets
            this.pathToTiled4UnityRoot = Path.GetDirectoryName(this.fullPathToFile);
            int index = this.pathToTiled4UnityRoot.LastIndexOf("Tiled4Unity", StringComparison.InvariantCultureIgnoreCase);
            if (index == -1)
            {
                Debug.LogError(String.Format("There is an error with your Tiled4Unity install. Could not find Tiled4Unity folder in {0}", file));
            }
            else
            {
                this.pathToTiled4UnityRoot = this.pathToTiled4UnityRoot.Remove(index + "Tiled4Unity".Length);
            }

            this.fullPathToFile = this.fullPathToFile.Replace(Path.DirectorySeparatorChar, '/');
            this.pathToTiled4UnityRoot = this.pathToTiled4UnityRoot.Replace(Path.DirectorySeparatorChar, '/');

            // Figure out the path from "Assets" to "Tiled4Unity" root folder
            this.assetPathToTiled4UnityRoot = this.pathToTiled4UnityRoot.Remove(0, Application.dataPath.Count());
            this.assetPathToTiled4UnityRoot = "Assets" + this.assetPathToTiled4UnityRoot;
        }

        public bool IsTiled4UnityTexture()
        {
            bool startsWith = this.fullPathToFile.Contains("/Tiled4Unity/Textures/");
            bool endsWithTxt = this.fullPathToFile.EndsWith(".txt");
            return startsWith && !endsWithTxt;
        }

        public bool IsTiled4UnityWavefrontObj()
        {
            bool contains = this.fullPathToFile.Contains("/Tiled4Unity/Meshes/");
            bool endsWith = this.fullPathToFile.EndsWith(".obj");
            return contains && endsWith;
        }

        public bool IsTiled4UnityPrefab()
        {
            bool startsWith = this.fullPathToFile.Contains("/Tiled4Unity/Prefabs/");
            bool endsWith = this.fullPathToFile.EndsWith(".prefab");
            return startsWith && endsWith;
        }

        public string GetMeshAssetPath(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string meshAsset = String.Format("{0}/Meshes/{1}.obj", this.assetPathToTiled4UnityRoot, name);
            return meshAsset;
        }

        public string GetMaterialAssetPath(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string materialAsset = String.Format("{0}/Materials/{1}.mat", this.assetPathToTiled4UnityRoot, name);
            return materialAsset;
        }

        public string GetTextureAssetPath(string filename)
        {
            // Keep the extention given (png, tga, etc.)
            filename = Path.GetFileName(filename);
            string textureAsset = String.Format("{0}/Textures/{1}", this.assetPathToTiled4UnityRoot, filename);
            return textureAsset;
        }

        public string GetXmlImportAssetPath(string name)
        {
            name = ImportXMLHelper.GetFilenameWithoutTiled4UnityExtension(name);
            string xmlAsset = String.Format("{0}/Imported/{1}.tiled4unity.xml", this.assetPathToTiled4UnityRoot, name);
            return xmlAsset;
        }

        public string GetPrefabAssetPath(string name, bool isResource, string extraPath)
        {
            string prefabAsset = "";
            if (isResource)
            {
                if (String.IsNullOrEmpty(extraPath))
                {
                    // Put the prefab into a "Resources" folder so it can be instantiated through script
                    prefabAsset = String.Format("{0}/Prefabs/Resources/{1}.prefab", this.assetPathToTiled4UnityRoot, name);
                }
                else
                {
                    // Put the prefab into a "Resources/extraPath" folder so it can be instantiated through script
                    prefabAsset = String.Format("{0}/Prefabs/Resources/{1}/{2}.prefab", this.assetPathToTiled4UnityRoot, extraPath, name);
                }
            }
            else
            {
                prefabAsset = String.Format("{0}/Prefabs/{1}.prefab", this.assetPathToTiled4UnityRoot, name);
            }

            return prefabAsset;
        }

        public void Dispose()
        {
        }
    }
}
