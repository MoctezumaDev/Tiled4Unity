using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Tiled4Unity
{
    [Serializable]
    public struct MeshMaterial
    {
        public string Mesh;
        public string Material;

        public MeshMaterial(string mesh, string material)
        {
            this.Mesh = mesh;
            this.Material = material;
        }
    }

    partial class TiledMapExporter
    {
        private List<MeshMaterial> CreateMeshMaterialsList()
        {
            // Each mesh in each viewable layer needs to have its material assigned to it
            List<MeshMaterial> elements = new List<MeshMaterial>();
            foreach (var layer in this.tmxMap.Layers)
            {
                if (layer.Visible == false)
                    continue;
                if (layer.Ignore == TmxLayer.IgnoreSettings.Visual)
                    continue;

                foreach (TmxMesh mesh in layer.Meshes)
                {
                    MeshMaterial meshMaterial = new MeshMaterial(mesh.UniqueMeshName, Path.ChangeExtension(mesh.TmxImage.AbsolutePath, ".mat"));
                    elements.Add(meshMaterial);
                }
            }

            // Each mesh for each TileObject needs its material assigned
            foreach (var tmxMesh in this.tmxMap.GetUniqueListOfVisibleObjectTileMeshes())
            {
                MeshMaterial meshMaterial = new MeshMaterial(tmxMesh.UniqueMeshName, tmxMesh.TmxImage.AbsolutePath);
                elements.Add(meshMaterial);
            }
            return elements;
        }

private List<XElement> CreateAssignMaterialsElements()
        {
            // Each mesh in each viewable layer needs to have its material assigned to it
            List<XElement> elements = new List<XElement>();
            foreach (var layer in this.tmxMap.Layers)
            {
                if (layer.Visible == false)
                    continue;
                if (layer.Ignore == TmxLayer.IgnoreSettings.Visual)
                    continue;

                foreach (TmxMesh mesh in layer.Meshes)
                {
                   XElement assignment =
                        new XElement("AssignMaterial",
                            new XAttribute("mesh", mesh.UniqueMeshName),
                            new XAttribute("material", Path.GetFileNameWithoutExtension(mesh.TmxImage.AbsolutePath)));

                    elements.Add(assignment);
                }
            }

            // Each mesh for each TileObject needs its material assigned
            foreach (var tmxMesh in this.tmxMap.GetUniqueListOfVisibleObjectTileMeshes())
            {
                XElement assignment =
                     new XElement("AssignMaterial",
                         new XAttribute("mesh", tmxMesh.UniqueMeshName),
                         new XAttribute("material", Path.GetFileNameWithoutExtension(tmxMesh.TmxImage.AbsolutePath)));

                    elements.Add(assignment);
            }

            return elements;
        }
    }
}