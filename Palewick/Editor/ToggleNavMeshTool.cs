#pragma warning disable 0618
using UnityEditor;
using UnityEditor.AI;
public class ToggleNavMeshTool
{
    [MenuItem("Tools/Palewick/Hide NavMesh")]
    public static void Hide()
    {
        NavMeshVisualizationSettings.showNavigation = 0;
    }
    [MenuItem("Tools/Palewick/Show NavMesh")]
    public static void Show()
    {
        NavMeshVisualizationSettings.showNavigation = 1;
    }
}
