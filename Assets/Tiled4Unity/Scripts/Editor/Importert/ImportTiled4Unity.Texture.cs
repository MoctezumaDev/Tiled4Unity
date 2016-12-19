using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace Tiled4Unity
{
    // Handled a texture being imported
    partial class ImportTiled4Unity
    {
        public void TextureImported(string texturePath)
        {
            // This is a fixup method due to materials and textures, under some conditions, being imported out of order
            Texture2D texture2d = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
            Material material = AssetDatabase.LoadAssetAtPath(GetMaterialAssetPath(texturePath), typeof(Material)) as Material;
            if (material == null)
            {
                Debug.LogError(String.Format("Error importing texture '{0}'. Could not find material. Try re-importing Tiled4Unity/Imported/[MapName].tiled4unity.xml file", texturePath));
            }
            else
            {
                material.SetTexture("_MainTex", texture2d);
            }
        }
    }
}
