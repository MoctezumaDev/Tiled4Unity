using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Tiled4Unity
{
    interface ICustomTiledImporter
    {
        // A game object within the prefab has some custom properites assigned through Tiled that are not consumed by Tiled4Unity
        // This callback gives customized importers a chance to react to such properites.
        void HandleCustomProperties(GameObject gameObject, IDictionary<string, string> customProperties);

        // Called just before the prefab is saved to the asset database
        // A last chance opporunity to modify it through script
        void CustomizePrefab(GameObject prefab);
    }
}

// Examples
/*
[Tiled4Unity.CustomTiledImporter]
class CustomImporterAddComponent : Tiled4Unity.ICustomTiledImporter
{
    public void HandleCustomProperties(UnityEngine.GameObject gameObject,
        IDictionary<string, string> props)
    {
        // Simply add a component to our GameObject
        if (props.ContainsKey("AddComp"))
        {
            gameObject.AddComponent(props["AddComp"]);
        }
    }


    public void CustomizePrefab(GameObject prefab)
    {
        // Do nothing
    }
}
*/
