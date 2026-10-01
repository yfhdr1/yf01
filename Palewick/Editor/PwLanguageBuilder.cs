using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class PwLanguageBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private const string ScenePath = "Assets/a.loby/Scene_Language.unity";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color BloodColor = new Color(0.85f, 0.1f, 0.08f, 1f);
        private static Font font;
        [MenuItem("Palewick/Build Language Screen")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                EditorUtility.DisplayDialog("Language", "Folder Assets/UI_Lobby not found.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Language", "This creates Scene_Language and adds it to Build Settings between Scene_Intro and Scene_Lobby. Continue?", "Build", "Cancel"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = 5;
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Transform root = canvasGo.transform;
            RectTransform bg = Stretch(Node("Background", root));
            Image bgImg = Img(bg.gameObject, Spr("lobby_bg.jpg"), new Color(0.55f, 0.5f, 0.5f, 1f), false);
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            bgImg.preserveAspect = false;
            RectTransform vig = Stretch(Node("Vignette", root));
            Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white, false);
            RectTransform blood = Node("BloodTop", root);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white, false);
            RectTransform box = Node("Box", root);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 700f));
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(-40f, 110f));
            Label(title.gameObject, "Choose your language", 62, BloodColor, TextAnchor.MiddleCenter);
            GameObject screenGo = new GameObject("LanguageScreen");
            PwLanguageScreen screen = screenGo.AddComponent<PwLanguageScreen>();
            RectTransform kurdish = Node("KurdishBtn", box);
            Place(kurdish, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(760f, 130f));
            screen.kurdishButton = PlateButton(kurdish.gameObject, "lobby_btn_start.png", "کوردی", 62, screen.PickKurdish, true);
            RectTransform arabic = Node("ArabicBtn", box);
            Place(arabic, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(760f, 130f));
            screen.arabicButton = PlateButton(arabic.gameObject, "lobby_btn_side.png", "العربية", 62, screen.PickArabic, true);
            RectTransform english = Node("EnglishBtn", box);
            Place(english, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -490f), new Vector2(760f, 130f));
            screen.englishButton = PlateButton(english.gameObject, "lobby_btn_side.png", "English", 62, screen.PickEnglish, false);
            RectTransform fade = Stretch(Node("Fade", root));
            Img(fade.gameObject, null, Color.black, false);
            CanvasGroup fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
            screen.fade = fadeGroup;
            screen.nextScene = "Scene_Lobby";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            EditorUtility.DisplayDialog("Language", "Scene_Language created and added to Build Settings.", "OK");
        }
        private static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].path == ScenePath)
                {
                    list[i].enabled = true;
                    EditorBuildSettings.scenes = list.ToArray();
                    return;
                }
            }
            int lobbyIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].path.EndsWith("Scene_Lobby.unity")) lobbyIndex = i;
            }
            EditorBuildSettingsScene entry = new EditorBuildSettingsScene(ScenePath, true);
            if (lobbyIndex >= 0) list.Insert(lobbyIndex, entry);
            else list.Add(entry);
            EditorBuildSettings.scenes = list.ToArray();
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
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
        private static Button PlateButton(GameObject go, string sprite, string text, int size, UnityAction action, bool rtl)
        {
            Image img = Img(go, Spr(sprite), Color.white, true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.65f, 0.5f, 0.5f, 1f);
            b.colors = cb;
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            RectTransform lr = Stretch(Node("Label", go.transform));
            lr.offsetMin = new Vector2(14f, 12f);
            lr.offsetMax = new Vector2(-14f, -6f);
            Text t = Label(lr.gameObject, text, size, TextColor, TextAnchor.MiddleCenter);
            if (rtl)
            {
                Font rtlFont = Resources.Load<Font>("Fonts/UniMahanBilal");
                if (rtlFont != null) t.font = rtlFont;
            }
            return b;
        }
    }
}
