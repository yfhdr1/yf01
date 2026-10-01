using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Palewick.EditorTools
{
    public static class PreBakeCollisionFixer
    {
        [MenuItem("Tools/Palewick/Fix Pre-Bake Collision")]
        public static void Fix()
        {
            HashSet<string> meshPaths = new HashSet<string>();
            EditorSceneManager.SaveOpenScenes();
            string startScene = SceneManager.GetActiveScene().path;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || string.IsNullOrEmpty(scene.path)) continue;
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                MeshCollider[] colliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include);
                foreach (MeshCollider collider in colliders)
                {
                    if (collider == null || collider.sharedMesh == null) continue;
                    string path = AssetDatabase.GetAssetPath(collider.sharedMesh);
                    if (!string.IsNullOrEmpty(path)) meshPaths.Add(path);
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;
                MeshCollider[] colliders = prefab.GetComponentsInChildren<MeshCollider>(true);
                foreach (MeshCollider collider in colliders)
                {
                    if (collider == null || collider.sharedMesh == null) continue;
                    string path = AssetDatabase.GetAssetPath(collider.sharedMesh);
                    if (!string.IsNullOrEmpty(path)) meshPaths.Add(path);
                }
            }
            int changedCount = 0;
            foreach (string path in meshPaths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                bool triangleEnabled = importer.HasPreBakeCollisionMesh(false);
                bool convexEnabled = importer.HasPreBakeCollisionMesh(true);
                importer.SetPreBakeCollisionMesh(false, true);
                importer.SetPreBakeCollisionMesh(true, false);
                if (!triangleEnabled || convexEnabled) changedCount++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
            Debug.Log("PreBakeCollisionFixer: checked " + meshPaths.Count + " model file(s), enabled triangle pre-bake and disabled convex pre-bake on " + changedCount + ".");
        }
    }
}
