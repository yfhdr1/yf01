using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class PwLoadout : MonoBehaviour
{
    public const string SkinProp = "pw_skin";
    private const float Interval = 1f;
    private static readonly Color[] SkinColors =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(0.62f, 0.09f, 0.09f, 1f),
        new Color(0.16f, 0.16f, 0.18f, 1f),
        new Color(0.86f, 0.66f, 0.22f, 1f)
    };
    private static PwLoadout instance;
    private static readonly Dictionary<int, int> appliedFlash = new Dictionary<int, int>();
    private float next;
    private int pushedSkin = -1;
    public static Color ColorFor(int index)
    {
        return SkinColors[Mathf.Clamp(index, 0, SkinColors.Length - 1)];
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
        PushSkin();
        ApplyAll();
    }
    private void PushSkin()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
        int value = PwShop.Skin;
        if (pushedSkin == value) return;
        pushedSkin = value;
        Hashtable props = new Hashtable();
        props[SkinProp] = value;
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
            int index = 0;
            if (mine)
            {
                index = PwShop.Skin;
            }
            else if (view != null && view.Owner != null)
            {
                object raw;
                if (view.Owner.CustomProperties.TryGetValue(SkinProp, out raw) && raw is int) index = (int)raw;
            }
            Tint(player.transform, index);
            if (mine) Flashlight(player.transform);
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
            if (r == null || r is ParticleSystemRenderer) continue;
            if (!(r is SkinnedMeshRenderer)) continue;
            Material m = r.material;
            if (m == null || !m.HasProperty("_Color")) continue;
            if (m.color != color) m.color = color;
        }
    }
    private static void Flashlight(Transform root)
    {
        if (root == null) return;
        int id = root.GetInstanceID();
        int want = PwShop.FlashlightUpgraded ? 1 : 0;
        int done;
        if (appliedFlash.TryGetValue(id, out done) && done == want) return;
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null || l.type != LightType.Spot) continue;
            if (want == 1)
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
        appliedFlash[id] = want;
    }
}
