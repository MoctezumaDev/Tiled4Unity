using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using Tiled4Unity;

public class TmxImporter : AssetPostprocessor
{
    public const string tmxExtension = ".tmx";        // Our custom extension
    public const string assetExtension = ".asset";        // Extension of newly created asset - it MUST be ".asset", nothing else is allowed...

    public static bool HasExtension(string asset)
    {
        return asset.EndsWith(tmxExtension, System.StringComparison.OrdinalIgnoreCase);
    }

    public static string AssetPathWithoutExtension(string asset, string extension)
    {
        return asset.Substring(0, asset.Length - extension.Length);
    }

    public static string ConvertToAssetPath(string asset)
    {
        string left = AssetPathWithoutExtension(asset, tmxExtension);
        return left + assetExtension;
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
        for (int i = 0; i < movedAssets.Length; i++)
        {
            if (HasExtension(movedFromAssetPaths[i]))
            {
                MoveAssetSettings(movedFromAssetPaths[i], movedAssets[i]);
                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }
        }

        foreach (string asset in importedAssets)
        {
            // This is our detection of file - by extension
            if (HasExtension(asset))
            {
                // Todo: Recover this
                ImportMyAsset(asset);
            }
        }

        foreach (string asset in deletedAssets)
        {
            if(HasExtension(asset))
            {
                DeleteAssetSettings(asset);
            }
        }
    }

    // Imports my asset from the file
    static void MoveAssetSettings(string fromAssetPath, string toAssetPath)
    {
        // Path to scriptable object
        string fromFileSettingsPath = ConvertToAssetPath(fromAssetPath);
        string toFileSettingsPath = ConvertToAssetPath(toAssetPath);

        AssetDatabase.MoveAsset(fromFileSettingsPath,toFileSettingsPath);
    }

    // Imports my asset from the file
    static void DeleteAssetSettings(string asset)
    {
        // Path to scriptable object
        string fileSettingsPath = ConvertToAssetPath(asset);

        if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fileSettingsPath)))
        {
            AssetDatabase.DeleteAsset(fileSettingsPath);
        }
    }

    // Imports my asset from the file
    static void ImportMyAsset(string asset)
    {
        // Path to scriptable object
        string fileSettingsPath = ConvertToAssetPath(asset);

        TmxImportSettings fileImportSettings = AssetDatabase.LoadAssetAtPath(fileSettingsPath, typeof(TmxImportSettings)) as TmxImportSettings;
        bool loaded = (fileImportSettings != null);

        if (!loaded)
        {
            fileImportSettings = ScriptableObject.CreateInstance<TmxImportSettings>();
            AssetDatabase.CreateAsset(fileImportSettings, fileSettingsPath);
            fileImportSettings.LinkTo(asset);
            EditorUtility.SetDirty(fileImportSettings);
            AssetDatabase.SaveAssets();
        }
        else
        {
            fileImportSettings.LinkTo(asset);
            // return; // Uncommenting here means that when the original file is changed, changes are ignored
        }

        TmxMap map = TmxMap.LoadFromFile(asset);

        string objectTypeXmlPath = AssetDatabase.GetAssetPath(Tile4UnitySettings.GetSettings().ObjectTypes);
        map.LoadObjectTypeXml(objectTypeXmlPath);

        if (map.IsLoaded)
        {
            TiledMapExporter exporter = new TiledMapExporter(map);
            exporter.Export(Application.dataPath + "\\Tiled4Unity", fileImportSettings);
        }
    }
}
