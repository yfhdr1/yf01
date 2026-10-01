using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Palewick.EditorTools
{
    public static class HorrorLightingTool
    {
        [MenuItem("Tools/Palewick/Apply Horror Lighting")]
        public static void ApplyHorrorLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.08f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.04f, 1f);
            RenderSettings.fogDensity = 0.025f;
            Light[] dirLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light light in dirLights)
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(0.18f, 0.24f, 0.35f, 1f);
                    light.intensity = 0.28f;
                    light.shadows = LightShadows.Soft;
                }
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Palewick: Perfect dark horror lighting applied!");
        }
    }
}
