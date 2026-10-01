using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class PwLoginBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color BloodColor = new Color(0.85f, 0.1f, 0.08f, 1f);
        private static readonly Color HintColor = new Color(0.75f, 0.68f, 0.66f, 0.85f);
        private static Font font;
        [MenuItem("Palewick/Build Login Screen")]
        public static void BuildMenu()
        {
            LobbyManager lobby = Object.FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
            if (lobby == null)
            {
                EditorUtility.DisplayDialog("Login", "Open Scene_Lobby first (LobbyManager not found).", "OK");
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
                EditorUtility.DisplayDialog("Login", "Canvas not found in the open scene.", "OK");
                return;
            }
            Undo.SetCurrentGroupName("Build Login Screen");
            int group = Undo.GetCurrentGroup();
            Build(canvas.transform, lobby);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(lobby.gameObject.scene);
            EditorUtility.DisplayDialog("Login", "Login screen and points badge built. Press Ctrl+S to save the scene.", "OK");
        }
        public static void Build(Transform root, LobbyManager lobby)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Kill(root, "LoginPanel");
            Kill(root, "PointsBadge");
            Kill(root, "AccountBar");
            Kill(root, "StartModePanel");
            Kill(root, "LanguagePanel");
            PwAuthUI auth = lobby.GetComponent<PwAuthUI>();
            if (auth == null) auth = Undo.AddComponent<PwAuthUI>(lobby.gameObject);
            Undo.RecordObject(auth, "Login");
            PwStartMode mode = lobby.GetComponent<PwStartMode>();
            if (mode == null) mode = Undo.AddComponent<PwStartMode>(lobby.gameObject);
            Undo.RecordObject(mode, "Login");
            mode.lobby = lobby;
            PwLanguageUI langUI = lobby.GetComponent<PwLanguageUI>();
            if (langUI == null) langUI = Undo.AddComponent<PwLanguageUI>(lobby.gameObject);
            Undo.RecordObject(langUI, "Login");
            BuildBadge(root);
            BuildAccountBar(root, auth);
            Transform loading = root.Find("LoadingPanel");
            if (loading != null) loading.SetAsLastSibling();
            BuildStartMode(root, mode);
            BuildPanel(root, auth);
            BuildLanguage(root, langUI);
            BindStart(root, mode);
            EditorUtility.SetDirty(auth);
            EditorUtility.SetDirty(mode);
            EditorUtility.SetDirty(langUI);
        }
        private static void Kill(Transform root, string name)
        {
            Transform old = root.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }
        private static void BuildBadge(Transform root)
        {
            RectTransform badge = Node("PointsBadge", root);
            Place(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -186f), new Vector2(420f, 96f));
            Img(badge.gameObject, Spr("lobby_nameplate.png"), Color.white, false);
            PwPointsHud hud = badge.gameObject.AddComponent<PwPointsHud>();
            RectTransform icon = Node("Icon", badge);
            Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(60f, 60f));
            Img(icon.gameObject, Spr("lobby_ember.png"), new Color(1f, 0.76f, 0.3f, 1f), false);
            hud.icon = icon;
            RectTransform value = Node("PointsValue", badge);
            Place(value, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(140f, 74f));
            hud.valueText = Label(value.gameObject, "0", 56, TextColor, TextAnchor.MiddleLeft);
            RectTransform label = Node("PointsLabel", badge);
            Place(label, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(200f, 60f));
            Label(label.gameObject, "Points", 38, HintColor, TextAnchor.MiddleRight);
            RectTransform dot = Node("SyncDot", badge);
            Place(dot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(30f, -18f), new Vector2(22f, 22f));
            hud.syncDot = Img(dot.gameObject, Spr("lobby_ember.png"), Color.white, false);
            RectTransform gain = Node("PointsGain", badge);
            Place(gain, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(200f, 70f));
            hud.gainText = Label(gain.gameObject, "+1", 48, new Color(1f, 0.85f, 0.35f, 1f), TextAnchor.MiddleCenter);
            gain.gameObject.SetActive(false);
            EditorUtility.SetDirty(hud);
        }
        private static void BuildAccountBar(Transform root, PwAuthUI auth)
        {
            RectTransform bar = Node("AccountBar", root);
            Place(bar, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -292f), new Vector2(420f, 140f));
            RectTransform name = Node("AccountName", bar);
            Place(name, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(420f, 52f));
            Text nameText = Label(name.gameObject, "", 32, HintColor, TextAnchor.MiddleRight);
            nameText.font = Plain();
            auth.accountText = nameText;
            RectTransform outBtn = Node("SignOutBtn", bar);
            Place(outBtn, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(250f, 78f));
            auth.signOutButton = PlateButton(outBtn.gameObject, "lobby_btn_side.png", "Sign Out", 38, auth.SignOut);
        }
        private static void BindStart(Transform root, PwStartMode mode)
        {
            Transform start = root.Find("StartButton");
            if (start == null) return;
            Button button = start.GetComponent<Button>();
            if (button == null) return;
            Undo.RecordObject(button, "Login");
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }
            UnityEventTools.AddPersistentListener(button.onClick, mode.Open);
            EditorUtility.SetDirty(button);
        }
        private static void BuildStartMode(Transform root, PwStartMode mode)
        {
            RectTransform shade = Stretch(Node("StartModePanel", root));
            Img(shade.gameObject, null, new Color(0f, 0f, 0f, 0.82f), true);
            mode.panel = shade.gameObject;
            RectTransform box = Node("Box", shade);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 560f));
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-60f, 96f));
            Label(title.gameObject, "How do you want to play?", 56, BloodColor, TextAnchor.MiddleCenter);
            RectTransform online = Node("PlayOnlineBtn", box);
            Place(online, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(760f, 130f));
            mode.onlineButton = PlateButton(online.gameObject, "lobby_btn_start.png", "Play Online", 56, mode.PlayOnline);
            RectTransform offline = Node("PlayOfflineBtn", box);
            Place(offline, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(760f, 130f));
            mode.offlineButton = PlateButton(offline.gameObject, "lobby_btn_side.png", "Play Offline", 56, mode.PlayOffline);
            RectTransform note = Node("OnlineNote", box);
            Place(note, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(900f, 52f));
            mode.onlineNote = Label(note.gameObject, "No internet connection", 34, new Color(1f, 0.5f, 0.35f, 1f), TextAnchor.MiddleCenter);
            note.gameObject.SetActive(false);
            RectTransform close = Node("CloseButton", box);
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(100f, 100f));
            mode.closeButton = RoundButton(close.gameObject, Spr("lobby_close.png"), mode.Close);
            shade.gameObject.SetActive(false);
        }
        private static void BuildLanguage(Transform root, PwLanguageUI langUI)
        {
            RectTransform shade = Stretch(Node("LanguagePanel", root));
            Img(shade.gameObject, null, new Color(0f, 0f, 0f, 0.96f), true);
            shade.SetAsLastSibling();
            langUI.panel = shade.gameObject;
            RectTransform art = Stretch(Node("Art", shade));
            Image artImg = Img(art.gameObject, Spr("lobby_bg.jpg"), new Color(0.3f, 0.26f, 0.26f, 1f), false);
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            artImg.preserveAspect = false;
            RectTransform vig = Stretch(Node("Vignette", shade));
            Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white, false);
            RectTransform blood = Node("BloodTop", shade);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white, false);
            RectTransform box = Node("Box", shade);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f));
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(-60f, 110f));
            Label(title.gameObject, "LANGUAGE", 72, BloodColor, TextAnchor.MiddleCenter);
            RectTransform kurdish = Node("KurdishBtn", box);
            Place(kurdish, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(700f, 140f));
            langUI.kurdishButton = PlateButton(kurdish.gameObject, "lobby_btn_start.png", "Kurdish", 60, langUI.PickKurdish);
            RectTransform arabic = Node("ArabicBtn", box);
            Place(arabic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(700f, 140f));
            langUI.arabicButton = PlateButton(arabic.gameObject, "lobby_btn_side.png", "Arabic", 60, langUI.PickArabic);
            RectTransform english = Node("EnglishBtn", box);
            Place(english, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(700f, 140f));
            langUI.englishButton = PlateButton(english.gameObject, "lobby_btn_side.png", "English", 60, langUI.PickEnglish);
        }
        private static void BuildPanel(Transform root, PwAuthUI auth)
        {
            RectTransform shade = Stretch(Node("LoginPanel", root));
            Img(shade.gameObject, null, new Color(0f, 0f, 0f, 0.94f), true);
            shade.SetAsLastSibling();
            auth.panel = shade.gameObject;
            RectTransform art = Stretch(Node("Art", shade));
            Image artImg = Img(art.gameObject, Spr("lobby_bg.jpg"), new Color(0.32f, 0.28f, 0.28f, 1f), false);
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            artImg.preserveAspect = false;
            RectTransform vig = Stretch(Node("Vignette", shade));
            Img(vig.gameObject, Spr("lobby_vignette.png"), Color.white, false);
            RectTransform blood = Node("BloodTop", shade);
            Place(blood, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f));
            Img(blood.gameObject, Spr("lobby_blood_top.png"), Color.white, false);
            RectTransform box = Node("Box", shade);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 900f));
            Image boxImg = Img(box.gameObject, Spr("lobby_panel.png"), Color.white, true);
            boxImg.type = Image.Type.Sliced;
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-40f, 96f));
            Label(title.gameObject, "Login", 70, BloodColor, TextAnchor.MiddleCenter);
            RectTransform mailLabel = Node("EmailLabel", box);
            Place(mailLabel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -140f), new Vector2(400f, 50f));
            Label(mailLabel.gameObject, "Email", 40, HintColor, TextAnchor.MiddleLeft);
            auth.emailInput = Field(box, "EmailInput", new Vector2(0f, -196f), "Enter email...", false);
            RectTransform passLabel = Node("PasswordLabel", box);
            Place(passLabel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -312f), new Vector2(400f, 50f));
            Label(passLabel.gameObject, "Password", 40, HintColor, TextAnchor.MiddleLeft);
            auth.passwordInput = Field(box, "PasswordInput", new Vector2(0f, -368f), "Enter password...", true);
            RectTransform signIn = Node("SignInBtn", box);
            Place(signIn, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-230f, -490f), new Vector2(420f, 110f));
            auth.signInButton = PlateButton(signIn.gameObject, "lobby_btn_start.png", "Sign In", 52, auth.SignIn);
            RectTransform signUp = Node("SignUpBtn", box);
            Place(signUp, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230f, -490f), new Vector2(420f, 110f));
            auth.signUpButton = PlateButton(signUp.gameObject, "lobby_btn_side.png", "Sign Up", 52, auth.SignUp);
            RectTransform google = Node("GoogleBtn", box);
            Place(google, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -616f), new Vector2(880f, 112f));
            auth.googleButton = PlateButton(google.gameObject, "lobby_btn_side.png", "Sign in with Google", 48, auth.SignInGoogle);
            RectTransform offline = Node("OfflineBtn", box);
            Place(offline, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -740f), new Vector2(560f, 96f));
            auth.offlineButton = PlateButton(offline.gameObject, "lobby_btn_side.png", "Play Offline", 42, auth.PlayOffline);
            offline.gameObject.SetActive(false);
            RectTransform status = Node("LoginStatus", box);
            Place(status, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(940f, 60f));
            auth.statusText = Label(status.gameObject, "", 38, new Color(1f, 0.5f, 0.35f, 1f), TextAnchor.MiddleCenter);
            RectTransform spin = Node("LoginSpinner", box);
            Place(spin, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 70f), new Vector2(72f, 72f));
            Img(spin.gameObject, Spr("lobby_spinner.png"), Color.white, false);
            auth.spinner = spin;
            spin.gameObject.SetActive(false);
        }
        private static InputField Field(Transform box, string name, Vector2 pos, string hint, bool password)
        {
            RectTransform input = Node(name, box);
            Place(input, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(880f, 104f));
            Image bg = Img(input.gameObject, Spr("lobby_panel.png"), new Color(0.42f, 0.37f, 0.37f, 1f), true);
            bg.type = Image.Type.Sliced;
            Font plain = Plain();
            RectTransform tx = Stretch(Node("Text", input));
            tx.offsetMin = new Vector2(24f, 6f);
            tx.offsetMax = new Vector2(-24f, -6f);
            Text txt = tx.gameObject.AddComponent<Text>();
            txt.font = plain;
            txt.fontSize = 42;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.supportRichText = false;
            txt.raycastTarget = false;
            RectTransform ph = Stretch(Node("Placeholder", input));
            ph.offsetMin = new Vector2(24f, 6f);
            ph.offsetMax = new Vector2(-24f, -6f);
            Text phTxt = ph.gameObject.AddComponent<Text>();
            phTxt.font = plain;
            phTxt.fontSize = 38;
            phTxt.fontStyle = FontStyle.Italic;
            phTxt.color = new Color(1f, 1f, 1f, 0.35f);
            phTxt.alignment = TextAnchor.MiddleLeft;
            phTxt.text = hint;
            phTxt.raycastTarget = false;
            InputField field = input.gameObject.AddComponent<InputField>();
            field.textComponent = txt;
            field.placeholder = phTxt;
            field.targetGraphic = bg;
            field.characterLimit = password ? 30 : 64;
            field.contentType = password ? InputField.ContentType.Password : InputField.ContentType.EmailAddress;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }
        private static Font Plain()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Login");
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
            t.font = font != null ? font : Plain();
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
            RectTransform lr = Stretch(Node("Label", go.transform));
            lr.offsetMin = new Vector2(12f, 10f);
            lr.offsetMax = new Vector2(-12f, -4f);
            Label(lr.gameObject, text, size, TextColor, TextAnchor.MiddleCenter);
            return b;
        }
    }
}
