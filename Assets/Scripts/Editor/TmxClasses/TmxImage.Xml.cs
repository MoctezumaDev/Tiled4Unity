﻿using System;
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
        public static TmxImage FromXml(XElement elemImage)
        {
            TmxImage tmxImage = new TmxImage();
            tmxImage.AbsolutePath = TmxHelper.GetAttributeAsFullPath(elemImage, "source");

#if TILED_2_UNITY_LITE
            // Do not open the image in Tiled2UnityLite (due to difficulty with GDI+ in some mono installs)
            int width = TmxHelper.GetAttributeAsInt(elemImage, "width");
            int height = TmxHelper.GetAttributeAsInt(elemImage, "height");
            tmxImage.Size = new Size(width, height);
#else
            try
            {
                tmxImage.ImageBitmap = TmxHelper.FromFileBitmap32bpp(tmxImage.AbsolutePath);
            }
            catch (FileNotFoundException fnf)
            {
                string msg = String.Format("Image file not found: {0}", tmxImage.AbsolutePath);
                throw new TmxException(msg, fnf);

                // Testing for when image files are missing. Just make up an image.
                //int width = TmxHelper.GetAttributeAsInt(elemImage, "width");
                //int height = TmxHelper.GetAttributeAsInt(elemImage, "height");
                //tmxImage.ImageBitmap = new TmxHelper.CreateBitmap32bpp(width, height);
                //using (Graphics g = Graphics.FromImage(tmxImage.ImageBitmap))
                //{
                //    int color32 = tmxImage.AbsolutePath.GetHashCode();
                //    Color color = Color.FromArgb(color32);
                //    color = Color.FromArgb(255, color);
                //    using (Brush brush = new SolidBrush(color))
                //    {
                //        g.FillRectangle(brush, new Rectangle(Point.Empty, tmxImage.ImageBitmap.Size));
                //    }
                //}
            }

            tmxImage.Size = new Size(tmxImage.ImageBitmap.width, tmxImage.ImageBitmap.height);
#endif

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
