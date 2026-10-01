using UnityEngine;
using UnityEditor;

public class MarkStaticTool
{
    [MenuItem("Tools/Mark Scene Static")]
    static void MarkStatic()
    {
        int count = 0;
        StaticEditorFlags flags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic;
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
        foreach (MeshRenderer r in renderers)
        {
            GameObject go = r.gameObject;
            if (go.GetComponentInParent<CharacterController>() != null) continue;
            if (go.GetComponentInParent<Animator>() != null) continue;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            count++;
        }
        Debug.Log("Marked static: " + count);
    }
}
