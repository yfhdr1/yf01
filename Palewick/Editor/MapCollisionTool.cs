#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapCollisionTool
{
    [MenuItem("Tools/Setup Solid Map Colliders")]
    public static void SetupSolidMapColliders()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        MeshFilter[] meshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
        int added = 0;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            GameObject target = meshFilter.gameObject;
            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (target.scene != activeScene) continue;
            if (!target.activeInHierarchy || !target.isStatic) continue;
            if (meshRenderer == null || !meshRenderer.enabled) continue;
            if (meshFilter.sharedMesh == null || meshFilter.sharedMesh.vertexCount == 0) continue;
            if (target.GetComponent<Collider>() != null) continue;
            if (ShouldSkip(target, meshRenderer)) continue;
            MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(target);
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            added++;
        }
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("Map Colliders", added + " solid colliders added.", "OK");
    }
    private static bool ShouldSkip(GameObject target, MeshRenderer meshRenderer)
    {
        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0 && target.layer == waterLayer) return true;
        if (target.GetComponent<Terrain>() != null) return true;
        string objectName = target.name.ToLowerInvariant();
        if (ContainsExcludedName(objectName)) return true;
        Material[] materials = meshRenderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null) continue;
            string materialName = material.name.ToLowerInvariant();
            string shaderName = material.shader == null ? string.Empty : material.shader.name.ToLowerInvariant();
            if (ContainsExcludedName(materialName) || ContainsExcludedName(shaderName)) return true;
        }
        return false;
    }
    private static bool ContainsExcludedName(string value)
    {
        return value.Contains("water") || value.Contains("grass") || value.Contains("weed") || value.Contains("foliage") || value.Contains("vegetation") || value.Contains("leaf") || value.Contains("leaves") || value.Contains("bush") || value.Contains("wind");
    }
}
#endif
