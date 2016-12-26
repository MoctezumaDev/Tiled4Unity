using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;

namespace Tiled4Unity
{
    // Class to help us manage the import status when a *.tiled4unity.xml file is (re)imported
    // Also holds onto the XML file in memory so that we don't have to keep opening it (an expensive operation) when different parts of the import process needs it.
    public class ImportXMLHelper
    {
        static protected Dictionary<string, ImportXMLHelper> _helpers = new Dictionary<string, ImportXMLHelper>();

        public XDocument XmlDocument { get; private set; }
        public string ImportName { get; private set; }
        public int ImportCounter;
        public int NumberOfElements { get; private set; }

        public static string GetFilenameWithoutTiled4UnityExtension(string filename)
        {
            // Chomp ".tiled4unity.xml" from the end of the file (if it exists) so that we get the proper of the file
            // (Note that Path.GetFileNameWithoutExtension will not work because file name can have extra '.' characters)
            string extension = ".tiled4unity.xml";
            if (filename.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
            {
                filename = filename.Substring(0, filename.Length - extension.Length);
            }

            return Path.GetFileName(filename);
        }

        public float Progress
        {
            get
            {
                if (NumberOfElements == 0) return 1.0f;
                return this.ImportCounter / (float)this.NumberOfElements;
            }
        }


        // We have many independent requests on the ImportXMLHelper so we can't take for granted it has been created yet.
        // However, if it has been created then use it.
        public static ImportXMLHelper ImportPath(string xmlPath)
        {
            string importName = GetFilenameWithoutTiled4UnityExtension(xmlPath);

            // Try to find
            if (_helpers.ContainsKey(importName))
            {
                return _helpers[importName];
            }

            // Couldn't find, so create.
            ImportXMLHelper importXmlHelper = new ImportXMLHelper(importName);
            _helpers.Add(importName, importXmlHelper);

            // Opening the XDocument itself can be expensive so start the progress bar just before we start
            importXmlHelper.XmlDocument = XDocument.Load(xmlPath);

            return importXmlHelper;
        }

        private ImportXMLHelper(string name)
        {
            ImportName = name;
            ImportCounter = 0;
            NumberOfElements = 0;
        }

        public static void RemovePath(string xmlPath)
        {
            string importName = GetFilenameWithoutTiled4UnityExtension(xmlPath);

            // Try to find
            if (_helpers.ContainsKey(importName))
            {
                _helpers.Remove(importName);
            }
        }

    }
}