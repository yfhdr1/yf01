using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class GameScreensBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color BloodColor = new Color(0.85f, 0.1f, 0.08f, 1f);
        private static Font font;
        [MenuItem("Palewick/Build Horror Game Screens")]
        public static void Build()
        {
            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Game Screens", "Open Scene_A first (Canvas with PubgPauseMenu not found).", "OK");
                return;
            }
            Transform death = canvas.transform.Find("DeathPanel");
            Transform loading = canvas.transform.Find("LoadingPanel");
            if (death == null || loading == null)
            {
                EditorUtility.DisplayDialog("Game Screens", "DeathPanel or LoadingPanel not found in the Canvas.", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                EditorUtility.DisplayDialog("Game Screens", "Folder Assets/UI_Lobby not found.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Game Screens", "This rebuilds the inside of DeathPanel and LoadingPanel with the horror style. Continue?", "Build", "Cancel"))
            {
                return;
            }
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Undo.SetCurrentGroupName("Build Horror Game Screens");
            int group = Undo.GetCurrentGroup();
            BuildLoading(loading);
            BuildDeath(death);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = death.gameObject;
            EditorUtility.DisplayDialog("Game Screens", "Loading and death screens built. Press Ctrl+S to save the scene.", "OK");
        }
        private static Canvas FindCanvas()
        {
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].isRootCanvas && all[i].transform.Find("PubgPauseMenu") != null) return all[i];
            }
            return null;
        }
        private static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
            }
        }
        private static Image PanelImage(GameObject go, Color color)
        {
            Image img = go.GetComponent<Image>();
            if (img == null) img = Undo.AddComponent<Image>(go);
            Undo.RecordObject(img, "Panel");
            img.sprite = null;
            img.color = color;
            img.raycastTarget = true;
            EditorUtility.SetDirty(img);
            return img;
        }
        private static void BuildLoading(Transform panel)
        {
            Clear(panel);
            PanelImage(panel.gameObject, Color.black);
            LoadingScreenFx fx = panel.GetComponent<LoadingScreenFx>();
            if (fx == null) fx = Undo.AddComponent<LoadingScreenFx>(panel.gameObject);
            Undo.RecordObject(fx, "Loading");
            RectTransform art = Stretch(Node("Art", panel));
            Img(art.gameObject, Spr("lobby_bg.jpg"), new Color(0.45f, 0.4f, 0.4f, 1f));
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            RectTransform vig = Stretch(Node("Vignette", panel));
            Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white);
            RectTransform blood = Node("BloodTop", panel);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white);
            RectTransform title = Node("Title", panel);
            Place(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 273f));
            Img(title.gameObject, Spr("lobby_title.png"), Color.white);
            RectTransform tipHead = Node("TipHeader", panel);
            Place(tipHead, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(600f, 60f));
            Label(tipHead.gameObject, "Tip", 44, BloodColor, TextAnchor.MiddleCenter);
            RectTransform tip = Node("TipText", panel);
            Place(tip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1600f, 70f));
            fx.tipText = Label(tip.gameObject, "Stay close to your friends", 46, TextColor, TextAnchor.MiddleCenter);
            RectTransform barBg = Node("BarBg", panel);
            Place(barBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1024f, 40f));
            Img(barBg.gameObject, Spr("lobby_bar_bg.png"), Color.white);
            RectTransform fill = Node("BarFill", barBg);
            Place(fill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 26f));
            Image fillImg = Img(fill.gameObject, Spr("lobby_bar_fill.png"), Color.white);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0.35f;
            fx.barFill = fillImg;
            RectTransform pct = Node("PercentText", panel);
            Place(pct, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(535f, 100f), new Vector2(200f, 60f));
            fx.percentText = Label(pct.gameObject, "35%", 44, TextColor, TextAnchor.MiddleLeft);
            RectTransform spin = Node("Spinner", panel);
            Place(spin, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 100f), new Vector2(120f, 120f));
            Img(spin.gameObject, Spr("lobby_spinner.png"), Color.white);
            fx.spinner = spin;
            RectTransform lt = Node("LoadingText", panel);
            Place(lt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-185f, 100f), new Vector2(420f, 70f));
            Label(lt.gameObject, "Loading...", 48, TextColor, TextAnchor.MiddleRight);
            EditorUtility.SetDirty(fx);
        }
        private static void BuildDeath(Transform panel)
        {
            Clear(panel);
            PanelImage(panel.gameObject, new Color(0f, 0f, 0f, 0.6f));
            DeathScreen ds = panel.GetComponent<DeathScreen>();
            if (ds == null) ds = Undo.AddComponent<DeathScreen>(panel.gameObject);
            Undo.RecordObject(ds, "Death");
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = Undo.AddComponent<CanvasGroup>(panel.gameObject);
            ds.group = cg;
            RectTransform red = Stretch(Node("RedPulse", panel));
            ds.redPulse = Img(red.gameObject, Spr("lobby_vignette.png"), new Color(1f, 0.25f, 0.25f, 0.7f));
            RectTransform blood = Node("BloodTop", panel);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 260f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white);
            RectTransform title = Node("DeathTitle", panel);
            Place(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(1400f, 220f));
            Text tt = Label(title.gameObject, "You Died", 190, BloodColor, TextAnchor.MiddleCenter);
            Outline ol = title.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
            ol.effectDistance = new Vector2(4f, -4f);
            tt.raycastTarget = false;
            ds.title = title;
            RectTransform sub = Node("DeathSubtitle", panel);
            Place(sub, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 70f));
            Label(sub.gameObject, "It found you", 54, TextColor, TextAnchor.MiddleCenter);
            RectTransform cd = Node("Countdown", panel);
            Place(cd, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, -250f), new Vector2(120f, 120f));
            ds.countdownText = Label(cd.gameObject, "5", 80, TextColor, TextAnchor.MiddleCenter);
            RectTransform respawn = Node("RespawnBtn", panel);
            Place(respawn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, -150f), new Vector2(400f, 120f));
            ds.respawnButton = PlateButton(respawn.gameObject, "lobby_btn_start.png", "Respawn", 60);
            RectTransform leave = Node("LeaveBtn", panel);
            Place(leave, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, -150f), new Vector2(400f, 120f));
            ds.leaveButton = PlateButton(leave.gameObject, "lobby_btn_side.png", "Leave", 60);
            EditorUtility.SetDirty(ds);
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Screens");
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
        private static Image Img(GameObject go, Sprite sprite, Color color)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
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
        private static Button PlateButton(GameObject go, string sprite, string text, int size)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = Spr(sprite);
            img.raycastTarget = true;
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.pressedColor = new Color(0.65f, 0.5f, 0.5f, 1f);
            cb.disabledColor = new Color(0.45f, 0.4f, 0.4f, 0.8f);
            b.colors = cb;
            RectTransform lr = Stretch(Node("Label", go.transform));
            lr.offsetMin = new Vector2(10f, 10f);
            lr.offsetMax = new Vector2(-10f, -4f);
            Label(lr.gameObject, text, size, TextColor, TextAnchor.MiddleCenter);
            return b;
        }
    }
}
