using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Palewick.EditorTools
{
    public static class ChatBuilder
    {
        private const string IconFolder = "Assets/UI_Icons";
        [MenuItem("Palewick/Create Chat In Canvas")]
        public static void Create()
        {
            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Chat", "Canvas with PubgPauseMenu not found in the open scene.", "OK");
                return;
            }
            Transform existing = canvas.transform.Find("ChatRoot");
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Chat", "ChatRoot already exists in the Canvas.", "OK");
                return;
            }
            GameObject go = new GameObject("ChatRoot", typeof(RectTransform));
            go.layer = 5;
            RectTransform root = go.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            int index = canvas.transform.childCount - 1;
            Transform menu = canvas.transform.Find("PubgPauseMenu");
            if (menu != null && menu.GetSiblingIndex() < index)
            {
                index = menu.GetSiblingIndex();
            }
            Transform loading = canvas.transform.Find("LoadingPanel");
            if (loading != null && loading.GetSiblingIndex() < index)
            {
                index = loading.GetSiblingIndex();
            }
            root.SetSiblingIndex(index);
            ChatSystem chat = go.AddComponent<ChatSystem>();
            chat.circleArt = SaveSprite("chat_circle.png", ChatSystem.MakeRoundSprite(128, 64), 63);
            chat.squareArt = SaveSprite("chat_square.png", ChatSystem.MakeRoundSprite(32, 5), 5);
            chat.bubbleArt = SaveSprite("chat_bubble.png", ChatSystem.MakeBubbleIcon(128), 0);
            chat.clockArt = SaveSprite("chat_clock.png", ChatSystem.MakeClockIcon(128), 0);
            chat.sendArt = SaveSprite("chat_send.png", ChatSystem.MakeSendIcon(128), 0);
            chat.panelArt = SaveSprite("chat_panel.png", ChatSystem.MakeRoundSprite(64, 18), 18);
            chat.BuildLayoutInEditor();
            Undo.RegisterCreatedObjectUndo(go, "Create Chat");
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
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
        private static Sprite SaveSprite(string fileName, Sprite generated, int border)
        {
            string path = IconFolder + "/" + fileName;
            if (!AssetDatabase.IsValidFolder(IconFolder))
            {
                AssetDatabase.CreateFolder("Assets", "UI_Icons");
            }
            Texture2D tex = generated.texture;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(generated);
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
