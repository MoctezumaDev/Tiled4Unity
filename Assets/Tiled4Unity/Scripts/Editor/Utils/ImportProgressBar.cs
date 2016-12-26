using UnityEngine;
using System.Collections;

//TODO: temporal class to be removed, it is used to properly split the editor and runtime lirbaries
public static class ImportProgressBar {

    public static void DisplayProgressBar(string importName, string detail, float progress)
    {
        string title = string.Format("Tiled4Unity Import ({0})", importName);
        UnityEditor.EditorUtility.DisplayProgressBar(title, detail, progress);
    }

    public static void HideProgressBar()
    {
        UnityEditor.EditorUtility.ClearProgressBar();  
    }
}
