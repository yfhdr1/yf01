using System.Collections.Generic;
using System.IO;
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
            Img(shade.gameObject, null, new Color(0f, 0f, 0f, 0.9f), true);
            ui.panel = shade.gameObject;
            RectTransform box = Node("Box", shade);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 1050f));
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-80f, 84f));
            Label(title.gameObject, "Shop", 62, BloodColor, TextAnchor.MiddleCenter);
            RectTransform sub = Node("Subtitle", box);
            Place(sub, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(-80f, 42f));
            Label(sub.gameObject, "Points come from map items and ads", 28, HintColor, TextAnchor.MiddleCenter);
            RectTransform close = Node("CloseButton", box);
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(96f, 96f));
            ui.closeButton = RoundButton(close.gameObject, Spr("lobby_close.png"), ui.Close);
            RectTransform badge = Node("ShopPoints", box);
            Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(290f, 72f));
            Img(badge.gameObject, Spr("lobby_nameplate.png"), new Color(1f, 1f, 1f, 0.9f), false);
            RectTransform badgeIcon = Node("Icon", badge);
            Place(badgeIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 0f), new Vector2(40f, 40f));
            Img(badgeIcon.gameObject, Spr("lobby_ember.png"), new Color(1f, 0.76f, 0.3f, 1f), false);
            RectTransform badgeValue = Node("PointsValue", badge);
            Place(badgeValue, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(160f, 54f));
            ui.pointsText = Label(badgeValue.gameObject, "0", 42, TextColor, TextAnchor.MiddleLeft);
            string[] tabNames = { "Character", "Gear", "Points" };
            List<PwShopUI.Section> sections = new List<PwShopUI.Section>();
            for (int t = 0; t < tabNames.Length; t++)
            {
                PwShopUI.Section section = new PwShopUI.Section();
                RectTransform tab = Node("Tab" + t, box);
                Place(tab, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((t - 1) * 448f, -152f), new Vector2(436f, 74f));
                Image fill = Img(tab.gameObject, Spr("lobby_btn_side.png"), new Color(0.14f, 0.12f, 0.13f, 1f), true);
                Button tabButton = tab.gameObject.AddComponent<Button>();
                tabButton.targetGraphic = fill;
                ColorBlock cb = tabButton.colors;
                cb.highlightedColor = new Color(1f, 0.92f, 0.92f, 1f);
                cb.pressedColor = new Color(0.7f, 0.55f, 0.55f, 1f);
                tabButton.colors = cb;
                RectTransform tabLabel = Stretch(Node("Label", tab));
                tabLabel.offsetMin = new Vector2(10f, 8f);
                tabLabel.offsetMax = new Vector2(-10f, -4f);
                section.tab = tabButton;
                section.tabFill = fill;
                section.tabText = Label(tabLabel.gameObject, tabNames[t], 40, TextColor, TextAnchor.MiddleCenter);
                RectTransform page = Stretch(Node("Section" + t, box));
                section.root = page.gameObject;
                sections.Add(section);
            }
            ui.sections = sections.ToArray();
            List<PwShopUI.Card> cards = new List<PwShopUI.Card>();
            int[] slot = new int[tabNames.Length];
            AddCard(cards, sections, slot, 0, 0, 0, 0, 0, -1, "Default Skin", "The normal white look", "Free for everyone", "shop_skin_white", Color.white);
            AddCard(cards, sections, slot, 0, 0, PwShop.SkinRed, PwShop.PriceRed, 1, -1, "Red Skin", "Blood red clown body", "All players see your color", "shop_skin_red", Color.white);
            AddCard(cards, sections, slot, 0, 0, PwShop.SkinBlack, PwShop.PriceBlack, 2, -1, "Black Skin", "Dark body for the night", "Harder to spot in the dark", "shop_skin_black", Color.white);
            AddCard(cards, sections, slot, 0, 0, PwShop.SkinGold, PwShop.PriceGold, 3, -1, "Gold Skin", "Shiny gold body", "The rarest look in the game", "shop_skin_gold", Color.white);
            AddCard(cards, sections, slot, 1, 0, PwShop.FlashUp, PwShop.PriceFlash, -1, -1, "Strong Flashlight", "Light range 45m instead of 30m", "Wider beam and brighter", "shop_flashlight", Color.white);
            AddCard(cards, sections, slot, 1, 0, 0, 0, -1, 0, "White Light", "Normal warm white beam", "Free for everyone", "shop_flashlight", new Color(1f, 0.96f, 0.88f, 1f));
            AddCard(cards, sections, slot, 1, 0, PwShop.LampRed, PwShop.PriceLamp, -1, 1, "Red Light", "Blood red beam", "Scary red light in the dark", "shop_flashlight", new Color(1f, 0.42f, 0.36f, 1f));
            AddCard(cards, sections, slot, 1, 0, PwShop.LampBlue, PwShop.PriceLamp, -1, 2, "Blue Light", "Cold blue beam", "Clear light for dark rooms", "shop_flashlight", new Color(0.5f, 0.72f, 1f, 1f));
            AddCard(cards, sections, slot, 2, 1, 0, 0, -1, -1, "Watch Ad", "Watch a short video", "Get 1 point every minute", "shop_ad", Color.white);
            ui.cards = cards.ToArray();
            RectTransform msg = Node("ShopMessage", box);
            Place(msg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1500f, 56f));
            ui.messageText = Label(msg.gameObject, "Not enough points", 38, new Color(1f, 0.5f, 0.35f, 1f), TextAnchor.MiddleCenter);
            msg.gameObject.SetActive(false);
            for (int i = 0; i < sections.Count; i++)
            {
                sections[i].root.SetActive(i == 0);
            }
            shade.gameObject.SetActive(false);
            EditorUtility.SetDirty(ui);
        }
        private static void AddCard(List<PwShopUI.Card> cards, List<PwShopUI.Section> sections, int[] slot, int section, int kind, int flag, int price, int skinIndex, int lampIndex, string name, string line1, string line2, string art, Color artColor)
        {
            int index = slot[section];
            slot[section] = index + 1;
            int col = index % 4;
            int line = index / 4;
            float[] columns = { -615f, -205f, 205f, 615f };
            RectTransform tile = Node("Card" + section + "_" + index, sections[section].root.transform);
            Place(tile, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(columns[col], -196f - line * 490f), new Vector2(390f, 470f));
            PwShopUI.Card card = new PwShopUI.Card();
            card.section = section;
            card.kind = kind;
            card.flag = flag;
            card.price = price;
            card.skinIndex = skinIndex;
            card.lampIndex = lampIndex;
            card.button = PlateButton(tile.gameObject, "lobby_btn_side.png", string.Empty, 1, null);
            RectTransform icon = Node("Icon", tile);
            Place(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(276f, 276f));
            Sprite iconSprite = FindSprite(art);
            if (iconSprite == null) iconSprite = Spr("lobby_avatar.png");
            Image iconImg = Img(icon.gameObject, iconSprite, artColor, false);
            iconImg.preserveAspect = true;
            RectTransform label = Node("Name", tile);
            Place(label, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -296f), new Vector2(-24f, 46f));
            Label(label.gameObject, name, 32, TextColor, TextAnchor.MiddleCenter);
            RectTransform d1 = Node("Desc1", tile);
            Place(d1, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -344f), new Vector2(-18f, 32f));
            Label(d1.gameObject, line1, 24, HintColor, TextAnchor.MiddleCenter);
            RectTransform d2 = Node("Desc2", tile);
            Place(d2, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -378f), new Vector2(-18f, 32f));
            Label(d2.gameObject, line2, 24, HintColor, TextAnchor.MiddleCenter);
            RectTransform price2 = Node("PriceValue", tile);
            Place(price2, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -416f), new Vector2(150f, 44f));
            card.priceText = Label(price2.gameObject, price.ToString(), 34, TextColor, TextAnchor.MiddleLeft);
            RectTransform coin = Node("Coin", price2);
            Place(coin, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(32f, 32f));
            Img(coin.gameObject, Spr("lobby_ember.png"), new Color(1f, 0.76f, 0.3f, 1f), false);
            RectTransform state = Node("State", tile);
            Place(state, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -416f), new Vector2(210f, 44f));
            card.stateText = Label(state.gameObject, "Buy", 32, new Color(1f, 0.85f, 0.45f, 1f), TextAnchor.MiddleRight);
            cards.Add(card);
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
        private static Sprite FindSprite(string file)
        {
            string[] guids = AssetDatabase.FindAssets(file + " t:Sprite");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileNameWithoutExtension(path) != file) continue;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
            }
            return null;
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
