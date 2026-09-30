using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class LobbyManager : MonoBehaviour
{
    private const string NameKey = "PlayerName";
    private const string MuteKey = "pw_lobbymute";
    private const string AudioKey = "pw_audio";
    public GameObject loadingPanel;
    public ServerBrowser serverBrowser;
    public GameObject namePanel;
    public GameObject nameCloseButton;
    public InputField nameInput;
    public Image nameInputBackground;
    public Text nameLabel;
    public Text namePreview;
    public TMP_Text serverNamePreview;
    public Text connectionText;
    public Image connectionDot;
    public GameObject exitPanel;
    public AudioSource musicSource;
    public AudioSource thunderSource;
    public Image soundButtonImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public RawImage fogBack;
    public RawImage fogFront;
    public Image flash;
    public Image vignette;
    public RectTransform emberArea;
    public RectTransform[] embers;
    public RectTransform[] drips;
    public RectTransform startButton;
    public RectTransform spinner;
    public RawImage characterView;
    public Transform stageModel;
    public Light lightningLight;
    public Light rimLight;
    private Image[] emberImages;
    private Vector2[] emberVelocity;
    private float[] emberLife;
    private float[] emberMaxLife;
    private float[] emberPhase;
    private Image[] dripImages;
    private float[] dripSpeed;
    private float[] dripOffset;
    private float nextFlash;
    private bool flashing;
    private bool dragging;
    private float lastPointerX;
    private float rimBase;
    private Color inputNormalColor;
    private Font rtlFont;
    private Font nameFont;
    private TMP_FontAsset rtlTmpFont;
    private bool rtlTmpTried;
    private string pendingNick;
    private float nextSlowUpdate;
    private int connectionState = -1;
    private readonly HashSet<TMP_Text> shapedLabels = new HashSet<TMP_Text>();
    private void Start()
    {
        string savedName = PlayerPrefs.GetString(NameKey, "");
        rtlFont = Resources.Load<Font>("Fonts/UniMahanBilal");
        if (nameLabel != null) nameFont = nameLabel.font;
        if (nameInputBackground != null) inputNormalColor = nameInputBackground.color;
        if (nameInput != null) nameInput.onValueChanged.AddListener(OnNameTyping);
        if (serverBrowser != null && serverBrowser.serverNameInput != null) serverBrowser.serverNameInput.onValueChanged.AddListener(OnServerNameTyping);
        if (namePreview != null) namePreview.gameObject.SetActive(false);
        if (serverNamePreview != null) serverNamePreview.gameObject.SetActive(false);
        if (namePanel != null) namePanel.SetActive(false);
        if (exitPanel != null) exitPanel.SetActive(false);
        if (string.IsNullOrEmpty(savedName))
        {
            OpenNamePanel();
        }
        else
        {
            SetSafeNickName(savedName);
            ShowName(savedName);
        }
        if (rimLight != null) rimBase = rimLight.intensity;
        if (lightningLight != null) lightningLight.intensity = 0f;
        if (flash != null) SetAlpha(flash, 0f);
        ApplyMute(PlayerPrefs.GetInt(MuteKey, 0) == 1);
        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying) musicSource.Play();
        SetupEmbers();
        SetupDrips();
        nextFlash = Time.unscaledTime + Random.Range(3f, 7f);
    }
    private void OnDisable()
    {
        flashing = false;
    }
    private void OnDestroy()
    {
        if (nameInput != null) nameInput.onValueChanged.RemoveListener(OnNameTyping);
        if (serverBrowser != null && serverBrowser.serverNameInput != null) serverBrowser.serverNameInput.onValueChanged.RemoveListener(OnServerNameTyping);
        AudioListener.volume = PlayerPrefs.GetFloat(AudioKey, 1f);
        if (rtlTmpFont != null) Destroy(rtlTmpFont);
    }
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;
        ScrollFog(fogBack, now, 0.012f);
        ScrollFog(fogFront, now, -0.022f);
        UpdateEmbers(dt, now);
        UpdateDrips(now);
        if (vignette != null) SetAlpha(vignette, 0.78f + 0.22f * Mathf.Sin(now * 1.3f));
        if (rimLight != null) rimLight.intensity = rimBase * (0.85f + 0.15f * Mathf.PerlinNoise(now * 2f, 0.3f));
        if (startButton != null) startButton.localScale = Vector3.one * (1f + Heartbeat(now) * 0.045f);
        if (spinner != null && spinner.gameObject.activeInHierarchy) spinner.Rotate(0f, 0f, -300f * dt);
        if (!flashing && now >= nextFlash) StartCoroutine(Lightning());
        UpdateDrag();
        if (now >= nextSlowUpdate)
        {
            nextSlowUpdate = now + 0.4f;
            UpdateConnection();
            ShapeServerItems();
            if (pendingNick != null) SetSafeNickName(pendingNick);
        }
    }
    private void UpdateConnection()
    {
        bool online = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
        int state = online ? 1 : 0;
        if (state == connectionState) return;
        connectionState = state;
        Color c = online ? new Color(0.45f, 0.9f, 0.4f, 1f) : new Color(0.95f, 0.22f, 0.18f, 1f);
        if (connectionText != null)
        {
            connectionText.text = online ? "Online" : "Offline";
            connectionText.color = c;
        }
        if (connectionDot != null) connectionDot.color = c;
    }
    private TMP_FontAsset RtlTmp()
    {
        if (!rtlTmpTried)
        {
            rtlTmpTried = true;
            if (rtlFont != null)
            {
                rtlTmpFont = TMP_FontAsset.CreateFontAsset(rtlFont);
                if (rtlTmpFont != null && TMP_Settings.defaultFontAsset != null)
                {
                    rtlTmpFont.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
                }
            }
        }
        return rtlTmpFont;
    }
    private void ShapeServerItems()
    {
        if (serverBrowser == null || serverBrowser.serverListContent == null) return;
        shapedLabels.RemoveWhere(l => l == null);
        Transform content = serverBrowser.serverListContent;
        for (int i = 0; i < content.childCount; i++)
        {
            TMP_Text label = content.GetChild(i).GetComponentInChildren<TMP_Text>(true);
            if (label == null || shapedLabels.Contains(label) || !HasRtl(label.text)) continue;
            TMP_FontAsset fa = RtlTmp();
            if (fa == null) continue;
            label.font = fa;
            label.text = PwRtl.Visual(label.text);
            shapedLabels.Add(label);
        }
    }
    private void OnNameTyping(string value)
    {
        if (nameInput == null || nameInput.textComponent == null) return;
        bool rtl = HasRtl(value);
        Color c = nameInput.textComponent.color;
        c.a = rtl && namePreview != null ? 0f : 1f;
        nameInput.textComponent.color = c;
        if (namePreview == null) return;
        namePreview.gameObject.SetActive(rtl);
        if (!rtl) return;
        if (rtlFont != null) namePreview.font = rtlFont;
        namePreview.text = PwRtl.Visual(value);
    }
    private void OnServerNameTyping(string value)
    {
        TMP_InputField field = serverBrowser != null ? serverBrowser.serverNameInput : null;
        if (field == null || field.textComponent == null) return;
        bool rtl = HasRtl(value);
        TMP_FontAsset fa = rtl ? RtlTmp() : null;
        bool usePreview = rtl && serverNamePreview != null && fa != null;
        if (fa != null) field.textComponent.font = fa;
        Color c = field.textComponent.color;
        c.a = usePreview ? 0f : 1f;
        field.textComponent.color = c;
        if (serverNamePreview == null) return;
        serverNamePreview.gameObject.SetActive(usePreview);
        if (!usePreview) return;
        serverNamePreview.font = fa;
        serverNamePreview.text = PwRtl.Visual(value);
    }
    private static bool HasRtl(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF')) return true;
        }
        return false;
    }
    private static float Heartbeat(float t)
    {
        float p = Mathf.Repeat(t, 1.4f);
        float a = Mathf.Exp(-Mathf.Pow((p - 0.1f) / 0.05f, 2f));
        float b = Mathf.Exp(-Mathf.Pow((p - 0.38f) / 0.06f, 2f)) * 0.6f;
        return a + b;
    }
    private static void ScrollFog(RawImage fog, float now, float speed)
    {
        if (fog == null) return;
        Rect r = fog.uvRect;
        r.x = Mathf.Repeat(now * speed, 1f);
        fog.uvRect = r;
    }
    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }
    private void SetupEmbers()
    {
        int count = embers != null ? embers.Length : 0;
        emberImages = new Image[count];
        emberVelocity = new Vector2[count];
        emberLife = new float[count];
        emberMaxLife = new float[count];
        emberPhase = new float[count];
        for (int i = 0; i < count; i++)
        {
            if (embers[i] == null) continue;
            emberImages[i] = embers[i].GetComponent<Image>();
            SpawnEmber(i, true);
        }
    }
    private void SpawnEmber(int i, bool anywhere)
    {
        if (emberArea == null) return;
        Rect area = emberArea.rect;
        float x = Random.Range(area.xMin, area.xMax);
        float y = anywhere ? Random.Range(area.yMin, area.yMax) : area.yMin - 20f;
        embers[i].anchoredPosition = new Vector2(x, y);
        float s = Random.Range(10f, 28f);
        embers[i].sizeDelta = new Vector2(s, s);
        emberVelocity[i] = new Vector2(Random.Range(-25f, 25f), Random.Range(45f, 120f));
        emberMaxLife[i] = Random.Range(5f, 11f);
        emberLife[i] = anywhere ? Random.Range(0f, emberMaxLife[i]) : emberMaxLife[i];
        emberPhase[i] = Random.Range(0f, 10f);
    }
    private void UpdateEmbers(float dt, float now)
    {
        if (embers == null || emberArea == null || emberImages == null) return;
        float top = emberArea.rect.yMax + 30f;
        for (int i = 0; i < embers.Length; i++)
        {
            RectTransform e = embers[i];
            if (e == null) continue;
            emberLife[i] -= dt;
            Vector2 p = e.anchoredPosition;
            p.x += (emberVelocity[i].x + Mathf.Sin(now * 1.7f + emberPhase[i]) * 30f) * dt;
            p.y += emberVelocity[i].y * dt;
            e.anchoredPosition = p;
            if (emberLife[i] <= 0f || p.y > top)
            {
                SpawnEmber(i, false);
                continue;
            }
            if (emberImages[i] != null)
            {
                float life = emberLife[i] / emberMaxLife[i];
                float fade = Mathf.Clamp01(life * 3f) * Mathf.Clamp01((1f - life) * 6f);
                float flicker = 0.6f + 0.4f * Mathf.PerlinNoise(now * 6f, emberPhase[i]);
                SetAlpha(emberImages[i], fade * flicker);
            }
        }
    }
    private void SetupDrips()
    {
        int count = drips != null ? drips.Length : 0;
        dripImages = new Image[count];
        dripSpeed = new float[count];
        dripOffset = new float[count];
        for (int i = 0; i < count; i++)
        {
            if (drips[i] == null) continue;
            dripImages[i] = drips[i].GetComponent<Image>();
            dripSpeed[i] = Random.Range(0.06f, 0.14f);
            dripOffset[i] = Random.Range(0f, 1f);
        }
    }
    private void UpdateDrips(float now)
    {
        if (drips == null || dripImages == null) return;
        for (int i = 0; i < drips.Length; i++)
        {
            if (drips[i] == null) continue;
            float p = Mathf.Repeat(now * dripSpeed[i] + dripOffset[i], 1f);
            float grow = Mathf.SmoothStep(0.15f, 1f, Mathf.Clamp01(p / 0.8f));
            drips[i].localScale = new Vector3(1f, grow, 1f);
            if (dripImages[i] != null) SetAlpha(dripImages[i], p < 0.8f ? 1f : 1f - (p - 0.8f) / 0.2f);
        }
    }
    private IEnumerator Lightning()
    {
        flashing = true;
        yield return Flash(0.75f, 0.12f);
        yield return new WaitForSecondsRealtime(0.07f);
        yield return Flash(0.45f, 0.35f);
        if (thunderSource != null && thunderSource.clip != null)
        {
            yield return new WaitForSecondsRealtime(Random.Range(0.3f, 1.2f));
            thunderSource.pitch = Random.Range(0.85f, 1.1f);
            thunderSource.volume = Random.Range(0.6f, 1f);
            thunderSource.Play();
        }
        nextFlash = Time.unscaledTime + Random.Range(7f, 15f);
        flashing = false;
    }
    private IEnumerator Flash(float strength, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            if (flash != null) SetAlpha(flash, strength * k);
            if (lightningLight != null) lightningLight.intensity = 6f * strength * k;
            yield return null;
        }
        if (flash != null) SetAlpha(flash, 0f);
        if (lightningLight != null) lightningLight.intensity = 0f;
    }
    private void UpdateDrag()
    {
        if (stageModel == null || characterView == null) return;
        if (!Input.GetMouseButton(0))
        {
            dragging = false;
            return;
        }
        Vector3 pos = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            dragging = !PanelOpen() && RectTransformUtility.RectangleContainsScreenPoint(characterView.rectTransform, pos, null);
            lastPointerX = pos.x;
            return;
        }
        if (!dragging) return;
        float dx = pos.x - lastPointerX;
        lastPointerX = pos.x;
        stageModel.Rotate(0f, -dx * 0.4f, 0f, Space.World);
    }
    private bool PanelOpen()
    {
        if (namePanel != null && namePanel.activeSelf) return true;
        if (exitPanel != null && exitPanel.activeSelf) return true;
        if (loadingPanel != null && loadingPanel.activeSelf) return true;
        if (serverBrowser != null && serverBrowser.serverPanel != null && serverBrowser.serverPanel.activeSelf) return true;
        return false;
    }
    private bool HasName()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(NameKey, ""));
    }
    public void OnStartPressed()
    {
        if (!HasName())
        {
            OpenNamePanel();
            return;
        }
        if (serverBrowser != null) serverBrowser.OpenServerPanel();
        else StartGame();
    }
    public void OnSinglePlayerPressed()
    {
        if (!HasName())
        {
            OpenNamePanel();
            return;
        }
        if (serverBrowser != null) serverBrowser.PlayOffline();
        else StartGame();
    }
    public void StartGame()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        SceneManager.LoadSceneAsync("Scene_A");
    }
    public void OpenNamePanel()
    {
        if (namePanel == null) return;
        namePanel.SetActive(true);
        if (nameCloseButton != null) nameCloseButton.SetActive(HasName());
        if (nameInput != null)
        {
            nameInput.text = PlayerPrefs.GetString(NameKey, "");
            OnNameTyping(nameInput.text);
        }
        if (nameInputBackground != null) nameInputBackground.color = inputNormalColor;
    }
    public void CloseNamePanel()
    {
        if (!HasName()) return;
        if (namePanel != null) namePanel.SetActive(false);
    }
    public void OnNameConfirmed()
    {
        string playerName = nameInput != null ? nameInput.text.Trim() : string.Empty;
        if (playerName.Length == 0)
        {
            if (nameInputBackground != null) nameInputBackground.color = new Color(0.65f, 0.1f, 0.1f, 0.95f);
            return;
        }
        PlayerPrefs.SetString(NameKey, playerName);
        PlayerPrefs.Save();
        SetSafeNickName(playerName);
        ShowName(playerName);
        if (namePanel != null) namePanel.SetActive(false);
    }
    private void ShowName(string playerName)
    {
        if (nameLabel == null) return;
        if (HasRtl(playerName))
        {
            if (rtlFont != null) nameLabel.font = rtlFont;
            nameLabel.text = PwRtl.Visual(playerName);
        }
        else
        {
            if (nameFont != null) nameLabel.font = nameFont;
            nameLabel.text = playerName;
        }
    }
    public void ToggleMute()
    {
        bool muted = PlayerPrefs.GetInt(MuteKey, 0) != 1;
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMute(muted);
    }
    private void ApplyMute(bool muted)
    {
        AudioListener.volume = muted ? 0f : PlayerPrefs.GetFloat(AudioKey, 1f);
        if (soundButtonImage != null)
        {
            Sprite s = muted ? soundOffSprite : soundOnSprite;
            if (s != null) soundButtonImage.sprite = s;
        }
    }
    public void AskExit()
    {
        if (exitPanel != null) exitPanel.SetActive(true);
    }
    public void CancelExit()
    {
        if (exitPanel != null) exitPanel.SetActive(false);
    }
    public void ConfirmExit()
    {
        Application.Quit();
    }
    private void SetSafeNickName(string name)
    {
        ClientState state = PhotonNetwork.NetworkClientState;
        if (PhotonNetwork.InRoom || state == ClientState.Disconnecting || state == ClientState.Leaving)
        {
            pendingNick = name;
            return;
        }
        pendingNick = null;
        PhotonNetwork.NickName = name;
    }
}
