using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class Minimap : MonoBehaviour
{
    public RawImage mapImage;
    public RectTransform markers;
    public Image localArrow;
    public Sprite dotSprite;
    public float range = 30f;
    public float height = 60f;
    private const float RenderInterval = 0.1f;
    private const float SearchInterval = 1f;
    private const int TexSize = 256;
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
    private RenderTexture texture;
    private Camera mapCamera;
    private Sprite generatedDot;
    private CharController_Motor localMotor;
    private GameObject loadingPanel;
    private float nextRender;
    private float nextSearch;
    private void Start()
    {
        if (mapImage == null)
        {
            Transform t = transform.Find("Mask/Map");
            mapImage = t != null ? t.GetComponent<RawImage>() : null;
        }
        if (markers == null)
        {
            markers = transform.Find("Mask/Markers") as RectTransform;
        }
        if (localArrow == null && markers != null)
        {
            Transform t = markers.Find("Me");
            localArrow = t != null ? t.GetComponent<Image>() : null;
        }
        if (mapImage == null || markers == null)
        {
            enabled = false;
            return;
        }
        if (dotSprite == null)
        {
            Texture2D tex = MakeDiscTexture(64, 7f);
            generatedDot = Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
            dotSprite = generatedDot;
        }
        texture = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
        texture.name = "MinimapTexture";
        texture.Create();
        mapImage.texture = texture;
        BuildCamera();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform lp = canvas.rootCanvas.transform.Find("LoadingPanel");
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
        if (mapImage != null && mapImage.texture == texture)
        {
            mapImage.texture = null;
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
        if (generatedDot != null)
        {
            Destroy(generatedDot.texture);
            Destroy(generatedDot);
        }
    }
    private void BuildCamera()
    {
        GameObject go = new GameObject("MinimapCamera");
        mapCamera = go.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = range;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.04f, 0.02f, 0.02f, 1f);
        mapCamera.cullingMask = ~(1 << 5);
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = height + 120f;
        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;
        mapCamera.useOcclusionCulling = false;
        mapCamera.depth = -50f;
        mapCamera.targetTexture = texture;
        mapCamera.enabled = false;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
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
            if (localArrow != null)
            {
                localArrow.enabled = false;
            }
            return;
        }
        Transform me = localMotor.transform;
        Vector3 center = me.position;
        if (localArrow != null)
        {
            localArrow.enabled = true;
            localArrow.color = TeamColor(localMotor);
            localArrow.rectTransform.anchoredPosition = Vector2.zero;
            localArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -me.eulerAngles.y);
            localArrow.rectTransform.SetAsLastSibling();
        }
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
        mapCamera.orthographicSize = range;
        mapCamera.transform.position = new Vector3(center.x, center.y + height, center.z);
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
        float half = markers.rect.width * 0.5f;
        Vector2 p = new Vector2(offset.x, offset.z) / range * half;
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
                playerDots.Add(m, NewDot("Player", Color.white, 20f));
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
            enemyDots.Add(e, NewDot("Monster", new Color(0.9f, 0.05f, 0.05f, 1f), 22f));
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
    private Image NewDot(string name, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = markers.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(markers, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.sprite = dotSprite;
        img.color = color;
        img.raycastTarget = false;
        img.enabled = false;
        return img;
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
    public static Texture2D MakeDiscTexture(int size, float border)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
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
        return tex;
    }
    public static Texture2D MakeArrowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
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
                bool inner = InArrow(c + (p - c) / 0.7f, tip, left, notch, right);
                float v = inner ? 1f : 0f;
                px[y * size + x] = new Color(v, v, v, outer ? 1f : 0f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
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
}
