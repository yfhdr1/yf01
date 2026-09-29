using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using Photon.Pun;
public class PauseMenuPUBG : MonoBehaviour
{
    private const string AudioKey = "pw_audio";
    private const string SensKey = "TouchSensitivity";
    private const string BrightKey = "pw_brightness2";
    private const string GraphicsKey = "pw_graphics";
    private const string FpsKey = "pw_fps";
    private const string ResKey = "pw_res";
    private const string AaKey = "pw_aa";
    private const string ShadowKey = "pw_shadows";
    private const string StyleKey = "pw_style";
    private const string BlindKey = "pw_cb";
    private const string FpsCounterKey = "pw_fpscounter";
    private const string PingKey = "pw_pingcounter";
    private const string TransUiKey = "pw_transui";
    private const string CamMotionKey = "pw_cammotion";
    private const string SwayKey = "pw_sway";
    private const string MenuSoundKey = "pw_menusound";
    private const string LayoutKey = "pw_layout";
    private const string LangKey = "pw_lang";
    private const string Vol3dKey = "pw_3dsound";
    private const string VibDamageKey = "pw_vibdamage";
    private const string VibMonsterKey = "pw_vibmonster";
    private const string GyroKey = "pw_gyro";
    private const string GyroSensKey = "pw_gyrosens";
    private const string GyroInvKey = "pw_gyroinv";
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.18f;
    private const float RowHeight = 84f;
    private const float SegHeight = 56f;
    private const float SensDefault = 0.15f;
    private static readonly string[] LevelKeys = { "low", "medium", "high", "ultra", "ultimate" };
    private static readonly string[] LevelNames = { "SMOOTH", "BALANCED", "HD", "ULTRA", "ULTIMATE" };
    private static readonly string[] VolKeys = { "pw_vol_steps", "pw_vol_monsters", "pw_vol_effects", "pw_vol_others" };
    private static readonly float[,] StyleValues = { { 0f, 0f, 0f }, { 25f, 8f, 0f }, { -10f, 12f, -5f }, { -5f, -15f, 6f }, { -25f, 20f, -10f } };
    private static readonly float[,] BlindMatrix = { { 100f, 0f, 0f, 0f, 100f, 0f, 0f, 0f, 100f }, { 80f, 20f, 0f, 0f, 70f, 30f, 0f, 20f, 80f }, { 60f, 40f, 0f, 20f, 80f, 0f, 0f, 20f, 80f }, { 95f, 5f, 0f, 0f, 85f, 15f, 0f, 45f, 55f } };
    private static readonly int[] FpsOptions = { 30, 45, 60, 90, 120, 144, 165, 185 };
    private static readonly int[] ResSteps = { 720, 1080, 1440 };
    private static readonly float[] SensPresets = { 0.08f, 0.15f, 0.3f };
    private static readonly string[] StyleTitles = { "Classic", "Colorful", "Realistic", "Soft", "Movie" };
    private static readonly string[] BlindTitles = { "Normal", "Deuteranopia", "Protanopia", "Tritanopia" };
    private static int nativeLong;
    private static int nativeShort;
    private static int currentSlot = 1;
    private static int lastTab = 2;
    private static int lang;
    [SerializeField] private Font rtlFont;
    private PostProcessLayer ppLayer;
    private PostProcessVolume ppVolume;
    private int appliedFxSlot = -1;
    private int appliedStyle = -1;
    private int appliedBlind = -1;
    private bool gradingCaptured;
    private float baseSat;
    private float baseContrast;
    private float baseTemp;
    private readonly float[] baseMixer = new float[9];
    private float fxTimer;
    private float infoTimer;
    private float audioTimer;
    private float flickerTimer = 3f;
    private float monsterPulse;
    private int lastHealth = -1;
    private PlayerHealth localHealth;
    private GameObject pausePanel;
    private CanvasGroup panelGroup;
    private RectTransform boxRect;
    private RectTransform root;
    private RectTransform popup;
    private Coroutine animRoutine;
    private Button pauseButton;
    private readonly List<PageInfo> pageInfos = new List<PageInfo>();
    private readonly List<Image> tabFills = new List<Image>();
    private readonly List<Image> tabBars = new List<Image>();
    private readonly List<Text> tabTexts = new List<Text>();
    private readonly List<System.Action> refreshers = new List<System.Action>();
    private readonly List<System.Action> liveRefreshers = new List<System.Action>();
    private readonly List<int> resValues = new List<int>();
    private readonly Dictionary<AudioSource, Vector4> audioBase = new Dictionary<AudioSource, Vector4>();
    private readonly Dictionary<RectTransform, Vector4[]> hudOriginal = new Dictionary<RectTransform, Vector4[]>();
    private Text titleText;
    private Text hudCounter;
    private Font uiFont;
    private Sprite grungeSprite;
    private Sprite vignetteSprite;
    private int currentTab;
    private int frameCount;
    private float frameTime;
    private int measuredFps;
    private float sensValue;
    private float volValue;
    private float brightValue;
    private Image brightnessOverlay;
    private Color baseAmbient;
    private readonly Color blood = new Color(0.78f, 0.06f, 0.08f, 1f);
    private readonly Color bloodBright = new Color(1f, 0.16f, 0.16f, 1f);
    private readonly Color bone = new Color(0.9f, 0.87f, 0.82f, 1f);
    private readonly Color ash = new Color(0.55f, 0.53f, 0.52f, 1f);
    private readonly Color rowColor = new Color(0.1f, 0.095f, 0.1f, 0.93f);
    private readonly Color sectionColor = new Color(0.15f, 0.13f, 0.13f, 0.96f);
    private readonly Color segBorder = new Color(0.34f, 0.32f, 0.32f, 1f);
    private readonly Color segFill = new Color(0.07f, 0.065f, 0.07f, 1f);
    private readonly Color segFillOn = new Color(0.4f, 0.03f, 0.05f, 1f);
    private readonly Color segDisBorder = new Color(0.17f, 0.16f, 0.16f, 1f);
    private readonly Color segDisFill = new Color(0.05f, 0.048f, 0.05f, 1f);
    private readonly Color segDisText = new Color(0.27f, 0.26f, 0.26f, 1f);
    private readonly List<KeyValuePair<Button, UnityAction>> buttonBindings = new List<KeyValuePair<Button, UnityAction>>();
    private readonly List<KeyValuePair<Slider, UnityAction<float>>> sliderBindings = new List<KeyValuePair<Slider, UnityAction<float>>>();
    private readonly Dictionary<GameObject, bool> hudStates = new Dictionary<GameObject, bool>();
    private readonly List<Behaviour> lockedComponents = new List<Behaviour>();
    private bool isOpen;
    private bool leaving;
    private class Seg
    {
        public Button button;
        public Image fill;
        public Image line;
        public Text text;
    }
    private class PageInfo
    {
        public GameObject container;
        public GameObject strip;
        public GameObject bottom;
        public readonly List<GameObject> subs = new List<GameObject>();
        public readonly List<Image> subFills = new List<Image>();
        public readonly List<Image> subLines = new List<Image>();
        public readonly List<Text> subTexts = new List<Text>();
        public int currentSub;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedOnLaunch()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(AudioKey, 1f);
        ApplyGraphicsLevel(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]));
        ApplyDisplay(SavedRes(), SupportedFps(PlayerPrefs.GetInt(FpsKey, 60)));
    }
    private void Start()
    {
        lang = Mathf.Clamp(PlayerPrefs.GetInt(LangKey, 0), 0, 2);
        baseAmbient = RenderSettings.ambientLight;
        pausePanel = FindChild(transform, "PausePanel");
        if (pausePanel != null)
        {
            panelGroup = pausePanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = pausePanel.AddComponent<CanvasGroup>();
            }
            pausePanel.SetActive(false);
        }
        GameObject box = FindChild(transform, "MenuBox");
        if (box != null)
        {
            boxRect = box.GetComponent<RectTransform>();
        }
        else if (pausePanel != null)
        {
            boxRect = NewRect("MenuBox", pausePanel.transform);
        }
        GameObject overlay = FindChild(transform.parent, "BrightnessOverlay");
        if (overlay != null)
        {
            brightnessOverlay = overlay.GetComponent<Image>();
            if (brightnessOverlay != null)
            {
                brightnessOverlay.raycastTarget = false;
            }
        }
        sensValue = PlayerPrefs.GetFloat(SensKey, SensDefault);
        volValue = PlayerPrefs.GetFloat(AudioKey, 1f);
        brightValue = PlayerPrefs.GetFloat(BrightKey, 1f);
        AudioListener.volume = volValue;
        ApplyBrightness(brightValue);
        ApplyGraphicsLevel(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]));
        resValues.Clear();
        resValues.AddRange(ResOptions());
        ApplyDisplay(SavedRes(), SupportedFps(PlayerPrefs.GetInt(FpsKey, 60)));
        GameObject pb = FindChild(transform.parent, "PauseButton");
        pauseButton = pb != null ? pb.GetComponent<Button>() : null;
        BindButton(pauseButton, TogglePause);
        Text anyText = GetComponentInChildren<Text>(true);
        uiFont = anyText != null && anyText.font != null ? anyText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (rtlFont == null)
        {
            rtlFont = Resources.Load<Font>("Fonts/UniMahanBilal");
        }
        grungeSprite = MakeGrungeSprite();
        vignetteSprite = MakeVignetteSprite();
        BuildMenu();
        BuildHudCounter();
        ApplyControlLayout();
        ApplyTransparentUi();
        RefreshAll();
    }
    private void RebuildMenu()
    {
        if (root != null)
        {
            Destroy(root.gameObject);
            root = null;
        }
        popup = null;
        pageInfos.Clear();
        tabFills.Clear();
        tabBars.Clear();
        tabTexts.Clear();
        refreshers.Clear();
        liveRefreshers.Clear();
        for (int i = buttonBindings.Count - 1; i >= 0; i--)
        {
            if (buttonBindings[i].Key != pauseButton)
            {
                if (buttonBindings[i].Key != null)
                {
                    buttonBindings[i].Key.onClick.RemoveListener(buttonBindings[i].Value);
                }
                buttonBindings.RemoveAt(i);
            }
        }
        for (int i = 0; i < sliderBindings.Count; i++)
        {
            if (sliderBindings[i].Key != null)
            {
                sliderBindings[i].Key.onValueChanged.RemoveListener(sliderBindings[i].Value);
            }
        }
        sliderBindings.Clear();
        BuildMenu();
        SelectTab(currentTab);
    }
    private void BuildMenu()
    {
        if (boxRect == null)
        {
            return;
        }
        foreach (Transform child in boxRect)
        {
            child.gameObject.SetActive(false);
        }
        GameObject backdrop = FindChild(transform, "Backdrop");
        if (backdrop != null)
        {
            Image bi = backdrop.GetComponent<Image>();
            if (bi != null)
            {
                bi.color = new Color(0f, 0f, 0f, 0.85f);
            }
        }
        Stretch(boxRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image boxImg = boxRect.GetComponent<Image>();
        if (boxImg == null)
        {
            boxImg = boxRect.gameObject.AddComponent<Image>();
        }
        boxImg.sprite = grungeSprite;
        boxImg.type = Image.Type.Tiled;
        boxImg.color = Color.white;
        root = NewRect("PwRoot", boxRect);
        Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image vig = AddImage(NewRect("Vignette", root), Color.white);
        vig.sprite = vignetteSprite;
        Stretch(vig.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image side = AddImage(NewRect("SideBar", root), new Color(0.03f, 0.028f, 0.03f, 0.96f));
        Stretch(side.rectTransform, new Vector2(0.83f, 0f), Vector2.one, Vector2.zero, new Vector2(0f, -100f));
        Image sideEdge = AddImage(NewRect("SideEdge", side.rectTransform), new Color(0.25f, 0.05f, 0.06f, 1f));
        Stretch(sideEdge.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f));
        Image topLine = AddImage(NewRect("TopLine", root), new Color(0.22f, 0.2f, 0.2f, 1f));
        Stretch(topLine.rectTransform, new Vector2(0.02f, 1f), new Vector2(0.81f, 1f), new Vector2(0f, -94f), new Vector2(0f, -92f));
        titleText = MakeText(root, D("title"), 40, TextAnchor.MiddleRight, bone, true);
        Place(titleText.rectTransform, Vector2.one, Vector2.one, new Vector2(-112f, -8f), new Vector2(360f, 86f));
        Image closeImg = AddImage(NewRect("CloseX", root), new Color(1f, 1f, 1f, 0.001f));
        closeImg.raycastTarget = true;
        Place(closeImg.rectTransform, Vector2.one, Vector2.one, new Vector2(-16f, -8f), new Vector2(86f, 86f));
        Button close = closeImg.gameObject.AddComponent<Button>();
        close.transition = Selectable.Transition.None;
        Text x = MakeText(closeImg.rectTransform, "X", 58, TextAnchor.MiddleCenter, bone, false);
        Stretch(x.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BindButton(close, () => SetPaused(false));
        string[] tabKeys = { "tab_account", "tab_basic", "tab_graphics", "tab_controls", "tab_sens", "tab_audio", "tab_lang" };
        for (int i = 0; i < tabKeys.Length; i++)
        {
            BuildTab(i, tabKeys[i]);
        }
        BuildAccountPage();
        BuildBasicPage();
        BuildGraphicsPage();
        BuildControlsPage();
        BuildSensitivityPage();
        BuildAudioPage();
        BuildLanguagePage();
        if (lang > 0)
        {
            MirrorChildren(root);
        }
    }
    private void BuildTab(int i, string key)
    {
        RectTransform rt = NewRect("Tab" + i, root);
        float top = -104f - i * 90f;
        rt.anchorMin = new Vector2(0.83f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(2f, top - 86f);
        rt.offsetMax = new Vector2(0f, top);
        Image fill = AddImage(rt, new Color(0f, 0f, 0f, 0.001f));
        fill.raycastTarget = true;
        fill.sprite = grungeSprite;
        fill.type = Image.Type.Tiled;
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        Image bar = AddImage(NewRect("Bar", rt), bloodBright);
        Stretch(bar.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 0f));
        Image line = AddImage(NewRect("Line", rt), new Color(0.16f, 0.15f, 0.15f, 1f));
        Stretch(line.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(14f, 0f), new Vector2(-14f, 2f));
        Text t = MakeText(rt, D(key), 29, TextAnchor.MiddleCenter, ash, false);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
        int index = i;
        BindButton(b, () => SelectTab(index));
        tabFills.Add(fill);
        tabBars.Add(bar);
        tabTexts.Add(t);
    }
    private RectTransform[] NewPage(string name, string[] subKeys)
    {
        PageInfo info = new PageInfo();
        RectTransform container = NewRect("Page" + name, root);
        Stretch(container, new Vector2(0.02f, 0f), new Vector2(0.81f, 1f), new Vector2(0f, 104f), new Vector2(0f, -106f));
        info.container = container.gameObject;
        RectTransform strip = NewRect("Strip" + name, root);
        Stretch(strip, new Vector2(0.02f, 1f), new Vector2(0.81f, 1f), new Vector2(0f, -92f), new Vector2(0f, -14f));
        info.strip = strip.gameObject;
        RectTransform[] contents = new RectTransform[subKeys.Length];
        for (int s = 0; s < subKeys.Length; s++)
        {
            RectTransform tab = NewRect("Sub" + s, strip);
            Place(tab, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(s * 304f, 0f), new Vector2(300f, 74f));
            Image fill = AddImage(tab, new Color(0.2f, 0.18f, 0.18f, 1f));
            fill.raycastTarget = true;
            Image line = AddImage(NewRect("Line", tab), bloodBright);
            Stretch(line.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f));
            Text t = MakeText(tab, D(subKeys[s]), 28, TextAnchor.MiddleCenter, bone, true);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (s < subKeys.Length - 1)
            {
                Image sep = AddImage(NewRect("Sep", tab), new Color(0.3f, 0.28f, 0.28f, 1f));
                Place(sep.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(2f, 40f));
            }
            Button b = tab.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            int pageIndex = pageInfos.Count;
            int subIndex = s;
            BindButton(b, () => SelectSub(pageIndex, subIndex));
            info.subFills.Add(fill);
            info.subLines.Add(line);
            info.subTexts.Add(t);
            RectTransform page = NewRect("Scroll" + s, container);
            Stretch(page, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image hit = AddImage(page, new Color(0f, 0f, 0f, 0.001f));
            hit.raycastTarget = true;
            page.gameObject.AddComponent<RectMask2D>();
            RectTransform content = NewRect("Content", page);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.padding = new RectOffset(0, 0, 0, 20);
            ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect sr = page.gameObject.AddComponent<ScrollRect>();
            sr.content = content;
            sr.viewport = page;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            info.subs.Add(page.gameObject);
            contents[s] = content;
        }
        RectTransform bottom = NewRect("Bottom" + name, root);
        Stretch(bottom, new Vector2(0.02f, 0f), new Vector2(0.81f, 0f), new Vector2(0f, 16f), new Vector2(0f, 88f));
        info.bottom = bottom.gameObject;
        pageInfos.Add(info);
        return contents;
    }
    private RectTransform Bottom()
    {
        return pageInfos[pageInfos.Count - 1].bottom.GetComponent<RectTransform>();
    }
    private void BuildAccountPage()
    {
        RectTransform c = NewPage("Account", new[] { "sub_match" })[0];
        Section(c, "sec_player");
        InfoRow(c, "row_name", null, () => Disp(string.IsNullOrEmpty(PhotonNetwork.NickName) ? L("guest") : PhotonNetwork.NickName));
        Section(c, "sec_connection");
        InfoRow(c, "row_server", "hint_server", ServerText);
        InfoRow(c, "row_room", "hint_room", RoomText);
        BottomButton(Bottom(), "leave", "hint_leave", ExitToLobby);
    }
    private void BuildBasicPage()
    {
        RectTransform c = NewPage("Basic", new[] { "sub_basic" })[0];
        Section(c, "sec_hud");
        SegRow(c, "row_fpscounter", "hint_fpscounter", OffOn(), () => PlayerPrefs.GetInt(FpsCounterKey, 0), v => { SaveInt(FpsCounterKey, v); UpdateHudCounter(); }, null);
        SegRow(c, "row_transui", "hint_transui", OffOn(), () => PlayerPrefs.GetInt(TransUiKey, 0), v => { SaveInt(TransUiKey, v); ApplyTransparentUi(); }, null);
        Section(c, "sec_camera");
        SegRow(c, "row_cammotion", "hint_cammotion", OffOn(), () => PlayerPrefs.GetInt(CamMotionKey, 1), v => { SaveInt(CamMotionKey, v); ApplyCameraToggles(); }, null);
        SegRow(c, "row_sway", "hint_sway", OffOn(), () => PlayerPrefs.GetInt(SwayKey, 1), v => { SaveInt(SwayKey, v); ApplyCameraToggles(); }, null);
        BottomButton(Bottom(), "reset", "hint_reset_basic", ResetBasic);
    }
    private void BuildGraphicsPage()
    {
        RectTransform c = NewPage("Graphics", new[] { "sub_graphics" })[0];
        Section(c, "sec_gfxpref");
        SegRow(c, "row_graphics", "hint_graphics", Opts("lvl_", 5), () => LevelSlot(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1])), v => SetGraphics(LevelKeys[v]), null);
        string[] fpsNames = new string[FpsOptions.Length];
        for (int i = 0; i < FpsOptions.Length; i++)
        {
            fpsNames[i] = FpsOptions[i].ToString();
        }
        Text fpsLabel = SegRow(c, "row_fps", null, fpsNames, () => System.Array.IndexOf(FpsOptions, SupportedFps(PlayerPrefs.GetInt(FpsKey, 60))), v => SetFps(FpsOptions[v]), v => IsFpsSupported(FpsOptions[v]));
        liveRefreshers.Add(() => fpsLabel.text = LabelText(L("row_fps"), string.Format(L("fps_now"), measuredFps)));
        string[] resNames = new string[resValues.Count];
        for (int i = 0; i < resValues.Count; i++)
        {
            resNames[i] = resValues[i] + "P";
        }
        Text resLabel = SegRow(c, "row_res", null, resNames, () => resValues.IndexOf(SavedRes()), SetRes, null);
        liveRefreshers.Add(() => resLabel.text = LabelText(L("row_res"), string.Format(L("res_now"), Mathf.Min(Screen.width, Screen.height))));
        Section(c, "sec_adv");
        SegRow(c, "row_aa", "hint_aa", new[] { D("disable"), "2x", "4x" }, AaIndex, v => { SaveInt(AaKey, v == 0 ? 0 : (v == 1 ? 2 : 4)); ReapplyQuality(); }, null);
        SegRow(c, "row_shadows", "hint_shadows", new[] { D("disable"), D("enable") }, () => QualitySettings.shadows == ShadowQuality.Disable ? 0 : 1, v => { SaveInt(ShadowKey, v); ReapplyQuality(); }, null);
        Section(c, "sec_style");
        SegRow(c, "row_style", "hint_needs", Opts("style_", 5), () => PlayerPrefs.GetInt(StyleKey, 0), v => { SaveInt(StyleKey, v); appliedStyle = -1; UpdatePostFX(); }, v => currentSlot > 0);
        Section(c, "sec_param");
        SliderRow(c, "row_bright", 0.5f, 1.5f, 0.05f, () => brightValue, v => { brightValue = v; SaveFloat(BrightKey, v); ApplyBrightness(v); }, v => Mathf.RoundToInt(v * 100f) + "%");
        Section(c, "sec_blind");
        SegRow(c, "row_mode", "hint_needs", Opts("blind_", 4), () => PlayerPrefs.GetInt(BlindKey, 0), v => { SaveInt(BlindKey, v); appliedBlind = -1; UpdatePostFX(); }, v => currentSlot > 0);
        BottomButton(Bottom(), "reset_gfx", "hint_reset_gfx", ResetGraphics);
    }
    private void BuildControlsPage()
    {
        RectTransform c = NewPage("Controls", new[] { "sub_controls" })[0];
        Section(c, "sec_layout");
        RectTransform row = NewRect("LayoutRow", c);
        LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 470f;
        le.minHeight = 470f;
        AddImage(row, rowColor);
        for (int i = 0; i < 2; i++)
        {
            BuildLayoutCard(row, i);
        }
        BottomButton(Bottom(), "reset", "hint_reset_layout", () => { SaveInt(LayoutKey, 0); ApplyControlLayout(); RefreshAll(); });
    }
    private void BuildLayoutCard(RectTransform row, int index)
    {
        bool mirrored = index == 1;
        RectTransform card = NewRect("Card" + index, row);
        card.anchorMin = new Vector2(0.03f + index * 0.36f, 0f);
        card.anchorMax = new Vector2(0.33f + index * 0.36f, 1f);
        card.offsetMin = new Vector2(0f, 24f);
        card.offsetMax = new Vector2(0f, -24f);
        Image border = AddImage(card, segBorder);
        border.raycastTarget = true;
        Image fill = AddImage(NewRect("Fill", card), new Color(0.13f, 0.12f, 0.12f, 1f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Image num = AddImage(NewRect("Num", card), new Color(0.05f, 0.045f, 0.05f, 1f));
        Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(64f, 64f));
        Text numText = MakeText(num.rectTransform, (index + 1).ToString(), 40, TextAnchor.MiddleCenter, bone, true);
        Stretch(numText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text l1 = MakeText(card, D(mirrored ? "lay_b1" : "lay_a1"), 24, TextAnchor.MiddleLeft, bone, false);
        Stretch(l1.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(96f, -52f), new Vector2(-12f, -16f));
        Text l2 = MakeText(card, D(mirrored ? "lay_b2" : "lay_a2"), 24, TextAnchor.MiddleLeft, bone, false);
        Stretch(l2.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(96f, -86f), new Vector2(-12f, -52f));
        RectTransform prev = NewRect("Preview", card);
        Stretch(prev, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -104f));
        Image left = AddImage(NewRect("Left", prev), new Color(0.2f, 0.19f, 0.19f, 1f));
        Stretch(left.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-2f, 0f));
        Image right = AddImage(NewRect("Right", prev), new Color(0.16f, 0.15f, 0.15f, 1f));
        Stretch(right.rectTransform, new Vector2(0.5f, 0f), Vector2.one, new Vector2(2f, 0f), Vector2.zero);
        Image stick = AddImage(NewRect("Stick", prev), new Color(0.35f, 0.33f, 0.33f, 1f));
        Place(stick.rectTransform, new Vector2(mirrored ? 0.75f : 0.25f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
        Image knob = AddImage(NewRect("Knob", stick.rectTransform), bone);
        Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        for (int b = 0; b < 3; b++)
        {
            Image btn = AddImage(NewRect("Btn" + b, prev), new Color(0.45f, 0.06f, 0.08f, 1f));
            Place(btn.rectTransform, new Vector2(mirrored ? 0.12f + b * 0.1f : 0.68f + b * 0.1f, 0.25f + (b % 2) * 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
        }
        Text move = MakeText(prev, D("move"), 24, TextAnchor.MiddleCenter, ash, false);
        Stretch(move.rectTransform, new Vector2(mirrored ? 0.5f : 0f, 0.72f), new Vector2(mirrored ? 1f : 0.5f, 0.95f), Vector2.zero, Vector2.zero);
        Text look = MakeText(prev, D("look"), 24, TextAnchor.MiddleCenter, ash, false);
        Stretch(look.rectTransform, new Vector2(mirrored ? 0f : 0.5f, 0.72f), new Vector2(mirrored ? 0.5f : 1f, 0.95f), Vector2.zero, Vector2.zero);
        Button button = card.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        BindButton(button, () => { SaveInt(LayoutKey, index); ApplyControlLayout(); RefreshAll(); });
        refreshers.Add(() =>
        {
            bool on = PlayerPrefs.GetInt(LayoutKey, 0) == index;
            border.color = on ? bloodBright : segBorder;
            numText.color = on ? bloodBright : bone;
        });
    }
    private void BuildSensitivityPage()
    {
        RectTransform c = NewPage("Sensitivity", new[] { "sub_camera" })[0];
        Section(c, "sec_overall");
        SegRow(c, "row_preset", null, Opts("sens_", 4), SensPresetIndex, v => { if (v < 3) { SetSensitivity(SensPresets[v]); } }, null);
        Section(c, "sec_camsens");
        SliderRow(c, "row_freelook", 0.05f, 0.5f, 0.0075f, () => sensValue, SetSensitivity, v => Mathf.RoundToInt(v / SensDefault * 100f) + "%");
        Section(c, "sec_gyro");
        SegRow(c, "row_gyro", "hint_gyro", OffOn(), () => PlayerPrefs.GetInt(GyroKey, 0), v => { SaveInt(GyroKey, v); ApplyGyro(); }, v => v == 0 || SystemInfo.supportsGyroscope);
        SegRow(c, "row_gyroinv", "hint_gyroinv", OffOn(), () => PlayerPrefs.GetInt(GyroInvKey, 0), v => { SaveInt(GyroInvKey, v); ApplyGyro(); }, v => v == 0 || SystemInfo.supportsGyroscope);
        SliderRow(c, "row_gyrosens", 0.2f, 3f, 0.1f, () => PlayerPrefs.GetFloat(GyroSensKey, 1f), v => { SaveFloat(GyroSensKey, v); ApplyGyro(); }, v => Mathf.RoundToInt(v * 100f) + "%");
        BottomButton(Bottom(), "reset", "hint_reset_sens", () => { SaveFloat(GyroSensKey, 1f); ApplyGyro(); SetSensitivity(SensDefault); });
    }
    private void BuildAudioPage()
    {
        RectTransform[] subs = NewPage("Audio", new[] { "sub_sound", "sub_haptic" });
        RectTransform c = subs[0];
        Section(c, "sec_volume");
        SliderRow(c, "row_master", 0f, 1f, 0.05f, () => volValue, SetVolume, v => Mathf.RoundToInt(v * 100f) + "%");
        string[] volRows = { "row_vol_steps", "row_vol_monsters", "row_vol_effects", "row_vol_others" };
        for (int i = 0; i < VolKeys.Length; i++)
        {
            string key = VolKeys[i];
            SliderRow(c, volRows[i], 0f, 1f, 0.05f, () => PlayerPrefs.GetFloat(key, 1f), v => { SaveFloat(key, v); audioTimer = 0f; }, v => Mathf.RoundToInt(v * 100f) + "%");
        }
        Section(c, "sec_soundopt");
        SegRow(c, "row_3d", "hint_3d", OffOn(), () => PlayerPrefs.GetInt(Vol3dKey, 1), v => { SaveInt(Vol3dKey, v); audioTimer = 0f; }, null);
        SegRow(c, "row_menusound", "hint_menusound", OffOn(), () => PlayerPrefs.GetInt(MenuSoundKey, 1), v => { SaveInt(MenuSoundKey, v); ApplyMenuSound(); }, null);
        RectTransform h = subs[1];
        Section(h, "sec_vibration");
        SegRow(h, "row_vibdamage", "hint_vibdamage", OffOn(), () => PlayerPrefs.GetInt(VibDamageKey, 1), v => SaveInt(VibDamageKey, v), null);
        SegRow(h, "row_vibmonster", "hint_vibmonster", OffOn(), () => PlayerPrefs.GetInt(VibMonsterKey, 0), v => SaveInt(VibMonsterKey, v), null);
        BottomButton(Bottom(), "reset", "hint_reset_audio", ResetAudio);
    }
    private void BuildLanguagePage()
    {
        RectTransform[] subs = NewPage("Language", new[] { "sub_language", "sub_network" });
        RectTransform c = subs[0];
        Section(c, "sec_uilang");
        Text labelText;
        RectTransform row = Row(c, "row_uilang", null, out labelText);
        Image border = AddImage(NewRect("Selector", row), segBorder);
        border.raycastTarget = true;
        Place(border.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(360f, SegHeight));
        Image fill = AddImage(NewRect("Fill", border.rectTransform), segFill);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Text cur = MakeText(border.rectTransform, LangName(lang), 27, TextAnchor.MiddleCenter, bone, false);
        Stretch(cur.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-60f, 0f));
        Text icon = MakeText(border.rectTransform, ">", 34, TextAnchor.MiddleCenter, bloodBright, true);
        Stretch(icon.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(-56f, 0f), Vector2.zero);
        Button open = border.gameObject.AddComponent<Button>();
        open.transition = Selectable.Transition.ColorTint;
        BindButton(open, OpenLanguagePopup);
        RectTransform n = subs[1];
        Section(n, "sec_connection");
        InfoRow(n, "row_server", "hint_server", ServerText);
        InfoRow(n, "row_room", "hint_room", RoomText);
        SegRow(n, "row_pingcounter", "hint_pingcounter", OffOn(), () => PlayerPrefs.GetInt(PingKey, 0), v => { SaveInt(PingKey, v); UpdateHudCounter(); }, null);
    }
    private void OpenLanguagePopup()
    {
        if (popup != null)
        {
            Destroy(popup.gameObject);
        }
        popup = NewRect("LangPopup", root);
        Stretch(popup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = AddImage(popup, new Color(0f, 0f, 0f, 0.78f));
        dim.raycastTarget = true;
        Image box = AddImage(NewRect("Box", popup), new Color(0.1f, 0.095f, 0.1f, 1f));
        box.sprite = grungeSprite;
        box.type = Image.Type.Tiled;
        box.color = new Color(1.6f, 1.5f, 1.5f, 1f);
        Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 470f));
        Image head = AddImage(NewRect("Head", box.rectTransform), new Color(0.07f, 0.065f, 0.07f, 1f));
        Stretch(head.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -80f), Vector2.zero);
        Image headBar = AddImage(NewRect("Bar", head.rectTransform), bloodBright);
        Stretch(headBar.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 0f));
        Text title = MakeText(head.rectTransform, D("row_uilang"), 32, TextAnchor.MiddleLeft, bloodBright, true);
        Stretch(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 0f), new Vector2(-90f, 0f));
        Image xImg = AddImage(NewRect("X", head.rectTransform), new Color(1f, 1f, 1f, 0.001f));
        xImg.raycastTarget = true;
        Place(xImg.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(72f, 72f));
        Text xText = MakeText(xImg.rectTransform, "X", 46, TextAnchor.MiddleCenter, bone, false);
        Stretch(xText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button xb = xImg.gameObject.AddComponent<Button>();
        xb.transition = Selectable.Transition.None;
        BindButton(xb, ClosePopup);
        int picked = lang;
        Image[] borders = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image b = AddImage(NewRect("Lang" + i, box.rectTransform), segBorder);
            b.raycastTarget = true;
            Place(b.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f + i * 280f, -120f), new Vector2(260f, 76f));
            Image f = AddImage(NewRect("Fill", b.rectTransform), segFill);
            Stretch(f.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            Text t = MakeText(b.rectTransform, LangName(i), 30, TextAnchor.MiddleCenter, bone, true);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button btn = b.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            borders[i] = b;
            int index = i;
            BindButton(btn, () =>
            {
                picked = index;
                for (int k = 0; k < 3; k++)
                {
                    borders[k].color = k == picked ? bloodBright : segBorder;
                }
            });
        }
        for (int k = 0; k < 3; k++)
        {
            borders[k].color = k == picked ? bloodBright : segBorder;
        }
        Image ok = AddImage(NewRect("Ok", box.rectTransform), blood);
        ok.raycastTarget = true;
        Place(ok.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(260f, 84f));
        Text okText = MakeText(ok.rectTransform, D("ok"), 32, TextAnchor.MiddleCenter, Color.white, true);
        Stretch(okText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button okb = ok.gameObject.AddComponent<Button>();
        okb.transition = Selectable.Transition.ColorTint;
        BindButton(okb, () =>
        {
            ClosePopup();
            if (picked != lang)
            {
                lang = picked;
                SaveInt(LangKey, lang);
                RebuildMenu();
                RefreshAll();
            }
        });
        if (lang > 0)
        {
            MirrorChildren(popup);
        }
    }
    private static void MirrorChildren(RectTransform parent)
    {
        foreach (Transform child in parent)
        {
            RectTransform rt = child as RectTransform;
            if (rt == null || rt.name == "LayoutRow")
            {
                continue;
            }
            Vector2 aMin = rt.anchorMin;
            Vector2 aMax = rt.anchorMax;
            rt.anchorMin = new Vector2(1f - aMax.x, aMin.y);
            rt.anchorMax = new Vector2(1f - aMin.x, aMax.y);
            rt.pivot = new Vector2(1f - rt.pivot.x, rt.pivot.y);
            rt.anchoredPosition = new Vector2(-rt.anchoredPosition.x, rt.anchoredPosition.y);
            Text t = rt.GetComponent<Text>();
            if (t != null)
            {
                t.alignment = FlipAlign(t.alignment);
            }
            MirrorChildren(rt);
        }
    }
    private static TextAnchor FlipAlign(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft:
                return TextAnchor.UpperRight;
            case TextAnchor.UpperRight:
                return TextAnchor.UpperLeft;
            case TextAnchor.MiddleLeft:
                return TextAnchor.MiddleRight;
            case TextAnchor.MiddleRight:
                return TextAnchor.MiddleLeft;
            case TextAnchor.LowerLeft:
                return TextAnchor.LowerRight;
            case TextAnchor.LowerRight:
                return TextAnchor.LowerLeft;
        }
        return a;
    }
    private void ClosePopup()
    {
        if (popup != null)
        {
            Destroy(popup.gameObject);
            popup = null;
        }
    }
    private static string LangName(int i)
    {
        if (i == 1)
        {
            return PwRtl.Visual("العربية");
        }
        if (i == 2)
        {
            return PwRtl.Visual("کوردی");
        }
        return "English";
    }
    private void Section(RectTransform content, string key)
    {
        RectTransform rt = NewRect("Section", content);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 62f;
        le.minHeight = 62f;
        AddImage(rt, sectionColor);
        Image bar = AddImage(NewRect("Bar", rt), blood);
        Stretch(bar.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
        Text t = MakeText(rt, D(key), 30, TextAnchor.MiddleLeft, bone, true);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), Vector2.zero);
    }
    private RectTransform Row(RectTransform content, string labelKey, string hintKey, out Text labelText)
    {
        RectTransform rt = NewRect("Row", content);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = RowHeight;
        le.minHeight = RowHeight;
        AddImage(rt, rowColor);
        labelText = MakeText(rt, LabelText(L(labelKey), hintKey == null ? null : L(hintKey)), 28, TextAnchor.MiddleLeft, bone, false);
        labelText.horizontalOverflow = lang == 0 ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        labelText.lineSpacing = 0.9f;
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), Vector2.zero);
        return rt;
    }
    private static string LabelText(string label, string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            return Disp(label);
        }
        if (lang == 0)
        {
            return label + "  <size=20><color=#8A8480>(" + hint + ")</color></size>";
        }
        return PwRtl.Visual(label) + "\n<size=20><color=#8A8480>" + PwRtl.Visual(hint) + "</color></size>";
    }
    private Text SegRow(RectTransform content, string labelKey, string hintKey, string[] options, System.Func<int> getSel, System.Action<int> onPick, System.Func<int, bool> isEnabled)
    {
        Text labelText;
        RectTransform row = Row(content, labelKey, hintKey, out labelText);
        float w = options.Length > 5 ? 128f : (options.Length > 3 ? 168f : 176f);
        float total = w * options.Length;
        labelText.rectTransform.offsetMax = new Vector2(-(total + 40f), 0f);
        RectTransform group = NewRect("Options", row);
        Place(group, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(total, SegHeight));
        Seg[] segs = new Seg[options.Length];
        int n = options.Length;
        for (int i = 0; i < n; i++)
        {
            int slot = i;
            RectTransform b = NewRect("Opt" + i, group);
            b.anchorMin = new Vector2(slot / (float)n, 0f);
            b.anchorMax = new Vector2((slot + 1) / (float)n, 1f);
            b.offsetMin = new Vector2(slot == 0 ? 0f : -1f, 0f);
            b.offsetMax = Vector2.zero;
            Image border = AddImage(b, segBorder);
            border.raycastTarget = true;
            Image fill = AddImage(NewRect("Fill", b), segFill);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            Image line = AddImage(NewRect("Line", b), bloodBright);
            Stretch(line.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(2f, 2f), new Vector2(-2f, 6f));
            Text t = MakeText(b, options[i], 25, TextAnchor.MiddleCenter, ash, false);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button btn = b.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = border;
            int index = i;
            BindButton(btn, () =>
            {
                if (isEnabled != null && !isEnabled(index))
                {
                    return;
                }
                onPick(index);
                RefreshAll();
            });
            segs[i] = new Seg { button = btn, fill = fill, line = line, text = t };
        }
        refreshers.Add(() =>
        {
            int sel = getSel();
            for (int i = 0; i < segs.Length; i++)
            {
                bool enabled = isEnabled == null || isEnabled(i);
                bool on = enabled && i == sel;
                segs[i].button.interactable = enabled;
                segs[i].button.image.color = enabled ? (on ? bloodBright : segBorder) : segDisBorder;
                segs[i].fill.color = enabled ? (on ? segFillOn : segFill) : segDisFill;
                segs[i].line.enabled = on;
                segs[i].text.color = enabled ? (on ? Color.white : ash) : segDisText;
                segs[i].text.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            }
        });
        return labelText;
    }
    private void SliderRow(RectTransform content, string labelKey, float min, float max, float step, System.Func<float> getVal, System.Action<float> setVal, System.Func<float, string> format)
    {
        Text labelText;
        RectTransform row = Row(content, labelKey, null, out labelText);
        labelText.rectTransform.offsetMax = new Vector2(-810f, 0f);
        RectTransform plus = SquareButton(row, "+", new Vector2(-16f, 0f));
        RectTransform area = NewRect("SliderArea", row);
        Place(area, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(460f, 40f));
        RectTransform minus = SquareButton(row, "-", new Vector2(-568f, 0f));
        Text value = MakeText(row, "", 30, TextAnchor.MiddleRight, bone, true);
        Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-648f, 0f), new Vector2(140f, 60f));
        Image bg = AddImage(area, new Color(0.2f, 0.19f, 0.19f, 1f));
        bg.raycastTarget = true;
        RectTransform fillArea = NewRect("Fill Area", area);
        Stretch(fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 12f), new Vector2(0f, -12f));
        Image fill = AddImage(NewRect("Fill", fillArea), blood);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform handleArea = NewRect("Handle Slide Area", area);
        Stretch(handleArea, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
        Image handle = AddImage(NewRect("Handle", handleArea), bone);
        handle.rectTransform.sizeDelta = new Vector2(24f, 12f);
        Slider slider = area.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = lang == 0 ? Slider.Direction.LeftToRight : Slider.Direction.RightToLeft;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(getVal());
        BindSlider(slider, v =>
        {
            setVal(v);
            value.text = format(v);
            RefreshAll();
        });
        BindButton(minus.GetComponent<Button>(), () => slider.value = Mathf.Clamp(slider.value - step, min, max));
        BindButton(plus.GetComponent<Button>(), () => slider.value = Mathf.Clamp(slider.value + step, min, max));
        refreshers.Add(() =>
        {
            float v = getVal();
            slider.SetValueWithoutNotify(v);
            value.text = format(v);
        });
    }
    private RectTransform SquareButton(RectTransform row, string label, Vector2 pos)
    {
        Image border = AddImage(NewRect("Btn" + label, row), segBorder);
        border.raycastTarget = true;
        Place(border.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), pos, new Vector2(60f, 60f));
        Image fill = AddImage(NewRect("Fill", border.rectTransform), segFill);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Text t = MakeText(border.rectTransform, label, 44, TextAnchor.MiddleCenter, bone, true);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button b = border.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        return border.rectTransform;
    }
    private void InfoRow(RectTransform content, string labelKey, string hintKey, System.Func<string> value)
    {
        Text labelText;
        RectTransform row = Row(content, labelKey, hintKey, out labelText);
        Text v = MakeText(row, "", 28, TextAnchor.MiddleRight, bone, true);
        Stretch(v.rectTransform, new Vector2(0.55f, 0f), Vector2.one, Vector2.zero, new Vector2(-24f, 0f));
        liveRefreshers.Add(() => v.text = value());
    }
    private void BottomButton(RectTransform bar, string labelKey, string hintKey, UnityAction action)
    {
        Image border = AddImage(NewRect("BottomBtn", bar), new Color(0.6f, 0.57f, 0.55f, 1f));
        border.raycastTarget = true;
        Place(border.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(300f, 66f));
        Image fill = AddImage(NewRect("Fill", border.rectTransform), new Color(0.06f, 0.055f, 0.06f, 1f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Text t = MakeText(border.rectTransform, D(labelKey), 27, TextAnchor.MiddleCenter, bone, false);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button b = border.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        BindButton(b, action);
        Text h = MakeText(bar, Disp("(" + L(hintKey) + ")"), 22, TextAnchor.MiddleLeft, ash, false);
        Stretch(h.rectTransform, Vector2.zero, Vector2.one, new Vector2(320f, 0f), Vector2.zero);
    }
    private static string[] OffOn()
    {
        return new[] { D("off"), D("on") };
    }
    private static string[] Opts(string prefix, int count)
    {
        string[] r = new string[count];
        for (int i = 0; i < count; i++)
        {
            r[i] = D(prefix + i);
        }
        return r;
    }
    private static string L(string key)
    {
        string[] v;
        if (PwText.Table.TryGetValue(key, out v))
        {
            return v[Mathf.Clamp(lang, 0, v.Length - 1)];
        }
        return key;
    }
    private static string D(string key)
    {
        return Disp(L(key));
    }
    private static string Disp(string s)
    {
        return lang == 0 ? s : PwRtl.Visual(s);
    }
    private string ServerText()
    {
        if (!PhotonNetwork.IsConnected)
        {
            return "<color=#8A8480>" + D("offline") + "</color>";
        }
        string region = string.IsNullOrEmpty(PhotonNetwork.CloudRegion) ? "-" : PhotonNetwork.CloudRegion.Replace("/*", "").ToUpper();
        string regionText = region == "EU" ? D("europe") : region;
        int ping = PhotonNetwork.GetPing();
        string col = ping < 120 ? "#3FBF5F" : (ping < 200 ? "#E0B040" : "#E04040");
        string pingText = "<color=" + col + ">" + ping + " ms</color>";
        return lang == 0 ? regionText + "   " + pingText : pingText + "   " + regionText;
    }
    private string RoomText()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return "<color=#8A8480>" + D("noroom") + "</color>";
        }
        return PhotonNetwork.CurrentRoom.Name + "   " + PhotonNetwork.CurrentRoom.PlayerCount + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
    }
    private static int AaIndex()
    {
        int aa = QualitySettings.antiAliasing;
        return aa >= 4 ? 2 : (aa >= 2 ? 1 : 0);
    }
    private int SensPresetIndex()
    {
        for (int i = 0; i < SensPresets.Length; i++)
        {
            if (Mathf.Abs(SensPresets[i] - sensValue) < 0.004f)
            {
                return i;
            }
        }
        return 3;
    }
    private static void SaveInt(string key, int v)
    {
        PlayerPrefs.SetInt(key, v);
        PlayerPrefs.Save();
    }
    private static void SaveFloat(string key, float v)
    {
        PlayerPrefs.SetFloat(key, v);
        PlayerPrefs.Save();
    }
    private void RefreshAll()
    {
        for (int i = 0; i < refreshers.Count; i++)
        {
            refreshers[i]();
        }
        for (int i = 0; i < liveRefreshers.Count; i++)
        {
            liveRefreshers[i]();
        }
    }
    private void ResetBasic()
    {
        SaveInt(FpsCounterKey, 0);
        SaveInt(TransUiKey, 0);
        SaveInt(CamMotionKey, 1);
        SaveInt(SwayKey, 1);
        UpdateHudCounter();
        ApplyTransparentUi();
        ApplyCameraToggles();
        RefreshAll();
    }
    private void ResetAudio()
    {
        SetVolume(1f);
        for (int i = 0; i < VolKeys.Length; i++)
        {
            SaveFloat(VolKeys[i], 1f);
        }
        SaveInt(Vol3dKey, 1);
        SaveInt(MenuSoundKey, 1);
        SaveInt(VibDamageKey, 1);
        SaveInt(VibMonsterKey, 0);
        ApplyMenuSound();
        audioTimer = 0f;
        RefreshAll();
    }
    private void ResetGraphics()
    {
        PlayerPrefs.DeleteKey(GraphicsKey);
        PlayerPrefs.DeleteKey(FpsKey);
        PlayerPrefs.DeleteKey(ResKey);
        PlayerPrefs.DeleteKey(AaKey);
        PlayerPrefs.DeleteKey(ShadowKey);
        PlayerPrefs.DeleteKey(StyleKey);
        PlayerPrefs.DeleteKey(BlindKey);
        PlayerPrefs.Save();
        brightValue = 1f;
        SaveFloat(BrightKey, 1f);
        ApplyBrightness(1f);
        SetGraphics(LevelKeys[1]);
        ApplyDisplay(SavedRes(), SupportedFps(60));
        appliedStyle = -1;
        appliedBlind = -1;
        UpdatePostFX();
        RefreshAll();
    }
    private void BuildHudCounter()
    {
        if (transform.parent == null)
        {
            return;
        }
        RectTransform rt = NewRect("FpsCounter", transform.parent);
        Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(260f, 70f));
        hudCounter = MakeText(rt, "", 26, TextAnchor.UpperLeft, new Color(0.35f, 1f, 0.45f, 1f), true);
        Stretch(hudCounter.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Outline o = hudCounter.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.8f);
        UpdateHudCounter();
    }
    private void UpdateHudCounter()
    {
        if (hudCounter == null)
        {
            return;
        }
        bool fps = PlayerPrefs.GetInt(FpsCounterKey, 0) == 1;
        bool ping = PlayerPrefs.GetInt(PingKey, 0) == 1;
        hudCounter.transform.parent.gameObject.SetActive(fps || ping);
        string text = "";
        if (fps)
        {
            text = measuredFps + " FPS";
        }
        if (ping)
        {
            int p = PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() : 0;
            text += (text.Length > 0 ? "\n" : "") + p + " ms";
        }
        hudCounter.text = text;
    }
    private void ApplyTransparentUi()
    {
        if (transform.parent == null)
        {
            return;
        }
        float a = PlayerPrefs.GetInt(TransUiKey, 0) == 1 ? 0.45f : 1f;
        foreach (Transform child in transform.parent)
        {
            if (IsHudExcluded(child) || child.name == "DeathPanel")
            {
                continue;
            }
            CanvasGroup g = child.GetComponent<CanvasGroup>();
            if (g == null)
            {
                if (a >= 1f)
                {
                    continue;
                }
                g = child.gameObject.AddComponent<CanvasGroup>();
            }
            g.alpha = a;
        }
    }
    private void ApplyControlLayout()
    {
        if (transform.parent == null)
        {
            return;
        }
        bool mirrored = PlayerPrefs.GetInt(LayoutKey, 0) == 1;
        foreach (Transform child in transform.parent)
        {
            if (IsHudExcluded(child) || child.name == "DeathPanel")
            {
                continue;
            }
            RectTransform rt = child as RectTransform;
            if (rt == null)
            {
                continue;
            }
            Vector4[] orig;
            if (!hudOriginal.TryGetValue(rt, out orig))
            {
                orig = new[] { new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y), new Vector4(rt.pivot.x, rt.pivot.y, rt.anchoredPosition.x, rt.anchoredPosition.y) };
                hudOriginal.Add(rt, orig);
            }
            if (mirrored)
            {
                rt.anchorMin = new Vector2(1f - orig[0].z, orig[0].y);
                rt.anchorMax = new Vector2(1f - orig[0].x, orig[0].w);
                rt.pivot = new Vector2(1f - orig[1].x, orig[1].y);
                rt.anchoredPosition = new Vector2(-orig[1].z, orig[1].w);
            }
            else
            {
                rt.anchorMin = new Vector2(orig[0].x, orig[0].y);
                rt.anchorMax = new Vector2(orig[0].z, orig[0].w);
                rt.pivot = new Vector2(orig[1].x, orig[1].y);
                rt.anchoredPosition = new Vector2(orig[1].z, orig[1].w);
            }
        }
    }
    private bool IsHudExcluded(Transform child)
    {
        return child == transform || child.name == "EventSystem" || child.name == "LoadingPanel" || child.name == "BrightnessOverlay" || child.name == "FpsCounter";
    }
    private void ApplyCameraToggles()
    {
        bool motion = PlayerPrefs.GetInt(CamMotionKey, 1) == 1;
        bool sway = PlayerPrefs.GetInt(SwayKey, 1) == 1;
        Camera cam = Camera.main;
        if (cam != null)
        {
            GTA6CameraEffects fx = cam.GetComponent<GTA6CameraEffects>();
            if (fx != null && fx.enabled != motion)
            {
                fx.enabled = motion;
            }
        }
        FlashlightSway[] sways = FindObjectsByType<FlashlightSway>(FindObjectsInactive.Include);
        for (int i = 0; i < sways.Length; i++)
        {
            if (sways[i] != null && sways[i].enabled != sway)
            {
                sways[i].enabled = sway;
            }
        }
    }
    private void ApplyMenuSound()
    {
        AudioListener.pause = isOpen && PlayerPrefs.GetInt(MenuSoundKey, 1) == 0;
    }
    private void ApplyAudioMix()
    {
        float[] vols = new float[VolKeys.Length];
        for (int i = 0; i < VolKeys.Length; i++)
        {
            vols[i] = PlayerPrefs.GetFloat(VolKeys[i], 1f);
        }
        bool use3d = PlayerPrefs.GetInt(Vol3dKey, 1) == 1;
        HashSet<AudioSource> steps = new HashSet<AudioSource>();
        FootstepSoundController[] feet = FindObjectsByType<FootstepSoundController>(FindObjectsInactive.Exclude);
        for (int i = 0; i < feet.Length; i++)
        {
            if (feet[i] != null && feet[i].audioSource != null)
            {
                steps.Add(feet[i].audioSource);
            }
        }
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource src = sources[i];
            if (src == null)
            {
                continue;
            }
            float mult;
            if (steps.Contains(src))
            {
                mult = vols[0];
            }
            else if (src.GetComponentInParent<EnemyAI>() != null)
            {
                mult = vols[1];
            }
            else
            {
                mult = vols[2];
            }
            PhotonView pv = src.GetComponentInParent<PhotonView>();
            if (pv != null && !pv.IsMine && pv.CompareTag("Player"))
            {
                mult *= vols[3];
            }
            Vector4 rec;
            if (!audioBase.TryGetValue(src, out rec))
            {
                rec = new Vector4(src.volume, src.volume, src.spatialBlend, src.spatialBlend);
            }
            if (!Mathf.Approximately(src.volume, rec.y))
            {
                rec.x = src.volume;
            }
            if (!Mathf.Approximately(src.spatialBlend, rec.w))
            {
                rec.z = src.spatialBlend;
            }
            float vol = rec.x * mult;
            float blend = use3d ? rec.z : 0f;
            src.volume = vol;
            src.spatialBlend = blend;
            rec.y = src.volume;
            rec.w = src.spatialBlend;
            audioBase[src] = rec;
        }
        List<AudioSource> dead = null;
        foreach (KeyValuePair<AudioSource, Vector4> pair in audioBase)
        {
            if (pair.Key == null)
            {
                if (dead == null)
                {
                    dead = new List<AudioSource>();
                }
                dead.Add(pair.Key);
            }
        }
        if (dead != null)
        {
            for (int i = 0; i < dead.Count; i++)
            {
                audioBase.Remove(dead[i]);
            }
        }
    }
    private void UpdateHaptics()
    {
        if (localHealth == null)
        {
            GameObject player = FindLocalPlayer();
            if (player != null)
            {
                localHealth = player.GetComponent<PlayerHealth>();
                lastHealth = localHealth != null ? localHealth.currentHealth : -1;
            }
        }
        if (localHealth != null)
        {
            int hp = localHealth.currentHealth;
            if (lastHealth >= 0 && hp < lastHealth && PlayerPrefs.GetInt(VibDamageKey, 1) == 1)
            {
                Vibrate();
            }
            lastHealth = hp;
        }
        if (PlayerPrefs.GetInt(VibMonsterKey, 0) == 1 && localHealth != null && !localHealth.IsDead)
        {
            monsterPulse -= 0.25f;
            if (monsterPulse <= 0f)
            {
                EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
                Vector3 p = localHealth.transform.position;
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i] != null && (enemies[i].transform.position - p).sqrMagnitude < 36f)
                    {
                        Vibrate();
                        monsterPulse = 1.5f;
                        break;
                    }
                }
            }
        }
    }
    private static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
    private void Update()
    {
        fxTimer -= Time.unscaledDeltaTime;
        if (fxTimer <= 0f)
        {
            fxTimer = 0.5f;
            UpdatePostFX();
            ApplyCameraToggles();
        }
        audioTimer -= Time.unscaledDeltaTime;
        if (audioTimer <= 0f)
        {
            audioTimer = 0.5f;
            ApplyAudioMix();
        }
        frameCount++;
        frameTime += Time.unscaledDeltaTime;
        if (frameTime >= 0.5f)
        {
            measuredFps = Mathf.RoundToInt(frameCount / frameTime);
            frameCount = 0;
            frameTime = 0f;
            UpdateHudCounter();
        }
        infoTimer -= Time.unscaledDeltaTime;
        if (infoTimer <= 0f)
        {
            infoTimer = 0.25f;
            UpdateHaptics();
            if (isOpen)
            {
                for (int i = 0; i < liveRefreshers.Count; i++)
                {
                    liveRefreshers[i]();
                }
            }
        }
        if (isOpen)
        {
            Flicker();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (popup != null)
            {
                ClosePopup();
            }
            else
            {
                TogglePause();
            }
        }
    }
    private void Flicker()
    {
        if (titleText == null)
        {
            return;
        }
        flickerTimer -= Time.unscaledDeltaTime;
        float a = 1f;
        if (flickerTimer < 0f)
        {
            a = Random.value < 0.5f ? 0.25f : 0.7f;
            if (flickerTimer < -0.18f)
            {
                flickerTimer = Random.Range(2.5f, 6f);
            }
        }
        titleText.color = new Color(bone.r, bone.g, bone.b, a);
    }
    private void SelectTab(int index)
    {
        currentTab = Mathf.Clamp(index, 0, pageInfos.Count - 1);
        lastTab = currentTab;
        for (int i = 0; i < pageInfos.Count; i++)
        {
            bool on = i == currentTab;
            pageInfos[i].container.SetActive(on);
            pageInfos[i].strip.SetActive(on);
            pageInfos[i].bottom.SetActive(on);
        }
        for (int i = 0; i < tabFills.Count; i++)
        {
            bool on = i == currentTab;
            tabFills[i].color = on ? new Color(0.55f, 0.06f, 0.08f, 1f) : new Color(0f, 0f, 0f, 0.001f);
            tabBars[i].enabled = on;
            tabTexts[i].color = on ? Color.white : ash;
            tabTexts[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }
        if (currentTab < pageInfos.Count)
        {
            SelectSub(currentTab, pageInfos[currentTab].currentSub);
        }
        RefreshAll();
    }
    private void SelectSub(int page, int sub)
    {
        if (page < 0 || page >= pageInfos.Count)
        {
            return;
        }
        PageInfo info = pageInfos[page];
        info.currentSub = Mathf.Clamp(sub, 0, info.subs.Count - 1);
        for (int i = 0; i < info.subs.Count; i++)
        {
            bool on = i == info.currentSub;
            info.subs[i].SetActive(on);
            info.subFills[i].color = on ? new Color(0.24f, 0.2f, 0.2f, 1f) : new Color(0f, 0f, 0f, 0.001f);
            info.subLines[i].enabled = on;
            info.subTexts[i].color = on ? bone : ash;
            if (on)
            {
                ScrollRect sr = info.subs[i].GetComponent<ScrollRect>();
                if (sr != null)
                {
                    sr.verticalNormalizedPosition = 1f;
                }
            }
        }
    }
    private void SetGraphics(string level)
    {
        PlayerPrefs.SetString(GraphicsKey, level);
        PlayerPrefs.Save();
        ApplyGraphicsLevel(level);
        appliedStyle = -1;
        appliedBlind = -1;
    }
    private static int LevelSlot(string level)
    {
        for (int i = 0; i < LevelKeys.Length; i++)
        {
            if (LevelKeys[i] == level)
            {
                return i;
            }
        }
        return 1;
    }
    private static void ApplyGraphicsLevel(string level)
    {
        int slot = LevelSlot(level);
        currentSlot = slot;
        string[] names = QualitySettings.names;
        int qualityIndex = -1;
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].ToUpper() == LevelNames[slot])
            {
                qualityIndex = i;
                break;
            }
        }
        if (qualityIndex < 0)
        {
            qualityIndex = Mathf.Clamp(slot, 0, Mathf.Max(0, names.Length - 1));
        }
        QualitySettings.SetQualityLevel(qualityIndex, true);
        ApplyAdvanced();
    }
    private static List<Resolution> RefreshModes()
    {
        List<Resolution> modes = new List<Resolution>();
        Resolution[] all = Screen.resolutions;
        for (int i = 0; i < all.Length; i++)
        {
            modes.Add(all[i]);
        }
        modes.Add(Screen.currentResolution);
        return modes;
    }
    private static bool FindRefreshFor(int fps, out Resolution best)
    {
        best = Screen.currentResolution;
        bool found = false;
        double bestHz = double.MaxValue;
        List<Resolution> modes = RefreshModes();
        for (int i = 0; i < modes.Count; i++)
        {
            double hz = modes[i].refreshRateRatio.value;
            if (hz < fps - 1.0)
            {
                continue;
            }
            double ratio = hz / fps;
            if (System.Math.Abs(ratio - System.Math.Round(ratio)) > 0.03)
            {
                continue;
            }
            if (hz < bestHz)
            {
                bestHz = hz;
                best = modes[i];
                found = true;
            }
        }
        return found;
    }
    private static bool IsFpsSupported(int fps)
    {
        if (Application.isEditor)
        {
            return true;
        }
        Resolution mode;
        return FindRefreshFor(fps, out mode);
    }
    private static int SupportedFps(int wanted)
    {
        if (IsFpsSupported(wanted))
        {
            return wanted;
        }
        for (int i = FpsOptions.Length - 1; i >= 0; i--)
        {
            if (FpsOptions[i] < wanted && IsFpsSupported(FpsOptions[i]))
            {
                return FpsOptions[i];
            }
        }
        return 30;
    }
    private static void CaptureNative()
    {
        if (nativeShort > 0)
        {
            return;
        }
        int w = Display.main.systemWidth;
        int h = Display.main.systemHeight;
        if (w <= 0 || h <= 0)
        {
            w = Screen.currentResolution.width;
            h = Screen.currentResolution.height;
        }
        nativeLong = Mathf.Max(w, h);
        nativeShort = Mathf.Min(w, h);
        if (nativeShort <= 0)
        {
            nativeLong = Mathf.Max(Screen.width, Screen.height);
            nativeShort = Mathf.Min(Screen.width, Screen.height);
        }
    }
    private static List<int> ResOptions()
    {
        CaptureNative();
        List<int> list = new List<int>();
        for (int i = 0; i < ResSteps.Length; i++)
        {
            if (ResSteps[i] < nativeShort - 20)
            {
                list.Add(ResSteps[i]);
            }
        }
        list.Add(nativeShort);
        while (list.Count > 4)
        {
            list.RemoveAt(0);
        }
        return list;
    }
    private static int SavedRes()
    {
        List<int> options = ResOptions();
        int fallback = options[0];
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] <= 1080)
            {
                fallback = options[i];
            }
        }
        int wanted = PlayerPrefs.GetInt(ResKey, fallback);
        int best = options[0];
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] <= wanted)
            {
                best = options[i];
            }
        }
        return best;
    }
    private static void ApplyDisplay(int resShort, int fps)
    {
        QualitySettings.vSyncCount = 0;
        if (!Application.isEditor)
        {
            CaptureNative();
            int h = Mathf.Clamp(resShort, 360, nativeShort);
            int w = Mathf.RoundToInt(nativeLong * (h / (float)nativeShort));
            w -= w % 2;
            h -= h % 2;
            if (Screen.width < Screen.height)
            {
                int tmp = w;
                w = h;
                h = tmp;
            }
            RefreshRate rate = Screen.currentResolution.refreshRateRatio;
            Resolution mode;
            if (FindRefreshFor(fps, out mode))
            {
                rate = mode.refreshRateRatio;
            }
            bool sizeChanged = Screen.width != w || Screen.height != h;
            bool rateChanged = System.Math.Abs(Screen.currentResolution.refreshRateRatio.value - rate.value) > 0.5;
            if (sizeChanged || rateChanged)
            {
                Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow, rate);
            }
        }
        Application.targetFrameRate = fps;
    }
    private void ReapplyQuality()
    {
        ApplyGraphicsLevel(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]));
        appliedFxSlot = -1;
        appliedStyle = -1;
        appliedBlind = -1;
    }
    private static void ApplyAdvanced()
    {
        int aa = PlayerPrefs.GetInt(AaKey, -1);
        if (aa >= 0)
        {
            QualitySettings.antiAliasing = aa;
        }
        int sh = PlayerPrefs.GetInt(ShadowKey, -1);
        if (sh == 0)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }
        else if (sh == 1 && QualitySettings.shadows == ShadowQuality.Disable)
        {
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 25f);
        }
    }
    private void SetFps(int fps)
    {
        if (!IsFpsSupported(fps))
        {
            return;
        }
        PlayerPrefs.SetInt(FpsKey, fps);
        PlayerPrefs.Save();
        ApplyDisplay(SavedRes(), fps);
    }
    private void SetRes(int index)
    {
        if (index < 0 || index >= resValues.Count)
        {
            return;
        }
        int res = resValues[index];
        PlayerPrefs.SetInt(ResKey, res);
        PlayerPrefs.Save();
        ApplyDisplay(res, SupportedFps(PlayerPrefs.GetInt(FpsKey, 60)));
    }
    private void UpdatePostFX()
    {
        if (ppLayer == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                ppLayer = cam.GetComponent<PostProcessLayer>();
                appliedFxSlot = -1;
            }
        }
        if (ppVolume == null)
        {
            ppVolume = FindAnyObjectByType<PostProcessVolume>();
            appliedFxSlot = -1;
            appliedStyle = -1;
            appliedBlind = -1;
            gradingCaptured = false;
        }
        if (ppLayer == null)
        {
            return;
        }
        if (appliedFxSlot != currentSlot)
        {
            appliedFxSlot = currentSlot;
            ppLayer.enabled = currentSlot > 0;
            if (ppVolume != null)
            {
                PostProcessProfile profile = ppVolume.profile;
                SetEffect<ColorGrading>(profile, currentSlot >= 1);
                SetEffect<Vignette>(profile, currentSlot >= 1);
                SetEffect<Bloom>(profile, currentSlot >= 2);
                SetEffect<Grain>(profile, currentSlot >= 2);
                SetEffect<AmbientOcclusion>(profile, currentSlot >= 3);
                AmbientOcclusion ao;
                if (profile.TryGetSettings(out ao))
                {
                    ao.quality.value = currentSlot >= 4 ? AmbientOcclusionQuality.High : AmbientOcclusionQuality.Medium;
                }
                Bloom bloom;
                if (profile.TryGetSettings(out bloom))
                {
                    bloom.fastMode.value = currentSlot < 3;
                }
            }
        }
        ApplyGrading();
    }
    private void ApplyGrading()
    {
        if (ppVolume == null)
        {
            return;
        }
        int style = Mathf.Clamp(PlayerPrefs.GetInt(StyleKey, 0), 0, StyleTitles.Length - 1);
        int blind = Mathf.Clamp(PlayerPrefs.GetInt(BlindKey, 0), 0, BlindTitles.Length - 1);
        if (style == appliedStyle && blind == appliedBlind)
        {
            return;
        }
        ColorGrading cg;
        if (!ppVolume.profile.TryGetSettings(out cg))
        {
            appliedStyle = style;
            appliedBlind = blind;
            return;
        }
        if (!gradingCaptured)
        {
            gradingCaptured = true;
            baseSat = cg.saturation.overrideState ? cg.saturation.value : 0f;
            baseContrast = cg.contrast.overrideState ? cg.contrast.value : 0f;
            baseTemp = cg.temperature.overrideState ? cg.temperature.value : 0f;
            baseMixer[0] = cg.mixerRedOutRedIn.value;
            baseMixer[1] = cg.mixerRedOutGreenIn.value;
            baseMixer[2] = cg.mixerRedOutBlueIn.value;
            baseMixer[3] = cg.mixerGreenOutRedIn.value;
            baseMixer[4] = cg.mixerGreenOutGreenIn.value;
            baseMixer[5] = cg.mixerGreenOutBlueIn.value;
            baseMixer[6] = cg.mixerBlueOutRedIn.value;
            baseMixer[7] = cg.mixerBlueOutGreenIn.value;
            baseMixer[8] = cg.mixerBlueOutBlueIn.value;
        }
        appliedStyle = style;
        appliedBlind = blind;
        cg.saturation.Override(Mathf.Clamp(baseSat + StyleValues[style, 0], -100f, 100f));
        cg.contrast.Override(Mathf.Clamp(baseContrast + StyleValues[style, 1], -100f, 100f));
        cg.temperature.Override(Mathf.Clamp(baseTemp + StyleValues[style, 2], -100f, 100f));
        float[] m = new float[9];
        for (int i = 0; i < 9; i++)
        {
            m[i] = blind == 0 ? baseMixer[i] : BlindMatrix[blind, i];
        }
        cg.mixerRedOutRedIn.Override(m[0]);
        cg.mixerRedOutGreenIn.Override(m[1]);
        cg.mixerRedOutBlueIn.Override(m[2]);
        cg.mixerGreenOutRedIn.Override(m[3]);
        cg.mixerGreenOutGreenIn.Override(m[4]);
        cg.mixerGreenOutBlueIn.Override(m[5]);
        cg.mixerBlueOutRedIn.Override(m[6]);
        cg.mixerBlueOutGreenIn.Override(m[7]);
        cg.mixerBlueOutBlueIn.Override(m[8]);
    }
    private static void SetEffect<T>(PostProcessProfile profile, bool on) where T : PostProcessEffectSettings
    {
        T effect;
        if (profile.TryGetSettings(out effect))
        {
            effect.enabled.value = on;
        }
    }
    private void SetSensitivity(float value)
    {
        sensValue = Mathf.Clamp(value, 0.05f, 0.5f);
        SaveFloat(SensKey, sensValue);
        GameObject player = FindLocalPlayer();
        if (player != null)
        {
            CharController_Motor motor = player.GetComponent<CharController_Motor>();
            if (motor != null)
            {
                motor.touchLookSensitivity = sensValue;
            }
        }
        RefreshAll();
    }
    private void ApplyGyro()
    {
        GameObject player = FindLocalPlayer();
        if (player == null)
        {
            return;
        }
        CharController_Motor motor = player.GetComponent<CharController_Motor>();
        if (motor != null)
        {
            motor.SetGyro(PlayerPrefs.GetInt(GyroKey, 0) == 1, PlayerPrefs.GetFloat(GyroSensKey, 1f), PlayerPrefs.GetInt(GyroInvKey, 0) == 1);
        }
    }
    private void SetVolume(float value)
    {
        volValue = Mathf.Clamp01(value);
        AudioListener.volume = volValue;
        SaveFloat(AudioKey, volValue);
    }
    private void ApplyBrightness(float value)
    {
        float v = Mathf.Clamp(value, 0.5f, 1.5f);
        RenderSettings.ambientLight = new Color(baseAmbient.r * v, baseAmbient.g * v, baseAmbient.b * v, baseAmbient.a);
        if (brightnessOverlay != null)
        {
            brightnessOverlay.color = new Color(0f, 0f, 0f, 0f);
        }
    }
    private void ExitToLobby()
    {
        if (leaving)
        {
            return;
        }
        leaving = true;
        AudioListener.pause = false;
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        StartCoroutine(WaitAndLoadLobby());
    }
    private IEnumerator WaitAndLoadLobby()
    {
        float timeout = 3f;
        while (PhotonNetwork.InRoom && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        SetPaused(false);
        AudioListener.pause = false;
        SceneManager.LoadScene("Scene_Lobby");
    }
    private RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }
    private static Image AddImage(RectTransform rt, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
    private Text MakeText(Transform parent, string value, int size, TextAnchor align, Color color, bool bold)
    {
        RectTransform rt = NewRect("Text", parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = lang > 0 && rtlFont != null ? rtlFont : (uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        t.text = value;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.resizeTextForBestFit = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = true;
        return t;
    }
    private static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        rt.localScale = Vector3.one;
    }
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.localScale = Vector3.one;
    }
    private static Sprite MakeGrungeSprite()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        float ox = Random.Range(0f, 100f);
        float oy = Random.Range(0f, 100f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float n = TileNoise(u, v, 4f, ox, oy) * 0.55f + TileNoise(u, v, 12f, ox + 30f, oy) * 0.3f + TileNoise(u, v, 40f, ox, oy + 60f) * 0.15f;
                float stain = Mathf.Clamp01((TileNoise(u, v, 3f, ox + 70f, oy + 20f) - 0.62f) * 4f);
                float g = 0.05f + n * 0.055f;
                px[y * size + x] = new Color(g + stain * 0.07f, g * 0.93f, g * 0.93f, 1f);
            }
        }
        for (int s = 0; s < 70; s++)
        {
            int x0 = Random.Range(0, size);
            int y0 = Random.Range(0, size);
            float ang = Random.Range(0f, Mathf.PI);
            int len = Random.Range(8, 40);
            float add = Random.Range(0.015f, 0.04f);
            for (int i = 0; i < len; i++)
            {
                int x = ((int)(x0 + Mathf.Cos(ang) * i) % size + size) % size;
                int y = ((int)(y0 + Mathf.Sin(ang) * i) % size + size) % size;
                Color c = px[y * size + x];
                px[y * size + x] = new Color(c.r + add, c.g + add, c.b + add, 1f);
            }
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
    private static float TileNoise(float u, float v, float freq, float ox, float oy)
    {
        float a = Mathf.PerlinNoise(ox + u * freq, oy + v * freq);
        float b = Mathf.PerlinNoise(ox + (u - 1f) * freq, oy + v * freq);
        float c = Mathf.PerlinNoise(ox + u * freq, oy + (v - 1f) * freq);
        float d = Mathf.PerlinNoise(ox + (u - 1f) * freq, oy + (v - 1f) * freq);
        float ab = Mathf.Lerp(a, b, u);
        float cd = Mathf.Lerp(c, d, u);
        return Mathf.Lerp(ab, cd, v);
    }
    private static Sprite MakeVignetteSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d)) * 0.85f;
                px[y * size + x] = new Color(0.16f, 0f, 0.01f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
    private void TogglePause()
    {
        if (leaving)
        {
            return;
        }
        SetPaused(!isOpen);
    }
    public void SetPaused(bool p)
    {
        if (isOpen == p)
        {
            return;
        }
        isOpen = p;
        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }
        if (p)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.blocksRaycasts = true;
            }
            SetHUDVisible(false);
            SelectTab(lastTab);
            LockPlayer();
            ApplyMenuSound();
            infoTimer = 0f;
            animRoutine = StartCoroutine(Anim(true));
        }
        else
        {
            if (panelGroup != null)
            {
                panelGroup.blocksRaycasts = false;
            }
            UnlockPlayer();
            ApplyMenuSound();
            animRoutine = StartCoroutine(Anim(false));
        }
    }
    private IEnumerator Anim(bool opening)
    {
        float time = opening ? OpenTime : CloseTime;
        float t = 0f;
        float startAlpha = panelGroup != null ? panelGroup.alpha : 1f;
        float startScale = boxRect != null ? boxRect.localScale.x : 1f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            if (panelGroup != null)
            {
                panelGroup.alpha = opening ? Mathf.Lerp(startAlpha, 1f, EaseOutCubic(k)) : Mathf.Lerp(startAlpha, 0f, k * k);
            }
            if (boxRect != null)
            {
                float s = opening ? Mathf.Lerp(1.04f, 1f, EaseOutCubic(k)) : Mathf.Lerp(startScale, 1.02f, k);
                boxRect.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
        if (panelGroup != null)
        {
            panelGroup.alpha = opening ? 1f : 0f;
        }
        if (boxRect != null)
        {
            boxRect.localScale = Vector3.one;
        }
        if (!opening)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
            SetHUDVisible(true);
            ApplyTransparentUi();
        }
        animRoutine = null;
    }
    private static float EaseOutCubic(float k)
    {
        float f = k - 1f;
        return 1f + f * f * f;
    }
    private void SetHUDVisible(bool visible)
    {
        if (transform.parent == null)
        {
            return;
        }
        if (!visible)
        {
            hudStates.Clear();
            foreach (Transform child in transform.parent)
            {
                if (IsHudExcluded(child))
                {
                    continue;
                }
                hudStates.Add(child.gameObject, child.gameObject.activeSelf);
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            foreach (KeyValuePair<GameObject, bool> pair in hudStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.SetActive(pair.Value);
                }
            }
            hudStates.Clear();
        }
    }
    private void LockPlayer()
    {
        lockedComponents.Clear();
        GameObject player = FindLocalPlayer();
        if (player == null)
        {
            return;
        }
        CharController_Motor motor = player.GetComponent<CharController_Motor>();
        if (motor != null && motor.enabled)
        {
            motor.enabled = false;
            lockedComponents.Add(motor);
        }
        PlayerInteraction interact = player.GetComponent<PlayerInteraction>();
        if (interact != null && interact.enabled)
        {
            interact.enabled = false;
            lockedComponents.Add(interact);
        }
    }
    private void UnlockPlayer()
    {
        foreach (Behaviour component in lockedComponents)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }
        lockedComponents.Clear();
    }
    private GameObject FindLocalPlayer()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsInactive.Include);
            foreach (PhotonView pv in views)
            {
                if (pv != null && pv.IsMine && pv.CompareTag("Player"))
                {
                    return pv.gameObject;
                }
            }
            return null;
        }
        CharController_Motor motor = FindAnyObjectByType<CharController_Motor>(FindObjectsInactive.Include);
        return motor != null ? motor.gameObject : null;
    }
    private void BindButton(Button btn, UnityAction action)
    {
        if (btn == null)
        {
            return;
        }
        btn.onClick.AddListener(action);
        buttonBindings.Add(new KeyValuePair<Button, UnityAction>(btn, action));
    }
    private void BindSlider(Slider slider, UnityAction<float> action)
    {
        if (slider == null)
        {
            return;
        }
        slider.onValueChanged.AddListener(action);
        sliderBindings.Add(new KeyValuePair<Slider, UnityAction<float>>(slider, action));
    }
    private GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }
        Transform direct = parent.Find(childName);
        if (direct != null)
        {
            return direct.gameObject;
        }
        foreach (Transform child in parent)
        {
            GameObject found = FindChild(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    private Button FindButtonInParent(string buttonName)
    {
        GameObject go = FindChild(transform.parent, buttonName);
        return go != null ? go.GetComponent<Button>() : null;
    }
    private void OnDestroy()
    {
        RenderSettings.ambientLight = baseAmbient;
        AudioListener.pause = false;
        if (grungeSprite != null)
        {
            Destroy(grungeSprite.texture);
            Destroy(grungeSprite);
        }
        if (vignetteSprite != null)
        {
            Destroy(vignetteSprite.texture);
            Destroy(vignetteSprite);
        }
        foreach (KeyValuePair<Button, UnityAction> pair in buttonBindings)
        {
            if (pair.Key != null)
            {
                pair.Key.onClick.RemoveListener(pair.Value);
            }
        }
        foreach (KeyValuePair<Slider, UnityAction<float>> pair in sliderBindings)
        {
            if (pair.Key != null)
            {
                pair.Key.onValueChanged.RemoveListener(pair.Value);
            }
        }
        PlayerPrefs.Save();
    }
}
public static class PwText
{
    public static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        { "title", new[] { "SETTINGS", "الإعدادات", "ڕێکخستنەکان" } },
        { "tab_account", new[] { "Account", "الحساب", "هەژمار" } },
        { "tab_basic", new[] { "Basic", "الأساسية", "بنەڕەتی" } },
        { "tab_graphics", new[] { "Graphics", "الرسومات", "گرافیک" } },
        { "tab_controls", new[] { "Controls", "مفاتيح التحكم", "کۆنتڕۆڵ" } },
        { "tab_sens", new[] { "Sensitivity", "الحساسية", "هەستیاری" } },
        { "tab_audio", new[] { "Audio", "الصوت", "دەنگ" } },
        { "tab_lang", new[] { "Language & Network", "اللغة والشبكة", "زمان و تۆڕ" } },
        { "sub_match", new[] { "Match Info", "معلومات المباراة", "زانیاریی یاری" } },
        { "sub_basic", new[] { "Basic Settings", "الإعدادات الأساسية", "ڕێکخستنە بنەڕەتییەکان" } },
        { "sub_graphics", new[] { "Graphics", "الرسومات", "گرافیک" } },
        { "sub_controls", new[] { "Control Layout", "تخطيط التحكم", "شێوازی کۆنتڕۆڵ" } },
        { "sub_camera", new[] { "Camera", "الكاميرا", "کامێرا" } },
        { "sub_sound", new[] { "Sound Settings", "إعدادات الصوت", "ڕێکخستنی دەنگ" } },
        { "sub_haptic", new[] { "Haptics", "الاهتزاز", "لەرینەوە" } },
        { "sub_language", new[] { "Language", "اللغة", "زمان" } },
        { "sub_network", new[] { "Network", "الشبكة", "تۆڕ" } },
        { "sec_player", new[] { "Player", "اللاعب", "یاریزان" } },
        { "row_name", new[] { "Player Name", "اسم اللاعب", "ناوی یاریزان" } },
        { "guest", new[] { "Guest", "ضيف", "میوان" } },
        { "sec_connection", new[] { "Connection", "الاتصال", "پەیوەندی" } },
        { "row_server", new[] { "Server", "الخادم", "سێرڤەر" } },
        { "hint_server", new[] { "Region of this match.", "منطقة هذه المباراة", "ناوچەی ئەم یارییە" } },
        { "row_room", new[] { "Room", "الغرفة", "ژوور" } },
        { "hint_room", new[] { "Players in this match.", "اللاعبون في المباراة", "یاریزانانی ئەم یارییە" } },
        { "offline", new[] { "Offline", "غير متصل", "پەیوەست نییە" } },
        { "noroom", new[] { "Not in a room", "لست في غرفة", "لە ژووردا نیت" } },
        { "europe", new[] { "Europe", "أوروبا", "ئەورووپا" } },
        { "leave", new[] { "Leave Match", "مغادرة المباراة", "جێهێشتنی یاری" } },
        { "hint_leave", new[] { "Leave the match and return to the lobby.", "اخرج من المباراة وارجع إلى الردهة", "یارییەکە جێبهێڵە و بگەڕێوە بۆ لۆبی" } },
        { "sec_hud", new[] { "HUD", "واجهة اللعب", "ڕووکاری یاری" } },
        { "row_fpscounter", new[] { "Show FPS Counter", "عرض عداد الإطارات", "پیشاندانی FPS" } },
        { "hint_fpscounter", new[] { "Real frame rate in the corner.", "المعدل الحقيقي في الزاوية", "ڕێژەی ڕاستەقینە لە گۆشەدا" } },
        { "row_transui", new[] { "Transparent UI Mode", "وضع الواجهة الشفافة", "ڕووکاری ڕوون" } },
        { "hint_transui", new[] { "See-through on-screen buttons.", "أزرار شاشة شفافة", "دوگمەکانی سەر شاشە ڕوون دەبن" } },
        { "sec_camera", new[] { "Camera", "الكاميرا", "کامێرا" } },
        { "row_cammotion", new[] { "Camera Motion", "حركة الكاميرا", "جووڵەی کامێرا" } },
        { "hint_cammotion", new[] { "Realistic head bob and breathing.", "اهتزاز الرأس والتنفس", "جووڵەی سەر و هەناسەی ڕاستەقینە" } },
        { "row_sway", new[] { "Flashlight Sway", "تمايل الكشاف", "جووڵەی لایت" } },
        { "hint_sway", new[] { "Flashlight follows the camera naturally.", "الكشاف يتبع الكاميرا بشكل طبيعي", "لایتەکە بە سروشتی شوێن کامێرا دەکەوێت" } },
        { "reset", new[] { "Reset", "إعادة ضبط", "ڕێکخستنەوە" } },
        { "hint_reset_basic", new[] { "Restore the default basic settings.", "استعادة الإعدادات الأساسية الافتراضية", "گەڕاندنەوەی ڕێکخستنە بنەڕەتییەکان" } },
        { "sec_gfxpref", new[] { "Graphics Preferences", "تفضيلات الرسومات", "هەڵبژاردنەکانی گرافیک" } },
        { "row_graphics", new[] { "Graphics", "الرسومات", "گرافیک" } },
        { "hint_graphics", new[] { "Lower it if the device gets hot.", "اخفضه إذا سخن الجهاز", "ئەگەر مۆبایل گەرم بوو کەمی بکەرەوە" } },
        { "lvl_0", new[] { "Smooth", "سلسة", "نەرم" } },
        { "lvl_1", new[] { "Balanced", "متوازنة", "هاوسەنگ" } },
        { "lvl_2", new[] { "HD", "عالية الدقة", "HD" } },
        { "lvl_3", new[] { "Ultra", "فائقة", "ئەڵترا" } },
        { "lvl_4", new[] { "Ultimate", "قصوى", "ئەوپەڕ" } },
        { "row_fps", new[] { "Frame Rate", "معدل الإطارات", "ڕێژەی فرەیم" } },
        { "fps_now", new[] { "Now {0} FPS. Grey = unsupported.", "الآن {0} FPS. الرمادي غير مدعوم", "ئێستا {0} FPS. خۆڵەمێشی پشتگیری ناکرێت" } },
        { "row_res", new[] { "Resolution", "الدقة", "ڕوونی" } },
        { "res_now", new[] { "Now {0}P. Lower runs faster.", "الآن {0}P. الأقل أسرع", "ئێستا {0}P. کەمتر خێراترە" } },
        { "sec_adv", new[] { "Advanced Graphics Settings", "إعدادات الرسومات المتقدمة", "ڕێکخستنی پێشکەوتووی گرافیک" } },
        { "row_aa", new[] { "Anti-aliasing", "تنعيم الحواف", "لووسکردنی لێوارەکان" } },
        { "hint_aa", new[] { "Smooths jagged edges. Costs performance.", "ينعم الحواف ويستهلك الأداء", "لێوارەکان لووس دەکات و کارایی کەم دەکات" } },
        { "disable", new[] { "Disable", "تعطيل", "ناچالاک" } },
        { "enable", new[] { "Enable", "تفعيل", "چالاک" } },
        { "row_shadows", new[] { "Shadows", "الظلال", "سێبەر" } },
        { "hint_shadows", new[] { "Real-time shadows from lights.", "ظلال فورية من الأضواء", "سێبەری ڕاستەوخۆی ڕووناکییەکان" } },
        { "sec_style", new[] { "Graphics Style", "نمط الرسوم", "ستایلی گرافیک" } },
        { "row_style", new[] { "Style", "النمط", "ستایل" } },
        { "hint_needs", new[] { "Needs Balanced or higher.", "يتطلب متوازنة أو أعلى", "پێویستی بە هاوسەنگ یان زیاترە" } },
        { "style_0", new[] { "Classic", "كلاسيكي", "کلاسیک" } },
        { "style_1", new[] { "Colorful", "ملون", "ڕەنگاوڕەنگ" } },
        { "style_2", new[] { "Realistic", "واقعي", "ڕاستەقینە" } },
        { "style_3", new[] { "Soft", "ناعم", "نەرم" } },
        { "style_4", new[] { "Movie", "سينمائي", "فیلم" } },
        { "sec_param", new[] { "Parameter Configuration", "تكوين المعايير", "ڕێکخستنی پێوەرەکان" } },
        { "row_bright", new[] { "Brightness", "السطوع", "ڕووناکی" } },
        { "sec_blind", new[] { "Colorblind Mode", "وضع عمى الألوان", "دۆخی کوێرەڕەنگی" } },
        { "row_mode", new[] { "Mode", "الوضع", "دۆخ" } },
        { "blind_0", new[] { "Normal", "عادي", "ئاسایی" } },
        { "blind_1", new[] { "Deuteranopia", "عمى الأخضر", "کوێری سەوز" } },
        { "blind_2", new[] { "Protanopia", "عمى الأحمر", "کوێری سوور" } },
        { "blind_3", new[] { "Tritanopia", "عمى الأزرق", "کوێری شین" } },
        { "reset_gfx", new[] { "Reset Graphics", "إعادة ضبط الرسومات", "ڕێکخستنەوەی گرافیک" } },
        { "hint_reset_gfx", new[] { "Restore the default graphics settings.", "استعادة إعدادات الرسومات الافتراضية", "گەڕاندنەوەی ڕێکخستنەکانی گرافیک" } },
        { "sec_layout", new[] { "Control Layout", "تخطيط التحكم", "شێوازی کۆنتڕۆڵ" } },
        { "lay_a1", new[] { "Left: Move", "يسار: حركة", "چەپ: جووڵە" } },
        { "lay_a2", new[] { "Right: Look and buttons", "يمين: الكاميرا والأزرار", "ڕاست: کامێرا و دوگمەکان" } },
        { "lay_b1", new[] { "Left: Look and buttons", "يسار: الكاميرا والأزرار", "چەپ: کامێرا و دوگمەکان" } },
        { "lay_b2", new[] { "Right: Move", "يمين: حركة", "ڕاست: جووڵە" } },
        { "move", new[] { "Move", "حركة", "جووڵە" } },
        { "look", new[] { "Look", "الكاميرا", "کامێرا" } },
        { "hint_reset_layout", new[] { "Restore the default control layout.", "استعادة تخطيط التحكم الافتراضي", "گەڕاندنەوەی شێوازی کۆنتڕۆڵ" } },
        { "sec_overall", new[] { "Overall", "الإجمالي", "گشتی" } },
        { "row_preset", new[] { "Preset", "الإعداد المسبق", "ئامادەکراو" } },
        { "sens_0", new[] { "Low", "منخفضة", "کەم" } },
        { "sens_1", new[] { "Medium", "متوسطة", "مامناوەند" } },
        { "sens_2", new[] { "High", "مرتفعة", "بەرز" } },
        { "sens_3", new[] { "Custom", "مخصص", "تایبەت" } },
        { "sec_camsens", new[] { "Camera Sensitivity (Free Look)", "حساسية الكاميرا (نظرة حرة)", "هەستیاریی کامێرا (سەیرکردنی ئازاد)" } },
        { "row_freelook", new[] { "Camera (Free Look)", "الكاميرا (نظرة حرة)", "کامێرا (سەیرکردنی ئازاد)" } },
        { "sec_gyro", new[] { "Gyroscope", "الجيروسكوب", "جایرۆسکۆپ" } },
        { "row_gyro", new[] { "Gyroscope", "الجيروسكوب", "جایرۆسکۆپ" } },
        { "hint_gyro", new[] { "Move the phone to look around.", "حرّك الهاتف لتحريك الكاميرا", "مۆبایلەکە بجوڵێنە بۆ جوڵاندنی کامێرا" } },
        { "row_gyroinv", new[] { "Invert Gyroscope", "عكس الجيروسكوب", "پێچەوانەکردنی جایرۆسکۆپ" } },
        { "hint_gyroinv", new[] { "Reverse the camera movement direction.", "عكس اتجاه حركة الكاميرا", "پێچەوانەکردنی ئاراستەی جوڵەی کامێرا" } },
        { "row_gyrosens", new[] { "Gyroscope Sensitivity", "حساسية الجيروسكوب", "هەستیاریی جایرۆسکۆپ" } },
        { "hint_reset_sens", new[] { "Restore the default sensitivity.", "استعادة الحساسية الافتراضية", "گەڕاندنەوەی هەستیاری" } },
        { "sec_volume", new[] { "Volume Controls", "عناصر التحكم بمستوى الصوت", "کۆنتڕۆڵی دەنگ" } },
        { "row_master", new[] { "Master", "الرئيسي", "گشتی" } },
        { "row_vol_steps", new[] { "Footsteps", "خطوات الأقدام", "دەنگی هەنگاو" } },
        { "row_vol_monsters", new[] { "Monsters", "الوحوش", "دڕندەکان" } },
        { "row_vol_effects", new[] { "Sound Effects", "المؤثرات الصوتية", "کاریگەرییە دەنگییەکان" } },
        { "row_vol_others", new[] { "Other Players", "اللاعبون الآخرون", "یاریزانەکانی تر" } },
        { "sec_soundopt", new[] { "Sound Options", "خيارات الصوت", "هەڵبژاردنەکانی دەنگ" } },
        { "row_3d", new[] { "Directional Sound", "الصوت الاتجاهي", "دەنگی ئاراستەیی" } },
        { "hint_3d", new[] { "Hear where sounds come from.", "اسمع مصدر الصوت", "بزانە دەنگ لە کوێوە دێت" } },
        { "row_menusound", new[] { "Game Sound in Menu", "صوت اللعبة في القائمة", "دەنگی یاری لە لیستەکەدا" } },
        { "hint_menusound", new[] { "Keep hearing the game here.", "استمر بسماع اللعبة", "لێرەش گوێت لە یارییەکە بێت" } },
        { "sec_vibration", new[] { "Vibration", "الاهتزاز", "لەرینەوە" } },
        { "row_vibdamage", new[] { "Vibrate on Damage", "اهتزاز عند الإصابة", "لەرینەوە لە کاتی برینداربوون" } },
        { "hint_vibdamage", new[] { "Phone vibrates when you get hit.", "يهتز الهاتف عند إصابتك", "مۆبایل دەلەرێتەوە کاتێک لێت دەدرێت" } },
        { "row_vibmonster", new[] { "Monster Nearby", "وحش قريب", "دڕندە نزیکە" } },
        { "hint_vibmonster", new[] { "Pulse when a monster is close.", "نبض عند اقتراب وحش", "لەرینەوە کاتێک دڕندە نزیک دەبێتەوە" } },
        { "hint_reset_audio", new[] { "Restore the default sound settings.", "استعادة إعدادات الصوت الافتراضية", "گەڕاندنەوەی ڕێکخستنەکانی دەنگ" } },
        { "sec_uilang", new[] { "Interface Language", "لغة الواجهة", "زمانی ڕووکار" } },
        { "row_uilang", new[] { "Interface Language", "لغة الواجهة", "زمانی ڕووکار" } },
        { "ok", new[] { "OK", "موافق", "باشە" } },
        { "row_pingcounter", new[] { "Show Ping on Screen", "عرض البنغ على الشاشة", "پیشاندانی پینگ لەسەر شاشە" } },
        { "hint_pingcounter", new[] { "Live connection delay in ms.", "تأخير الاتصال المباشر", "دواکەوتنی پەیوەندی بە ms" } },
        { "off", new[] { "Off", "إيقاف", "کوژاوە" } },
        { "on", new[] { "On", "تشغيل", "هەڵکراو" } }
    };
}
public static class PwRtl
{
    private static Dictionary<char, char[]> forms;
    private static void Init()
    {
        if (forms != null)
        {
            return;
        }
        forms = new Dictionary<char, char[]>();
        Add('\u0622', '\uFE82', '\0', '\0');
        Add('\u0623', '\uFE84', '\0', '\0');
        Add('\u0624', '\uFE86', '\0', '\0');
        Add('\u0625', '\uFE88', '\0', '\0');
        Add('\u0626', '\uFE8A', '\uFE8B', '\uFE8C');
        Add('\u0627', '\uFE8E', '\0', '\0');
        Add('\u0628', '\uFE90', '\uFE91', '\uFE92');
        Add('\u0629', '\uFE94', '\0', '\0');
        Add('\u062A', '\uFE96', '\uFE97', '\uFE98');
        Add('\u062B', '\uFE9A', '\uFE9B', '\uFE9C');
        Add('\u062C', '\uFE9E', '\uFE9F', '\uFEA0');
        Add('\u062D', '\uFEA2', '\uFEA3', '\uFEA4');
        Add('\u062E', '\uFEA6', '\uFEA7', '\uFEA8');
        Add('\u062F', '\uFEAA', '\0', '\0');
        Add('\u0630', '\uFEAC', '\0', '\0');
        Add('\u0631', '\uFEAE', '\0', '\0');
        Add('\u0632', '\uFEB0', '\0', '\0');
        Add('\u0633', '\uFEB2', '\uFEB3', '\uFEB4');
        Add('\u0634', '\uFEB6', '\uFEB7', '\uFEB8');
        Add('\u0635', '\uFEBA', '\uFEBB', '\uFEBC');
        Add('\u0636', '\uFEBE', '\uFEBF', '\uFEC0');
        Add('\u0637', '\uFEC2', '\uFEC3', '\uFEC4');
        Add('\u0638', '\uFEC6', '\uFEC7', '\uFEC8');
        Add('\u0639', '\uFECA', '\uFECB', '\uFECC');
        Add('\u063A', '\uFECE', '\uFECF', '\uFED0');
        Add('\u0641', '\uFED2', '\uFED3', '\uFED4');
        Add('\u0642', '\uFED6', '\uFED7', '\uFED8');
        Add('\u0643', '\uFEDA', '\uFEDB', '\uFEDC');
        Add('\u0644', '\uFEDE', '\uFEDF', '\uFEE0');
        Add('\u0645', '\uFEE2', '\uFEE3', '\uFEE4');
        Add('\u0646', '\uFEE6', '\uFEE7', '\uFEE8');
        Add('\u0647', '\uFEEA', '\u06BE', '\uFBAB');
        Add('\u0648', '\uFEEE', '\0', '\0');
        Add('\u0649', '\uFEF0', '\0', '\0');
        Add('\u064A', '\uFEF2', '\uFEF3', '\uFEF4');
        Add('\u067E', '\uFB57', '\uFB58', '\uFB59');
        Add('\u0686', '\uFB7B', '\uFB7C', '\uFB7D');
        Add('\u0695', '\uE001', '\0', '\0');
        Add('\u0698', '\uFB8B', '\0', '\0');
        Add('\u06A4', '\uFB6B', '\uFB6C', '\uFB6D');
        Add('\u06A9', '\uFB8F', '\uFB90', '\uFB91');
        Add('\u06AF', '\uFB93', '\uFB94', '\uFB95');
        Add('\u06B5', '\uE007', '\uE008', '\uE009');
        Add('\u06C6', '\uFBDA', '\0', '\0');
        Add('\u06CC', '\uFBFD', '\uFBFE', '\uFBFF');
        Add('\u06CE', '\uE004', '\uE005', '\uE006');
        Add('\u06D5', '\uE000', '\0', '\0');
    }
    private static void Add(char c, char fin, char ini, char med)
    {
        forms[c] = new[] { c, fin, ini, med };
    }
    private static int JoinType(char c)
    {
        if (c == '\u0640')
        {
            return 2;
        }
        char[] f;
        if (forms.TryGetValue(c, out f))
        {
            return f[2] != '\0' ? 2 : 1;
        }
        return 0;
    }
    private static bool IsMark(char c)
    {
        return (c >= '\u064B' && c <= '\u065F') || c == '\u0670' || c == '\u200C' || c == '\u200D';
    }
    private static char LamAlef(char lam, char alef, bool final)
    {
        if (lam == '\u06B5')
        {
            return alef == '\u0627' ? (final ? '\uE002' : '\uE003') : '\0';
        }
        switch (alef)
        {
            case '\u0622':
                return final ? '\uFEF6' : '\uFEF5';
            case '\u0623':
                return final ? '\uFEF8' : '\uFEF7';
            case '\u0625':
                return final ? '\uFEFA' : '\uFEF9';
            case '\u0627':
                return final ? '\uFEFC' : '\uFEFB';
        }
        return '\0';
    }
    public static string Shape(string input)
    {
        Init();
        StringBuilder clean = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            if (!IsMark(input[i]))
            {
                clean.Append(input[i]);
            }
        }
        string s = clean.ToString();
        StringBuilder sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char[] f;
            if (!forms.TryGetValue(c, out f))
            {
                sb.Append(c);
                continue;
            }
            bool prevJoins = i > 0 && JoinType(s[i - 1]) == 2;
            if ((c == '\u0644' || c == '\u06B5') && i + 1 < s.Length)
            {
                char lig = LamAlef(c, s[i + 1], prevJoins);
                if (lig != '\0')
                {
                    sb.Append(lig);
                    i++;
                    continue;
                }
            }
            bool nextJoins = JoinType(c) == 2 && i + 1 < s.Length && JoinType(s[i + 1]) >= 1;
            int idx = prevJoins ? (nextJoins ? 3 : 1) : (nextJoins ? 2 : 0);
            char outc = f[idx] != '\0' ? f[idx] : f[0];
            sb.Append(outc);
        }
        return sb.ToString();
    }
    private static bool IsRtl(char c)
    {
        return (c >= '\u0600' && c <= '\u06FF') || (c >= '\uE000' && c <= '\uE00F') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF');
    }
    private static char Mirror(char c)
    {
        switch (c)
        {
            case '(':
                return ')';
            case ')':
                return '(';
            case '[':
                return ']';
            case ']':
                return '[';
            case '<':
                return '>';
            case '>':
                return '<';
        }
        return c;
    }
    public static string Visual(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }
        string s = Shape(input);
        int n = s.Length;
        bool[] ltr = new bool[n];
        int[] strong = new int[n];
        for (int i = 0; i < n; i++)
        {
            char c = s[i];
            strong[i] = IsRtl(c) ? 2 : (char.IsLetterOrDigit(c) ? 1 : 0);
        }
        for (int i = 0; i < n; i++)
        {
            if (strong[i] == 1)
            {
                ltr[i] = true;
                continue;
            }
            if (strong[i] == 2)
            {
                continue;
            }
            int prev = 0;
            for (int k = i - 1; k >= 0; k--)
            {
                if (strong[k] != 0)
                {
                    prev = strong[k];
                    break;
                }
            }
            int next = 0;
            for (int k = i + 1; k < n; k++)
            {
                if (strong[k] != 0)
                {
                    next = strong[k];
                    break;
                }
            }
            ltr[i] = prev == 1 && next == 1;
        }
        StringBuilder sb = new StringBuilder(n);
        int end = n - 1;
        while (end >= 0)
        {
            int start = end;
            while (start - 1 >= 0 && ltr[start - 1] == ltr[end])
            {
                start--;
            }
            if (ltr[end])
            {
                sb.Append(s, start, end - start + 1);
            }
            else
            {
                for (int k = end; k >= start; k--)
                {
                    sb.Append(Mirror(s[k]));
                }
            }
            end = start - 1;
        }
        return sb.ToString();
    }
}
