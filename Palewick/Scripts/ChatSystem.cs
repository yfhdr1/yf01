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
    private const int MaxLines = 8;
    private const int MaxChars = 80;
    private const int WrapChars = 42;
    private const float ShowTime = 10f;
    private const float FadeTime = 1.5f;
    private const float SendCooldown = 1f;
    private static readonly string[] OpenLabels = { "Chat", "دردشة", "چات" };
    private static readonly string[] SendLabels = { "Send", "إرسال", "ناردن" };
    private static readonly string[] HintLabels = { "Type a message...", "اكتب رسالة...", "نامەیەک بنووسە..." };
    private readonly Color blood = new Color(0.78f, 0.06f, 0.08f, 1f);
    private readonly Color bone = new Color(0.9f, 0.87f, 0.82f, 1f);
    private readonly Color ash = new Color(0.55f, 0.53f, 0.52f, 1f);
    private readonly List<Text> lines = new List<Text>();
    private readonly List<float> lineTimes = new List<float>();
    private Font latinFont;
    private Font rtlFont;
    private RectTransform logBox;
    private Image logBg;
    private GameObject inputBar;
    private InputField input;
    private Text inputText;
    private Text preview;
    private Text placeholder;
    private Text openText;
    private Text sendText;
    private Button openButton;
    private Button sendButton;
    private bool isOpen;
    private bool callbacksAdded;
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
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(ToggleOpen);
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
    }
    private void Start()
    {
        latinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rtlFont = Resources.Load<Font>("Fonts/UniMahanBilal");
        if (rtlFont == null)
        {
            rtlFont = latinFont;
        }
        Build();
        SetOpen(false);
    }
    private void Build()
    {
        RectTransform root = (RectTransform)transform;
        Image openBg = AddImage(NewRect("OpenChat", root), new Color(0.06f, 0.05f, 0.05f, 0.85f));
        openBg.raycastTarget = true;
        Place(openBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 250f), new Vector2(150f, 64f));
        Image openLine = AddImage(NewRect("Line", openBg.rectTransform), blood);
        Stretch(openLine.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
        openText = MakeText(openBg.rectTransform, "", 28, TextAnchor.MiddleCenter, bone);
        Stretch(openText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), Vector2.zero);
        openButton = openBg.gameObject.AddComponent<Button>();
        openButton.onClick.AddListener(ToggleOpen);
        logBg = AddImage(NewRect("ChatLog", root), new Color(0f, 0f, 0f, 0f));
        logBox = logBg.rectTransform;
        Place(logBox, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(24f, -30f), new Vector2(640f, 240f));
        VerticalLayoutGroup v = logBox.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.LowerLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        v.spacing = 2f;
        v.padding = new RectOffset(12, 12, 8, 8);
        logBox.gameObject.AddComponent<RectMask2D>();
        Image barBg = AddImage(NewRect("InputBar", root), new Color(0.05f, 0.045f, 0.05f, 0.95f));
        inputBar = barBg.gameObject;
        Place(barBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(24f, -40f), new Vector2(640f, 72f));
        Image fieldBg = AddImage(NewRect("Field", barBg.rectTransform), new Color(0.12f, 0.11f, 0.11f, 1f));
        fieldBg.raycastTarget = true;
        Stretch(fieldBg.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-150f, -6f));
        inputText = MakeText(fieldBg.rectTransform, "", 28, TextAnchor.MiddleLeft, bone);
        inputText.font = latinFont;
        inputText.supportRichText = false;
        inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        Stretch(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
        placeholder = MakeText(fieldBg.rectTransform, "", 26, TextAnchor.MiddleLeft, ash);
        Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
        preview = MakeText(fieldBg.rectTransform, "", 28, TextAnchor.MiddleRight, bone);
        preview.font = rtlFont;
        preview.supportRichText = false;
        Stretch(preview.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
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
        Image sendBg = AddImage(NewRect("Send", barBg.rectTransform), blood);
        sendBg.raycastTarget = true;
        Stretch(sendBg.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(-140f, 6f), new Vector2(-6f, -6f));
        sendText = MakeText(sendBg.rectTransform, "", 28, TextAnchor.MiddleCenter, Color.white);
        Stretch(sendText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        sendButton = sendBg.gameObject.AddComponent<Button>();
        sendButton.onClick.AddListener(Send);
        ApplyLanguage();
    }
    private void ApplyLanguage()
    {
        int lang = Lang();
        builtLang = lang;
        Font f = lang == 0 ? latinFont : rtlFont;
        openText.font = f;
        sendText.font = f;
        placeholder.font = f;
        openText.text = lang == 0 ? OpenLabels[0] : PwRtl.Visual(OpenLabels[lang]);
        sendText.text = lang == 0 ? SendLabels[0] : PwRtl.Visual(SendLabels[lang]);
        placeholder.text = lang == 0 ? HintLabels[0] : PwRtl.Visual(HintLabels[lang]);
        placeholder.alignment = lang == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
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
        inputBar.SetActive(open);
        logBg.color = new Color(0f, 0f, 0f, open ? 0.55f : 0f);
        if (open)
        {
            input.text = "";
            OnTyping("");
            input.ActivateInputField();
            input.Select();
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
            SetOpen(false);
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
            AddLine(nick, msg, true);
        }
        SetOpen(false);
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
        AddLine(nick, msg, mine);
    }
    private void AddLine(string nick, string msg, bool mine)
    {
        if (logBox == null)
        {
            return;
        }
        if (msg.Length > MaxChars)
        {
            msg = msg.Substring(0, MaxChars);
        }
        bool rtl = HasRtl(msg) || HasRtl(nick);
        Text t = MakeText(logBox, "", 26, TextAnchor.LowerLeft, bone);
        t.font = rtl ? rtlFont : latinFont;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        string nameColor = mine ? "#E8C07A" : "#FF4040";
        List<string> parts = Wrap(msg, WrapChars);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }
            string body = rtl ? PwRtl.Visual(parts[i]) : parts[i];
            if (i == 0)
            {
                string name = "<color=" + nameColor + ">" + (HasRtl(nick) ? PwRtl.Visual(nick) : nick) + "</color>";
                sb.Append(rtl ? body + "  :" + name : name + ":  " + body);
            }
            else
            {
                sb.Append(body);
            }
        }
        t.text = sb.ToString();
        lines.Add(t);
        lineTimes.Add(Time.unscaledTime);
        while (lines.Count > MaxLines)
        {
            if (lines[0] != null)
            {
                Destroy(lines[0].gameObject);
            }
            lines.RemoveAt(0);
            lineTimes.RemoveAt(0);
        }
    }
    private void Update()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Text t = lines[i];
            if (t == null)
            {
                continue;
            }
            float age = Time.unscaledTime - lineTimes[i];
            float a = isOpen ? 1f : Mathf.Clamp01(1f - (age - ShowTime) / FadeTime);
            Color c = t.color;
            if (!Mathf.Approximately(c.a, a))
            {
                c.a = a;
                t.color = c;
                Outline o = t.GetComponent<Outline>();
                if (o != null)
                {
                    o.effectColor = new Color(0f, 0f, 0f, 0.9f * a);
                }
            }
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
