using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class PwLocalizer : MonoBehaviour
{
    private const string LangKey = "pw_lang";
    private const float ScanInterval = 1.5f;
    private const float CheckInterval = 0.3f;
    private class Entry
    {
        public Text text;
        public TMP_Text tmp;
        public string source;
        public string applied;
        public Font font;
        public TextAnchor alignment;
        public TMP_FontAsset tmpFont;
        public TextAlignmentOptions tmpAlignment;
        public bool Alive
        {
            get { return tmp != null || text != null; }
        }
        public string Value
        {
            get { return tmp != null ? tmp.text : text.text; }
            set
            {
                if (tmp != null) tmp.text = value;
                else text.text = value;
            }
        }
        public void Capture()
        {
            source = Value;
            if (tmp != null)
            {
                tmpFont = tmp.font;
                tmpAlignment = tmp.alignment;
            }
            else
            {
                font = text.font;
                alignment = text.alignment;
            }
        }
        public void Restore()
        {
            if (tmp != null)
            {
                tmp.font = tmpFont;
                tmp.alignment = tmpAlignment;
            }
            else
            {
                text.font = font;
                text.alignment = alignment;
            }
        }
        public string Name
        {
            get { return tmp != null ? tmp.gameObject.name : text.gameObject.name; }
        }
    }
    private static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]>
    {
        { "play", new[] { "العب", "یاری بکە" } },
        { "start", new[] { "ابدأ", "دەستپێبکە" } },
        { "start game", new[] { "بدء اللعبة", "دەستپێکردنی یاری" } },
        { "settings", new[] { "الإعدادات", "ڕێکخستنەکان" } },
        { "exit", new[] { "خروج", "دەرچوون" } },
        { "quit", new[] { "خروج", "دەرچوون" } },
        { "back", new[] { "رجوع", "گەڕانەوە" } },
        { "create room", new[] { "إنشاء غرفة", "دروستکردنی ژوور" } },
        { "join room", new[] { "الانضمام إلى غرفة", "چوونە ناو ژوور" } },
        { "join", new[] { "انضمام", "بەشداربوون" } },
        { "create", new[] { "إنشاء", "دروستکردن" } },
        { "room name", new[] { "اسم الغرفة", "ناوی ژوور" } },
        { "rooms", new[] { "الغرف", "ژوورەکان" } },
        { "players", new[] { "اللاعبون", "یاریزانەکان" } },
        { "player", new[] { "اللاعب", "یاریزان" } },
        { "connecting...", new[] { "جارٍ الاتصال...", "پەیوەندی دەکرێت..." } },
        { "connecting", new[] { "جارٍ الاتصال", "پەیوەندی دەکرێت" } },
        { "loading...", new[] { "جارٍ التحميل...", "بارکردن..." } },
        { "loading", new[] { "جارٍ التحميل", "بارکردن" } },
        { "you died", new[] { "لقد مت", "تۆ مردیت" } },
        { "you are dead", new[] { "لقد مت", "تۆ مردیت" } },
        { "game over", new[] { "انتهت اللعبة", "یاری کۆتایی هات" } },
        { "respawn", new[] { "إعادة الظهور", "دووبارە دەرکەوتنەوە" } },
        { "retry", new[] { "أعد المحاولة", "دووبارە هەوڵبدەرەوە" } },
        { "try again", new[] { "أعد المحاولة", "دووبارە هەوڵبدەرەوە" } },
        { "main menu", new[] { "القائمة الرئيسية", "لیستی سەرەکی" } },
        { "menu", new[] { "القائمة", "لیست" } },
        { "lobby", new[] { "الردهة", "لۆبی" } },
        { "leave", new[] { "مغادرة", "جێهێشتن" } },
        { "leave room", new[] { "مغادرة الغرفة", "جێهێشتنی ژوور" } },
        { "name", new[] { "الاسم", "ناو" } },
        { "enter name...", new[] { "أدخل الاسم...", "ناوەکەت بنووسە..." } },
        { "enter your name", new[] { "أدخل اسمك", "ناوەکەت بنووسە" } },
        { "refresh", new[] { "تحديث", "نوێکردنەوە" } },
        { "ok", new[] { "موافق", "باشە" } },
        { "cancel", new[] { "إلغاء", "هەڵوەشاندنەوە" } },
        { "yes", new[] { "نعم", "بەڵێ" } },
        { "no", new[] { "لا", "نەخێر" } },
        { "waiting for players...", new[] { "في انتظار اللاعبين...", "چاوەڕوانی یاریزانەکان..." } },
        { "offline", new[] { "غير متصل", "ئۆفلاین" } },
        { "online", new[] { "متصل", "ئۆنلاین" } },
        { "single player", new[] { "لاعب واحد", "تاکە یاریزان" } },
        { "multiplayer", new[] { "متعدد اللاعبين", "فرە یاریزان" } },
        { "resume", new[] { "استئناف", "بەردەوامبوون" } },
        { "pause", new[] { "إيقاف مؤقت", "وەستان" } },
        { "tap to start", new[] { "انقر للبدء", "کرتە بکە بۆ دەستپێکردن" } },
        { "press any key", new[] { "اضغط أي زر", "هەر دوگمەیەک دابگرە" } },
        { "skip", new[] { "تخطي", "تێپەڕاندن" } },
        { "keep your flashlight off to stay hidden", new[] { "أطفئ مصباحك لتبقى مختبئاً", "چراکەت بکوژێنەوە بۆ ئەوەی شاراوە بمێنیتەوە" } },
        { "running makes noise. walk when it is near", new[] { "الركض يصدر ضجيجاً. امشِ عندما يكون قريباً", "ڕاکردن دەنگ دروست دەکات. کاتێک نزیکە بە هێواشی بڕۆ" } },
        { "stay close to your friends", new[] { "ابقَ قريباً من أصدقائك", "لە هاوڕێکانت نزیک بە" } },
        { "doors will not stop it forever", new[] { "الأبواب لن توقفه إلى الأبد", "دەرگاکان بۆ هەمیشە ڕای ناگرن" } },
        { "listen for its footsteps", new[] { "استمع إلى وقع خطواته", "گوێ لە دەنگی پێیەکانی بگرە" } },
        { "use headphones for the best experience", new[] { "استخدم سماعات الرأس للحصول على أفضل تجربة", "بۆ باشترین ئەزموون هێدفۆن بەکاربهێنە" } },
        { "tip", new[] { "نصيحة", "ئامۆژگاری" } },
        { "enter text...", new[] { "اكتب هنا...", "لێرە بنووسە..." } },
        { "enter room name...", new[] { "اكتب اسم الغرفة...", "ناوی ژوور بنووسە..." } },
        { "server name", new[] { "اسم الخادم", "ناوی سێرڤەر" } },
        { "servers", new[] { "الخوادم", "سێرڤەرەکان" } },
        { "create server", new[] { "إنشاء خادم", "دروستکردنی سێرڤەر" } },
        { "close", new[] { "إغلاق", "داخستن" } },
        { "ready", new[] { "جاهز", "ئامادەیە" } },
        { "disconnected", new[] { "انقطع الاتصال", "پەیوەندی پچڕا" } },
        { "reconnecting...", new[] { "جارٍ إعادة الاتصال...", "دووبارە پەیوەندی دەکرێتەوە..." } },
        { "not connected", new[] { "غير متصل", "پەیوەندی نییە" } },
        { "creating...", new[] { "جارٍ الإنشاء...", "دروست دەکرێت..." } },
        { "joining...", new[] { "جارٍ الانضمام...", "دەچێتە ژوورەوە..." } },
        { "name taken", new[] { "الاسم مستخدم", "ئەم ناوە گیراوە" } },
        { "create failed", new[] { "فشل الإنشاء", "دروستکردن سەرکەوتوو نەبوو" } },
        { "server full", new[] { "الخادم ممتلئ", "سێرڤەرەکە پڕە" } },
        { "join failed", new[] { "فشل الانضمام", "چوونەژوورەوە سەرکەوتوو نەبوو" } }
    };
    private readonly List<Entry> entries = new List<Entry>();
    private readonly Dictionary<Object, Entry> lookup = new Dictionary<Object, Entry>();
    private readonly HashSet<string> reported = new HashSet<string>();
    private Font rtlFont;
    private TMP_FontAsset rtlTmpFont;
    private bool tmpFontTried;
    private int lang = -1;
    private float nextScan;
    private float nextCheck;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (FindAnyObjectByType<PwLocalizer>() != null)
        {
            return;
        }
        GameObject go = new GameObject("PwLocalizer");
        DontDestroyOnLoad(go);
        go.AddComponent<PwLocalizer>();
    }
    public static int CurrentLang()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(LangKey, 0), 0, 2);
    }
    private void Awake()
    {
        rtlFont = Resources.Load<Font>("Fonts/UniMahanBilal");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (rtlTmpFont != null)
        {
            Destroy(rtlTmpFont);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextScan = 0f;
    }
    private void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextScan)
        {
            nextScan = now + ScanInterval;
            Scan();
        }
        if (now >= nextCheck)
        {
            nextCheck = now + CheckInterval;
            int l = CurrentLang();
            bool changed = l != lang;
            lang = l;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry e = entries[i];
                if (!e.Alive)
                {
                    entries.RemoveAt(i);
                    continue;
                }
                if (e.Value != e.applied)
                {
                    e.Capture();
                    Apply(e);
                }
                else if (changed)
                {
                    Apply(e);
                }
            }
        }
    }
    private void Scan()
    {
        CleanLookup();
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        for (int i = 0; i < texts.Length; i++)
        {
            Text t = texts[i];
            if (t == null || lookup.ContainsKey(t) || Skip(t.transform) || IsInputText(t))
            {
                continue;
            }
            Entry e = new Entry { text = t };
            e.Capture();
            entries.Add(e);
            lookup[t] = e;
            Apply(e);
        }
        TMP_Text[] tmps = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        for (int i = 0; i < tmps.Length; i++)
        {
            TMP_Text t = tmps[i];
            if (t == null || lookup.ContainsKey(t) || Skip(t.transform) || IsInputText(t))
            {
                continue;
            }
            Entry e = new Entry { tmp = t };
            e.Capture();
            entries.Add(e);
            lookup[t] = e;
            Apply(e);
        }
    }
    private void CleanLookup()
    {
        List<Object> dead = null;
        foreach (KeyValuePair<Object, Entry> kv in lookup)
        {
            if (kv.Key == null)
            {
                if (dead == null)
                {
                    dead = new List<Object>();
                }
                dead.Add(kv.Key);
            }
        }
        if (dead != null)
        {
            for (int i = 0; i < dead.Count; i++)
            {
                lookup.Remove(dead[i]);
            }
        }
    }
    private static bool Skip(Transform t)
    {
        Transform p = t;
        while (p != null)
        {
            string n = p.name;
            if (n == "PubgPauseMenu" || n == "ChatRoot" || n == "FpsCounter" || n == "PlayerName" || n == "ServerList" || n == "NamePreview" || n == "ServerNamePreview")
            {
                return true;
            }
            p = p.parent;
        }
        return false;
    }
    private static bool IsInputText(Text t)
    {
        InputField f = t.GetComponentInParent<InputField>(true);
        return f != null && f.textComponent == t;
    }
    private static bool IsInputText(TMP_Text t)
    {
        TMP_InputField f = t.GetComponentInParent<TMP_InputField>(true);
        return f != null && f.textComponent == t;
    }
    private TMP_FontAsset RtlTmpFont()
    {
        if (!tmpFontTried)
        {
            tmpFontTried = true;
            if (rtlFont != null)
            {
                rtlTmpFont = TMP_FontAsset.CreateFontAsset(rtlFont);
            }
        }
        return rtlTmpFont;
    }
    private void Apply(Entry e)
    {
        string src = e.source;
        if (lang <= 0 || string.IsNullOrEmpty(src) || !HasLetters(src) || HasRtl(src))
        {
            e.Restore();
            if (e.Value != src)
            {
                e.Value = src;
            }
            e.applied = src;
            return;
        }
        string key = src.Trim().ToLowerInvariant();
        string[] tr;
        if (!Words.TryGetValue(key, out tr))
        {
            Report("PW_MISSING", src, e.Name);
            e.Restore();
            e.Value = src;
            e.applied = src;
            return;
        }
        string shown = PwRtl.Visual(tr[lang - 1]);
        if (e.tmp != null)
        {
            TMP_FontAsset fa = RtlTmpFont();
            if (fa == null)
            {
                e.Restore();
                e.Value = src;
                e.applied = src;
                return;
            }
            e.tmp.font = fa;
            e.tmp.alignment = MirrorTmp(e.tmpAlignment);
        }
        else
        {
            if (rtlFont != null)
            {
                e.text.font = rtlFont;
            }
            e.text.alignment = Mirror(e.alignment);
        }
        e.Value = shown;
        e.applied = e.Value;
    }
    private static TextAlignmentOptions MirrorTmp(TextAlignmentOptions a)
    {
        switch (a)
        {
            case TextAlignmentOptions.TopLeft: return TextAlignmentOptions.TopRight;
            case TextAlignmentOptions.TopRight: return TextAlignmentOptions.TopLeft;
            case TextAlignmentOptions.Left: return TextAlignmentOptions.Right;
            case TextAlignmentOptions.Right: return TextAlignmentOptions.Left;
            case TextAlignmentOptions.BottomLeft: return TextAlignmentOptions.BottomRight;
            case TextAlignmentOptions.BottomRight: return TextAlignmentOptions.BottomLeft;
            case TextAlignmentOptions.MidlineLeft: return TextAlignmentOptions.MidlineRight;
            case TextAlignmentOptions.MidlineRight: return TextAlignmentOptions.MidlineLeft;
            case TextAlignmentOptions.BaselineLeft: return TextAlignmentOptions.BaselineRight;
            case TextAlignmentOptions.BaselineRight: return TextAlignmentOptions.BaselineLeft;
            default: return a;
        }
    }
    private void Report(string tag, string text, string objectName)
    {
        if (string.IsNullOrEmpty(text) || !HasLetters(text) || HasRtl(text))
        {
            return;
        }
        string clean = text.Replace("\n", " ").Trim();
        if (reported.Add(tag + clean))
        {
            Debug.Log(tag + " | " + clean + " | " + objectName);
        }
    }
    private static TextAnchor Mirror(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
            case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
            case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
            case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
            case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
            case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
            default: return a;
        }
    }
    private static bool HasLetters(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsLetter(s[i]))
            {
                return true;
            }
        }
        return false;
    }
    private static bool HasRtl(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF') || (c >= '\uE000' && c <= '\uE0FF'))
            {
                return true;
            }
        }
        return false;
    }
}
