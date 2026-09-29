using System.Collections;
using System.Collections.Generic;
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
    private const string TransUiKey = "pw_transui";
    private const string CamMotionKey = "pw_cammotion";
    private const string SwayKey = "pw_sway";
    private const string MenuSoundKey = "pw_menusound";
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.18f;
    private const float RowHeight = 84f;
    private const float SegHeight = 56f;
    private static readonly string[] LevelKeys = { "low", "medium", "high", "ultra", "ultimate" };
    private static readonly string[] LevelNames = { "SMOOTH", "BALANCED", "HD", "ULTRA", "ULTIMATE" };
    private static readonly string[] LevelTitles = { "Smooth", "Balanced", "HD", "Ultra", "Ultimate" };
    private static readonly string[] TabTitles = { "Account", "Basic", "Graphics", "Sensitivity", "Audio" };
    private static readonly string[] SubTitles = { "Match Info", "Basic Settings", "Graphics", "Camera", "Sound" };
    private static readonly string[] StyleTitles = { "Classic", "Colorful", "Realistic", "Soft", "Movie" };
    private static readonly float[,] StyleValues = { { 0f, 0f, 0f }, { 25f, 8f, 0f }, { -10f, 12f, -5f }, { -5f, -15f, 6f }, { -25f, 20f, -10f } };
    private static readonly string[] BlindTitles = { "Normal", "Deuteranopia", "Protanopia", "Tritanopia" };
    private static readonly float[,] BlindMatrix = { { 100f, 0f, 0f, 0f, 100f, 0f, 0f, 0f, 100f }, { 80f, 20f, 0f, 0f, 70f, 30f, 0f, 20f, 80f }, { 60f, 40f, 0f, 20f, 80f, 0f, 0f, 20f, 80f }, { 95f, 5f, 0f, 0f, 85f, 15f, 0f, 45f, 55f } };
    private static readonly int[] FpsOptions = { 30, 45, 60, 90, 120, 144, 165, 185 };
    private static readonly int[] ResSteps = { 720, 1080, 1440 };
    private static readonly float[] SensPresets = { 0.08f, 0.15f, 0.3f };
    private static int nativeLong;
    private static int nativeShort;
    private static int currentSlot = 1;
    private static int lastTab = 2;
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
    private float flickerTimer = 3f;
    private GameObject pausePanel;
    private CanvasGroup panelGroup;
    private RectTransform boxRect;
    private RectTransform root;
    private Coroutine animRoutine;
    private readonly List<GameObject> pageRoots = new List<GameObject>();
    private readonly List<GameObject> bottomBars = new List<GameObject>();
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<Image> tabFills = new List<Image>();
    private readonly List<Image> tabBars = new List<Image>();
    private readonly List<Text> tabTexts = new List<Text>();
    private readonly List<System.Action> refreshers = new List<System.Action>();
    private readonly List<System.Action> liveRefreshers = new List<System.Action>();
    private readonly List<int> resValues = new List<int>();
    private Text subTabText;
    private Text titleText;
    private Text fpsCounter;
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedOnLaunch()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(AudioKey, 1f);
        ApplyGraphicsLevel(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]));
        ApplyDisplay(SavedRes(), SupportedFps(PlayerPrefs.GetInt(FpsKey, 60)));
    }
    private void Start()
    {
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
        GameObject overlay = FindChild(transform.parent, "BrightnessOverlay");
        if (overlay != null)
        {
            brightnessOverlay = overlay.GetComponent<Image>();
            if (brightnessOverlay != null)
            {
                brightnessOverlay.raycastTarget = false;
            }
        }
        sensValue = PlayerPrefs.GetFloat(SensKey, 0.15f);
        volValue = PlayerPrefs.GetFloat(AudioKey, 1f);
        brightValue = PlayerPrefs.GetFloat(BrightKey, 1f);
        AudioListener.volume = volValue;
        ApplyBrightness(brightValue);
        ApplyGraphicsLevel(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]));
        resValues.Clear();
        resValues.AddRange(ResOptions());
        ApplyDisplay(SavedRes(), SupportedFps(PlayerPrefs.GetInt(FpsKey, 60)));
        BindButton(FindButtonInParent("PauseButton"), TogglePause);
        BuildMenu();
        BuildFpsCounter();
        ApplyTransparentUi();
        RefreshAll();
    }
    private void BuildMenu()
    {
        if (boxRect == null)
        {
            return;
        }
        Text anyText = GetComponentInChildren<Text>(true);
        uiFont = anyText != null && anyText.font != null ? anyText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        grungeSprite = MakeGrungeSprite();
        vignetteSprite = MakeVignetteSprite();
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
        Image vig = AddImage(NewRect("Vignette", root), new Color(1f, 1f, 1f, 1f));
        vig.sprite = vignetteSprite;
        Stretch(vig.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image side = AddImage(NewRect("SideBar", root), new Color(0.03f, 0.028f, 0.03f, 0.96f));
        Stretch(side.rectTransform, new Vector2(0.83f, 0f), Vector2.one, Vector2.zero, new Vector2(0f, -100f));
        Image sideEdge = AddImage(NewRect("SideEdge", side.rectTransform), new Color(0.25f, 0.05f, 0.06f, 1f));
        Stretch(sideEdge.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f));
        Image topLine = AddImage(NewRect("TopLine", root), new Color(0.22f, 0.2f, 0.2f, 1f));
        Stretch(topLine.rectTransform, new Vector2(0.02f, 1f), new Vector2(0.81f, 1f), new Vector2(0f, -94f), new Vector2(0f, -92f));
        Image sub = AddImage(NewRect("SubTab", root), new Color(0.2f, 0.18f, 0.18f, 1f));
        Place(sub.rectTransform, new Vector2(0.02f, 1f), new Vector2(0f, 1f), new Vector2(0f, -16f), new Vector2(300f, 74f));
        Image subLine = AddImage(NewRect("SubTabLine", sub.rectTransform), bloodBright);
        Stretch(subLine.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f));
        subTabText = MakeText(sub.rectTransform, "", 30, TextAnchor.MiddleCenter, bone, true);
        Stretch(subTabText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        titleText = MakeText(root, "SETTINGS", 40, TextAnchor.MiddleRight, bone, true);
        Place(titleText.rectTransform, Vector2.one, Vector2.one, new Vector2(-112f, -8f), new Vector2(340f, 86f));
        Image closeImg = AddImage(NewRect("CloseX", root), new Color(1f, 1f, 1f, 0.001f));
        closeImg.raycastTarget = true;
        Place(closeImg.rectTransform, Vector2.one, Vector2.one, new Vector2(-16f, -8f), new Vector2(86f, 86f));
        Button close = closeImg.gameObject.AddComponent<Button>();
        close.transition = Selectable.Transition.None;
        Text x = MakeText(closeImg.rectTransform, "X", 58, TextAnchor.MiddleCenter, bone, false);
        Stretch(x.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BindButton(close, () => SetPaused(false));
        for (int i = 0; i < TabTitles.Length; i++)
        {
            BuildTab(i);
        }
        BuildAccountPage();
        BuildBasicPage();
        BuildGraphicsPage();
        BuildSensitivityPage();
        BuildAudioPage();
    }
    private void BuildTab(int i)
    {
        RectTransform rt = NewRect("Tab" + TabTitles[i], root);
        float top = -104f - i * 92f;
        rt.anchorMin = new Vector2(0.83f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(2f, top - 88f);
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
        Text t = MakeText(rt, TabTitles[i], 30, TextAnchor.MiddleRight, ash, false);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-24f, 0f));
        int index = i;
        BindButton(b, () => SelectTab(index));
        tabButtons.Add(b);
        tabFills.Add(fill);
        tabBars.Add(bar);
        tabTexts.Add(t);
    }
    private RectTransform NewPage(string name)
    {
        RectTransform page = NewRect("Page" + name, root);
        Stretch(page, new Vector2(0.02f, 0f), new Vector2(0.81f, 1f), new Vector2(0f, 104f), new Vector2(0f, -106f));
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
        pageRoots.Add(page.gameObject);
        RectTransform bottom = NewRect("Bottom" + name, root);
        Stretch(bottom, new Vector2(0.02f, 0f), new Vector2(0.81f, 0f), new Vector2(0f, 16f), new Vector2(0f, 88f));
        bottomBars.Add(bottom.gameObject);
        return content;
    }
    private RectTransform Bottom(int page)
    {
        return bottomBars[page].GetComponent<RectTransform>();
    }
    private void BuildAccountPage()
    {
        RectTransform c = NewPage("Account");
        Section(c, "Player");
        InfoRow(c, "Player Name", "", () => string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Guest" : PhotonNetwork.NickName);
        Section(c, "Connection");
        InfoRow(c, "Server", "Photon region used for this match.", ServerText);
        InfoRow(c, "Room", "Players currently in this match.", RoomText);
        BottomButton(Bottom(0), "Leave Match", "Leave the match and return to the lobby.", ExitToLobby);
    }
    private void BuildBasicPage()
    {
        RectTransform c = NewPage("Basic");
        Section(c, "HUD");
        SegRow(c, "Show FPS Counter", "Shows the real frame rate in the corner.", new[] { "Off", "On" }, () => PlayerPrefs.GetInt(FpsCounterKey, 0), v => { SaveInt(FpsCounterKey, v); UpdateFpsCounterVisible(); }, null);
        SegRow(c, "Transparent UI Mode", "Makes the on-screen buttons see-through.", new[] { "Off", "On" }, () => PlayerPrefs.GetInt(TransUiKey, 0), v => { SaveInt(TransUiKey, v); ApplyTransparentUi(); }, null);
        Section(c, "Camera");
        SegRow(c, "Camera Motion", "Realistic head bob, breathing and landing shake.", new[] { "Off", "On" }, () => PlayerPrefs.GetInt(CamMotionKey, 1), v => { SaveInt(CamMotionKey, v); ApplyCameraToggles(); }, null);
        SegRow(c, "Flashlight Sway", "Flashlight follows the camera with a natural delay.", new[] { "Off", "On" }, () => PlayerPrefs.GetInt(SwayKey, 1), v => { SaveInt(SwayKey, v); ApplyCameraToggles(); }, null);
        BottomButton(Bottom(1), "Reset", "Restore the default basic settings.", ResetBasic);
    }
    private void BuildGraphicsPage()
    {
        RectTransform c = NewPage("Graphics");
        Section(c, "Graphics Preferences");
        SegRow(c, "Graphics", "Lower it if the device gets hot or the game becomes choppy.", LevelTitles, () => LevelSlot(PlayerPrefs.GetString(GraphicsKey, LevelKeys[1])), v => SetGraphics(LevelKeys[v]), null);
        string[] fpsNames = new string[FpsOptions.Length];
        for (int i = 0; i < FpsOptions.Length; i++)
        {
            fpsNames[i] = FpsOptions[i].ToString();
        }
        Text fpsLabel = SegRow(c, "Frame Rate", "", fpsNames, () => System.Array.IndexOf(FpsOptions, SupportedFps(PlayerPrefs.GetInt(FpsKey, 60))), v => SetFps(FpsOptions[v]), v => IsFpsSupported(FpsOptions[v]));
        liveRefreshers.Add(() => fpsLabel.text = LabelText("Frame Rate", (measuredFps > 0 ? "Now " + measuredFps + " FPS. " : "") + "Grey = unsupported."));
        string[] resNames = new string[resValues.Count];
        for (int i = 0; i < resValues.Count; i++)
        {
            resNames[i] = resValues[i] + "P";
        }
        Text resLabel = SegRow(c, "Resolution", "", resNames, () => resValues.IndexOf(SavedRes()), SetRes, null);
        liveRefreshers.Add(() => resLabel.text = LabelText("Resolution", "Now " + Mathf.Min(Screen.width, Screen.height) + "P. Lower runs faster."));
        Section(c, "Advanced Graphics Settings");
        SegRow(c, "Anti-aliasing", "Smooths jagged edges. Costs performance.", new[] { "Disable", "2x", "4x" }, AaIndex, v => { SaveInt(AaKey, v == 0 ? 0 : (v == 1 ? 2 : 4)); ReapplyQuality(); }, null);
        SegRow(c, "Shadows", "Real-time shadows from lights.", new[] { "Disable", "Enable" }, () => QualitySettings.shadows == ShadowQuality.Disable ? 0 : 1, v => { SaveInt(ShadowKey, v); ReapplyQuality(); }, null);
        Section(c, "Graphics Style");
        SegRow(c, "Style", "Color look of the world. Needs Balanced or higher.", StyleTitles, () => PlayerPrefs.GetInt(StyleKey, 0), v => { SaveInt(StyleKey, v); appliedStyle = -1; UpdatePostFX(); }, v => currentSlot > 0);
        Section(c, "Parameter Configuration");
        SliderRow(c, "Brightness", "", 0.5f, 1.5f, 0.05f, () => brightValue, v => { brightValue = v; SaveFloat(BrightKey, v); ApplyBrightness(v); }, v => Mathf.RoundToInt(v * 100f) + "%");
        Section(c, "Colorblind Mode");
        SegRow(c, "Mode", "Color filter for color vision deficiency. Needs Balanced or higher.", BlindTitles, () => PlayerPrefs.GetInt(BlindKey, 0), v => { SaveInt(BlindKey, v); appliedBlind = -1; UpdatePostFX(); }, v => currentSlot > 0);
        BottomButton(Bottom(2), "Reset Graphics", "Restore the default graphics settings.", ResetGraphics);
    }
    private void BuildSensitivityPage()
    {
        RectTransform c = NewPage("Sensitivity");
        Section(c, "Camera Sensitivity");
        SegRow(c, "Preset", "Quick sensitivity presets.", new[] { "Low", "Medium", "High" }, SensPresetIndex, v => SetSensitivity(SensPresets[v]), null);
        SliderRow(c, "Camera (Free Look)", "", 0.05f, 0.5f, 0.01f, () => sensValue, SetSensitivity, v => Mathf.RoundToInt(Mathf.InverseLerp(0.05f, 0.5f, v) * 100f) + "%");
        BottomButton(Bottom(3), "Reset", "Restore the default sensitivity.", () => SetSensitivity(0.15f));
    }
    private void BuildAudioPage()
    {
        RectTransform c = NewPage("Audio");
        Section(c, "Sound");
        SliderRow(c, "Master Volume", "", 0f, 1f, 0.05f, () => volValue, SetVolume, v => Mathf.RoundToInt(v * 100f) + "%");
        SegRow(c, "Game Sound in Menu", "Keep hearing the game while this menu is open.", new[] { "Off", "On" }, () => PlayerPrefs.GetInt(MenuSoundKey, 1), v => { SaveInt(MenuSoundKey, v); ApplyMenuSound(); }, null);
        BottomButton(Bottom(4), "Reset", "Restore the default sound settings.", () => { SetVolume(1f); SaveInt(MenuSoundKey, 1); ApplyMenuSound(); RefreshAll(); });
    }
    private void Section(RectTransform content, string title)
    {
        RectTransform rt = NewRect("Section", content);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 62f;
        le.minHeight = 62f;
        AddImage(rt, sectionColor);
        Image bar = AddImage(NewRect("Bar", rt), blood);
        Stretch(bar.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
        Text t = MakeText(rt, title, 30, TextAnchor.MiddleLeft, bone, true);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), Vector2.zero);
    }
    private RectTransform Row(RectTransform content, string label, string hint, out Text labelText)
    {
        RectTransform rt = NewRect("Row", content);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = RowHeight;
        le.minHeight = RowHeight;
        AddImage(rt, rowColor);
        labelText = MakeText(rt, LabelText(label, hint), 28, TextAnchor.MiddleLeft, bone, false);
        labelText.supportRichText = true;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.lineSpacing = 0.9f;
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), Vector2.zero);
        return rt;
    }
    private static string LabelText(string label, string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            return label;
        }
        return label + "  <size=20><color=#8A8480>(" + hint + ")</color></size>";
    }
    private Text SegRow(RectTransform content, string label, string hint, string[] options, System.Func<int> getSel, System.Action<int> onPick, System.Func<int, bool> isEnabled)
    {
        Text labelText;
        RectTransform row = Row(content, label, hint, out labelText);
        float w = options.Length > 5 ? 128f : (options.Length > 3 ? 168f : 176f);
        float total = w * options.Length;
        labelText.rectTransform.offsetMax = new Vector2(-(total + 40f), 0f);
        RectTransform group = NewRect("Options", row);
        Place(group, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(total, SegHeight));
        Seg[] segs = new Seg[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            RectTransform b = NewRect("Opt" + i, group);
            b.anchorMin = new Vector2(i / (float)options.Length, 0f);
            b.anchorMax = new Vector2((i + 1) / (float)options.Length, 1f);
            b.offsetMin = new Vector2(i == 0 ? 0f : -1f, 0f);
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
    private void SliderRow(RectTransform content, string label, string hint, float min, float max, float step, System.Func<float> getVal, System.Action<float> setVal, System.Func<float, string> format)
    {
        Text labelText;
        RectTransform row = Row(content, label, hint, out labelText);
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
        slider.direction = Slider.Direction.LeftToRight;
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
    private void InfoRow(RectTransform content, string label, string hint, System.Func<string> value)
    {
        Text labelText;
        RectTransform row = Row(content, label, hint, out labelText);
        Text v = MakeText(row, "", 28, TextAnchor.MiddleRight, bone, true);
        v.supportRichText = true;
        Stretch(v.rectTransform, new Vector2(0.45f, 0f), Vector2.one, Vector2.zero, new Vector2(-24f, 0f));
        liveRefreshers.Add(() => v.text = value());
    }
    private void BottomButton(RectTransform bar, string label, string hint, UnityAction action)
    {
        Image border = AddImage(NewRect("BottomBtn", bar), new Color(0.6f, 0.57f, 0.55f, 1f));
        border.raycastTarget = true;
        Place(border.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(260f, 66f));
        Image fill = AddImage(NewRect("Fill", border.rectTransform), new Color(0.06f, 0.055f, 0.06f, 1f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Text t = MakeText(border.rectTransform, label, 28, TextAnchor.MiddleCenter, bone, false);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button b = border.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        BindButton(b, action);
        Text h = MakeText(bar, "(" + hint + ")", 22, TextAnchor.MiddleLeft, ash, false);
        Stretch(h.rectTransform, Vector2.zero, Vector2.one, new Vector2(280f, 0f), Vector2.zero);
    }
    private string ServerText()
    {
        if (!PhotonNetwork.IsConnected)
        {
            return "<color=#8A8480>Offline</color>";
        }
        string region = string.IsNullOrEmpty(PhotonNetwork.CloudRegion) ? "-" : PhotonNetwork.CloudRegion.Replace("/*", "").ToUpper();
        if (region == "EU")
        {
            region = "Europe";
        }
        int ping = PhotonNetwork.GetPing();
        string col = ping < 120 ? "#3FBF5F" : (ping < 200 ? "#E0B040" : "#E04040");
        return region + "   <color=" + col + ">" + ping + " ms</color>";
    }
    private string RoomText()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return "<color=#8A8480>Not in a room</color>";
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
            if (Mathf.Abs(SensPresets[i] - sensValue) < 0.005f)
            {
                return i;
            }
        }
        return -1;
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
        UpdateFpsCounterVisible();
        ApplyTransparentUi();
        ApplyCameraToggles();
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
    private void BuildFpsCounter()
    {
        if (transform.parent == null)
        {
            return;
        }
        RectTransform rt = NewRect("FpsCounter", transform.parent);
        Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(260f, 44f));
        fpsCounter = MakeText(rt, "", 26, TextAnchor.MiddleLeft, new Color(0.35f, 1f, 0.45f, 1f), true);
        Stretch(fpsCounter.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Outline o = fpsCounter.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.8f);
        UpdateFpsCounterVisible();
    }
    private void UpdateFpsCounterVisible()
    {
        if (fpsCounter != null)
        {
            fpsCounter.transform.parent.gameObject.SetActive(PlayerPrefs.GetInt(FpsCounterKey, 0) == 1);
        }
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
    private void Update()
    {
        fxTimer -= Time.unscaledDeltaTime;
        if (fxTimer <= 0f)
        {
            fxTimer = 0.5f;
            UpdatePostFX();
            ApplyCameraToggles();
        }
        frameCount++;
        frameTime += Time.unscaledDeltaTime;
        if (frameTime >= 0.5f)
        {
            measuredFps = Mathf.RoundToInt(frameCount / frameTime);
            frameCount = 0;
            frameTime = 0f;
            if (fpsCounter != null && fpsCounter.gameObject.activeInHierarchy)
            {
                fpsCounter.text = measuredFps + " FPS";
            }
        }
        if (isOpen)
        {
            infoTimer -= Time.unscaledDeltaTime;
            if (infoTimer <= 0f)
            {
                infoTimer = 0.5f;
                for (int i = 0; i < liveRefreshers.Count; i++)
                {
                    liveRefreshers[i]();
                }
            }
            Flicker();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
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
        currentTab = Mathf.Clamp(index, 0, TabTitles.Length - 1);
        lastTab = currentTab;
        for (int i = 0; i < pageRoots.Count; i++)
        {
            bool on = i == currentTab;
            pageRoots[i].SetActive(on);
            bottomBars[i].SetActive(on);
            if (on)
            {
                ScrollRect sr = pageRoots[i].GetComponent<ScrollRect>();
                if (sr != null)
                {
                    sr.verticalNormalizedPosition = 1f;
                }
            }
        }
        for (int i = 0; i < tabButtons.Count; i++)
        {
            bool on = i == currentTab;
            tabFills[i].color = on ? new Color(0.55f, 0.06f, 0.08f, 1f) : new Color(0f, 0f, 0f, 0.001f);
            tabBars[i].enabled = on;
            tabTexts[i].color = on ? Color.white : ash;
            tabTexts[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }
        if (subTabText != null)
        {
            subTabText.text = SubTitles[currentTab];
        }
        RefreshAll();
    }
    private void SetGraphics(string level)
    {
        PlayerPrefs.SetString(GraphicsKey, level);
        PlayerPrefs.Save();
        ApplyGraphicsLevel(level);
        appliedStyle = -1;
        appliedBlind = -1;
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
        t.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
