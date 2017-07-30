using System;
using UnityEngine;
using System.IO;
using System.Xml.Linq;

namespace Tiled4Unity
{
    partial class TmxImage
    {
        public static TmxImage FromXml(XElement elemImage, string projectPath)
        {
            TmxImage tmxImage = new TmxImage();
            tmxImage.AbsolutePath = projectPath+elemImage.Attribute("source").Value;

            try
            {
                Texture ImageBitmap = TmxHelper.FromFileBitmap32bpp(tmxImage.AbsolutePath);
                tmxImage.Size = new Size(ImageBitmap.width, ImageBitmap.height);
            }
            catch (FileNotFoundException fnf)
            {
                tmxImage.Size = new Size(0, 0);
                string msg = String.Format("Image file not found: {0}", tmxImage.AbsolutePath);
                throw new TmxException(msg, fnf);
            }

            

            // Some images use a transparency color key instead of alpha (blerg)
            tmxImage.TransparentColor = TmxHelper.GetAttributeAsString(elemImage, "trans", "");
            if (!String.IsNullOrEmpty(tmxImage.TransparentColor))
            {
                //TODO: Transparent color? this would require a material
                //Color transColor = TmxHelper.ColorFromHtml(tmxImage.TransparentColor);
                //tmxImage.ImageBitmap.(transColor);
            }

            return tmxImage;
        }
    }
}
