#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
#define T2U_IS_UNITY_4
#endif

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
    // Handles a Mesh being imported.
    // At this point we should have everything we need to build out any prefabs for the tiled map object
    partial class ImportTiled4Unity
    {
        // By the time this is called, our assets should be ready to create the map prefab
        public void MeshImported(string objPath)
        {
            // String the mesh type (.obj) from the path
            string objName = Path.GetFileNameWithoutExtension(objPath);

            // TODO: Create prefab
            // Get the XML file that this mesh came from
            /*string xmlPath = GetXmlImportAssetPath(objName);

            ImportXMLHelper importBehaviour = ImportXMLHelper.ImportPath(xmlPath);

            string log = String.Format("Create prefab: {0}",
                Path.GetFileNameWithoutExtension(GetPrefabAssetPath(objName, false, null)));
            ImportProgressBar.DisplayProgressBar(log, importBehaviour.ImportName, importBehaviour.Progress);
            importBehaviour.ImportCounter++;

            foreach (var xmlPrefab in importBehaviour.XmlDocument.Root.Elements("Prefab"))
            {
                CreatePrefab(xmlPrefab, objPath);
            }*/
        }

        private void CreatePrefab(TmxMap map, string path)
        {

            // Part 1: Create the prefab
            string prefabName = map.Name;
            float prefabScale = 1.0f;
            GameObject tempPrefab = new GameObject(prefabName);
            HandleTiledAttributes(tempPrefab, map);

            // Part 2: Build out the prefab
            // We may have an 'isTrigger' attribute that we want our children to obey
            // bool isTrigger = ImportUtils.GetAttributeAsBoolean(xmlPrefab, "isTrigger", false);
            AddGameObjectsTo(tempPrefab, map);

            // Part 3: Apply the scale only after all children have been added
            tempPrefab.transform.localScale = new Vector3(prefabScale, prefabScale, prefabScale);

            // Part 4: Save the prefab, keeping references intact.
            string prefabPath = GetPrefabAssetPath(prefabName, true, "");
            UnityEngine.Object finalPrefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            if (finalPrefab == null)
            {
                // The prefab needs to be created
                ImportUtils.ReadyToWrite(prefabPath);
                finalPrefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
            }

            // Replace the prefab, keeping connections based on name.
            PrefabUtility.ReplacePrefab(tempPrefab, finalPrefab, ReplacePrefabOptions.ReplaceNameBased);

            // Destroy the instance from the current scene hiearchy.
            UnityEngine.Object.DestroyImmediate(tempPrefab);
        }

        private void ApplySortingLayer(GameObject child, string sortingLayer)
        {
            // Apply the sorting to the renderer of the mesh object we just copied into the child
            Renderer renderer = child.GetComponent<Renderer>();
            
            if (!String.IsNullOrEmpty(sortingLayer) && !SortingLayerExposedEditor.GetSortingLayerNames().Contains(sortingLayer))
            {
                Debug.LogError(string.Format("Sorting Layer \"{0}\" does not exist. Check your Project Settings -> Tags and Layers", sortingLayer));
                renderer.sortingLayerName = "Default";
            }
            else
            {
                renderer.sortingLayerName = sortingLayer;
            }
        }

        private List<GameObject> CreateMeshElementsForLayer(TmxLayer layer)
        {
            List<GameObject> meshes = new List<GameObject>();

            foreach (TmxMesh mesh in layer.Meshes)
            {
                GameObject go = new GameObject(mesh.UniqueMeshName);

                ApplySortingLayer(go, layer.SortingLayerName);

                GameObject goMesh = CreateCopyFromMeshObj(mesh.ObjectName, "", layer.Opacity);
                goMesh.transform.SetParent(go.transform, false);

                if (mesh.FullAnimationDurationMs > 0)
                {
                    AddTileAnimatorsTo(goMesh, mesh);
                }

                meshes.Add(go);
            }

            return meshes;
        }

        private void AddGameObjectsTo(GameObject parent, TmxMap map)
        {
            foreach(TmxLayer layer in map.Layers)
            {
                float depth_z = 0;
                if (Settings.DepthBufferEnabled && layer.SortingOrder != 0)
                {
                    float mapLogicalHeight = map.MapSizeInPixels().Height;
                    float tileHeight = map.TileHeight;

                    depth_z = layer.SortingOrder * tileHeight / mapLogicalHeight * -1.0f;
                }

                GameObject goLayer = new GameObject(layer.Name);
                goLayer.transform.SetParent(parent.transform, false);
                goLayer.transform.localPosition = new Vector3(layer.Offset.x, layer.Offset.y, depth_z);

                if (layer.Ignore != TmxLayer.IgnoreSettings.Visual)
                {
                    // Submeshes for the layer (layer+material)
                    // var meshElements = CreateMeshElementsForLayer(layer);
                    // layerElement.Add(meshElements);
                    CreateMeshElementsForLayer(layer);
                }


                // Collision data for the layer
                if (layer.Ignore != TmxLayer.IgnoreSettings.Collision)
                {
                    // foreach (var collisionLayer in layer.CollisionLayers)
                    // {
                        //var collisionElements = CreateCollisionElementForLayer(collisionLayer);
                        //layerElement.Add(collisionElements);
                    //}
                }
            }
        }

        /*private void AddGameObjectsTo(GameObject parent, XElement xml, string obj)
        {
            foreach (XElement goXml in xml.Elements("GameObject"))
            {
                string name = ImportUtils.GetAttributeAsString(goXml, "name", "");
                string copyFrom = ImportUtils.GetAttributeAsString(goXml, "copy", "");

                GameObject child = null;
                if (!String.IsNullOrEmpty(copyFrom))
                {
                    float opacity = ImportUtils.GetAttributeAsFloat(goXml, "opacity", 1);
                    child = CreateCopyFromMeshObj(copyFrom, objPath, opacity);
                    if (child == null)
                    {
                        // We're in trouble. Errors should already be in the log.
                        return;
                    }

                    // Apply the sorting to the renderer of the mesh object we just copied into the child
                    Renderer renderer = child.GetComponent<Renderer>();

                    string sortingLayer = ImportUtils.GetAttributeAsString(goXml, "sortingLayerName", "");
                    if (!String.IsNullOrEmpty(sortingLayer) && !SortingLayerExposedEditor.GetSortingLayerNames().Contains(sortingLayer))
                    {
                        Debug.LogError(string.Format("Sorting Layer \"{0}\" does not exist. Check your Project Settings -> Tags and Layers", sortingLayer));
                        renderer.sortingLayerName = "Default";
                    }
                    else
                    {
                        renderer.sortingLayerName = sortingLayer;
                    }

                    // Set the sorting order
                    renderer.sortingOrder = ImportUtils.GetAttributeAsInt(goXml, "sortingOrder", 0);
                }
                else
                {
                    child = new GameObject();
                }

                if (!String.IsNullOrEmpty(name))
                {
                    child.name = name;
                }

                // Assign the child to the parent
                child.transform.parent = parent.transform;

                // Set the position
                float x = ImportUtils.GetAttributeAsFloat(goXml, "x", 0);
                float y = ImportUtils.GetAttributeAsFloat(goXml, "y", 0);
                float z = ImportUtils.GetAttributeAsFloat(goXml, "z", 0);
                child.transform.localPosition = new Vector3(x, y, z);

                // Add any tile animators
                AddTileAnimatorsTo(child, goXml);

                // Do we have any collision data?
                // Check if we are setting 'isTrigger' for ourselves or for our childen
                bool isTrigger = ImportUtils.GetAttributeAsBoolean(goXml, "isTrigger", isParentTrigger);
                AddCollidersTo(child, isTrigger, goXml);

                // Do we have any children of our own?
                AddGameObjectsTo(child, goXml, isTrigger, objPath, customImporters);

                // Does this game object have a tag?
                AssignTagTo(child, goXml);

                // Does this game object have a layer?
                AssignLayerTo(child, goXml);

                // Set scale and rotation *after* children are added otherwise Unity will have child+parent transform cancel each other out
                float sx = ImportUtils.GetAttributeAsFloat(goXml, "scaleX", 1.0f);
                float sy = ImportUtils.GetAttributeAsFloat(goXml, "scaleY", 1.0f);
                child.transform.localScale = new Vector3(sx, sy, 1.0f);

                // Set the rotation
                // Use negative rotation on the z component because of change in coordinate systems between Tiled and Unity
                Vector3 localRotation = new Vector3();
                localRotation.x = (ImportUtils.GetAttributeAsBoolean(goXml, "flipY", false) == true) ? 180.0f : 0.0f;
                localRotation.y = (ImportUtils.GetAttributeAsBoolean(goXml, "flipX", false) == true) ? 180.0f : 0.0f;
                localRotation.z = -ImportUtils.GetAttributeAsFloat(goXml, "rotation", 0);
                child.transform.eulerAngles = localRotation;
            }
        }*/

        private void AssignLayerTo(GameObject gameObject, string layerName)
        {
            if (String.IsNullOrEmpty(layerName))
                return;

            int layerId = LayerMask.NameToLayer(layerName);
            if (layerId == -1)
            {
                string msg = String.Format("Layer '{0}' is not defined for '{1}'. Check project settings in Edit->Project Settings->Tags & Layers",
                    layerName,
                    GetFullGameObjectName(gameObject.transform));
                Debug.LogError(msg);
                return;
            }

            // Set the layer on ourselves (and our children)
            AssignLayerIdTo(gameObject, layerId);
        }

        private void AssignLayerIdTo(GameObject gameObject, int layerId)
        {
            if (gameObject == null)
                return;

            gameObject.layer = layerId;

            foreach (Transform child in gameObject.transform)
            {
                if (child.gameObject == null)
                    continue;

                // Do not set the layerId on a child that has already had his layerId explicitly set
                if (child.gameObject.layer != 0)
                    continue;

                AssignLayerIdTo(child.gameObject, layerId);
            }
        }

        private void AssignTagTo(GameObject gameObject, XElement xml)
        {
            string tag = ImportUtils.GetAttributeAsString(xml, "tag", "");
            if (String.IsNullOrEmpty(tag))
                return;

            // Let the user know if the tag doesn't exist in our project sttings
            try
            {
                gameObject.tag = tag;
            }
            catch (UnityException)
            {
                string msg = String.Format("Tag '{0}' is not defined for '{1}'. Check project settings in Edit->Project Settings->Tags & Layers",
                    tag,
                    GetFullGameObjectName(gameObject.transform));
                Debug.LogError(msg);
            }
        }

        private string GetFullGameObjectName(Transform xform)
        {
            if (xform == null)
                return "";
            string parentName = GetFullGameObjectName(xform.parent);

            if (String.IsNullOrEmpty(parentName))
                return xform.name;

            return String.Format("{0}/{1}", parentName, xform.name);
        }

        private void AddCollidersTo(GameObject gameObject, bool isTrigger, XElement xml)
        {
            // Box colliders
            foreach (XElement xmlBoxCollider2D in xml.Elements("BoxCollider2D"))
            {
                BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = isTrigger;
                float width = ImportUtils.GetAttributeAsFloat(xmlBoxCollider2D, "width");
                float height = ImportUtils.GetAttributeAsFloat(xmlBoxCollider2D, "height");
                collider.size = new Vector2(width, height);

#if T2U_IS_UNITY_4
                collider.center = new Vector2(width * 0.5f, -height * 0.5f);
#else
                collider.offset = new Vector2(width * 0.5f, -height * 0.5f);
#endif
                // Apply the offsets (if any)
                float offset_x = ImportUtils.GetAttributeAsFloat(xmlBoxCollider2D, "offsetX", 0);
                float offset_y = ImportUtils.GetAttributeAsFloat(xmlBoxCollider2D, "offsetY", 0);

#if T2U_IS_UNITY_4
                collider.center += new Vector2(offset_x, offset_y);
#else
                collider.offset += new Vector2(offset_x, offset_y);
#endif
            }

            // Circle colliders
            foreach (XElement xmlCircleCollider2D in xml.Elements("CircleCollider2D"))
            {
                CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = isTrigger;
                float radius = ImportUtils.GetAttributeAsFloat(xmlCircleCollider2D, "radius");
                collider.radius = radius;
#if T2U_IS_UNITY_4
                collider.center = new Vector2(radius, -radius);
#else
                collider.offset = new Vector2(radius, -radius);
#endif

                // Apply the offsets (if any)
                float offset_x = ImportUtils.GetAttributeAsFloat(xmlCircleCollider2D, "offsetX", 0);
                float offset_y = ImportUtils.GetAttributeAsFloat(xmlCircleCollider2D, "offsetY", 0);

#if T2U_IS_UNITY_4
                collider.center += new Vector2(offset_x, offset_y);
#else
                collider.offset += new Vector2(offset_x, offset_y);
#endif
            }

            // Edge colliders
            foreach (XElement xmlEdgeCollider2D in xml.Elements("EdgeCollider2D"))
            {
                EdgeCollider2D collider = gameObject.AddComponent<EdgeCollider2D>();
                collider.isTrigger = isTrigger;
                string data = xmlEdgeCollider2D.Element("Points").Value;

                // The data looks like this:
                //  x0,y0 x1,y1 x2,y2 ...
                var points = from pt in data.Split(' ')
                             let x = Convert.ToSingle(pt.Split(',')[0])
                             let y = Convert.ToSingle(pt.Split(',')[1])
                             select new Vector2(x, y);

                collider.points = points.ToArray();

                // Apply the offsets (if any)
                float offset_x = ImportUtils.GetAttributeAsFloat(xmlEdgeCollider2D, "offsetX", 0);
                float offset_y = ImportUtils.GetAttributeAsFloat(xmlEdgeCollider2D, "offsetY", 0);

#if T2U_IS_UNITY_4
                // This is kind of a hack for Unity 4.x which doesn't support offset/center on the edge collider
                var offsetPoints = from pt in points
                                   select new Vector2(pt.x + offset_x, pt.y + offset_y);
                collider.points = offsetPoints.ToArray();

#else
                collider.offset += new Vector2(offset_x, offset_y);
#endif
            }

            // Polygon colliders
            foreach (XElement xmlPolygonCollider2D in xml.Elements("PolygonCollider2D"))
            {
                PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();
                collider.isTrigger = isTrigger;

                // Apply the offsets (if any)
                float offset_x = ImportUtils.GetAttributeAsFloat(xmlPolygonCollider2D, "offsetX", 0);
                float offset_y = ImportUtils.GetAttributeAsFloat(xmlPolygonCollider2D, "offsetY", 0);

                var paths = xmlPolygonCollider2D.Elements("Path").ToArray();
                collider.pathCount = paths.Count();

                for (int p = 0; p < collider.pathCount; ++p)
                {
                    string data = paths[p].Value;

                    // The data looks like this:
                    //  x0,y0 x1,y1 x2,y2 ...
                    var points = from pt in data.Split(' ')
                                 let x = Convert.ToSingle(pt.Split(',')[0])
                                 let y = Convert.ToSingle(pt.Split(',')[1])
#if T2U_IS_UNITY_4
                                 // Hack for Unity 4.x
                                 select new Vector2(x + offset_x, y + offset_y);
#else
                                 select new Vector2(x, y);
#endif

                    collider.SetPath(p, points.ToArray());
                }

#if !T2U_IS_UNITY_4
                collider.offset += new Vector2(offset_x, offset_y);
#endif
            }
        }

        private GameObject CreateCopyFromMeshObj(string copyFromName, string objPath, float opacity)
        {
            // Find a matching game object within the mesh object and "copy" it
            // (In Unity terms, the Instantiated object is a copy)
            UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(objPath);
            foreach (var obj in objects)
            {
                if (obj.name != copyFromName)
                    continue;

                // We have a match but is it a game object?
                GameObject gameObj = GameObject.Instantiate(obj) as GameObject;
                if (gameObj == null)
                    continue;

                // Add a component that will control our initial shader properties
                TiledInitialShaderProperties shaderProps = gameObj.AddComponent<TiledInitialShaderProperties>();
                shaderProps.InitialOpacity = opacity;

                // Reset the name so it is not decorated by the Instantiate call
                gameObj.name = obj.name;
                return gameObj;
            }

            // If we're here then there's an error with the mesh name
            Debug.LogError(String.Format("No mesh named '{0}' to copy from.", copyFromName));
            return null;
        }

        private void AddTileAnimatorsTo(GameObject gameObject, TmxMesh mesh)
        {
            TileAnimator tileAnimator = gameObject.AddComponent<TileAnimator>();
            tileAnimator.StartTime = mesh.StartTimeMs * 0.001f;
            tileAnimator.Duration = mesh.DurationMs * 0.001f;
            tileAnimator.TotalAnimationTime = mesh.FullAnimationDurationMs * 0.001f;
        }

        private void HandleTiledAttributes(GameObject gameObject, TmxMap tmxMap)
        {
            // Add the TiledMap component
            TiledMap map = gameObject.AddComponent<TiledMap>();
            map.Orientation = tmxMap.Orientation;
            map.StaggerAxis = tmxMap.StaggerAxis;
            map.StaggerIndex = tmxMap.StaggerIndex;
            map.HexSideLength = tmxMap.HexSideLength;
            map.NumLayers = tmxMap.Layers.Count;
            map.NumTilesWide = tmxMap.Width;
            map.NumTilesHigh = tmxMap.Height;
            map.TileWidth = tmxMap.TileWidth;
            map.TileHeight = tmxMap.TileHeight;
            map.ExportScale = 1.0f;
            map.MapWidthInPixels = tmxMap.MapSizeInPixels().Width;
            map.MapHeightInPixels = tmxMap.MapSizeInPixels().Height;
        }

    } // end class
} // end namespace
#endif