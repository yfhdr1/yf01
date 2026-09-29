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
    private const int MaxFloating = 4;
    private const int MaxChars = 80;
    private const int BubbleWrap = 30;
    private const int FloatWrap = 38;
    private const float ShowTime = 8f;
    private const float FadeTime = 1.5f;
    private const float SendCooldown = 1f;
    private const float PanelWidth = 760f;
    private static readonly string[] TitleLabels = { "Chat", "الدردشة", "چات" };
    private static readonly string[] SendLabels = { "Send", "إرسال", "ناردن" };
    private static readonly string[] HintLabels = { "Enter message", "اكتب رسالة", "نامەیەک بنووسە" };
    private readonly Color blood = new Color(0.78f, 0.06f, 0.08f, 1f);
    private readonly Color bone = new Color(0.9f, 0.87f, 0.82f, 1f);
    private readonly Color ash = new Color(0.6f, 0.58f, 0.56f, 1f);
    private readonly List<GameObject> historyRows = new List<GameObject>();
    private readonly List<CanvasGroup> floatLines = new List<CanvasGroup>();
    private readonly List<float> floatTimes = new List<float>();
    private Font latinFont;
    private Font rtlFont;
    private Sprite roundSprite;
    private Sprite circleSprite;
    private Sprite bubbleSprite;
    private RectTransform floatBox;
    private GameObject iconObj;
    private GameObject badge;
    private GameObject panel;
    private RectTransform content;
    private ScrollRect scroll;
    private InputField input;
    private Text inputText;
    private Text preview;
    private Text placeholder;
    private Text titleText;
    private Text sendText;
    private Button iconButton;
    private Button sendButton;
    private Button collapseButton;
    private bool isOpen;
    private bool callbacksAdded;
    private bool scrollPending;
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
            rt.SetSiblingIndex(menu.GetSiblingIndex());
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
        if (iconButton != null)
        {
            iconButton.onClick.RemoveListener(Open);
        }
        if (collapseButton != null)
        {
            collapseButton.onClick.RemoveListener(Close);
        }
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(Send);
        }
        if (input != null)
        {
            input.onValueChanged.RemoveListener(OnTyping);
            input.onEndEdit.RemoveListener(OnEndEdit);
        }
        DestroySprite(roundSprite);
        DestroySprite(circleSprite);
        DestroySprite(bubbleSprite);
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
        roundSprite = MakeRoundSprite(64, 22);
        circleSprite = MakeRoundSprite(128, 64);
        bubbleSprite = MakeBubbleIcon(128);
        Build();
        SetOpen(false);
    }
    private void Build()
    {
        RectTransform root = (RectTransform)transform;
        floatBox = NewRect("ChatFloat", root);
        Place(floatBox, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(28f, 10f), new Vector2(700f, 230f));
        VerticalLayoutGroup fv = floatBox.gameObject.AddComponent<VerticalLayoutGroup>();
        fv.childAlignment = TextAnchor.LowerLeft;
        fv.childControlWidth = true;
        fv.childControlHeight = true;
        fv.childForceExpandWidth = false;
        fv.childForceExpandHeight = false;
        fv.spacing = 6f;
        Image icon = AddImage(NewRect("ChatIcon", root), new Color(0.78f, 0.06f, 0.08f, 0.8f));
        icon.sprite = circleSprite;
        icon.raycastTarget = true;
        iconObj = icon.gameObject;
        Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(80f, -200f), new Vector2(96f, 96f));
        Image inner = AddImage(NewRect("Inner", icon.rectTransform), new Color(0.03f, 0.025f, 0.03f, 0.85f));
        inner.sprite = circleSprite;
        Stretch(inner.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Image glyph = AddImage(NewRect("Glyph", icon.rectTransform), bone);
        glyph.sprite = bubbleSprite;
        Stretch(glyph.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -18f));
        Image dot = AddImage(NewRect("Badge", icon.rectTransform), new Color(1f, 0.15f, 0.15f, 1f));
        dot.sprite = circleSprite;
        Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(26f, 26f));
        badge = dot.gameObject;
        badge.SetActive(false);
        iconButton = icon.gameObject.AddComponent<Button>();
        iconButton.onClick.AddListener(Open);
        Image bg = AddImage(NewRect("ChatPanel", root), new Color(0.02f, 0.018f, 0.022f, 0.88f));
        bg.raycastTarget = true;
        panel = bg.gameObject;
        RectTransform prt = bg.rectTransform;
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 0.5f);
        prt.sizeDelta = new Vector2(PanelWidth, 0f);
        prt.anchoredPosition = Vector2.zero;
        Image edge = AddImage(NewRect("Edge", prt), new Color(0.45f, 0.05f, 0.07f, 1f));
        Stretch(edge.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(-3f, 0f), Vector2.zero);
        Image head = AddImage(NewRect("Header", prt), new Color(0.07f, 0.06f, 0.065f, 1f));
        Stretch(head.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -86f), new Vector2(-3f, 0f));
        Image headLine = AddImage(NewRect("Line", head.rectTransform), blood);
        Stretch(headLine.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 3f));
        titleText = MakeText(head.rectTransform, "", 34, TextAnchor.MiddleCenter, bone);
        titleText.fontStyle = FontStyle.Bold;
        Stretch(titleText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform view = NewRect("Messages", prt);
        Stretch(view, Vector2.zero, Vector2.one, new Vector2(0f, 112f), new Vector2(-3f, -90f));
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
        cv.spacing = 10f;
        cv.padding = new RectOffset(0, 0, 14, 14);
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        Image bar = AddImage(NewRect("InputBar", prt), new Color(0.06f, 0.055f, 0.06f, 1f));
        Stretch(bar.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(-3f, 108f));
        Image fieldBg = AddImage(NewRect("Field", bar.rectTransform), new Color(0.15f, 0.14f, 0.14f, 1f));
        fieldBg.sprite = roundSprite;
        fieldBg.type = Image.Type.Sliced;
        fieldBg.raycastTarget = true;
        Stretch(fieldBg.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 18f), new Vector2(-196f, -18f));
        inputText = MakeText(fieldBg.rectTransform, "", 28, TextAnchor.MiddleLeft, bone);
        inputText.supportRichText = false;
        inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        Stretch(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
        placeholder = MakeText(fieldBg.rectTransform, "", 26, TextAnchor.MiddleLeft, ash);
        Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
        preview = MakeText(fieldBg.rectTransform, "", 28, TextAnchor.MiddleRight, bone);
        preview.font = rtlFont;
        preview.supportRichText = false;
        Stretch(preview.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
        input = fieldBg.gameObject.AddComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = MaxChars;
        input.lineType = InputField.LineType.SingleLine;
        input.shouldHideMobileInput = false;
        input.customCaretColor = true;
        input.caretColor = bone;
        input.onValueChanged.AddListener(OnTyping);
        input.onEndEdit.AddListener(OnEndEdit);
        Image sendBg = AddImage(NewRect("Send", bar.rectTransform), blood);
        sendBg.sprite = roundSprite;
        sendBg.type = Image.Type.Sliced;
        sendBg.raycastTarget = true;
        Stretch(sendBg.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(-176f, 18f), new Vector2(-20f, -18f));
        sendText = MakeText(sendBg.rectTransform, "", 30, TextAnchor.MiddleCenter, Color.white);
        sendText.fontStyle = FontStyle.Bold;
        Stretch(sendText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        sendButton = sendBg.gameObject.AddComponent<Button>();
        sendButton.onClick.AddListener(Send);
        Image tab = AddImage(NewRect("Collapse", prt), new Color(0.07f, 0.06f, 0.065f, 0.95f));
        tab.sprite = roundSprite;
        tab.type = Image.Type.Sliced;
        tab.raycastTarget = true;
        Place(tab.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-14f, 0f), new Vector2(60f, 140f));
        Text arrow = MakeText(tab.rectTransform, "<", 40, TextAnchor.MiddleCenter, bone);
        arrow.fontStyle = FontStyle.Bold;
        Stretch(arrow.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), Vector2.zero);
        collapseButton = tab.gameObject.AddComponent<Button>();
        collapseButton.onClick.AddListener(Close);
        ApplyLanguage();
    }
    private void ApplyLanguage()
    {
        int lang = Lang();
        builtLang = lang;
        Font f = lang == 0 ? latinFont : rtlFont;
        titleText.font = f;
        sendText.font = f;
        placeholder.font = f;
        titleText.text = lang == 0 ? TitleLabels[0] : PwRtl.Visual(TitleLabels[lang]);
        sendText.text = lang == 0 ? SendLabels[0] : PwRtl.Visual(SendLabels[lang]);
        placeholder.text = lang == 0 ? HintLabels[0] : PwRtl.Visual(HintLabels[lang]);
        placeholder.alignment = lang == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
    }
    private void Open()
    {
        SetOpen(true);
    }
    private void Close()
    {
        SetOpen(false);
    }
    private void SetOpen(bool open)
    {
        isOpen = open;
        if (builtLang != Lang())
        {
            ApplyLanguage();
        }
        panel.SetActive(open);
        iconObj.SetActive(!open);
        floatBox.gameObject.SetActive(!open);
        if (open)
        {
            badge.SetActive(false);
            scrollPending = true;
        }
        else
        {
            input.DeactivateInputField();
        }
    }
    private void OnTyping(string value)
    {
        bool rtl = HasRtl(value);
        inputText.color = rtl ? new Color(0f, 0f, 0f, 0f) : bone;
        input.caretColor = rtl ? new Color(0f, 0f, 0f, 0f) : bone;
        preview.text = rtl ? PwRtl.Visual(value) : "";
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
        string msg = Clean(input.text);
        if (msg.Length == 0 || Time.unscaledTime < nextSendTime)
        {
            return;
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
            AddMessage(nick, msg, true);
        }
        input.text = "";
        OnTyping("");
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
        bool mine = PhotonNetwork.LocalPlayer != null && photonEvent.Sender == PhotonNetwork.LocalPlayer.ActorNumber;
        AddMessage(nick, msg, mine);
    }
    private void AddMessage(string nick, string msg, bool mine)
    {
        if (content == null)
        {
            return;
        }
        if (msg.Length > MaxChars)
        {
            msg = msg.Substring(0, MaxChars);
        }
        AddHistoryRow(nick, msg, mine);
        AddFloatLine(nick, msg, mine);
        if (!isOpen && !mine)
        {
            badge.SetActive(true);
        }
        scrollPending = true;
    }
    private void AddHistoryRow(string nick, string msg, bool mine)
    {
        bool rtl = HasRtl(msg);
        RectTransform row = NewRect("Msg", content);
        VerticalLayoutGroup rv = row.gameObject.AddComponent<VerticalLayoutGroup>();
        rv.childAlignment = mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        rv.childControlWidth = true;
        rv.childControlHeight = true;
        rv.childForceExpandWidth = false;
        rv.childForceExpandHeight = false;
        rv.spacing = 4f;
        rv.padding = new RectOffset(24, 90, 0, 0);
        if (mine)
        {
            rv.padding = new RectOffset(90, 24, 0, 0);
        }
        Text name = MakeText(row, Shape(nick), 22, mine ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, mine ? new Color(0.91f, 0.75f, 0.48f, 1f) : new Color(1f, 0.3f, 0.3f, 1f));
        name.font = HasRtl(nick) ? rtlFont : latinFont;
        name.supportRichText = false;
        Image bubble = AddImage(NewRect("Bubble", row), mine ? new Color(0.42f, 0.05f, 0.07f, 0.95f) : new Color(0.17f, 0.16f, 0.16f, 0.95f));
        bubble.sprite = roundSprite;
        bubble.type = Image.Type.Sliced;
        HorizontalLayoutGroup bh = bubble.gameObject.AddComponent<HorizontalLayoutGroup>();
        bh.childControlWidth = true;
        bh.childControlHeight = true;
        bh.childForceExpandWidth = false;
        bh.childForceExpandHeight = false;
        bh.padding = new RectOffset(20, 20, 12, 12);
        Text body = MakeText(bubble.rectTransform, WrapShape(msg, BubbleWrap), 27, rtl ? TextAnchor.UpperRight : TextAnchor.UpperLeft, bone);
        body.font = rtl ? rtlFont : latinFont;
        body.supportRichText = false;
        historyRows.Add(row.gameObject);
        while (historyRows.Count > MaxHistory)
        {
            if (historyRows[0] != null)
            {
                Destroy(historyRows[0]);
            }
            historyRows.RemoveAt(0);
        }
    }
    private void AddFloatLine(string nick, string msg, bool mine)
    {
        bool rtl = HasRtl(msg) || HasRtl(nick);
        Image strip = AddImage(NewRect("Line", floatBox), new Color(0f, 0f, 0f, 0.5f));
        strip.sprite = roundSprite;
        strip.type = Image.Type.Sliced;
        HorizontalLayoutGroup sh = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        sh.childControlWidth = true;
        sh.childControlHeight = true;
        sh.childForceExpandWidth = false;
        sh.childForceExpandHeight = false;
        sh.padding = new RectOffset(16, 16, 6, 6);
        CanvasGroup group = strip.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        string nameColor = mine ? "#E8C07A" : "#FF4D4D";
        string name = "<color=" + nameColor + ">" + Shape(nick) + "</color>";
        List<string> parts = Wrap(msg, FloatWrap);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }
            string part = Shape(parts[i]);
            if (i == 0)
            {
                sb.Append(rtl ? part + "  :" + name : name + ":  " + part);
            }
            else
            {
                sb.Append(part);
            }
        }
        Text t = MakeText(strip.rectTransform, sb.ToString(), 25, rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, bone);
        t.font = rtl ? rtlFont : latinFont;
        floatLines.Add(group);
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
    }
    private void LateUpdate()
    {
        for (int i = floatLines.Count - 1; i >= 0; i--)
        {
            CanvasGroup g = floatLines[i];
            if (g == null)
            {
                continue;
            }
            float age = Time.unscaledTime - floatTimes[i];
            float a = Mathf.Clamp01(1f - (age - ShowTime) / FadeTime);
            if (!Mathf.Approximately(g.alpha, a))
            {
                g.alpha = a;
            }
            if (a <= 0f)
            {
                Destroy(g.gameObject);
                floatLines.RemoveAt(i);
                floatTimes.RemoveAt(i);
            }
        }
        if (scrollPending && isOpen && scroll != null)
        {
            scrollPending = false;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(false);
        }
    }
    private static string Shape(string s)
    {
        return HasRtl(s) ? PwRtl.Visual(s) : s;
    }
    private static string WrapShape(string s, int max)
    {
        List<string> parts = Wrap(s, max);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }
            sb.Append(Shape(parts[i]));
        }
        return sb.ToString();
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
    private static Sprite MakeRoundSprite(int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        float b = Mathf.Min(radius, size / 2 - 1);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }
    private static Sprite MakeBubbleIcon(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.56f);
        float rx = size * 0.44f;
        float ry = size * 0.34f;
        Vector2 t0 = new Vector2(size * 0.22f, size * 0.36f);
        Vector2 t1 = new Vector2(size * 0.42f, size * 0.26f);
        Vector2 t2 = new Vector2(size * 0.12f, size * 0.06f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dx = (p.x - c.x) / rx;
                float dy = (p.y - c.y) / ry;
                float e = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - e) * rx * 0.9f);
                if (InTriangle(p, t0, t1, t2))
                {
                    a = 1f;
                }
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 dc = new Vector2(c.x + k * size * 0.2f, c.y);
                    float dd = Vector2.Distance(p, dc);
                    float r = size * 0.065f;
                    if (dd < r + 1f)
                    {
                        a = Mathf.Min(a, Mathf.Clamp01(dd - r));
                    }
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
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
