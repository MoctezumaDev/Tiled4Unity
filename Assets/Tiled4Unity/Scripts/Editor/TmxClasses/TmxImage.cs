using System;
using UnityEngine;

namespace Tiled4Unity
{
    [System.Serializable]
    public partial class TmxImage
    {
        [SerializeField]
        private string _absolutePath;
        public string AbsolutePath { get { return _absolutePath; } private set { _absolutePath = value; } }

        [SerializeField]
        private Size _size;
        public Size Size { get { return _size; } private set { _size = value; } }

        [SerializeField]
        private string _transparentColor;
        public String TransparentColor { get { return _transparentColor; } private set { _transparentColor = value; } }
    }
}
