using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using Tiled4Unity;

public class TmxImporter : AssetPostprocessor
{
    static string extension = ".tmx";        // Our custom extension
    static string newExtension = ".asset";        // Extension of newly created asset - it MUST be ".asset", nothing else is allowed...

    public static bool HasExtension(string asset)
    {
        return asset.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase);
    }

    public static string ConvertToInternalPath(string asset)
    {
        string left = asset.Substring(0, asset.Length - extension.Length);
        return left + newExtension;
    }

    // This is called always when importing something
    static void OnPostprocessAllAssets
        (
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
    {
        foreach (string asset in importedAssets)
        {
            // This is our detection of file - by extension
            if (HasExtension(asset))
            {
                TextReader stream = new StreamReader(asset);
                string firstLine = stream.ReadLine();
                stream.Close();

                // Also we check first file line
                //if (firstLine.Equals("MagicLine", StringComparison.OrdinalIgnoreCase))
                {
                    ImportMyAsset(asset);
                }
                /*else
                {
                    Debug.LogError("Cannot import \"" + asset + "\": bad format!");
                }*/
            }
        }
    }

    // Imports my asset from the file
    static void ImportMyAsset(string asset)
    {
        TmxMap map = TmxMap.LoadFromFile(asset);

        string objectTypeXmlPath = AssetDatabase.GetAssetPath(Tile4UnitySettings.GetSettings().ObjectTypes);
        map.LoadObjectTypeXml(objectTypeXmlPath);

        if (map.IsLoaded)
        {
            TiledMapExporter exporter = new TiledMapExporter(map);
            exporter.Export(Application.dataPath+ "\\Tiled4Unity");
        }
        // Path to out new asset
        //string newPath = ConvertToInternalPath(asset);

        // MyAsset is imported asset type, it should derive from ScriptableObject, probably
        //MyAsset numSeq = AssetDatabase.LoadAssetAtPath(newPath, typeof(MyAsset)) as MyAsset;
        //bool loaded = (numSeq != null);

        //if (!loaded)
        //{
        //    numSeq = ScriptableObject.CreateInstance<NumSeqAsset>();
        //}
        //else
        //{
        // return; // Uncommenting here means that when the original file is changed, changes are ignored
        //}

        // Here we load our asset from original file
        //numSeq.Load(asset);

        //if (!loaded)
        //{
        //    AssetDatabase.CreateAsset(numSeq, newPath);
        //}

        //AssetDatabase.SaveAssets();
    }
}
