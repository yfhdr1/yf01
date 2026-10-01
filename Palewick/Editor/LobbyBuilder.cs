using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class LobbyBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private const string PlayerPrefab = "Assets/Resources/WhiteclownPlayer.prefab";
        private const string RtPath = "Assets/UI_Lobby/LobbyStageRT.renderTexture";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color HintColor = new Color(0.75f, 0.68f, 0.66f, 0.8f);
        private static Font font;
        [MenuItem("Palewick/Build Horror Lobby")]
        public static void Build()
        {
            LobbyManager lobby = Object.FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
            if (lobby == null)
            {
                EditorUtility.DisplayDialog("Lobby", "Open Scene_Lobby first (LobbyManager not found).", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                EditorUtility.DisplayDialog("Lobby", "Folder Assets/UI_Lobby not found.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Lobby", "This deletes everything inside the lobby Canvas and builds the new horror lobby. Continue?", "Build", "Cancel"))
            {
                return;
            }
            PrepareImports();
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Canvas canvas = FindCanvas(lobby);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Lobby", "Canvas not found in the open scene.", "OK");
                return;
            }
            Undo.SetCurrentGroupName("Build Horror Lobby");
            int group = Undo.GetCurrentGroup();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(canvas.transform.GetChild(i).gameObject);
            }
            ServerBrowser browser = Object.FindAnyObjectByType<ServerBrowser>(FindObjectsInactive.Include);
            if (browser == null) browser = Undo.AddComponent<ServerBrowser>(lobby.gameObject);
            Undo.RecordObject(lobby, "Lobby");
            Undo.RecordObject(browser, "Browser");
            lobby.serverBrowser = browser;
            Transform root = canvas.transform;
            RectTransform bg = Stretch(Node("Background", root));
            Image bgImg = Img(bg.gameObject, Spr("lobby_bg.jpg"), Color.white, false);
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            bgImg.preserveAspect = false;
            RectTransform fogB = Node("FogBack", root);
            Place(fogB, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(0f, 440f));
            lobby.fogBack = Raw(fogB.gameObject, Tex("lobby_fog.png"), new Color(1f, 1f, 1f, 0.55f), new Rect(0f, 0f, 1.4f, 1f));
            RectTransform glow = Node("FloorGlow", root);
            Place(glow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(640f, 170f));
            Img(glow.gameObject, Spr("lobby_floor_glow.png"), new Color(1f, 1f, 1f, 0.85f), false);
            RectTransform view = Node("CharacterView", root);
            Place(view, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(720f, 960f));
            RawImage viewImg = view.gameObject.AddComponent<RawImage>();
            viewImg.raycastTarget = true;
            lobby.characterView = viewImg;
            RectTransform fogF = Node("FogFront", root);
            Place(fogF, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(0f, 340f));
            lobby.fogFront = Raw(fogF.gameObject, Tex("lobby_fog.png"), new Color(1f, 1f, 1f, 0.4f), new Rect(0.3f, 0f, 1.1f, 1f));
            RectTransform vig = Stretch(Node("Vignette", root));
            lobby.vignette = Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white, false);
            RectTransform emberArea = Stretch(Node("Embers", root));
            lobby.emberArea = emberArea;
            List<RectTransform> embers = new List<RectTransform>();
            Sprite emberSprite = Spr("lobby_ember.png");
            for (int i = 0; i < 22; i++)
            {
                RectTransform e = Node("Ember" + i, emberArea);
                Place(e, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 11) * 80f, (i % 5) * 120f - 300f), new Vector2(18f, 18f));
                Img(e.gameObject, emberSprite, Color.white, false);
                embers.Add(e);
            }
            lobby.embers = embers.ToArray();
            RectTransform blood = Node("BloodTop", root);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white, false);
            List<RectTransform> drips = new List<RectTransform>();
            Sprite dripSprite = Spr("lobby_drip.png");
            float[] dripX = { 0.07f, 0.19f, 0.33f, 0.46f, 0.61f, 0.74f, 0.86f, 0.95f };
            for (int i = 0; i < dripX.Length; i++)
            {
                RectTransform d = Node("Drip" + i, blood);
                Place(d, new Vector2(dripX[i], 1f), new Vector2(dripX[i], 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(22f, 90f + (i % 3) * 30f));
                Img(d.gameObject, dripSprite, Color.white, false);
                drips.Add(d);
            }
            lobby.drips = drips.ToArray();
            RectTransform title = Node("Title", root);
            Place(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(860f, 235f));
            Img(title.gameObject, Spr("lobby_title.png"), Color.white, false);
            RectTransform plate = Node("NamePlate", root);
            Place(plate, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -52f), new Vector2(460f, 106f));
            Image plateImg = Img(plate.gameObject, Spr("lobby_nameplate.png"), Color.white, true);
            Button plateBtn = plate.gameObject.AddComponent<Button>();
            plateBtn.targetGraphic = plateImg;
            UnityEventTools.AddPersistentListener(plateBtn.onClick, lobby.OpenNamePanel);
            RectTransform nameText = Node("PlayerName", plate);
            Place(nameText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            nameText.offsetMin = new Vector2(90f, 14f);
            nameText.offsetMax = new Vector2(-20f, -10f);
            lobby.nameLabel = Label(nameText.gameObject, "Player", 44, TextColor, TextAnchor.MiddleLeft);
            RectTransform avatar = Node("Avatar", root);
            Place(avatar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(150f, 150f));
            Image avatarImg = Img(avatar.gameObject, Spr("lobby_avatar.png"), Color.white, true);
            Button avatarBtn = avatar.gameObject.AddComponent<Button>();
            avatarBtn.targetGraphic = avatarImg;
            UnityEventTools.AddPersistentListener(avatarBtn.onClick, lobby.OpenNamePanel);
            RectTransform exit = Node("ExitBtn", root);
            Place(exit, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -36f), new Vector2(130f, 130f));
            RoundButton(exit.gameObject, Spr("lobby_exit.png"), lobby.AskExit);
            RectTransform sound = Node("SoundBtn", root);
            Place(sound, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -36f), new Vector2(130f, 130f));
            lobby.soundOnSprite = Spr("lobby_sound_on.png");
            lobby.soundOffSprite = Spr("lobby_sound_off.png");
            lobby.soundButtonImage = RoundButton(sound.gameObject, lobby.soundOnSprite, lobby.ToggleMute);
            RectTransform servers = Node("ServersBtn", root);
            Place(servers, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 70f), new Vector2(430f, 102f));
            PlateButton(servers.gameObject, "lobby_btn_side.png", "Servers", 50, lobby.OnStartPressed);
            RectTransform start = Node("StartButton", root);
            Place(start, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-320f, 150f), new Vector2(540f, 164f));
            PlateButton(start.gameObject, "lobby_btn_start.png", "START", 104, lobby.OnStartPressed);
            lobby.startButton = start;
            RectTransform flash = Stretch(Node("LightningFlash", root));
            lobby.flash = Img(flash.gameObject, null, new Color(0.8f, 0.87f, 1f, 0f), false);
            GameObject serverPanel = BuildServerPanel(root, browser, lobby);
            RectTransform badge = Node("ConnectionBadge", root);
            Place(badge, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-320f, 318f), new Vector2(540f, 56f));
            RectTransform dot = Node("Dot", badge);
            Place(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, 0f), new Vector2(34f, 34f));
            lobby.connectionDot = Img(dot.gameObject, Spr("lobby_ember.png"), new Color(0.95f, 0.22f, 0.18f, 1f), false);
            RectTransform conn = Node("ConnectionText", badge);
            Place(conn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(240f, 56f));
            lobby.connectionText = Label(conn.gameObject, "Offline", 44, new Color(0.95f, 0.22f, 0.18f, 1f), TextAnchor.MiddleCenter);
            RectTransform status = Node("StatusText", root);
            Place(status, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-320f, 262f), new Vector2(540f, 60f));
            TextMeshProUGUI statusTmp = Tmp(status.gameObject, "Connecting...", 34, HintColor, TextAlignmentOptions.Center);
            browser.statusText = statusTmp;
            BuildNamePanel(root, lobby);
            BuildExitPanel(root, lobby);
            GameObject loading = BuildLoading(root, lobby);
            browser.serverPanel = serverPanel;
            browser.loadingPanel = loading;
            lobby.loadingPanel = loading;
            PwLoginBuilder.Build(root, lobby);
            BuildAudio(lobby);
            BuildStage(lobby, viewImg);
            EditorUtility.SetDirty(lobby);
            EditorUtility.SetDirty(browser);
            if (scaler != null) EditorUtility.SetDirty(scaler);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(lobby.gameObject.scene);
            Selection.activeGameObject = canvas.gameObject;
            EditorUtility.DisplayDialog("Lobby", "Horror lobby built. Press Ctrl+S to save the scene.", "OK");
        }
        private static Canvas FindCanvas(LobbyManager lobby)
        {
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].isRootCanvas && all[i].gameObject.scene == lobby.gameObject.scene) return all[i];
            }
            return null;
        }
        private static void PrepareImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool fog = path.EndsWith("lobby_fog.png", StringComparison.OrdinalIgnoreCase);
                bool changed = false;
                TextureImporterType type = fog ? TextureImporterType.Default : TextureImporterType.Sprite;
                if (ti.textureType != type)
                {
                    ti.textureType = type;
                    changed = true;
                }
                if (!fog && ti.spriteImportMode != SpriteImportMode.Single)
                {
                    ti.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                TextureWrapMode wrap = fog ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (ti.wrapMode != wrap)
                {
                    ti.wrapMode = wrap;
                    changed = true;
                }
                if (ti.mipmapEnabled)
                {
                    ti.mipmapEnabled = false;
                    changed = true;
                }
                if (!ti.alphaIsTransparency && !path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    ti.alphaIsTransparency = true;
                    changed = true;
                }
                if (path.EndsWith("lobby_panel.png", StringComparison.OrdinalIgnoreCase) && ti.spriteBorder != new Vector4(40f, 40f, 40f, 40f))
                {
                    ti.spriteBorder = new Vector4(40f, 40f, 40f, 40f);
                    changed = true;
                }
                if (changed) ti.SaveAndReimport();
            }
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static Texture Tex(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Lobby");
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
        private static RawImage Raw(GameObject go, Texture tex, Color color, Rect uv)
        {
            RawImage raw = go.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = color;
            raw.uvRect = uv;
            raw.raycastTarget = false;
            return raw;
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
        private static TextMeshProUGUI Tmp(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
        private static Image RoundButton(GameObject go, Sprite sprite, UnityAction action)
        {
            Image img = Img(go, sprite, Color.white, true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.pressedColor = new Color(0.7f, 0.55f, 0.55f, 1f);
            b.colors = cb;
            UnityEventTools.AddPersistentListener(b.onClick, action);
            return img;
        }
        private static Button PlateButton(GameObject go, string sprite, string text, int size, UnityAction action)
        {
            Image img = Img(go, Spr(sprite), Color.white, true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.65f, 0.5f, 0.5f, 1f);
            b.colors = cb;
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            RectTransform lr = Node("Label", go.transform);
            Stretch(lr);
            lr.offsetMin = new Vector2(10f, 10f);
            lr.offsetMax = new Vector2(-10f, -4f);
            Label(lr.gameObject, text, size, TextColor, TextAnchor.MiddleCenter);
            return b;
        }
        private static RectTransform Modal(string name, Transform root, Vector2 size, Vector2 offset, float dim)
        {
            RectTransform shade = Stretch(Node(name, root));
            Img(shade.gameObject, null, new Color(0f, 0f, 0f, dim), true);
            RectTransform box = Node("Box", shade);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, size);
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            return box;
        }
        private static void Title(Transform box, string text)
        {
            RectTransform t = Node("Title", box);
            Place(t, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-40f, 90f));
            Label(t.gameObject, text, 64, new Color(0.85f, 0.1f, 0.08f, 1f), TextAnchor.MiddleCenter);
        }
        private static GameObject BuildServerPanel(Transform root, ServerBrowser browser, LobbyManager lobby)
        {
            RectTransform box = Modal("ServerPanel", root, new Vector2(1000f, 800f), new Vector2(-120f, -40f), 0.6f);
            GameObject panel = box.parent.gameObject;
            Title(box, "Servers");
            RectTransform close = Node("CloseButton_ServerPanel", box);
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(110f, 110f));
            RoundButton(close.gameObject, Spr("lobby_close.png"), browser.CloseServerPanel);
            RectTransform input = Node("ServerNameInput", box);
            Place(input, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -130f), new Vector2(-470f, 96f));
            Image inputBg = Img(input.gameObject, Spr("lobby_panel.png"), new Color(0.45f, 0.4f, 0.4f, 1f), true);
            inputBg.type = Image.Type.Sliced;
            RectTransform area = Stretch(Node("Text Area", input));
            area.offsetMin = new Vector2(24f, 8f);
            area.offsetMax = new Vector2(-24f, -8f);
            area.gameObject.AddComponent<RectMask2D>();
            RectTransform ph = Stretch(Node("Placeholder", area));
            TextMeshProUGUI phText = Tmp(ph.gameObject, "Enter room name...", 38, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.MidlineLeft);
            phText.fontStyle = FontStyles.Italic;
            RectTransform tx = Stretch(Node("Text", area));
            TextMeshProUGUI txText = Tmp(tx.gameObject, "", 38, Color.white, TextAlignmentOptions.MidlineLeft);
            RectTransform pv = Stretch(Node("ServerNamePreview", area));
            lobby.serverNamePreview = Tmp(pv.gameObject, "", 38, Color.white, TextAlignmentOptions.MidlineRight);
            pv.gameObject.SetActive(false);
            TMP_InputField field = input.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = txText;
            field.placeholder = phText;
            field.characterLimit = 20;
            field.targetGraphic = inputBg;
            browser.serverNameInput = field;
            RectTransform create = Node("CreateServerButton", box);
            Place(create, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -128f), new Vector2(400f, 100f));
            PlateButton(create.gameObject, "lobby_btn_start.png", "Create Server", 44, browser.CreateServer);
            RectTransform list = Node("ServerList", box);
            Place(list, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            list.offsetMin = new Vector2(40f, 160f);
            list.offsetMax = new Vector2(-40f, -260f);
            Img(list.gameObject, null, new Color(0f, 0f, 0f, 0.45f), true);
            ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = Stretch(Node("Viewport", list));
            viewport.offsetMin = new Vector2(12f, 12f);
            viewport.offsetMax = new Vector2(-12f, -12f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Node("Content", viewport);
            Place(content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 0f));
            VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            ContentSizeFitter csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            RectTransform solo = Node("SinglePlayerBtn", box);
            Place(solo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(460f, 100f));
            PlateButton(solo.gameObject, "lobby_btn_side.png", "Single Player", 46, lobby.OnSinglePlayerPressed);
            RectTransform item = Node("ServerItem", content);
            Image itemImg = Img(item.gameObject, Spr("lobby_btn_side.png"), Color.white, true);
            Button itemBtn = item.gameObject.AddComponent<Button>();
            itemBtn.targetGraphic = itemImg;
            LayoutElement le = item.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 100f;
            RectTransform itemText = Stretch(Node("Text (TMP)", item));
            itemText.offsetMin = new Vector2(30f, 6f);
            itemText.offsetMax = new Vector2(-30f, -6f);
            Tmp(itemText.gameObject, "Server   1/4", 40, TextColor, TextAlignmentOptions.MidlineLeft);
            item.gameObject.SetActive(false);
            browser.serverListContent = content;
            browser.serverItemPrefab = item.gameObject;
            panel.SetActive(false);
            return panel;
        }
        private static void BuildNamePanel(Transform root, LobbyManager lobby)
        {
            RectTransform box = Modal("NamePanel", root, new Vector2(900f, 520f), Vector2.zero, 0.85f);
            GameObject panel = box.parent.gameObject;
            Title(box, "Enter your name");
            RectTransform close = Node("CloseButton", box);
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(100f, 100f));
            RoundButton(close.gameObject, Spr("lobby_close.png"), lobby.CloseNamePanel);
            lobby.nameCloseButton = close.gameObject;
            RectTransform input = Node("NameInput", box);
            Place(input, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(680f, 104f));
            Image inputBg = Img(input.gameObject, Spr("lobby_panel.png"), new Color(0.45f, 0.4f, 0.4f, 1f), true);
            inputBg.type = Image.Type.Sliced;
            lobby.nameInputBackground = inputBg;
            Font plain = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform tx = Stretch(Node("Text", input));
            tx.offsetMin = new Vector2(20f, 6f);
            tx.offsetMax = new Vector2(-20f, -6f);
            Text txt = tx.gameObject.AddComponent<Text>();
            txt.font = plain;
            txt.fontSize = 44;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = false;
            txt.raycastTarget = false;
            RectTransform ph = Stretch(Node("Placeholder", input));
            ph.offsetMin = new Vector2(20f, 6f);
            ph.offsetMax = new Vector2(-20f, -6f);
            Text phTxt = ph.gameObject.AddComponent<Text>();
            phTxt.font = plain;
            phTxt.fontSize = 40;
            phTxt.fontStyle = FontStyle.Italic;
            phTxt.color = new Color(1f, 1f, 1f, 0.35f);
            phTxt.alignment = TextAnchor.MiddleCenter;
            phTxt.text = "Enter name...";
            phTxt.raycastTarget = false;
            InputField field = input.gameObject.AddComponent<InputField>();
            field.textComponent = txt;
            field.placeholder = phTxt;
            field.characterLimit = 20;
            RectTransform npv = Stretch(Node("NamePreview", input));
            npv.offsetMin = new Vector2(20f, 6f);
            npv.offsetMax = new Vector2(-20f, -6f);
            Text npvText = npv.gameObject.AddComponent<Text>();
            Font rtl = Resources.Load<Font>("Fonts/UniMahanBilal");
            npvText.font = rtl != null ? rtl : plain;
            npvText.fontSize = 44;
            npvText.color = Color.white;
            npvText.alignment = TextAnchor.MiddleCenter;
            npvText.raycastTarget = false;
            npv.gameObject.SetActive(false);
            lobby.namePreview = npvText;
            field.targetGraphic = inputBg;
            lobby.nameInput = field;
            RectTransform ok = Node("ConfirmBtn", box);
            Place(ok, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 110f));
            PlateButton(ok.gameObject, "lobby_btn_start.png", "OK", 70, lobby.OnNameConfirmed);
            lobby.namePanel = panel;
            panel.SetActive(false);
        }
        private static void BuildExitPanel(Transform root, LobbyManager lobby)
        {
            RectTransform box = Modal("ExitPanel", root, new Vector2(760f, 400f), Vector2.zero, 0.8f);
            GameObject panel = box.parent.gameObject;
            Title(box, "Exit");
            RectTransform yes = Node("YesBtn", box);
            Place(yes, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 60f), new Vector2(300f, 110f));
            PlateButton(yes.gameObject, "lobby_btn_start.png", "Yes", 64, lobby.ConfirmExit);
            RectTransform no = Node("NoBtn", box);
            Place(no, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(170f, 60f), new Vector2(300f, 110f));
            PlateButton(no.gameObject, "lobby_btn_side.png", "No", 64, lobby.CancelExit);
            lobby.exitPanel = panel;
            panel.SetActive(false);
        }
        private static GameObject BuildLoading(Transform root, LobbyManager lobby)
        {
            RectTransform panel = Stretch(Node("LoadingPanel", root));
            Img(panel.gameObject, null, Color.black, true);
            RectTransform bg = Stretch(Node("Art", panel));
            Img(bg.gameObject, Spr("lobby_bg.jpg"), new Color(0.35f, 0.3f, 0.3f, 1f), false);
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            RectTransform vig = Stretch(Node("Vignette", panel));
            Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white, false);
            RectTransform title = Node("Title", panel);
            Place(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(860f, 235f));
            Img(title.gameObject, Spr("lobby_title.png"), Color.white, false);
            RectTransform spin = Node("Spinner", panel);
            Place(spin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(170f, 170f));
            Img(spin.gameObject, Spr("lobby_spinner.png"), Color.white, false);
            lobby.spinner = spin;
            RectTransform txt = Node("LoadingText", panel);
            Place(txt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(800f, 90f));
            Label(txt.gameObject, "Loading...", 60, TextColor, TextAnchor.MiddleCenter);
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }
        private static void BuildAudio(LobbyManager lobby)
        {
            lobby.musicSource = AudioChild(lobby.transform, "LobbyMusic", "lobby_music.wav", true, 0.6f);
            lobby.thunderSource = AudioChild(lobby.transform, "LobbyThunder", "lobby_thunder.wav", false, 0.9f);
        }
        private static AudioSource AudioChild(Transform parent, string name, string clip, bool loop, float volume)
        {
            Transform old = parent.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Lobby");
            go.transform.SetParent(parent, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ArtFolder + "/" + clip);
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = volume;
            src.spatialBlend = 0f;
            return src;
        }
        private static void BuildStage(LobbyManager lobby, RawImage view)
        {
            GameObject old = GameObject.Find("LobbyStage");
            if (old != null) Undo.DestroyObjectImmediate(old);
            GameObject stage = new GameObject("LobbyStage");
            Undo.RegisterCreatedObjectUndo(stage, "Lobby");
            stage.transform.position = new Vector3(0f, -1000f, 0f);
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RtPath);
            if (rt == null)
            {
                rt = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 2;
                rt.name = "LobbyStageRT";
                AssetDatabase.CreateAsset(rt, RtPath);
            }
            view.texture = rt;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            Bounds bounds = new Bounds(stage.transform.position + Vector3.up, new Vector3(0.6f, 1.8f, 0.6f));
            if (prefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(model, "Lobby");
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "LobbyCharacter";
                model.transform.SetParent(stage.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, -12f, 0f);
                model.transform.localScale = prefab.transform.localScale;
                StripToVisual(model);
                Renderer[] rs = model.GetComponentsInChildren<Renderer>(true);
                bool has = false;
                for (int i = 0; i < rs.Length; i++)
                {
                    if (!rs[i].enabled || !rs[i].gameObject.activeInHierarchy) continue;
                    if (rs[i] is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                    if (!has)
                    {
                        bounds = rs[i].bounds;
                        has = true;
                    }
                    else bounds.Encapsulate(rs[i].bounds);
                }
                lobby.stageModel = model.transform;
            }
            float h = Mathf.Max(0.5f, bounds.size.y);
            float fov = 26f;
            float dist = h * 0.62f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 target = bounds.center + Vector3.up * h * 0.02f;
            GameObject camGo = new GameObject("StageCamera");
            Undo.RegisterCreatedObjectUndo(camGo, "Lobby");
            camGo.transform.SetParent(stage.transform, false);
            camGo.transform.position = target + new Vector3(0f, h * 0.06f, dist);
            camGo.transform.LookAt(target);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = dist + h * 3f;
            cam.cullingMask = ~(1 << 5);
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.depth = -5f;
            cam.targetTexture = rt;
            lobby.rimLight = AddLight(stage.transform, "RimLight", LightType.Spot, new Color(1f, 0.12f, 0.08f), 6f, target + new Vector3(1.6f, h * 0.9f, -2.2f), target, 12f, 55f);
            AddLight(stage.transform, "KeyLight", LightType.Spot, new Color(1f, 0.86f, 0.76f), 2.6f, target + new Vector3(-1.8f, h * 0.8f, 2.4f), target, 14f, 50f);
            AddLight(stage.transform, "FillLight", LightType.Point, new Color(0.3f, 0.42f, 0.75f), 1.4f, target + new Vector3(1.8f, 0f, 2f), target, 8f, 0f);
            lobby.lightningLight = AddLight(stage.transform, "LightningLight", LightType.Spot, new Color(0.75f, 0.85f, 1f), 0f, target + new Vector3(-0.5f, h * 1.6f, 1.5f), target, 14f, 70f);
        }
        private static Light AddLight(Transform parent, string name, LightType type, Color color, float intensity, Vector3 pos, Vector3 target, float range, float angle)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Lobby");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.LookAt(target);
            Light l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            if (type == LightType.Spot) l.spotAngle = angle;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }
        private static bool Keep(Component c)
        {
            return c is Transform || c is Animator || c is Renderer || c is MeshFilter;
        }
        private static void StripToVisual(GameObject model)
        {
            Transform[] all = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(all[i].gameObject);
            }
            for (int pass = 0; pass < 8; pass++)
            {
                bool removed = false;
                Component[] comps = model.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    Component c = comps[i];
                    if (c == null || Keep(c) || !CanRemove(c)) continue;
                    Object.DestroyImmediate(c);
                    removed = true;
                }
                if (!removed) break;
            }
            Animator[] anims = model.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].applyRootMotion = false;
                anims[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }
        private static bool CanRemove(Component c)
        {
            Component[] others = c.GetComponents<Component>();
            Type t = c.GetType();
            for (int i = 0; i < others.Length; i++)
            {
                Component o = others[i];
                if (o == null || o == c) continue;
                object[] reqs = o.GetType().GetCustomAttributes(typeof(RequireComponent), true);
                for (int r = 0; r < reqs.Length; r++)
                {
                    RequireComponent rc = (RequireComponent)reqs[r];
                    if (Needs(rc.m_Type0, t) || Needs(rc.m_Type1, t) || Needs(rc.m_Type2, t)) return false;
                }
            }
            return true;
        }
        private static bool Needs(Type required, Type t)
        {
            return required != null && required.IsAssignableFrom(t);
        }
    }
}
