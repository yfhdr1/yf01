using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Palewick.EditorTools
{
    public static class HudCornerLock
    {
        private static readonly HashSet<string> Skip = new HashSet<string> { "PubgPauseMenu", "LoadingPanel", "DeathPanel", "BrightnessOverlay", "EventSystem", "FpsCounter", "MinimapRoot", "ChatRoot", "Measure", "PointsBadge" };
        [MenuItem("Palewick/Lock HUD To Screen Corners")]
        public static void Run()
        {
            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("HUD", "Canvas with PubgPauseMenu not found in the open scene.", "OK");
                return;
            }
            Canvas.ForceUpdateCanvases();
            List<RectTransform> targets = new List<RectTransform>();
            Collect(canvas.transform, targets);
            Transform minimapRoot = canvas.transform.Find("MinimapRoot");
            if (minimapRoot != null)
            {
                Collect(minimapRoot, targets);
            }
            Transform chatRoot = canvas.transform.Find("ChatRoot");
            if (chatRoot != null)
            {
                Collect(chatRoot, targets);
            }
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (Lock(targets[i]))
                {
                    count++;
                }
            }
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorUtility.DisplayDialog("HUD", count + " HUD elements locked to their nearest screen corner.", "OK");
        }
        private static void Collect(Transform parent, List<RectTransform> list)
        {
            foreach (Transform child in parent)
            {
                RectTransform rt = child as RectTransform;
                if (rt == null || Skip.Contains(rt.name))
                {
                    continue;
                }
                bool full = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
                if (full)
                {
                    continue;
                }
                list.Add(rt);
            }
        }
        private static bool Lock(RectTransform rt)
        {
            RectTransform parent = rt.parent as RectTransform;
            if (parent == null)
            {
                return false;
            }
            Rect p = parent.rect;
            Vector2 size = rt.rect.size;
            Vector2 pivotPos = rt.localPosition;
            Vector2 center = pivotPos + Vector2.Scale(new Vector2(0.5f, 0.5f) - rt.pivot, size);
            float ax = center.x > p.center.x ? 1f : 0f;
            float ay = center.y > p.center.y ? 1f : 0f;
            Vector2 corner = new Vector2(ax > 0.5f ? p.xMax : p.xMin, ay > 0.5f ? p.yMax : p.yMin);
            Undo.RecordObject(rt, "Lock HUD To Corners");
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            rt.sizeDelta = size;
            rt.anchoredPosition = pivotPos - corner;
            EditorUtility.SetDirty(rt);
            return true;
        }
        private static Canvas FindCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].transform.Find("PubgPauseMenu") != null)
                {
                    return canvases[i];
                }
            }
            return null;
        }
    }
}
