using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
public class ChatSystem : MonoBehaviour, IOnEventCallback
{
    private const byte ChatEventCode = 42;
    private const int MaxHistory = 40;
    private const int MaxFloating = 3;
    private const int MaxChars = 80;
    private const int RowWrap = 24;
    private const float ShowTime = 6f;
    private const float FadeTime = 1f;
    private const float SendCooldown = 1f;
    private const float PanelWidth = 470f;
    private const float PanelHeight = 600f;
    private const float ColumnWidth = 92f;
    private const float BarHeight = 78f;
    private static readonly string[] HintLabels = { "Tap to type", "انقر للكتابة", "کرتە بکە بۆ نووسین" };
    private static readonly string[] TeamLabels = { "Team", "الفريق", "تیم" };
    private static readonly string[] EmptyLabels = { "No messages yet", "لا توجد رسائل بعد", "هیچ نامەیەک نییە" };
    private static readonly string[][] QuickMessages =
    {
        new[] { "Help!", "Monster here!", "Follow me!", "Run!", "Wait for me!", "Let's go!", "Thanks!", "Sorry!", "OK!", "No!" },
        new[] { "النجدة!", "الوحش هنا!", "اتبعني!", "اركض!", "انتظرني!", "هيا بنا!", "شكرًا!", "آسف!", "حسنًا!", "لا!" },
        new[] { "یارمەتی!", "دڕندەکە لێرەیە!", "بەدوامدا وەرە!", "ڕابکە!", "چاوەڕێم بکە!", "با بڕۆین!", "سوپاس!", "ببورە!", "باشە!", "نەخێر!" }
    };
    private static readonly Color[] TeamColors =
    {
        new Color(0.93f, 0.78f, 0.1f, 1f),
        new Color(0.9f, 0.45f, 0.12f, 1f),
        new Color(0.2f, 0.5f, 0.9f, 1f),
        new Color(0.35f, 0.7f, 0.2f, 1f)
    };
    private readonly Color yellow = new Color(0.98f, 0.8f, 0.12f, 1f);
    private readonly Color iconIdle = new Color(1f, 1f, 1f, 0.85f);
    private readonly Color separator = new Color(1f, 1f, 1f, 0.1f);
    private readonly List<GameObject> historyRows = new List<GameObject>();
    private readonly List<CanvasGroup> floatLines = new List<CanvasGroup>();
    private readonly List<float> floatTimes = new List<float>();
    private readonly List<GameObject> quickRows = new List<GameObject>();
    private readonly List<Button> boundButtons = new List<Button>();
    private Font latinFont;
    private Font rtlFont;
    private Sprite circleSprite;
    private Sprite bubbleSprite;
    private Sprite clockSprite;
    private Sprite sendSprite;
    private Sprite squareSprite;
    private Sprite panelSprite;
    private AudioClip popClip;
    private AudioSource audioSource;
    private CanvasGroup panelGroup;
    private Text badgeText;
    private Text emptyText;
    private Image sendImage;
    private int unread;
    private float openTime;
    private RectTransform floatBox;
    private GameObject iconObject;
    private Image chatGlyph;
    private GameObject closeGlyph;
    private GameObject badge;
    private GameObject panel;
    private RectTransform historyContent;
    private ScrollRect historyScroll;
    private GameObject historyView;
    private RectTransform quickContent;
    private GameObject quickView;
    private Image tabHistoryIcon;
    private Image tabQuickIcon;
    private Image tabHistoryMark;
    private Image tabQuickMark;
    private InputField input;
    private Text inputText;
    private Text preview;
    private Text placeholder;
    private Text teamText;
    private GameObject loadingPanel;
    private bool hiddenByLoading;
    private bool isOpen;
    private bool callbacksAdded;
    private bool scrollPending;
    private int currentTab;
    private float nextSendTime;
    private int builtLang = -1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || c.gameObject.scene != scene)
            {
                continue;
            }
            Transform menu = c.transform.Find("PubgPauseMenu");
            if (menu == null || c.transform.Find("ChatRoot") != null)
            {
                continue;
            }
            GameObject go = new GameObject("ChatRoot", typeof(RectTransform));
            go.layer = c.gameObject.layer;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(c.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            int index = menu.GetSiblingIndex();
            Transform loading = c.transform.Find("LoadingPanel");
            if (loading != null && loading.GetSiblingIndex() < index)
            {
                index = loading.GetSiblingIndex();
            }
            rt.SetSiblingIndex(index);
            go.AddComponent<ChatSystem>();
            return;
        }
    }
    private static int Lang()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt("pw_lang", 0), 0, 2);
    }
    private void OnEnable()
    {
        if (!callbacksAdded)
        {
            PhotonNetwork.AddCallbackTarget(this);
            callbacksAdded = true;
        }
    }
    private void OnDisable()
    {
        if (callbacksAdded)
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            callbacksAdded = false;
        }
    }
    private void OnDestroy()
    {
        if (callbacksAdded)
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            callbacksAdded = false;
        }
        for (int i = 0; i < boundButtons.Count; i++)
        {
            if (boundButtons[i] != null)
            {
                boundButtons[i].onClick.RemoveAllListeners();
            }
        }
        boundButtons.Clear();
        if (input != null)
        {
            input.onValueChanged.RemoveListener(OnTyping);
            input.onEndEdit.RemoveListener(OnEndEdit);
        }
        DestroySprite(circleSprite);
        DestroySprite(bubbleSprite);
        DestroySprite(clockSprite);
        DestroySprite(sendSprite);
        DestroySprite(squareSprite);
        DestroySprite(panelSprite);
        if (popClip != null)
        {
            Destroy(popClip);
        }
    }
    private static void DestroySprite(Sprite s)
    {
        if (s != null)
        {
            Destroy(s.texture);
            Destroy(s);
        }
    }
    private void Start()
    {
        latinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rtlFont = Resources.Load<Font>("Fonts/UniMahanBilal");
        if (rtlFont == null)
        {
            rtlFont = latinFont;
        }
        circleSprite = MakeRoundSprite(128, 64);
        squareSprite = MakeRoundSprite(32, 5);
        bubbleSprite = MakeBubbleIcon(128);
        clockSprite = MakeClockIcon(128);
        sendSprite = MakeSendIcon(128);
        panelSprite = MakeRoundSprite(64, 18);
        popClip = MakePop();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.35f;
        Build();
        SetOpen(false);
        if (transform.parent != null)
        {
            Transform lp = transform.parent.Find("LoadingPanel");
            loadingPanel = lp != null ? lp.gameObject : null;
        }
    }
    private void Bind(Button b, UnityEngine.Events.UnityAction a)
    {
        b.onClick.AddListener(a);
        boundButtons.Add(b);
    }
    private void Build()
    {
        RectTransform root = (RectTransform)transform;
        Image icon = AddImage(NewRect("ChatIcon", root), new Color(0.12f, 0.12f, 0.12f, 0.5f));
        icon.sprite = circleSprite;
        icon.raycastTarget = true;
        iconObject = icon.gameObject;
        Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-230f, -330f), new Vector2(100f, 100f));
        chatGlyph = AddImage(NewRect("Glyph", icon.rectTransform), Color.white);
        chatGlyph.sprite = bubbleSprite;
        Stretch(chatGlyph.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -24f));
        RectTransform x = NewRect("Close", icon.rectTransform);
        Stretch(x, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        for (int i = 0; i < 2; i++)
        {
            Image line = AddImage(NewRect("X" + i, x), Color.white);
            Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 5f));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
        }
        closeGlyph = x.gameObject;
        Image dot = AddImage(NewRect("Badge", icon.rectTransform), new Color(0.95f, 0.2f, 0.15f, 1f));
        dot.sprite = circleSprite;
        Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(34f, 34f));
        badgeText = MakeText(dot.rectTransform, "", 20, TextAnchor.MiddleCenter, Color.white);
        badgeText.fontStyle = FontStyle.Bold;
        Stretch(badgeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        badge = dot.gameObject;
        badge.SetActive(false);
        Button iconButton = icon.gameObject.AddComponent<Button>();
        Bind(iconButton, ToggleOpen);
        floatBox = NewRect("ChatFloat", root);
        Place(floatBox, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-290f, -290f), new Vector2(PanelWidth + 60f, 260f));
        VerticalLayoutGroup fv = floatBox.gameObject.AddComponent<VerticalLayoutGroup>();
        fv.childAlignment = TextAnchor.UpperRight;
        fv.childControlWidth = true;
        fv.childControlHeight = true;
        fv.childForceExpandWidth = true;
        fv.childForceExpandHeight = false;
        fv.spacing = 2f;
        Image bg = AddImage(NewRect("ChatPanel", root), new Color(0.02f, 0.02f, 0.03f, 0.78f));
        bg.sprite = panelSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = true;
        panel = bg.gameObject;
        panelGroup = panel.AddComponent<CanvasGroup>();
        Outline edge = panel.AddComponent<Outline>();
        edge.effectColor = new Color(1f, 1f, 1f, 0.08f);
        edge.effectDistance = new Vector2(1.5f, -1.5f);
        RectTransform prt = bg.rectTransform;
        Place(prt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -180f), new Vector2(PanelWidth, PanelHeight));
        Image column = AddImage(NewRect("Column", prt), new Color(0f, 0f, 0f, 0f));
        Stretch(column.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, BarHeight), new Vector2(ColumnWidth, 0f));
        Image colLine = AddImage(NewRect("Line", column.rectTransform), separator);
        Stretch(colLine.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero);
        tabQuickIcon = MakeTab(column.rectTransform, 0, bubbleSprite, out tabQuickMark);
        tabHistoryIcon = MakeTab(column.rectTransform, 1, clockSprite, out tabHistoryMark);
        RectTransform area = NewRect("Area", prt);
        Stretch(area, Vector2.zero, Vector2.one, new Vector2(ColumnWidth, BarHeight), Vector2.zero);
        historyView = MakeList(area, "History", out historyContent, out historyScroll);
        ScrollRect quickScroll;
        quickView = MakeList(area, "Quick", out quickContent, out quickScroll);
        emptyText = MakeText(historyView.transform, "", 23, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.4f));
        Stretch(emptyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image bar = AddImage(NewRect("Bar", prt), new Color(1f, 1f, 1f, 0.04f));
        Stretch(bar.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BarHeight));
        Image barLine = AddImage(NewRect("Line", bar.rectTransform), separator);
        Stretch(barLine.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -2f), Vector2.zero);
        teamText = MakeText(bar.rectTransform, "", 24, TextAnchor.MiddleCenter, yellow);
        Stretch(teamText.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(ColumnWidth, 0f));
        Image teamLine = AddImage(NewRect("Sep", bar.rectTransform), separator);
        Place(teamLine.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(ColumnWidth, 0f), new Vector2(2f, 44f));
        RectTransform field = NewRect("Field", bar.rectTransform);
        Stretch(field, Vector2.zero, Vector2.one, new Vector2(ColumnWidth + 10f, 8f), new Vector2(-78f, -8f));
        Image fieldHit = AddImage(field, new Color(1f, 1f, 1f, 0.001f));
        fieldHit.raycastTarget = true;
        field.gameObject.AddComponent<RectMask2D>();
        inputText = MakeText(field, "", 24, TextAnchor.MiddleLeft, Color.white);
        inputText.supportRichText = false;
        inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        Stretch(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        placeholder = MakeText(field, "", 23, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.45f));
        Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        preview = MakeText(field, "", 24, TextAnchor.MiddleRight, Color.white);
        preview.font = rtlFont;
        preview.supportRichText = false;
        Stretch(preview.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        input = field.gameObject.AddComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = MaxChars;
        input.lineType = InputField.LineType.SingleLine;
        input.shouldHideMobileInput = false;
        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.onValueChanged.AddListener(OnTyping);
        input.onEndEdit.AddListener(OnEndEdit);
        Image send = AddImage(NewRect("Send", bar.rectTransform), yellow);
        sendImage = send;
        send.sprite = sendSprite;
        send.raycastTarget = true;
        Place(send.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(52f, 52f));
        Button sendButton = send.gameObject.AddComponent<Button>();
        Bind(sendButton, Send);
        UpdateSendLook("");
        ApplyLanguage();
        SelectTab(1);
    }
    private Image MakeTab(RectTransform column, int index, Sprite sprite, out Image mark)
    {
        RectTransform tab = NewRect("Tab" + index, column);
        tab.anchorMin = new Vector2(0f, 1f);
        tab.anchorMax = new Vector2(1f, 1f);
        tab.pivot = new Vector2(0.5f, 1f);
        tab.offsetMin = new Vector2(0f, -(index + 1) * 96f);
        tab.offsetMax = new Vector2(-2f, -index * 96f);
        Image hit = AddImage(tab, new Color(1f, 1f, 1f, 0.001f));
        hit.raycastTarget = true;
        mark = AddImage(NewRect("Mark", tab), new Color(1f, 1f, 1f, 0.07f));
        Stretch(mark.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 6f), new Vector2(0f, -6f));
        Image accent = AddImage(NewRect("Accent", mark.rectTransform), yellow);
        Stretch(accent.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 10f), new Vector2(5f, -10f));
        Image ic = AddImage(NewRect("Icon", tab), iconIdle);
        ic.sprite = sprite;
        Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
        Button b = tab.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        int i = index;
        Bind(b, () => SelectTab(i));
        return ic;
    }
    private GameObject MakeList(RectTransform area, string name, out RectTransform content, out ScrollRect scroll)
    {
        RectTransform view = NewRect(name, area);
        Stretch(view, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image hit = AddImage(view, new Color(0f, 0f, 0f, 0.001f));
        hit.raycastTarget = true;
        view.gameObject.AddComponent<RectMask2D>();
        content = NewRect("Content", view);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VerticalLayoutGroup cv = content.gameObject.AddComponent<VerticalLayoutGroup>();
        cv.childAlignment = TextAnchor.UpperLeft;
        cv.childControlWidth = true;
        cv.childControlHeight = true;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;
        cv.spacing = 0f;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return view.gameObject;
    }
    private void SelectTab(int tab)
    {
        currentTab = tab;
        historyView.SetActive(tab == 1);
        quickView.SetActive(tab == 0);
        tabHistoryIcon.color = tab == 1 ? yellow : iconIdle;
        tabQuickIcon.color = tab == 0 ? yellow : iconIdle;
        tabHistoryMark.enabled = tab == 1;
        tabQuickMark.enabled = tab == 0;
        if (tab == 1)
        {
            scrollPending = true;
        }
    }
    private void ApplyLanguage()
    {
        int lang = Lang();
        builtLang = lang;
        Font f = lang == 0 ? latinFont : rtlFont;
        teamText.font = f;
        placeholder.font = f;
        teamText.text = lang == 0 ? TeamLabels[0] : PwRtl.Visual(TeamLabels[lang]);
        placeholder.text = lang == 0 ? HintLabels[0] : PwRtl.Visual(HintLabels[lang]);
        placeholder.alignment = lang == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        emptyText.font = f;
        emptyText.text = lang == 0 ? EmptyLabels[0] : PwRtl.Visual(EmptyLabels[lang]);
        emptyText.gameObject.SetActive(historyRows.Count == 0);
        for (int i = 0; i < quickRows.Count; i++)
        {
            if (quickRows[i] != null)
            {
                Destroy(quickRows[i]);
            }
        }
        quickRows.Clear();
        string[] list = QuickMessages[lang];
        for (int i = 0; i < list.Length; i++)
        {
            string msg = list[i];
            RectTransform row = NewRect("Quick" + i, quickContent);
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;
            le.minHeight = 60f;
            Image hit = AddImage(row, new Color(1f, 1f, 1f, 0.001f));
            hit.raycastTarget = true;
            Image line = AddImage(NewRect("Line", row), separator);
            Stretch(line.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(12f, 0f), new Vector2(-12f, 2f));
            Text t = MakeText(row, lang == 0 ? msg : PwRtl.Visual(msg), 25, lang == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, Color.white);
            t.font = f;
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            Button b = row.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.ColorTint;
            b.targetGraphic = hit;
            ColorBlock cb = b.colors;
            cb.pressedColor = new Color(1f, 1f, 1f, 0.2f);
            b.colors = cb;
            Bind(b, () => SendText(msg));
            quickRows.Add(row.gameObject);
        }
    }
    private void ToggleOpen()
    {
        SetOpen(!isOpen);
    }
    private void SetOpen(bool open)
    {
        isOpen = open;
        if (builtLang != Lang())
        {
            ApplyLanguage();
        }
        panel.SetActive(open);
        floatBox.gameObject.SetActive(!open);
        chatGlyph.enabled = !open;
        closeGlyph.SetActive(open);
        if (open)
        {
            unread = 0;
            badge.SetActive(false);
            scrollPending = true;
            openTime = Time.unscaledTime;
            panelGroup.alpha = 0f;
            panel.transform.localScale = new Vector3(0.94f, 0.94f, 1f);
        }
        else
        {
            input.DeactivateInputField();
        }
    }
    private void OnTyping(string value)
    {
        bool rtl = HasRtl(value);
        inputText.color = rtl ? new Color(0f, 0f, 0f, 0f) : Color.white;
        input.caretColor = rtl ? new Color(0f, 0f, 0f, 0f) : Color.white;
        preview.text = rtl ? PwRtl.Visual(value) : "";
        bool tooLong = rtl && preview.preferredWidth > preview.rectTransform.rect.width;
        preview.alignment = tooLong ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        UpdateSendLook(value);
    }
    private void UpdateSendLook(string value)
    {
        if (sendImage != null)
        {
            sendImage.color = Clean(value).Length > 0 ? yellow : new Color(yellow.r, yellow.g, yellow.b, 0.45f);
        }
    }
    private static AudioClip MakePop()
    {
        int rate = 44100;
        int count = rate / 8;
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 38f) * Mathf.Clamp01(t * 400f);
            float f = Mathf.Lerp(880f, 1320f, Mathf.Clamp01(t * 20f));
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.6f;
        }
        AudioClip clip = AudioClip.Create("ChatPop", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
    private void OnEndEdit(string value)
    {
        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        bool done = input.touchScreenKeyboard != null && input.touchScreenKeyboard.status == TouchScreenKeyboard.Status.Done;
        if (enter || done)
        {
            Send();
        }
    }
    private void Send()
    {
        if (input == null)
        {
            return;
        }
        if (SendText(input.text))
        {
            input.text = "";
            OnTyping("");
        }
    }
    private bool SendText(string raw)
    {
        string msg = Clean(raw);
        if (msg.Length == 0 || Time.unscaledTime < nextSendTime)
        {
            return false;
        }
        nextSendTime = Time.unscaledTime + SendCooldown;
        string nick = Clean(PhotonNetwork.NickName);
        if (nick.Length == 0)
        {
            nick = PhotonNetwork.LocalPlayer != null ? "Player " + PhotonNetwork.LocalPlayer.ActorNumber : "Player";
        }
        if (PhotonNetwork.InRoom)
        {
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(ChatEventCode, new object[] { nick, msg }, options, SendOptions.SendReliable);
        }
        else
        {
            AddMessage(nick, msg, 1);
        }
        SelectTab(1);
        return true;
    }
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != ChatEventCode)
        {
            return;
        }
        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length < 2)
        {
            return;
        }
        string nick = Clean(data[0] as string);
        string msg = Clean(data[1] as string);
        if (msg.Length == 0)
        {
            return;
        }
        AddMessage(nick, msg, TeamNumber(photonEvent.Sender));
        bool mine = PhotonNetwork.LocalPlayer != null && photonEvent.Sender == PhotonNetwork.LocalPlayer.ActorNumber;
        if (!mine)
        {
            if (!isOpen)
            {
                unread++;
                badgeText.text = unread > 9 ? "9+" : unread.ToString();
                badge.SetActive(true);
            }
            if (audioSource != null && popClip != null && !hiddenByLoading)
            {
                audioSource.PlayOneShot(popClip);
            }
        }
    }
    private static int TeamNumber(int actor)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList == null)
        {
            return 1;
        }
        List<int> ids = new List<int>();
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            ids.Add(players[i].ActorNumber);
        }
        ids.Sort();
        int index = ids.IndexOf(actor);
        return index < 0 ? 1 : index + 1;
    }
    private void AddMessage(string nick, string msg, int number)
    {
        if (historyContent == null)
        {
            return;
        }
        if (msg.Length > MaxChars)
        {
            msg = msg.Substring(0, MaxChars);
        }
        historyRows.Add(MakeRow(historyContent, nick, msg, number, false));
        while (historyRows.Count > MaxHistory)
        {
            if (historyRows[0] != null)
            {
                Destroy(historyRows[0]);
            }
            historyRows.RemoveAt(0);
        }
        GameObject f = MakeRow(floatBox, nick, msg, number, true);
        CanvasGroup g = f.AddComponent<CanvasGroup>();
        g.blocksRaycasts = false;
        g.interactable = false;
        floatLines.Add(g);
        floatTimes.Add(Time.unscaledTime);
        while (floatLines.Count > MaxFloating)
        {
            if (floatLines[0] != null)
            {
                Destroy(floatLines[0].gameObject);
            }
            floatLines.RemoveAt(0);
            floatTimes.RemoveAt(0);
        }
        emptyText.gameObject.SetActive(false);
        scrollPending = true;
    }
    private static string Hex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
    private static string Vis(string s)
    {
        return HasRtl(s) ? PwRtl.Visual(s) : s;
    }
    private GameObject MakeRow(RectTransform parent, string nick, string msg, int number, bool floating)
    {
        bool rtlUi = Lang() > 0;
        bool rightSide = floating || rtlUi;
        bool rtlMsg = HasRtl(msg);
        if (nick.Length > 14)
        {
            nick = nick.Substring(0, 14);
        }
        Color team = TeamColors[Mathf.Clamp(number - 1, 0, TeamColors.Length - 1)];
        string head = nick + ":";
        List<string> parts = Wrap(head + " " + msg, RowWrap);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }
            string line = parts[i];
            if (i == 0 && line.StartsWith(head))
            {
                string rest = line.Substring(head.Length).Trim();
                string name = "<color=#" + Hex(team) + ">" + (rtlMsg ? ":" + Vis(nick) : Vis(nick) + ":") + "</color>";
                if (rtlMsg)
                {
                    sb.Append(Vis(rest)).Append(' ').Append(name);
                }
                else
                {
                    sb.Append(name).Append(' ').Append(Vis(rest));
                }
            }
            else
            {
                sb.Append(Vis(line));
            }
        }
        float height = (floating ? 14f : 26f) + parts.Count * 30f;
        RectTransform row = NewRect("Msg", parent);
        LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        if (!floating)
        {
            Image line = AddImage(NewRect("Line", row), separator);
            Stretch(line.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(12f, 0f), new Vector2(-12f, 2f));
        }
        float top = floating ? -7f : -14f;
        Image sq = AddImage(NewRect("Num", row), team);
        sq.sprite = squareSprite;
        sq.type = Image.Type.Sliced;
        Place(sq.rectTransform, new Vector2(rightSide ? 1f : 0f, 1f), new Vector2(rightSide ? 1f : 0f, 1f), new Vector2(rightSide ? -14f : 14f, top), new Vector2(30f, 30f));
        Text num = MakeText(sq.rectTransform, number.ToString(), 21, TextAnchor.MiddleCenter, Color.white);
        num.fontStyle = FontStyle.Bold;
        Stretch(num.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Shadow ns = num.gameObject.AddComponent<Shadow>();
        ns.effectColor = new Color(0f, 0f, 0f, 0.5f);
        Text t = MakeText(row, sb.ToString(), 24, rightSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft, Color.white);
        t.font = rtlMsg || HasRtl(nick) ? rtlFont : latinFont;
        t.supportRichText = true;
        t.lineSpacing = 1f;
        if (rightSide)
        {
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-54f, top + 2f));
        }
        else
        {
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(54f, 0f), new Vector2(-12f, top + 2f));
        }
        if (!floating)
        {
            Shadow sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
            sh.effectDistance = new Vector2(2f, -2f);
        }
        return row.gameObject;
    }
    private void LateUpdate()
    {
        bool loading = loadingPanel != null && loadingPanel.activeInHierarchy;
        if (loading != hiddenByLoading)
        {
            hiddenByLoading = loading;
            if (loading)
            {
                SetOpen(false);
                floatBox.gameObject.SetActive(false);
            }
            iconObject.SetActive(!loading);
            if (!loading)
            {
                SetOpen(false);
            }
        }
        if (isOpen && panelGroup != null && panelGroup.alpha < 1f)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.12f);
            panelGroup.alpha = k;
            float sc = Mathf.Lerp(0.94f, 1f, k);
            panel.transform.localScale = new Vector3(sc, sc, 1f);
        }
        for (int i = floatLines.Count - 1; i >= 0; i--)
        {
            CanvasGroup g = floatLines[i];
            if (g == null)
            {
                continue;
            }
            float age = Time.unscaledTime - floatTimes[i];
            float a = Mathf.Min(Mathf.Clamp01(age / 0.2f), Mathf.Clamp01(1f - (age - ShowTime) / FadeTime));
            if (!Mathf.Approximately(g.alpha, a))
            {
                g.alpha = a;
            }
            if (a <= 0f && age > ShowTime)
            {
                Destroy(g.gameObject);
                floatLines.RemoveAt(i);
                floatTimes.RemoveAt(i);
            }
        }
        if (scrollPending && isOpen && currentTab == 1 && historyScroll != null)
        {
            scrollPending = false;
            Canvas.ForceUpdateCanvases();
            historyScroll.verticalNormalizedPosition = 0f;
        }
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(false);
        }
    }
    private static List<string> Wrap(string text, int max)
    {
        List<string> result = new List<string>();
        string[] words = text.Split(' ');
        StringBuilder line = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            string w = words[i];
            while (w.Length > max)
            {
                if (line.Length > 0)
                {
                    result.Add(line.ToString());
                    line.Length = 0;
                }
                result.Add(w.Substring(0, max));
                w = w.Substring(max);
            }
            if (line.Length > 0 && line.Length + 1 + w.Length > max)
            {
                result.Add(line.ToString());
                line.Length = 0;
            }
            if (line.Length > 0)
            {
                line.Append(' ');
            }
            line.Append(w);
        }
        if (line.Length > 0 || result.Count == 0)
        {
            result.Add(line.ToString());
        }
        return result;
    }
    private static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }
        StringBuilder sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '<' || c == '>' || c == '\n' || c == '\r' || c == '\t')
            {
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }
    private static bool HasRtl(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'))
            {
                return true;
            }
        }
        return false;
    }
    private static Texture2D NewTex(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
    private static Sprite MakeRoundSprite(int size, int radius)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        float b = Mathf.Min(radius, size / 2 - 1);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }
    private static float RoundRectAlpha(Vector2 p, Vector2 min, Vector2 max, float r)
    {
        float cx = Mathf.Clamp(p.x, min.x + r, max.x - r);
        float cy = Mathf.Clamp(p.y, min.y + r, max.y - r);
        float d = Vector2.Distance(p, new Vector2(cx, cy));
        return Mathf.Clamp01(r - d + 0.5f);
    }
    private static Sprite MakeBubbleIcon(int size)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        Vector2 min = new Vector2(size * 0.08f, size * 0.3f);
        Vector2 max = new Vector2(size * 0.92f, size * 0.88f);
        float r = size * 0.12f;
        Vector2 t0 = new Vector2(size * 0.22f, size * 0.32f);
        Vector2 t1 = new Vector2(size * 0.44f, size * 0.32f);
        Vector2 t2 = new Vector2(size * 0.24f, size * 0.12f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float a = RoundRectAlpha(p, min, max, r);
                if (InTriangle(p, t0, t1, t2))
                {
                    a = 1f;
                }
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 dc = new Vector2(size * 0.5f + k * size * 0.22f, size * 0.59f);
                    float dd = Vector2.Distance(p, dc);
                    float rr = size * 0.07f;
                    if (dd < rr + 1f)
                    {
                        a = Mathf.Min(a, Mathf.Clamp01(dd - rr));
                    }
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    private static Sprite MakeClockIcon(int size)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float outer = size * 0.46f;
        float inner = size * 0.36f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, c);
                float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                float w = size * 0.045f;
                if (Mathf.Abs(p.x - c.x) < w && p.y >= c.y - w && p.y <= c.y + size * 0.26f)
                {
                    a = 1f;
                }
                if (Mathf.Abs(p.y - c.y) < w && p.x >= c.x - w && p.x <= c.x + size * 0.2f)
                {
                    a = 1f;
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    private static Sprite MakeSendIcon(int size)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        Vector2 left = new Vector2(size * 0.06f, size * 0.5f);
        Vector2 tip = new Vector2(size * 0.94f, size * 0.9f);
        Vector2 bottom = new Vector2(size * 0.5f, size * 0.06f);
        Vector2 notch = new Vector2(size * 0.44f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = InTriangle(p, left, tip, notch) || InTriangle(p, bottom, tip, notch);
                px[y * size + x] = new Color(1f, 1f, 1f, inside ? 1f : 0f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
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
    private Text MakeText(Transform parent, string value, int size, TextAnchor align, Color color)
    {
        RectTransform rt = NewRect("Text", parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = latinFont;
        t.text = value;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
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
    }
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }
}
