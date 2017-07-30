using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;


namespace Tiled4Unity
{
    [System.Serializable]
    public struct TmxObj
    {
        public string fileName;
        public string data;

        public TmxObj(string fileName, string fileData)
        {
            this.fileName = fileName;
            this.data = fileData;
        }
    }

    partial class TiledMapExporter
    {
        private class TmxImageComparer : IEqualityComparer<TmxImage>
        {
            public bool Equals(TmxImage lhs, TmxImage rhs)
            {
                return lhs.AbsolutePath.ToLower() == rhs.AbsolutePath.ToLower();
            }

            public int GetHashCode(TmxImage tmxImage)
            {
                return tmxImage.AbsolutePath.GetHashCode();
            }
        }

        private TmxObj CreateMesh()
        {
            StringWriter objBuilder = BuildObjString();
            return new TmxObj(this.tmxMap.Name + ".obj", StringToBase64String(objBuilder.ToString()));
        }

        private IEnumerable<TmxImage> GetImagesInLayers()
        {
            // Add all image files as compressed base64 strings
            var layerImages = from layer in this.tmxMap.Layers
                              where layer.Visible == true
                              from rawTileId in layer.TileIds
                              where rawTileId != 0
                              let tileId = TmxMath.GetTileIdWithoutFlags(rawTileId)
                              let tile = this.tmxMap.Tiles[tileId]
                              select tile.TmxImage;
            return layerImages;
        }

        private IEnumerable<TmxImage> GetImagesInObjects()
        {
            // Tile Objects may have images not yet references by a layer
            var objectImages = from objectGroup in this.tmxMap.ObjectGroups
                               where objectGroup.Visible == true
                               from tmxObject in objectGroup.Objects
                               where tmxObject.Visible == true
                               where tmxObject is TmxObjectTile
                               let tmxTileObject = tmxObject as TmxObjectTile
                               from mesh in tmxTileObject.Tile.Meshes
                               select mesh.TmxImage;
            return objectImages;
        }

        private List<TmxImage> CreateImagesList()
        {
            var layerImages = GetImagesInLayers();
            var objectImages = GetImagesInObjects();

            // Combine image paths from tile layers and object layers
            List<TmxImage> images = new List<TmxImage>();
            images.AddRange(layerImages);
            images.AddRange(objectImages);

            // Get rid of duplicate images
            TmxImageComparer imageComparer = new TmxImageComparer();
            images = images.Distinct(imageComparer).ToList();

            return images;
        }

        private List<XElement> CreateImportFilesElements(string exportToUnityProjectPath)
        {
            List<XElement> elements = new List<XElement>();

            // Add the mesh file as raw text
            {
                StringWriter objBuilder = BuildObjString();

                XElement mesh =
                    new XElement("ImportMesh",
                        new XAttribute("filename", this.tmxMap.Name + ".obj"),
                        StringToBase64String(objBuilder.ToString()));

                elements.Add(mesh);
            }

            {
                // Add all image files as compressed base64 strings
                var layerImages = from layer in this.tmxMap.Layers
                                  where layer.Visible == true
                                  from rawTileId in layer.TileIds
                                  where rawTileId != 0
                                  let tileId = TmxMath.GetTileIdWithoutFlags(rawTileId)
                                  let tile = this.tmxMap.Tiles[tileId]
                                  select tile.TmxImage;

                // Tile Objects may have images not yet references by a layer
                var objectImages = from objectGroup in this.tmxMap.ObjectGroups
                                   where objectGroup.Visible == true
                                   from tmxObject in objectGroup.Objects
                                   where tmxObject.Visible == true
                                   where tmxObject is TmxObjectTile
                                   let tmxTileObject = tmxObject as TmxObjectTile
                                   from mesh in tmxTileObject.Tile.Meshes
                                   select mesh.TmxImage;

                // Combine image paths from tile layers and object layers
                List<TmxImage> images = new List<TmxImage>();
                images.AddRange(layerImages);
                images.AddRange(objectImages);

                // Get rid of duplicate images
                TmxImageComparer imageComparer = new TmxImageComparer();
                images = images.Distinct(imageComparer).ToList();

                foreach (TmxImage image in images)
                {
                    // The source texture is internal if it has a sibling *.meta file
                    // We don't want to copy internal textures into Unity because they are already there.
                    bool isInternal = File.Exists(image.AbsolutePath + ".meta");
                    if (isInternal)
                    {
                        // The texture is already in the Unity project so don't import
                        XElement xmlInternalTexture = new XElement("InternalTexture");

                        // The path to the texture will be WRT to the Unity project root
                        string assetPath = image.AbsolutePath;
                        assetPath = assetPath.TrimStart('\\');
                        assetPath = assetPath.TrimStart('/');
                        Console.WriteLine("InternalTexture : {0}", assetPath);

                        // Path to texture in the asset directory
                        xmlInternalTexture.SetAttributeValue("assetPath", assetPath);

                        // Transparent color key?
                        if (!String.IsNullOrEmpty(image.TransparentColor))
                        {
                            xmlInternalTexture.SetAttributeValue("alphaColorKey", image.TransparentColor);
                        }

                        // Are we using depth shaders on our materials?
                        if (Settings.DepthBufferEnabled)
                        {
                            xmlInternalTexture.SetAttributeValue("usesDepthShaders", true);
                        }

                        elements.Add(xmlInternalTexture);
                    }
                    //TODO external images, we might not support them
                    else
                    {
                        // The texture needs to be imported into the Unity project (under Tiled4Unity's care)
                        XElement xmlImportTexture = new XElement("ImportTexture");

                        // Note that compression is not available in Unity. Go with Base64 string. Blerg.
                        Console.WriteLine("ImportTexture : will import '{0}' to {1}", image.AbsolutePath, Path.Combine(exportToUnityProjectPath, "Textures"));

                        // Is there a color key for transparency?
                        if (!String.IsNullOrEmpty(image.TransparentColor))
                        {
                            xmlImportTexture.SetAttributeValue("alphaColorKey", image.TransparentColor);
                        }

                        // Are we using depth shaders on our materials?
                        if (Settings.DepthBufferEnabled)
                        {
                            xmlImportTexture.SetAttributeValue("usesDepthShaders", true);
                        }

                        // Bake the image file into the xml
                        xmlImportTexture.Add(new XAttribute("filename", Path.GetFileName(image.AbsolutePath)), FileToBase64String(image.AbsolutePath));

                        elements.Add(xmlImportTexture);
                    }
                }
            }

            return elements;
        }

    } // end class
} // end namespace