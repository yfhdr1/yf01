using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class PwLoadout : MonoBehaviour
{
    public const string LookProp = "pw_look";
    private const float Interval = 1f;
    private static readonly Color[] SkinColors =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(0.62f, 0.09f, 0.09f, 1f),
        new Color(0.16f, 0.16f, 0.18f, 1f),
        new Color(0.86f, 0.66f, 0.22f, 1f)
    };
    private static readonly Color[] LampColors =
    {
        new Color(1f, 0.96f, 0.88f, 1f),
        new Color(1f, 0.28f, 0.2f, 1f),
        new Color(0.42f, 0.68f, 1f, 1f)
    };
    private static PwLoadout instance;
    private static readonly Dictionary<Transform, int> appliedFlash = new Dictionary<Transform, int>();
    private float next;
    private int pushedLook = -1;
    public static Color ColorFor(int index)
    {
        return SkinColors[Mathf.Clamp(index, 0, SkinColors.Length - 1)];
    }
    public static Color LampColorFor(int index)
    {
        return LampColors[Mathf.Clamp(index, 0, LampColors.Length - 1)];
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PwLoadout");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PwLoadout>();
    }
    private void Awake()
    {
        instance = this;
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    private void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + Interval;
        PushLook();
        ApplyAll();
    }
    private void PushLook()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
        int value = PwShop.Look;
        if (pushedLook == value) return;
        pushedLook = value;
        Hashtable props = new Hashtable();
        props[LookProp] = value;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    private void ApplyAll()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerHealth player = players[i];
            if (player == null) continue;
            PhotonView view = player.GetComponent<PhotonView>();
            bool mine = !PhotonNetwork.InRoom || view == null || view.IsMine;
            int look = 0;
            if (mine)
            {
                look = PwShop.Look;
            }
            else if (view != null && view.Owner != null)
            {
                object raw;
                if (view.Owner.CustomProperties.TryGetValue(LookProp, out raw) && raw is int) look = (int)raw;
            }
            int skin = look % 10;
            int lamp = (look / 10) % 10;
            bool upgraded = look >= 100;
            Tint(player.transform, skin);
            Flashlight(player.transform, lamp, upgraded);
        }
    }
    public static void Tint(Transform root, int index)
    {
        if (root == null) return;
        Color color = ColorFor(index);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !(r is SkinnedMeshRenderer)) continue;
            Material m = r.material;
            if (m == null || !m.HasProperty("_Color")) continue;
            if (m.color != color) m.color = color;
        }
    }
    private static void Clean()
    {
        if (appliedFlash.Count < 8) return;
        List<Transform> dead = new List<Transform>();
        foreach (KeyValuePair<Transform, int> pair in appliedFlash)
        {
            if (pair.Key == null) dead.Add(pair.Key);
        }
        for (int i = 0; i < dead.Count; i++)
        {
            appliedFlash.Remove(dead[i]);
        }
    }
    private static void Flashlight(Transform root, int lamp, bool upgraded)
    {
        if (root == null) return;
        Clean();
        int want = (upgraded ? 100 : 0) + Mathf.Clamp(lamp, 0, 2);
        int done;
        if (appliedFlash.TryGetValue(root, out done) && done == want) return;
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null || l.type != LightType.Spot) continue;
            l.color = LampColorFor(lamp);
            if (upgraded)
            {
                l.range = 45f;
                l.spotAngle = 95f;
                l.intensity = 6f;
            }
            else
            {
                l.range = 30f;
                l.spotAngle = 80f;
                l.intensity = 4f;
            }
        }
        appliedFlash[root] = want;
    }
}
