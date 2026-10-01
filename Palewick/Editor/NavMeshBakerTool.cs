using UnityEngine;
using UnityEditor;
public class NavMeshBakerTool : Editor
{
    [MenuItem("Tools/Palewick/Bake NavMesh Ground")]
    public static void Bake()
    {
#pragma warning disable 0618
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore 0618
        Debug.Log("تم توليد خريطة الحركة للوحش بنجاح!");
    }
}
