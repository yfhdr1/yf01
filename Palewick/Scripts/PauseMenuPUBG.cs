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
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.18f;
    private static readonly string[] LevelKeys = { "low", "medium", "high", "ultra", "ultimate" };
    private static readonly string[] LevelNames = { "SMOOTH", "BALANCED", "HD", "ULTRA", "ULTIMATE" };
    private static readonly int[] FpsOptions = { 30, 45, 60, 90, 120, 144 };
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
    private readonly Button[] gfxButtons = new Button[5];
    private readonly Button[] fpsButtons = new Button[6];
    private GameObject fpsLabel;
    private bool gfxLayoutDone;
    private int frameCount;
    private float frameTime;
    private Slider sensitivitySlider;
    private Slider volumeSlider;
    private Slider brightnessSlider;
    private Image brightnessOverlay;
    private Color baseAmbient;
    private readonly Color tabOn = new Color(0.13f, 0.14f, 0.16f, 1f);
    private readonly Color tabOff = new Color(0.055f, 0.065f, 0.075f, 1f);
    private readonly Color gfxOn = new Color(0.95f, 0.66f, 0f, 1f);
    private readonly Color gfxOff = new Color(0.24f, 0.25f, 0.27f, 1f);
    private readonly Color textOn = new Color(0.95f, 0.66f, 0f, 1f);
    private readonly Color textOff = Color.white;
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
        ApplyFps(SupportedFps(PlayerPrefs.GetInt(FpsKey, 60)));
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
        BuildExtraGraphicsUI();
        sensitivitySlider = FindSlider("SensitivitySlider");
        volumeSlider = FindSlider("VolumeSlider");
        brightnessSlider = FindSlider("BrightnessSlider");
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
        BindButton(gfxButtons[0], () => SetGraphics(LevelKeys[0]));
        BindButton(gfxButtons[1], () => SetGraphics(LevelKeys[1]));
        BindButton(gfxButtons[2], () => SetGraphics(LevelKeys[2]));
        BindButton(gfxButtons[3], () => SetGraphics(LevelKeys[3]));
        BindButton(gfxButtons[4], () => SetGraphics(LevelKeys[4]));
        for (int i = 0; i < fpsButtons.Length; i++)
        {
            int fpsValue = FpsOptions[i];
            BindButton(fpsButtons[i], () => SetFps(fpsValue));
        }
        BindButton(FindButton("ExitButton"), ExitToLobby);
        BindButton(FindButton("ResumeButton"), () => SetPaused(false));
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
        AudioListener.volume = vol;
        ApplyBrightness(bright);
        string level = PlayerPrefs.GetString(GraphicsKey, LevelKeys[1]);
        ApplyGraphicsLevel(level);
        HighlightGfx(level);
        int fps = SupportedFps(PlayerPrefs.GetInt(FpsKey, 60));
        ApplyFps(fps);
        HighlightFps(fps);
    }
    private void BuildExtraGraphicsUI()
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
        SetLabel(ultimate, "ULTIMATE");
        gfxButtons[4] = ultimate.GetComponent<Button>();
        GameObject label = FindChild(page, "Label");
        if (label != null)
        {
            fpsLabel = FindChild(page, "FpsLabel");
            if (fpsLabel == null)
            {
                fpsLabel = Instantiate(label, page);
                fpsLabel.name = "FpsLabel";
            }
            SetLabel(fpsLabel, "FPS");
        }
        for (int i = 0; i < fpsButtons.Length; i++)
        {
            string fpsName = "Fps" + FpsOptions[i];
            GameObject go = FindChild(page, fpsName);
            if (go == null)
            {
                go = Instantiate(gfxButtons[0].gameObject, page);
                go.name = fpsName;
            }
            SetLabel(go, FpsOptions[i].ToString());
            fpsButtons[i] = go.GetComponent<Button>();
        }
    }
    private void LayoutGraphicsRows()
    {
        if (gfxLayoutDone || gfxButtons[0] == null || gfxButtons[1] == null || gfxButtons[3] == null)
        {
            return;
        }
        RectTransform r0 = gfxButtons[0].GetComponent<RectTransform>();
        RectTransform page = r0.parent as RectTransform;
        if (page == null || !page.gameObject.activeInHierarchy)
        {
            return;
        }
        Canvas.ForceUpdateCanvases();
        Rect b0 = LocalRect(r0, page);
        Rect b1 = LocalRect(gfxButtons[1].GetComponent<RectTransform>(), page);
        Rect b3 = LocalRect(gfxButtons[3].GetComponent<RectTransform>(), page);
        if (b0.width < 2f || b0.height < 2f)
        {
            return;
        }
        gfxLayoutDone = true;
        float w = b0.width;
        float h = b0.height;
        float gap = Mathf.Max(4f, b1.xMin - b0.xMax);
        float right = b3.xMax;
        float rowY = b0.center.y;
        float fpsY = rowY - h * 1.6f;
        PlaceRow(gfxButtons, right, w, h, gap, rowY);
        PlaceRow(fpsButtons, right, w, h, gap, fpsY);
        GameObject label = FindChild(page, "Label");
        if (fpsLabel != null && label != null)
        {
            RectTransform lr = fpsLabel.GetComponent<RectTransform>();
            RectTransform orig = label.GetComponent<RectTransform>();
            lr.localPosition = orig.localPosition + new Vector3(0f, fpsY - rowY, 0f);
        }
    }
    private static Rect LocalRect(RectTransform rt, RectTransform space)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        Vector3 a = space.InverseTransformPoint(c[0]);
        Vector3 b = space.InverseTransformPoint(c[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }
    private static void PlaceRow(Button[] row, float right, float w, float h, float gap, float y)
    {
        int count = row.Length;
        for (int i = 0; i < count; i++)
        {
            if (row[i] == null)
            {
                continue;
            }
            RectTransform rt = row[i].GetComponent<RectTransform>();
            float x = right - (count - 1 - i) * (w + gap) - w * 0.5f;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.localPosition = new Vector3(x, y, rt.localPosition.z);
        }
    }
    private static void SetLabel(GameObject go, string value)
    {
        Text t = go.GetComponentInChildren<Text>(true);
        if (t != null)
        {
            t.text = value;
        }
        TMP_Text tm = go.GetComponentInChildren<TMP_Text>(true);
        if (tm != null)
        {
            tm.text = value;
        }
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
            int measured = Mathf.RoundToInt(frameCount / frameTime);
            frameCount = 0;
            frameTime = 0f;
            if (fpsLabel != null && fpsLabel.activeInHierarchy)
            {
                SetLabel(fpsLabel, "FPS  " + measured);
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
                float s = opening ? Mathf.Lerp(0.92f, 1f, EaseOutBack(k)) : Mathf.Lerp(startScale, 0.96f, k);
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
            float end = opening ? 1f : 0.96f;
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
    private static float EaseOutBack(float k)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float f = k - 1f;
        return 1f + c3 * f * f * f + c1 * f * f;
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
            if (pages[i] != null)
            {
                pages[i].SetActive(i == index);
            }
            if (tabs[i] != null)
            {
                if (tabs[i].image != null)
                {
                    tabs[i].image.color = i == index ? tabOn : tabOff;
                }
                Text t = tabs[i].GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.color = i == index ? textOn : textOff;
                }
            }
        }
        if (index == 3)
        {
            LayoutGraphicsRows();
        }
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
    private static void ApplyFps(int fps)
    {
        QualitySettings.vSyncCount = 0;
        if (!Application.isEditor)
        {
            Resolution mode;
            if (FindRefreshFor(fps, out mode))
            {
                double current = Screen.currentResolution.refreshRateRatio.value;
                if (System.Math.Abs(current - mode.refreshRateRatio.value) > 0.5)
                {
                    Screen.SetResolution(Screen.width, Screen.height, FullScreenMode.FullScreenWindow, mode.refreshRateRatio);
                }
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
        ApplyFps(fps);
        HighlightFps(fps);
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
            fpsButtons[i].interactable = supported;
            SetButtonState(fpsButtons[i], FpsOptions[i] == fps);
            if (!supported && fpsButtons[i].image != null)
            {
                fpsButtons[i].image.color = new Color(gfxOff.r, gfxOff.g, gfxOff.b, 0.3f);
            }
        }
    }
    private void SetButtonState(Button button, bool on)
    {
        if (button == null)
        {
            return;
        }
        if (button.image != null)
        {
            button.image.color = on ? gfxOn : gfxOff;
        }
        Text t = button.GetComponentInChildren<Text>();
        if (t != null)
        {
            t.color = on ? textOn : textOff;
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
