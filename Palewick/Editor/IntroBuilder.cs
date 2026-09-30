using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class IntroBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color BloodColor = new Color(0.85f, 0.1f, 0.08f, 1f);
        private static Font font;
        [MenuItem("Palewick/Build Horror Intro")]
        public static void Build()
        {
            IntroManager intro = Object.FindAnyObjectByType<IntroManager>(FindObjectsInactive.Include);
            if (intro == null)
            {
                EditorUtility.DisplayDialog("Intro", "Open Scene_Intro first (IntroManager not found).", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                EditorUtility.DisplayDialog("Intro", "Folder Assets/UI_Lobby not found.", "OK");
                return;
            }
            Canvas canvas = null;
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].isRootCanvas && all[i].gameObject.scene == intro.gameObject.scene)
                {
                    canvas = all[i];
                    break;
                }
            }
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Intro", "Canvas not found in the open scene.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Intro", "This deletes everything inside " + canvas.name + " and builds the new horror intro. The video stays. Continue?", "Build", "Cancel"))
            {
                return;
            }
            PrepareImports();
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Undo.SetCurrentGroupName("Build Horror Intro");
            int group = Undo.GetCurrentGroup();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                EditorUtility.SetDirty(scaler);
            }
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(canvas.transform.GetChild(i).gameObject);
            }
            Undo.RecordObject(intro, "Intro");
            Transform root = canvas.transform;
            RectTransform warning = Stretch(Node("Warning", root));
            Img(warning.gameObject, null, Color.black, true);
            intro.warningGroup = warning.gameObject.AddComponent<CanvasGroup>();
            RectTransform phones = Node("Headphones", warning);
            Place(phones, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(230f, 230f));
            Img(phones.gameObject, Spr("lobby_headphones.png"), Color.white, false);
            RectTransform wText = Node("WarningText", warning);
            Place(wText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1600f, 90f));
            Label(wText.gameObject, "Use headphones for the best experience", 58, TextColor, TextAnchor.MiddleCenter);
            RectTransform skip = Node("SkipButton", root);
            Place(skip, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 60f), new Vector2(320f, 100f));
            Image skipImg = Img(skip.gameObject, Spr("lobby_btn_side.png"), Color.white, true);
            Button skipBtn = skip.gameObject.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            ColorBlock cb = skipBtn.colors;
            cb.pressedColor = new Color(0.65f, 0.5f, 0.5f, 1f);
            skipBtn.colors = cb;
            RectTransform skipLabel = Stretch(Node("Text", skip));
            skipLabel.offsetMin = new Vector2(10f, 10f);
            skipLabel.offsetMax = new Vector2(-10f, -4f);
            Label(skipLabel.gameObject, "Skip", 54, TextColor, TextAnchor.MiddleCenter);
            skip.gameObject.SetActive(false);
            intro.skipButton = skipBtn;
            RectTransform loading = Stretch(Node("LoadingRoot", root));
            Img(loading.gameObject, null, Color.black, true);
            RectTransform art = Stretch(Node("Art", loading));
            Img(art.gameObject, Spr("lobby_bg.jpg"), new Color(0.45f, 0.4f, 0.4f, 1f), false);
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            RectTransform vig = Stretch(Node("Vignette", loading));
            Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white, false);
            RectTransform blood = Node("BloodTop", loading);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white, false);
            RectTransform title = Node("Title", loading);
            Place(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 273f));
            Img(title.gameObject, Spr("lobby_title.png"), Color.white, false);
            RectTransform tipHead = Node("TipHeader", loading);
            Place(tipHead, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(600f, 60f));
            Label(tipHead.gameObject, "Tip", 44, BloodColor, TextAnchor.MiddleCenter);
            RectTransform tip = Node("TipText", loading);
            Place(tip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1600f, 70f));
            intro.tipText = Label(tip.gameObject, "Stay close to your friends", 46, TextColor, TextAnchor.MiddleCenter);
            RectTransform barBg = Node("BarBg", loading);
            Place(barBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1024f, 40f));
            Img(barBg.gameObject, Spr("lobby_bar_bg.png"), Color.white, false);
            RectTransform fill = Node("BarFill", barBg);
            Place(fill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 26f));
            Image fillImg = Img(fill.gameObject, Spr("lobby_bar_fill.png"), Color.white, false);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0.35f;
            intro.barFill = fillImg;
            RectTransform pct = Node("PercentText", loading);
            Place(pct, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(535f, 100f), new Vector2(200f, 60f));
            intro.percentText = Label(pct.gameObject, "35%", 44, TextColor, TextAnchor.MiddleLeft);
            RectTransform spin = Node("Spinner", loading);
            Place(spin, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 100f), new Vector2(120f, 120f));
            Img(spin.gameObject, Spr("lobby_spinner.png"), Color.white, false);
            intro.spinner = spin;
            RectTransform lt = Node("LoadingText", loading);
            Place(lt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-185f, 100f), new Vector2(420f, 70f));
            Label(lt.gameObject, "Loading...", 48, TextColor, TextAnchor.MiddleRight);
            loading.gameObject.SetActive(false);
            intro.loadingRoot = loading.gameObject;
            RectTransform fade = Stretch(Node("FadeImage", root));
            Img(fade.gameObject, null, Color.black, false);
            CanvasGroup fg = fade.gameObject.AddComponent<CanvasGroup>();
            fg.alpha = 0f;
            fg.blocksRaycasts = false;
            fg.interactable = false;
            intro.fadeGroup = fg;
            EditorUtility.SetDirty(intro);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);
            Selection.activeGameObject = canvas.gameObject;
            EditorUtility.DisplayDialog("Intro", "Horror intro built. Press Ctrl+S to save the scene.", "OK");
        }
        private static void PrepareImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("lobby_fog.png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null)
                {
                    continue;
                }
                bool changed = false;
                if (ti.textureType != TextureImporterType.Sprite)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (ti.spriteImportMode != SpriteImportMode.Single)
                {
                    ti.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (ti.mipmapEnabled)
                {
                    ti.mipmapEnabled = false;
                    changed = true;
                }
                if (changed)
                {
                    ti.SaveAndReimport();
                }
            }
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Intro");
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
        private static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }
        private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
        private static Image Img(GameObject go, Sprite sprite, Color color, bool raycast)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }
        private static Text Label(GameObject go, string text, int size, Color color, TextAnchor align)
        {
            Text t = go.AddComponent<Text>();
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(3f, -3f);
            return t;
        }
    }
}
