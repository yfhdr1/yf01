using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
public class PauseMenuPUBG : MonoBehaviour
{
    private const string AudioKey = "pw_audio";
    private const string SensKey = "TouchSensitivity";
    private const string BrightKey = "pw_brightness2";
    private const string GraphicsKey = "pw_graphics";
    private const float OpenTime = 0.25f;
    private const float CloseTime = 0.18f;
    private static readonly string[] LevelKeys = { "low", "medium", "high", "ultra" };
    private static readonly string[] LevelNames = { "SMOOTH", "BALANCED", "HD", "ULTRA" };
    private static readonly float[] RenderScales = { 0.7f, 0.85f, 1f, 1f };
    private static int nativeWidth;
    private static int nativeHeight;
    private GameObject pausePanel;
    private CanvasGroup panelGroup;
    private RectTransform boxRect;
    private Coroutine animRoutine;
    private readonly GameObject[] pages = new GameObject[4];
    private readonly Button[] tabs = new Button[4];
    private readonly Button[] gfxButtons = new Button[4];
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
    private void Start()
    {
        if (nativeWidth == 0)
        {
            nativeWidth = Screen.width;
            nativeHeight = Screen.height;
        }
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
    }
    private void Update()
    {
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
    }
    private void SetGraphics(string level)
    {
        PlayerPrefs.SetString(GraphicsKey, level);
        PlayerPrefs.Save();
        ApplyGraphicsLevel(level);
        HighlightGfx(level);
    }
    private int LevelSlot(string level)
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
    private void ApplyGraphicsLevel(string level)
    {
        int slot = LevelSlot(level);
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
        Application.targetFrameRate = 60;
        ApplyRenderScale(RenderScales[slot]);
    }
    private void ApplyRenderScale(float scale)
    {
        if (Application.isEditor || nativeWidth <= 0 || nativeHeight <= 0)
        {
            return;
        }
        int w = Mathf.Max(320, Mathf.RoundToInt(nativeWidth * scale));
        int h = Mathf.Max(240, Mathf.RoundToInt(nativeHeight * scale));
        if (Screen.width != w || Screen.height != h)
        {
            Screen.SetResolution(w, h, true);
        }
    }
    private void HighlightGfx(string level)
    {
        for (int i = 0; i < 4; i++)
        {
            if (gfxButtons[i] != null)
            {
                bool on = LevelKeys[i] == level;
                if (gfxButtons[i].image != null)
                {
                    gfxButtons[i].image.color = on ? gfxOn : gfxOff;
                }
                Text t = gfxButtons[i].GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.color = on ? textOn : textOff;
                }
            }
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
