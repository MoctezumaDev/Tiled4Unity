#if !UNITY_WEBPLAYER
// Note: This parital class is not compiled in for WebPlayer builds.
// The Unity Webplayer is deprecated. If you *must* use it then make sure Tiled4Unity assets are imported via another build target first.
using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using UnityEditor;
using UnityEngine;


namespace Tiled4Unity
{
    // Partial class for the importer that deals with Materials
    partial class ImportTiled4Unity
    {
        // We need to call this while the renderers on the model is having its material assigned to it
        // This is invoked for every submesh in the .obj wavefront mesh
        public Material FixMaterialForMeshRenderer(string objName, Renderer renderer)
        {
            string tmxImportSettings = ImportXMLHelper.GetFilenameWithoutTiled4UnityExtension(objName);
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0} {1}", typeof(TmxImportSettings), tmxImportSettings));
            if(guids.Length > 0)
            {
                TmxImportSettings settings = AssetDatabase.LoadAssetAtPath<TmxImportSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));

                // The mesh to match
                string meshName = renderer.name;

                MeshMaterial meshMaterial = settings.meshMaterials.FirstOrDefault(entry => entry.Mesh == meshName);

                if(string.IsNullOrEmpty(meshMaterial.Material))
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendFormat("Could not find mesh named '{0}' for material matching\n", renderer.name);
                    Debug.LogError(builder.ToString());

                    return null;
                }

                string materialName = meshMaterial.Material;
                string materialPath = GetMaterialAssetPath(materialName);

                // Assign the material
                Material material = AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;
                if (material == null)
                {
                    Debug.LogError(String.Format("Could not find material: {0}", materialName));
                }

                return material;
            }

            Debug.LogError(String.Format("Could not find import settings."));

            return null;
        }

    }
}
#endif