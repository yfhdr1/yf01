using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using TMPro;
using Photon.Pun;
public class PauseMenuPUBG : MonoBehaviour
{
    private const string AudioKey = "pw_audio";
    private const string SensKey = "TouchSensitivity";
    private const string BrightKey = "pw_brightness2";
    private const string GraphicsKey = "pw_graphics";
    private const string FpsKey = "pw_fps";
    private const string ResKey = "pw_res";
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.18f;
    private const float RowH = 64f;
    private static readonly string[] LevelKeys = { "low", "medium", "high", "ultra", "ultimate" };
    private static readonly string[] LevelNames = { "SMOOTH", "BALANCED", "HD", "ULTRA", "ULTIMATE" };
    private static readonly string[] LevelTitles = { "Smooth", "Balanced", "HD", "Ultra", "Ultimate" };
    private static readonly string[] TabTitles = { "Controls", "Basic", "Audio", "Graphics" };
    private static readonly string[] SubTitles = { "Sensitivity", "Display", "Sound", "Graphics" };
    private static readonly int[] FpsOptions = { 30, 45, 60, 90, 120, 144, 165, 185 };
    private static readonly int[] ResSteps = { 720, 1080, 1440 };
    private static int nativeLong;
    private static int nativeShort;
    private static int currentSlot = 1;
    private PostProcessLayer ppLayer;
    private PostProcessVolume ppVolume;
    private int appliedFxSlot = -1;
    private float fxTimer;
    private GameObject pausePanel;
    private CanvasGroup panelGroup;
    private RectTransform boxRect;
    private Coroutine animRoutine;
    private readonly GameObject[] pages = new GameObject[4];
    private readonly Button[] tabs = new Button[4];
    private readonly Image[] tabAccents = new Image[4];
    private readonly Button[] gfxButtons = new Button[5];
    private readonly Button[] fpsButtons = new Button[8];
    private readonly Button[] resButtons = new Button[4];
    private readonly List<int> resValues = new List<int>();
    private readonly Dictionary<Button, Image> segFills = new Dictionary<Button, Image>();
    private Text fpsHeader;
    private Text resHeader;
    private Text subTabText;
    private Font uiFont;
    private int frameCount;
    private float frameTime;
    private int measuredFps;
    private Slider sensitivitySlider;
    private Slider volumeSlider;
    private Slider brightnessSlider;
    private Image brightnessOverlay;
    private Color baseAmbient;
    private readonly Color accent = new Color(0.86f, 0.13f, 0.16f, 1f);
    private readonly Color boxColor = new Color(0.075f, 0.08f, 0.09f, 0.97f);
    private readonly Color contentColor = new Color(0.12f, 0.125f, 0.14f, 0.92f);
    private readonly Color sideColor = new Color(0.045f, 0.05f, 0.06f, 1f);
    private readonly Color tabOn = new Color(0.24f, 0.06f, 0.08f, 1f);
    private readonly Color tabOff = new Color(0.065f, 0.07f, 0.085f, 1f);
    private readonly Color lineColor = new Color(0.2f, 0.21f, 0.24f, 1f);
    private readonly Color borderOff = new Color(0.42f, 0.44f, 0.48f, 1f);
    private readonly Color fillOff = new Color(0.09f, 0.095f, 0.11f, 0.95f);
    private readonly Color fillOn = new Color(0.5f, 0.05f, 0.08f, 0.95f);
    private readonly Color borderDis = new Color(0.2f, 0.21f, 0.23f, 1f);
    private readonly Color fillDis = new Color(0.055f, 0.06f, 0.07f, 0.9f);
    private readonly Color textOff = new Color(0.8f, 0.82f, 0.85f, 1f);
    private readonly Color textDis = new Color(0.3f, 0.31f, 0.34f, 1f);
    private readonly List<KeyValuePair<Button, UnityAction>> buttonBindings = new List<KeyValuePair<Button, UnityAction>>();
    private readonly List<KeyValuePair<Slider, UnityAction<float>>> sliderBindings = new List<KeyValuePair<Slider, UnityAction<float>>>();
    private readonly Dictionary<GameObject, bool> hudStates = new Dictionary<GameObject, bool>();
    private readonly List<Behaviour> lockedComponents = new List<Behaviour>();
    private bool isOpen;
    private bool leaving;
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
        pages[0] = FindChild(transform, "PageControls");
        pages[1] = FindChild(transform, "PageBasic");
        pages[2] = FindChild(transform, "PageAudio");
        pages[3] = FindChild(transform, "PageGraphics");
        tabs[0] = FindButton("TabControls");
        tabs[1] = FindButton("TabBasic");
        tabs[2] = FindButton("TabAudio");
        tabs[3] = FindButton("TabGraphics");
        gfxButtons[0] = FindButton("GfxLow");
        gfxButtons[1] = FindButton("GfxMedium");
        gfxButtons[2] = FindButton("GfxHigh");
        gfxButtons[3] = FindButton("GfxUltra");
        sensitivitySlider = FindSlider("SensitivitySlider");
        volumeSlider = FindSlider("VolumeSlider");
        brightnessSlider = FindSlider("BrightnessSlider");
        BuildExtraButtons();
        ApplySkin();
        GameObject overlay = FindChild(transform.parent, "BrightnessOverlay");
        if (overlay != null)
        {
            brightnessOverlay = overlay.GetComponent<Image>();
            if (brightnessOverlay != null)
            {
                brightnessOverlay.raycastTarget = false;
            }
        }
        BindButton(FindButtonInParent("PauseButton"), TogglePause);
        BindButton(tabs[0], () => SelectTab(0));
        BindButton(tabs[1], () => SelectTab(1));
        BindButton(tabs[2], () => SelectTab(2));
        BindButton(tabs[3], () => SelectTab(3));
        for (int i = 0; i < gfxButtons.Length; i++)
        {
            string key = LevelKeys[i];
            BindButton(gfxButtons[i], () => SetGraphics(key));
        }
        for (int i = 0; i < fpsButtons.Length; i++)
        {
            int fpsValue = FpsOptions[i];
            BindButton(fpsButtons[i], () => SetFps(fpsValue));
        }
        for (int i = 0; i < resButtons.Length; i++)
        {
            int resIndex = i;
            BindButton(resButtons[i], () => SetRes(resIndex));
        }
        BindButton(FindButton("ExitButton"), ExitToLobby);
        BindButton(FindButton("ResumeButton"), () => SetPaused(false));
        BindButton(FindButton("CloseX"), () => SetPaused(false));
        float sens = PlayerPrefs.GetFloat(SensKey, 0.15f);
        float vol = PlayerPrefs.GetFloat(AudioKey, 1f);
        float bright = PlayerPrefs.GetFloat(BrightKey, 1f);
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.05f;
            sensitivitySlider.maxValue = 0.5f;
            sensitivitySlider.value = sens;
            BindSlider(sensitivitySlider, SetSensitivity);
        }
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = vol;
            BindSlider(volumeSlider, SetVolume);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0.5f;
            brightnessSlider.maxValue = 1.5f;
            brightnessSlider.value = bright;
            BindSlider(brightnessSlider, SetBrightness);
        }
        SkinSliderValue(sensitivitySlider);
        SkinSliderValue(volumeSlider);
        SkinSliderValue(brightnessSlider);
        AudioListener.volume = vol;
        ApplyBrightness(bright);
        string level = PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]);
        ApplyGraphicsLevel(level);
        HighlightGfx(level);
        int fps = SupportedFps(PlayerPrefs.GetInt(FpsKey, 60));
        int res = SavedRes();
        ApplyDisplay(res, fps);
        HighlightFps(fps);
        HighlightRes(res);
        UpdateHeaders();
    }
    private void BuildExtraButtons()
    {
        if (gfxButtons[0] == null || gfxButtons[3] == null)
        {
            return;
        }
        Transform page = gfxButtons[0].transform.parent;
        LayoutGroup group = page.GetComponent<LayoutGroup>();
        if (group != null)
        {
            group.enabled = false;
        }
        GameObject ultimate = FindChild(page, "GfxUltimate");
        if (ultimate == null)
        {
            ultimate = Instantiate(gfxButtons[3].gameObject, page);
            ultimate.name = "GfxUltimate";
        }
        gfxButtons[4] = ultimate.GetComponent<Button>();
        for (int i = 0; i < fpsButtons.Length; i++)
        {
            string fpsName = "Fps" + FpsOptions[i];
            GameObject go = FindChild(page, fpsName);
            if (go == null)
            {
                go = Instantiate(gfxButtons[0].gameObject, page);
                go.name = fpsName;
            }
            fpsButtons[i] = go.GetComponent<Button>();
        }
        resValues.Clear();
        resValues.AddRange(ResOptions());
        for (int i = 0; i < resButtons.Length; i++)
        {
            string resName = "Res" + i;
            GameObject go = FindChild(page, resName);
            if (i >= resValues.Count)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
                resButtons[i] = null;
                continue;
            }
            if (go == null)
            {
                go = Instantiate(gfxButtons[0].gameObject, page);
                go.name = resName;
            }
            resButtons[i] = go.GetComponent<Button>();
        }
    }
    private void ApplySkin()
    {
        Text anyText = GetComponentInChildren<Text>(true);
        uiFont = anyText != null && anyText.font != null ? anyText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        HideChild("Separator");
        HideChild("TitleLine");
        HideChild("Watermark");
        HideChild("TitleText");
        HideChild("ResumeButton");
        GameObject backdrop = FindChild(transform, "Backdrop");
        if (backdrop != null)
        {
            Image bi = backdrop.GetComponent<Image>();
            if (bi != null)
            {
                bi.color = new Color(0f, 0f, 0f, 0.65f);
            }
        }
        if (boxRect == null)
        {
            return;
        }
        Stretch(boxRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image boxImg = boxRect.GetComponent<Image>();
        if (boxImg != null)
        {
            boxImg.sprite = null;
            boxImg.type = Image.Type.Simple;
            boxImg.color = boxColor;
        }
        RectTransform content = MakeImage("ContentBg", boxRect, contentColor);
        Stretch(content, new Vector2(0.02f, 0f), new Vector2(0.81f, 1f), new Vector2(0f, 110f), new Vector2(0f, -104f));
        content.SetAsFirstSibling();
        RectTransform side = MakeImage("SideBar", boxRect, sideColor);
        Stretch(side, new Vector2(0.83f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -106f));
        side.SetAsFirstSibling();
        RectTransform strip = MakeImage("SubLine", boxRect, lineColor);
        Stretch(strip, new Vector2(0.02f, 1f), new Vector2(0.81f, 1f), new Vector2(0f, -92f), new Vector2(0f, -90f));
        RectTransform sub = MakeImage("SubTab", boxRect, new Color(0.17f, 0.18f, 0.2f, 1f));
        Place(sub, new Vector2(0.02f, 1f), new Vector2(0f, 1f), new Vector2(0f, -14f), new Vector2(280f, 76f));
        RectTransform subLine = MakeImage("SubTabLine", sub, accent);
        Stretch(subLine, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f));
        subTabText = MakeText("SubTabText", sub, "", 30, TextAnchor.MiddleCenter, Color.white, true);
        Stretch(subTabText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text title = MakeText("SettingsTitle", boxRect, "Settings", 40, TextAnchor.MiddleRight, Color.white, true);
        Place(title.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-116f, -10f), new Vector2(320f, 86f));
        Button close = FindButton("CloseX");
        if (close == null)
        {
            RectTransform cr = MakeImage("CloseX", boxRect, new Color(1f, 1f, 1f, 0.001f));
            close = cr.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            Text x = MakeText("X", cr, "X", 54, TextAnchor.MiddleCenter, Color.white, false);
            Stretch(x.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
        Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -10f), new Vector2(86f, 86f));
        for (int i = 0; i < tabs.Length; i++)
        {
            SkinTab(i);
        }
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null)
            {
                continue;
            }
            RectTransform pr = pages[i].GetComponent<RectTransform>();
            Stretch(pr, new Vector2(0.02f, 0f), new Vector2(0.81f, 1f), new Vector2(30f, 130f), new Vector2(-30f, -124f));
            GameObject label = FindChild(pages[i].transform, "Label");
            if (label != null)
            {
                label.SetActive(false);
            }
        }
        SkinSliderPage(0, sensitivitySlider, "Sensitivity", "Camera look speed when dragging the screen.");
        SkinSliderPage(1, brightnessSlider, "Brightness", "Adjust if the game looks too dark or too bright.");
        SkinSliderPage(2, volumeSlider, "Volume", "Master volume for all game sounds.");
        if (pages[3] != null)
        {
            RectTransform gp = pages[3].GetComponent<RectTransform>();
            Text q = MakeHeader(gp, "QualityHeader", 0f);
            q.text = HeaderText("Graphics", "Lower the setting if the device becomes too hot or the game becomes choppy.");
            fpsHeader = MakeHeader(gp, "FpsHeader", -164f);
            resHeader = MakeHeader(gp, "ResHeader", -328f);
            for (int i = 0; i < gfxButtons.Length; i++)
            {
                SkinSegment(gfxButtons[i], LevelTitles[i], i * 0.14f, (i + 1) * 0.14f, -56f);
            }
            for (int i = 0; i < fpsButtons.Length; i++)
            {
                SkinSegment(fpsButtons[i], FpsOptions[i] + " FPS", i * 0.11f, (i + 1) * 0.11f, -220f);
            }
            for (int i = 0; i < resButtons.Length; i++)
            {
                if (resButtons[i] != null)
                {
                    SkinSegment(resButtons[i], resValues[i] + "P", i * 0.14f, (i + 1) * 0.14f, -384f);
                }
            }
        }
        Button exit = FindButton("ExitButton");
        if (exit != null)
        {
            RectTransform er = exit.GetComponent<RectTransform>();
            Place(er, new Vector2(0.02f, 0f), new Vector2(0f, 0f), new Vector2(0f, 24f), new Vector2(300f, 70f));
            SkinButtonBody(exit, "Leave Match", 28);
            PaintSegment(exit, new Color(0.75f, 0.77f, 0.8f, 1f), fillOff, Color.white, true);
            Text hint = MakeText("ExitHint", boxRect, "(Leave the match and return to the lobby.)", 22, TextAnchor.MiddleLeft, new Color(0.55f, 0.57f, 0.6f, 1f), false);
            Place(hint.rectTransform, new Vector2(0.02f, 0f), new Vector2(0f, 0f), new Vector2(320f, 24f), new Vector2(1000f, 70f));
        }
    }
    private void SkinTab(int i)
    {
        if (tabs[i] == null)
        {
            return;
        }
        RectTransform rt = tabs[i].GetComponent<RectTransform>();
        float top = -110f - i * 98f;
        rt.anchorMin = new Vector2(0.83f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, top - 96f);
        rt.offsetMax = new Vector2(0f, top);
        rt.localScale = Vector3.one;
        tabs[i].transition = Selectable.Transition.None;
        if (tabs[i].image != null)
        {
            tabs[i].image.sprite = null;
            tabs[i].image.type = Image.Type.Simple;
        }
        Transform old = rt.Find("TabAccent");
        RectTransform acc = old != null ? old as RectTransform : MakeImage("TabAccent", rt, accent);
        Stretch(acc, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(7f, 0f));
        tabAccents[i] = acc.GetComponent<Image>();
        if (rt.Find("TabLine") == null)
        {
            RectTransform line = MakeImage("TabLine", rt, new Color(0.13f, 0.14f, 0.16f, 1f));
            Stretch(line, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f));
        }
        Text t = tabs[i].GetComponentInChildren<Text>(true);
        if (t == null)
        {
            t = MakeText("Text", rt, "", 30, TextAnchor.MiddleCenter, textOff, false);
        }
        t.gameObject.SetActive(true);
        StyleText(t, TabTitles[i], 30, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), Vector2.zero);
        HideTmp(tabs[i].gameObject);
    }
    private void SkinSliderPage(int page, Slider slider, string title, string hint)
    {
        if (pages[page] == null)
        {
            return;
        }
        RectTransform pr = pages[page].GetComponent<RectTransform>();
        Text h = MakeHeader(pr, "Header", 0f);
        h.text = HeaderText(title, hint);
        if (slider == null)
        {
            return;
        }
        RectTransform sr = slider.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 1f);
        sr.anchorMax = new Vector2(0.62f, 1f);
        sr.pivot = new Vector2(0f, 1f);
        sr.offsetMin = new Vector2(0f, -124f);
        sr.offsetMax = new Vector2(0f, -90f);
        sr.localScale = Vector3.one;
        Image bg = slider.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = null;
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.22f, 0.23f, 0.26f, 1f);
        }
        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                fill.sprite = null;
                fill.type = Image.Type.Simple;
                fill.color = accent;
            }
        }
        if (slider.handleRect != null)
        {
            slider.handleRect.sizeDelta = new Vector2(30f, 24f);
            Image handle = slider.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = Color.white;
            }
        }
        slider.transition = Selectable.Transition.None;
        Text v = MakeText("Value", pr, "", 34, TextAnchor.MiddleLeft, Color.white, true);
        v.rectTransform.anchorMin = new Vector2(0.65f, 1f);
        v.rectTransform.anchorMax = new Vector2(0.85f, 1f);
        v.rectTransform.pivot = new Vector2(0f, 1f);
        v.rectTransform.offsetMin = new Vector2(0f, -137f);
        v.rectTransform.offsetMax = new Vector2(0f, -77f);
    }
    private void SkinSliderValue(Slider slider)
    {
        if (slider == null)
        {
            return;
        }
        Transform vt = slider.transform.parent.Find("Value");
        if (vt == null)
        {
            return;
        }
        Text v = vt.GetComponent<Text>();
        UnityAction<float> show = value => v.text = Mathf.RoundToInt(Mathf.InverseLerp(slider.minValue, slider.maxValue, value) * 100f) + "%";
        show(slider.value);
        BindSlider(slider, show);
    }
    private Text MakeHeader(RectTransform page, string name, float y)
    {
        Transform old = page.Find(name);
        Text h = old != null ? old.GetComponent<Text>() : MakeText(name, page, "", 30, TextAnchor.MiddleLeft, Color.white, false);
        h.supportRichText = true;
        h.rectTransform.anchorMin = new Vector2(0f, 1f);
        h.rectTransform.anchorMax = new Vector2(1f, 1f);
        h.rectTransform.pivot = new Vector2(0f, 1f);
        h.rectTransform.offsetMin = new Vector2(0f, y - 50f);
        h.rectTransform.offsetMax = new Vector2(0f, y);
        return h;
    }
    private static string HeaderText(string title, string hint)
    {
        return "<b>" + title + "</b>  <size=22><color=#8C9099>(" + hint + ")</color></size>";
    }
    private void UpdateHeaders()
    {
        if (fpsHeader != null)
        {
            string now = measuredFps > 0 ? "Current: " + measuredFps + " FPS. " : "";
            fpsHeader.text = HeaderText("Frame Rate", now + "Grey options are not supported by this screen.");
        }
        if (resHeader != null)
        {
            resHeader.text = HeaderText("Resolution", "Current: " + Mathf.Min(Screen.width, Screen.height) + "P. Lower resolution runs faster.");
        }
    }
    private void SkinSegment(Button b, string label, float x0, float x1, float top)
    {
        if (b == null)
        {
            return;
        }
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, 1f);
        rt.anchorMax = new Vector2(x1, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(-1f, top - RowH);
        rt.offsetMax = new Vector2(1f, top);
        rt.localScale = Vector3.one;
        SkinButtonBody(b, label, 26);
    }
    private void SkinButtonBody(Button b, string label, int size)
    {
        b.transition = Selectable.Transition.None;
        if (b.image != null)
        {
            b.image.sprite = null;
            b.image.type = Image.Type.Simple;
            b.image.preserveAspect = false;
            b.image.color = borderOff;
        }
        RectTransform rt = b.GetComponent<RectTransform>();
        Transform old = rt.Find("SegFill");
        RectTransform fill = old != null ? old as RectTransform : MakeImage("SegFill", rt, fillOff);
        Stretch(fill, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        fill.SetAsFirstSibling();
        segFills[b] = fill.GetComponent<Image>();
        Text t = b.GetComponentInChildren<Text>(true);
        if (t == null)
        {
            t = MakeText("Text", rt, "", size, TextAnchor.MiddleCenter, textOff, false);
        }
        t.gameObject.SetActive(true);
        StyleText(t, label, size, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        t.transform.SetAsLastSibling();
        HideTmp(b.gameObject);
    }
    private void StyleText(Text t, string value, int size, TextAnchor align)
    {
        t.text = value;
        t.font = uiFont;
        t.fontSize = size;
        t.alignment = align;
        t.resizeTextForBestFit = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.rectTransform.localScale = Vector3.one;
        Outline ol = t.GetComponent<Outline>();
        if (ol != null)
        {
            ol.enabled = false;
        }
        Shadow sh = t.GetComponent<Shadow>();
        if (sh != null)
        {
            sh.enabled = false;
        }
    }
    private static void HideTmp(GameObject go)
    {
        TMP_Text[] tms = go.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tms.Length; i++)
        {
            tms[i].gameObject.SetActive(false);
        }
    }
    private void HideChild(string name)
    {
        GameObject go = FindChild(transform, name);
        if (go != null)
        {
            go.SetActive(false);
        }
    }
    private RectTransform MakeImage(string name, Transform parent, Color color)
    {
        Transform old = parent.Find(name);
        if (old != null)
        {
            return old as RectTransform;
        }
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = name == "CloseX";
        return rt;
    }
    private Text MakeText(string name, Transform parent, string value, int size, TextAnchor align, Color color, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = parent.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        StyleText(t, value, size, align);
        t.color = color;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
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
    private void Update()
    {
        fxTimer -= Time.unscaledDeltaTime;
        if (fxTimer <= 0f)
        {
            fxTimer = 0.5f;
            UpdatePostFX();
        }
        frameCount++;
        frameTime += Time.unscaledDeltaTime;
        if (frameTime >= 0.5f)
        {
            measuredFps = Mathf.RoundToInt(frameCount / frameTime);
            frameCount = 0;
            frameTime = 0f;
            if (isOpen)
            {
                UpdateHeaders();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
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
            SelectTab(0);
            LockPlayer();
            animRoutine = StartCoroutine(Anim(true));
        }
        else
        {
            if (panelGroup != null)
            {
                panelGroup.blocksRaycasts = false;
            }
            UnlockPlayer();
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
                float s = opening ? Mathf.Lerp(0.97f, 1f, EaseOutCubic(k)) : Mathf.Lerp(startScale, 0.98f, k);
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
            float end = opening ? 1f : 0.98f;
            boxRect.localScale = new Vector3(end, end, 1f);
        }
        if (!opening)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
            SetHUDVisible(true);
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
                if (child == transform)
                {
                    continue;
                }
                if (child.name == "EventSystem" || child.name == "LoadingPanel" || child.name == "BrightnessOverlay")
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
    private void SelectTab(int index)
    {
        for (int i = 0; i < 4; i++)
        {
            bool on = i == index;
            if (pages[i] != null)
            {
                pages[i].SetActive(on);
            }
            if (tabs[i] == null)
            {
                continue;
            }
            if (tabs[i].image != null)
            {
                tabs[i].image.color = on ? tabOn : tabOff;
            }
            if (tabAccents[i] != null)
            {
                tabAccents[i].enabled = on;
            }
            Text t = tabs[i].GetComponentInChildren<Text>();
            if (t != null)
            {
                t.color = on ? Color.white : textOff;
                t.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            }
        }
        if (subTabText != null)
        {
            subTabText.text = SubTitles[Mathf.Clamp(index, 0, 3)];
        }
        UpdateHeaders();
    }
    private void SetGraphics(string level)
    {
        PlayerPrefs.SetString(GraphicsKey, level);
        PlayerPrefs.Save();
        ApplyGraphicsLevel(level);
        HighlightGfx(level);
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
        HighlightFps(fps);
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
        HighlightRes(res);
    }
    private void HighlightRes(int res)
    {
        for (int i = 0; i < resButtons.Length; i++)
        {
            if (resButtons[i] != null && i < resValues.Count)
            {
                SetButtonState(resButtons[i], resValues[i] == res);
            }
        }
    }
    private void HighlightFps(int fps)
    {
        for (int i = 0; i < fpsButtons.Length; i++)
        {
            if (fpsButtons[i] == null)
            {
                continue;
            }
            bool supported = IsFpsSupported(FpsOptions[i]);
            fpsButtons[i].gameObject.SetActive(true);
            fpsButtons[i].interactable = supported;
            if (supported)
            {
                SetButtonState(fpsButtons[i], FpsOptions[i] == fps);
            }
            else
            {
                PaintSegment(fpsButtons[i], borderDis, fillDis, textDis, false);
            }
        }
    }
    private void SetButtonState(Button button, bool on)
    {
        if (button == null)
        {
            return;
        }
        PaintSegment(button, on ? accent : borderOff, on ? fillOn : fillOff, on ? Color.white : textOff, on);
    }
    private void PaintSegment(Button button, Color border, Color fill, Color text, bool bold)
    {
        if (button.image != null)
        {
            button.image.color = border;
        }
        Image f;
        if (segFills.TryGetValue(button, out f) && f != null)
        {
            f.color = fill;
        }
        Text t = button.GetComponentInChildren<Text>();
        if (t != null)
        {
            t.color = text;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }
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
        }
        if (ppLayer == null || appliedFxSlot == currentSlot)
        {
            return;
        }
        appliedFxSlot = currentSlot;
        ppLayer.enabled = currentSlot > 0;
        if (ppVolume == null)
        {
            return;
        }
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
    private static void SetEffect<T>(PostProcessProfile profile, bool on) where T : PostProcessEffectSettings
    {
        T effect;
        if (profile.TryGetSettings(out effect))
        {
            effect.enabled.value = on;
        }
    }
    private void HighlightGfx(string level)
    {
        for (int i = 0; i < gfxButtons.Length; i++)
        {
            SetButtonState(gfxButtons[i], LevelKeys[i] == level);
        }
    }
    private void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SensKey, value);
        PlayerPrefs.Save();
        GameObject player = FindLocalPlayer();
        if (player == null)
        {
            return;
        }
        CharController_Motor motor = player.GetComponent<CharController_Motor>();
        if (motor != null)
        {
            motor.touchLookSensitivity = value;
        }
    }
    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(AudioKey, value);
        PlayerPrefs.Save();
    }
    private void SetBrightness(float value)
    {
        PlayerPrefs.SetFloat(BrightKey, value);
        PlayerPrefs.Save();
        ApplyBrightness(value);
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
        SceneManager.LoadScene("Scene_Lobby");
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
    private Button FindButton(string buttonName)
    {
        GameObject go = FindChild(transform, buttonName);
        return go != null ? go.GetComponent<Button>() : null;
    }
    private Button FindButtonInParent(string buttonName)
    {
        GameObject go = FindChild(transform.parent, buttonName);
        return go != null ? go.GetComponent<Button>() : null;
    }
    private Slider FindSlider(string sliderName)
    {
        GameObject go = FindChild(transform, sliderName);
        return go != null ? go.GetComponent<Slider>() : null;
    }
    private void OnDestroy()
    {
        RenderSettings.ambientLight = baseAmbient;
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
