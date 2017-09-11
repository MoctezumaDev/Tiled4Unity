#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
#define T2U_USE_LEGACY_IMPORTER
#else
#undef T2U_USE_LEGACY_IMPORTER
#endif

using System.Collections;
using System.IO;
using System.Linq;
using System.Xml;
using System;

using UnityEditor;
using UnityEngine;

namespace Tiled4Unity
{
    // Assets that are imported to "Tiled4Unity/..." will use this post processor
    public class TiledAssetPostProcessor : AssetPostprocessor
    {
        private static bool UseThisImporter(string assetPath)
        {
            // Certain file types are ignored by this asset post processor (i.e. scripts)
            // (Note that an empty string as the extension is a folder)
            string[] ignoreThese = { ".cs", ".txt",  ".shader", "", };
            if (ignoreThese.Any(ext => String.Compare(ext, Path.GetExtension(assetPath), true) == 0))
            {
                return false;
            }

            // Note: This importer can never be used if UNITY_WEBPLAYER is the configuration
            bool useThisImporter = false;

            // Is this file relative to our Tiled4Unity export marker file?
            // If so, then we want to use this asset postprocessor
            string path = assetPath;
            while (!String.IsNullOrEmpty(path))
            {
                path = Path.GetDirectoryName(path);
                string exportMarkerPath = Path.Combine(path, "Tiled4Unity.export.txt");
                if (File.Exists(exportMarkerPath))
                {
                    // This is a file under the Tiled4Unity root.
                    useThisImporter = true;
                    break;
                }
            }

            return useThisImporter;
        }

        private bool UseThisImporter()
        {
            return UseThisImporter(this.assetPath);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
            foreach (string imported in importedAssets)
            {
                if (UseThisImporter(imported))
                {
                   //Debug.Log(string.Format("Imported: {0}", imported));
                }
                else
                {
                    continue;
                }

                using (ImportTiled4Unity t4uImporter = new ImportTiled4Unity(imported))
                {
                    if (t4uImporter.IsTiled4UnityTexture())
                    {
                        // A texture was imported and the material assigned to it may need to be fixed
                        t4uImporter.TextureImported(imported);
                    }
                    else if (t4uImporter.IsTiled4UnityWavefrontObj())
                    {
                        // Now that the mesh has been imported we will build the prefab
                        t4uImporter.MeshImported(imported);
                    }
                    else if (t4uImporter.IsTiled4UnityPrefab())
                    {
                        // Now the the prefab is built and imported we are done
                        t4uImporter.ImportFinished(imported);
                        Debug.Log(string.Format("Imported prefab from Tiled map editor: {0}", imported));
                    }
                }
            }
        }

        private void OnPreprocessModel()
        {
            if (!UseThisImporter())
                return;

            ModelImporter modelImporter = this.assetImporter as ModelImporter;

            // Keep normals otherwise Unity will complain about needing them.
            // Normals may not be a bad idea anyhow
#if T2U_USE_LEGACY_IMPORTER
            modelImporter.normalImportMode = ModelImporterTangentSpaceMode.Import;
            modelImporter.tangentImportMode = ModelImporterTangentSpaceMode.None;
#else
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.None;
#endif

            // Don't need animations or tangents.
            modelImporter.generateAnimations = ModelImporterGenerateAnimations.None;
            modelImporter.animationType = ModelImporterAnimationType.None;

            // Do not need mesh colliders on import.
            modelImporter.addCollider = false;

            // We will create and assign our own materials.
            // This gives us more control over their construction.
            modelImporter.importMaterials = false;
        }

        private void OnPostprocessModel(GameObject gameObject)
        {
            if (!UseThisImporter())
                return;

            // Each mesh renderer has the ability to set the a sort layer but it takes some work with Unity to expose it.
            foreach (MeshRenderer mr in gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                mr.gameObject.AddComponent<SortingLayerExposed>();

                // No shadows
                mr.receiveShadows = false;
#if UNITY_5_3_OR_NEWER
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
                mr.castShadows = false;
#endif

                // No probes
#if UNITY_5_5_OR_NEWER
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#elif UNITY_5_3_OR_NEWER
                mr.useLightProbes = false;
#else
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
#endif
            }
        }

        private Material OnAssignMaterialModel(Material defaultMaterial, Renderer renderer)
        {
            if (!UseThisImporter())
                return null;

            // This is the only reliable place to assign materials in the import chain.
            // It kind of sucks because we have to go about making the mesh/material association in a roundabout way.

            // Note: This seems dangerous, but getting to the name of the base gameObject appears to be take some work.
            // The root gameObject, at this point, seems to have "_root" appeneded to it.
            // Once the model if finished being imported it drops this postifx
            // This is something that could change without our knowledge
            string rootName = renderer.transform.root.gameObject.name;
            int rootIndex = rootName.LastIndexOf("_root");
            if (rootIndex != -1)
            {
                rootName = rootName.Remove(rootIndex);
            }

            ImportTiled4Unity importer = new ImportTiled4Unity(this.assetPath);
            return importer.FixMaterialForMeshRenderer(rootName, renderer);
        }

        private void OnPreprocessTexture()
        {
            if (!UseThisImporter())
                return;

            if (!string.IsNullOrEmpty(this.assetImporter.userData))
            {
                // The texture has already been exported and we don't want to reset the texture import settings
                // This allows users to change their texture settings and have those changes stick.
                return;
            }

            // Put some dummy UserData on the importer so we know not to apply these settings again.
            this.assetImporter.userData = "tiled4unity";

            TextureImporter textureImporter = this.assetImporter as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.convertToNormalmap = false;
            
            textureImporter.alphaIsTransparency = true;
            textureImporter.spriteImportMode = SpriteImportMode.None;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.wrapMode = TextureWrapMode.Clamp;

#if UNITY_5_5_OR_NEWER
            textureImporter.alphaSource = TextureImporterAlphaSource.None;
            textureImporter.sRGBTexture = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
#else
            textureImporter.lightmap = false;
            textureImporter.grayscaleToAlpha = false;
            textureImporter.linearTexture = false;
            textureImporter.generateCubemap = TextureImporterGenerateCubemap.None;
            textureImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
#endif

        }

    }
}
