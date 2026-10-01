using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class PwShopBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color BloodColor = new Color(0.85f, 0.1f, 0.08f, 1f);
        private static readonly Color HintColor = new Color(0.75f, 0.68f, 0.66f, 0.85f);
        private static Font font;
        [MenuItem("Palewick/Build Shop")]
        public static void BuildMenu()
        {
            LobbyManager lobby = Object.FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
            if (lobby == null)
            {
                EditorUtility.DisplayDialog("Shop", "Open Scene_Lobby first (LobbyManager not found).", "OK");
                return;
            }
            Canvas canvas = null;
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].isRootCanvas && all[i].gameObject.scene == lobby.gameObject.scene) canvas = all[i];
            }
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Shop", "Canvas not found in the open scene.", "OK");
                return;
            }
            Undo.SetCurrentGroupName("Build Shop");
            int group = Undo.GetCurrentGroup();
            Build(canvas.transform, lobby);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(lobby.gameObject.scene);
            EditorUtility.DisplayDialog("Shop", "Shop built. Press Ctrl+S to save the scene.", "OK");
        }
        public static void Build(Transform root, LobbyManager lobby)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Kill(root, "ShopPanel");
            Kill(root, "ShopBtn");
            PwShopUI ui = lobby.GetComponent<PwShopUI>();
            if (ui == null) ui = Undo.AddComponent<PwShopUI>(lobby.gameObject);
            Undo.RecordObject(ui, "Shop");
            RectTransform openBtn = Node("ShopBtn", root);
            Place(openBtn, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, -60f), new Vector2(430f, 102f));
            PlateButton(openBtn.gameObject, "lobby_btn_side.png", "Shop", 50, ui.Open);
            RectTransform shade = Stretch(Node("ShopPanel", root));
            Img(shade.gameObject, null, new Color(0f, 0f, 0f, 0.88f), true);
            ui.panel = shade.gameObject;
            RectTransform box = Node("Box", shade);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 860f));
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-60f, 92f));
            Label(title.gameObject, "Shop", 66, BloodColor, TextAnchor.MiddleCenter);
            RectTransform close = Node("CloseButton", box);
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(100f, 100f));
            ui.closeButton = RoundButton(close.gameObject, Spr("lobby_close.png"), ui.Close);
            RectTransform badge = Node("ShopPoints", box);
            Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(300f, 76f));
            Img(badge.gameObject, Spr("lobby_nameplate.png"), new Color(1f, 1f, 1f, 0.9f), false);
            RectTransform badgeIcon = Node("Icon", badge);
            Place(badgeIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 0f), new Vector2(42f, 42f));
            Img(badgeIcon.gameObject, Spr("lobby_ember.png"), new Color(1f, 0.76f, 0.3f, 1f), false);
            RectTransform badgeValue = Node("PointsValue", badge);
            Place(badgeValue, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(160f, 56f));
            ui.pointsText = Label(badgeValue.gameObject, "0", 44, TextColor, TextAnchor.MiddleLeft);
            string[] names = { "Default Skin", "Red Skin", "Black Skin", "Gold Skin", "Strong Flashlight" };
            int[] flags = { 0, PwShop.SkinRed, PwShop.SkinBlack, PwShop.SkinGold, PwShop.FlashUp };
            int[] prices = { 0, PwShop.PriceRed, PwShop.PriceBlack, PwShop.PriceGold, PwShop.PriceFlash };
            int[] skins = { 0, 1, 2, 3, -1 };
            List<PwShopUI.Card> cards = new List<PwShopUI.Card>();
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform row = Node("Card" + i, box);
                Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f - i * 126f), new Vector2(1140f, 112f));
                PwShopUI.Card card = new PwShopUI.Card();
                card.flag = flags[i];
                card.price = prices[i];
                card.skinIndex = skins[i];
                card.button = PlateButton(row.gameObject, "lobby_btn_side.png", string.Empty, 1, null);
                Transform oldLabel = row.Find("Label");
                if (oldLabel != null) Undo.DestroyObjectImmediate(oldLabel.gameObject);
                if (skins[i] >= 0)
                {
                    RectTransform swatch = Node("Swatch", row);
                    Place(swatch, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(56f, 56f));
                    Img(swatch.gameObject, Spr("lobby_ember.png"), PwLoadout.ColorFor(skins[i]), false);
                }
                RectTransform name = Node("Name", row);
                Place(name, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(560f, 64f));
                Label(name.gameObject, names[i], 42, TextColor, TextAnchor.MiddleLeft);
                RectTransform coin = Node("Coin", row);
                Place(coin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 0f), new Vector2(38f, 38f));
                Img(coin.gameObject, Spr("lobby_ember.png"), new Color(1f, 0.76f, 0.3f, 1f), false);
                RectTransform price = Node("PriceValue", row);
                Place(price, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(140f, 56f));
                card.priceText = Label(price.gameObject, prices[i].ToString(), 40, TextColor, TextAnchor.MiddleLeft);
                RectTransform state = Node("State", row);
                Place(state, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(320f, 60f));
                card.stateText = Label(state.gameObject, "Buy", 40, HintColor, TextAnchor.MiddleRight);
                cards.Add(card);
            }
            ui.cards = cards.ToArray();
            RectTransform msg = Node("ShopMessage", box);
            Place(msg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(1100f, 58f));
            ui.messageText = Label(msg.gameObject, "Not enough points", 38, new Color(1f, 0.5f, 0.35f, 1f), TextAnchor.MiddleCenter);
            msg.gameObject.SetActive(false);
            shade.gameObject.SetActive(false);
            EditorUtility.SetDirty(ui);
        }
        private static void Kill(Transform root, string name)
        {
            Transform old = root.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Shop");
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
        private static Button RoundButton(GameObject go, Sprite sprite, UnityAction action)
        {
            Image img = Img(go, sprite, Color.white, true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.pressedColor = new Color(0.7f, 0.55f, 0.55f, 1f);
            b.colors = cb;
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            return b;
        }
        private static Button PlateButton(GameObject go, string sprite, string text, int size, UnityAction action)
        {
            Image img = Img(go, Spr(sprite), Color.white, true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.65f, 0.5f, 0.5f, 1f);
            cb.disabledColor = new Color(0.45f, 0.4f, 0.4f, 0.75f);
            b.colors = cb;
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            if (string.IsNullOrEmpty(text)) return b;
            RectTransform lr = Stretch(Node("Label", go.transform));
            lr.offsetMin = new Vector2(12f, 10f);
            lr.offsetMax = new Vector2(-12f, -4f);
            Label(lr.gameObject, text, size, TextColor, TextAnchor.MiddleCenter);
            return b;
        }
    }
}
