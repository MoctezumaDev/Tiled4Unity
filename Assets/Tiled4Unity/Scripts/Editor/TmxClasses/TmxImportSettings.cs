using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tiled4Unity
{
    public class JsonHelper
    {
        //Usage:
        //YouObject[] objects = JsonHelper.getJsonArray<YouObject> (jsonString);
        public static T[] getJsonArray<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.array;
        }

        //Usage:
        //string jsonString = JsonHelper.arrayToJson<YouObject>(objects);
        public static string arrayToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.array = array;
            return JsonUtility.ToJson(wrapper);
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }

    [System.Serializable]
    public class TmxImportSettings : ScriptableObject
    {

        [SerializeField]
        public AssetReference _assetReference;

        [SerializeField]
        public TmxObj mesh;

        [SerializeField]
        private string _imagesList;

        public TmxImage[] images
        {
            get { return JsonHelper.getJsonArray<TmxImage>(_imagesList); }
            set { _imagesList = JsonHelper.arrayToJson(value); }
        }

        [SerializeField]
        private string _meshMaterials;

        public MeshMaterial[] meshMaterials
        {
            get { return JsonHelper.getJsonArray<MeshMaterial>(_meshMaterials); }
            set { _meshMaterials = JsonHelper.arrayToJson(value); }
        }

        public void LinkTo(string path)
        {
            _assetReference = new AssetReference(path);
        }

        public void OnEnable()
        {
            if (_assetReference != null)
            {
                if (_assetReference.IsValid())
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(this.GetInstanceID()));
                }
                else
                {
                    string originalSettingsPath = AssetDatabase.GetAssetPath(this.GetInstanceID());
                    string filePath = TmxImporter.AssetPathWithoutExtension(_assetReference.GetPath(), TmxImporter.tmxExtension);
                    string settingsPath = TmxImporter.AssetPathWithoutExtension(originalSettingsPath, TmxImporter.assetExtension);

                    if (filePath != settingsPath)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(this.GetInstanceID()));
                    }
                }
            }
        }

    }
}
