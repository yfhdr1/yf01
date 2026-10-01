using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Palewick.EditorTools
{
    public static class PwPluginMetaFixer
    {
        [MenuItem("Palewick/Fix Plugin Meta Files")]
        public static void Run()
        {
            string[] all = AssetDatabase.GetAllAssetPaths();
            List<string> targets = new List<string>();
            for (int i = 0; i < all.Length; i++)
            {
                string path = all[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.StartsWith("Assets/")) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null) continue;
                targets.Add(path);
            }
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("Plugins", "No plugin files found.", "OK");
                return;
            }
            AssetDatabase.ForceReserializeAssets(targets, ForceReserializeAssetsOptions.ReserializeMetadata);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.DisplayDialog("Plugins", targets.Count + " plugin meta files upgraded. Restart Unity to clear the old warnings.", "OK");
        }
    }
}
