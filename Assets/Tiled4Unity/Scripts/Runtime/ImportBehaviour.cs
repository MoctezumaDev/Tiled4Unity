#if !UNITY_WEBPLAYER
// Note: This behaviour cannot be used in WebPlayer
using System;

#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Xml.Linq;
#endif

using UnityEngine;

namespace Tiled4Unity
{
    // Class to help us manage the import status when a *.tiled4unity.xml file is (re)imported
    // Also holds onto the XML file in memory so that we don't have to keep opening it (an expensive operation) when different parts of the import process needs it.
    // This is a *temporary* behaviour we add to the hierarchy only while importing. It should not be around for runtime.
    public class ImportBehaviour : MonoBehaviour
    {
        public string ImportName;

        public XDocument XmlDocument { get; private set; }

        public int ImportCounter = 0;
        public int NumberOfElements = 0;

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
        

        // We have many independent requests on the ImportBehaviour so we can't take for granted it has been created yet.
        // However, if it has been created then use it.
        public static ImportBehaviour FindOrCreateImportBehaviour(string xmlPath, Action<string, string, float> onProgress)
        {
            string importName = ImportBehaviour.GetFilenameWithoutTiled4UnityExtension(xmlPath);

            // Try to find
            foreach (var status in UnityEngine.Object.FindObjectsOfType<ImportBehaviour>())
            {
                if (String.Compare(status.ImportName, importName, true) == 0)
                {
                    return status;
                }
            }

            // Couldn't find, so create.
            GameObject gameObject = new GameObject("__temp_tiled4unity_import");
#if !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_2 && !UNITY_4_3
            gameObject.transform.SetAsFirstSibling();
#endif

            ImportBehaviour importStatus = gameObject.AddComponent<ImportBehaviour>();
            importStatus.ImportName = importName;

            // Opening the XDocument itself can be expensive so start the progress bar just before we start
            //importStatus.StartProgressBar(xmlPath);
            importStatus.XmlDocument = XDocument.Load(xmlPath);

            importStatus.NumberOfElements = importStatus.XmlDocument.Element("Tiled4Unity").Elements().Count();
            onProgress(xmlPath, importName, importStatus.Progress);
            importStatus.ImportCounter++;

            return importStatus;
        }

        // In case this behaviour leaks out of an import and into the runtime, complain.
        private void Update()
        {
            Debug.LogError(String.Format("ImportBehaviour {0} left in scene after importing. Check if import was successful and remove this object from scene {1}", this.ImportName, this.gameObject.name));
        }

    }
}
#endif // if UNITY_WEBPLAYER