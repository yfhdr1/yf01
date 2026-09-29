using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
public class Minimap : MonoBehaviour
{
    private const float Diameter = 250f;
    private const float Range = 30f;
    private const float Height = 60f;
    private const float RenderInterval = 0.1f;
    private const float SearchInterval = 1f;
    private const int TexSize = 256;
    private const float FrameInner = 0.67f;
    private const float FrameCenter = 0.46f;
    private static readonly Color[] TeamColors =
    {
        new Color(0.93f, 0.78f, 0.1f, 1f),
        new Color(0.9f, 0.45f, 0.12f, 1f),
        new Color(0.2f, 0.5f, 0.9f, 1f),
        new Color(0.35f, 0.7f, 0.2f, 1f)
    };
    private readonly Dictionary<CharController_Motor, Image> playerDots = new Dictionary<CharController_Motor, Image>();
    private readonly Dictionary<EnemyAI, Image> enemyDots = new Dictionary<EnemyAI, Image>();
    private readonly List<CharController_Motor> removeMotors = new List<CharController_Motor>();
    private readonly List<EnemyAI> removeEnemies = new List<EnemyAI>();
    private RectTransform markers;
    private Image localArrow;
    private RenderTexture texture;
    private Camera mapCamera;
    private Sprite circleSprite;
    private Sprite dotSprite;
    private Sprite arrowSprite;
    private CharController_Motor localMotor;
    private GameObject loadingPanel;
    private float nextRender;
    private float nextSearch;
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
            if (menu == null || c.transform.Find("MinimapRoot") != null)
            {
                continue;
            }
            GameObject go = new GameObject("MinimapRoot", typeof(RectTransform));
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
            go.AddComponent<Minimap>();
            return;
        }
    }
    private void Start()
    {
        circleSprite = MakeDisc(128, 0f);
        dotSprite = MakeDisc(64, 7f);
        arrowSprite = MakeArrow(64);
        texture = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
        texture.name = "MinimapTexture";
        texture.Create();
        BuildCamera();
        BuildUi();
        if (transform.parent != null)
        {
            Transform lp = transform.parent.Find("LoadingPanel");
            loadingPanel = lp != null ? lp.gameObject : null;
        }
    }
    private void OnDestroy()
    {
        if (mapCamera != null)
        {
            mapCamera.targetTexture = null;
            Destroy(mapCamera.gameObject);
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
        DestroySprite(circleSprite);
        DestroySprite(dotSprite);
        DestroySprite(arrowSprite);
    }
    private static void DestroySprite(Sprite s)
    {
        if (s != null)
        {
            Destroy(s.texture);
            Destroy(s);
        }
    }
    private void BuildCamera()
    {
        GameObject go = new GameObject("MinimapCamera");
        mapCamera = go.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = Range;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.04f, 0.02f, 0.02f, 1f);
        mapCamera.cullingMask = ~(1 << 5);
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = Height + 120f;
        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;
        mapCamera.useOcclusionCulling = false;
        mapCamera.depth = -50f;
        mapCamera.targetTexture = texture;
        mapCamera.enabled = false;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
    private void BuildUi()
    {
        RectTransform root = (RectTransform)transform;
        RectTransform box = NewRect("Minimap", root);
        Place(box, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -170f), new Vector2(Diameter, Diameter));
        Image maskImage = AddImage(NewRect("Mask", box), Color.white);
        maskImage.sprite = circleSprite;
        Stretch(maskImage.rectTransform);
        Mask mask = maskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        Image bg = AddImage(NewRect("Back", maskImage.rectTransform), new Color(0.05f, 0.02f, 0.02f, 0.85f));
        Stretch(bg.rectTransform);
        RawImage map = NewRect("Map", maskImage.rectTransform).gameObject.AddComponent<RawImage>();
        map.texture = texture;
        map.color = new Color(1f, 0.92f, 0.9f, 0.92f);
        map.raycastTarget = false;
        Stretch(map.rectTransform);
        markers = NewRect("Markers", maskImage.rectTransform);
        Stretch(markers);
        Sprite frameSprite = Resources.Load<Sprite>("hud_minimap_frame");
        if (frameSprite != null)
        {
            float f = Diameter / FrameInner;
            Image frame = AddImage(NewRect("Frame", box), Color.white);
            frame.sprite = frameSprite;
            Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -(0.5f - FrameCenter) * f), new Vector2(f, f));
        }
        else
        {
            Outline o = maskImage.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.6f, 0.03f, 0.04f, 1f);
            o.effectDistance = new Vector2(3f, -3f);
        }
        localArrow = AddImage(NewRect("Me", markers), TeamColors[0]);
        localArrow.sprite = arrowSprite;
        Place(localArrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
    }
    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        if (now >= nextSearch)
        {
            nextSearch = now + SearchInterval;
            Search();
        }
        if (localMotor == null || !localMotor.isActiveAndEnabled)
        {
            localMotor = null;
            localArrow.enabled = false;
            return;
        }
        Transform me = localMotor.transform;
        Vector3 center = me.position;
        localArrow.enabled = true;
        localArrow.color = TeamColor(localMotor);
        localArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -me.eulerAngles.y);
        localArrow.rectTransform.SetAsLastSibling();
        foreach (KeyValuePair<CharController_Motor, Image> kv in playerDots)
        {
            if (kv.Key == null || kv.Value == null)
            {
                continue;
            }
            bool show = kv.Key != localMotor && kv.Key.gameObject.activeInHierarchy;
            kv.Value.enabled = show;
            if (show)
            {
                kv.Value.color = TeamColor(kv.Key);
                kv.Value.rectTransform.anchoredPosition = ToMap(kv.Key.transform.position - center);
            }
        }
        float pulse = 1f + Mathf.Sin(now * 6f) * 0.15f;
        foreach (KeyValuePair<EnemyAI, Image> kv in enemyDots)
        {
            if (kv.Key == null || kv.Value == null)
            {
                continue;
            }
            bool show = kv.Key.gameObject.activeInHierarchy;
            kv.Value.enabled = show;
            if (show)
            {
                kv.Value.rectTransform.anchoredPosition = ToMap(kv.Key.transform.position - center);
                kv.Value.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        bool loading = loadingPanel != null && loadingPanel.activeInHierarchy;
        if (!loading && now >= nextRender)
        {
            nextRender = now + RenderInterval;
            RenderMap(center);
        }
    }
    private void RenderMap(Vector3 center)
    {
        mapCamera.transform.position = new Vector3(center.x, center.y + Height, center.z);
        bool fog = RenderSettings.fog;
        Color ambient = RenderSettings.ambientLight;
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f, 1f);
        try
        {
            mapCamera.Render();
        }
        finally
        {
            RenderSettings.fog = fog;
            RenderSettings.ambientLight = ambient;
        }
    }
    private Vector2 ToMap(Vector3 offset)
    {
        float half = Diameter * 0.5f;
        Vector2 p = new Vector2(offset.x, offset.z) / Range * half;
        float max = half - 12f;
        if (p.magnitude > max)
        {
            p = p.normalized * max;
        }
        return p;
    }
    private void Search()
    {
        CharController_Motor[] motors = FindObjectsByType<CharController_Motor>(FindObjectsInactive.Include);
        for (int i = 0; i < motors.Length; i++)
        {
            CharController_Motor m = motors[i];
            if (m == null)
            {
                continue;
            }
            if (localMotor == null && m.isActiveAndEnabled && m.IsLocal)
            {
                localMotor = m;
            }
            if (!playerDots.ContainsKey(m))
            {
                Image dot = AddImage(NewRect("Player", markers), Color.white);
                dot.sprite = dotSprite;
                Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
                playerDots.Add(m, dot);
            }
        }
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Include);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyAI e = enemies[i];
            if (e == null || enemyDots.ContainsKey(e))
            {
                continue;
            }
            Image dot = AddImage(NewRect("Monster", markers), new Color(0.9f, 0.05f, 0.05f, 1f));
            dot.sprite = dotSprite;
            Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            enemyDots.Add(e, dot);
        }
        removeMotors.Clear();
        foreach (KeyValuePair<CharController_Motor, Image> kv in playerDots)
        {
            if (kv.Key == null)
            {
                removeMotors.Add(kv.Key);
                if (kv.Value != null)
                {
                    Destroy(kv.Value.gameObject);
                }
            }
        }
        for (int i = 0; i < removeMotors.Count; i++)
        {
            playerDots.Remove(removeMotors[i]);
        }
        removeEnemies.Clear();
        foreach (KeyValuePair<EnemyAI, Image> kv in enemyDots)
        {
            if (kv.Key == null)
            {
                removeEnemies.Add(kv.Key);
                if (kv.Value != null)
                {
                    Destroy(kv.Value.gameObject);
                }
            }
        }
        for (int i = 0; i < removeEnemies.Count; i++)
        {
            enemyDots.Remove(removeEnemies[i]);
        }
    }
    private static Color TeamColor(CharController_Motor m)
    {
        int number = 1;
        if (PhotonNetwork.InRoom && m.photonView != null && PhotonNetwork.PlayerList != null)
        {
            List<int> ids = new List<int>();
            Player[] players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length; i++)
            {
                ids.Add(players[i].ActorNumber);
            }
            ids.Sort();
            int index = ids.IndexOf(m.photonView.OwnerActorNr);
            number = index < 0 ? 1 : index + 1;
        }
        return TeamColors[Mathf.Clamp(number - 1, 0, TeamColors.Length - 1)];
    }
    private static Texture2D NewTex(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
    private static Sprite MakeDisc(int size, float border)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        float r = size * 0.5f - 1f;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - d + 0.5f);
                float w = border > 0f ? Mathf.Clamp01(r - border - d + 0.5f) : 1f;
                px[y * size + x] = new Color(w, w, w, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    private static Sprite MakeArrow(int size)
    {
        Texture2D tex = NewTex(size);
        Color[] px = new Color[size * size];
        Vector2 tip = new Vector2(size * 0.5f, size * 0.96f);
        Vector2 left = new Vector2(size * 0.1f, size * 0.06f);
        Vector2 notch = new Vector2(size * 0.5f, size * 0.3f);
        Vector2 right = new Vector2(size * 0.9f, size * 0.06f);
        Vector2 c = new Vector2(size * 0.5f, size * 0.45f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool outer = InArrow(p, tip, left, notch, right);
                Vector2 q = c + (p - c) / 0.7f;
                bool inner = InArrow(q, tip, left, notch, right);
                float v = inner ? 1f : 0f;
                px[y * size + x] = new Color(v, v, v, outer ? 1f : 0f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    private static bool InArrow(Vector2 p, Vector2 tip, Vector2 left, Vector2 notch, Vector2 right)
    {
        return InTriangle(p, tip, left, notch) || InTriangle(p, tip, notch, right);
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
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
