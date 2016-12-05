using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tiled2Unity
{
    partial class TmxImage
    {
        public static TmxImage FromXml(XElement elemImage, string projectPath)
        {
            TmxImage tmxImage = new TmxImage();
            tmxImage.AbsolutePath = projectPath+elemImage.Attribute("source").Value;

            try
            {
                tmxImage.ImageBitmap = TmxHelper.FromFileBitmap32bpp(tmxImage.AbsolutePath);
            }
            catch (FileNotFoundException fnf)
            {
                string msg = String.Format("Image file not found: {0}", tmxImage.AbsolutePath);
                throw new TmxException(msg, fnf);
            }

            tmxImage.Size = new Size(tmxImage.ImageBitmap.width, tmxImage.ImageBitmap.height);

            // Some images use a transparency color key instead of alpha (blerg)
            tmxImage.TransparentColor = TmxHelper.GetAttributeAsString(elemImage, "trans", "");
            if (!String.IsNullOrEmpty(tmxImage.TransparentColor))
            {
#if !TILED_2_UNITY_LITE
                Color transColor = TmxHelper.ColorFromHtml(tmxImage.TransparentColor);
                //TODO: Transparent color?
                //tmxImage.ImageBitmap.MakeTransparent(transColor);
#endif
            }

            return tmxImage;
        }
    }
}
