using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class AssetReference {

    [SerializeField]
    string _guid;

    public AssetReference(string path)
    {
        this._guid = AssetDatabase.AssetPathToGUID(path);
    }

    public string GetPath()
    {
        return AssetDatabase.GUIDToAssetPath(this._guid);
    }

    public bool IsValid()
    {
        return string.IsNullOrEmpty(this.GetPath());
    }

    public string GUID
    {
        get
        {
            return this._guid;
        }
    }
}
