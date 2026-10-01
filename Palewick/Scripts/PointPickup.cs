using UnityEngine;
using Photon.Pun;
public class PointPickup : MonoBehaviour, IInteractable
{
    public int value = 1;
    public bool autoPickup = true;
    public float spinSpeed = 70f;
    public float bobHeight = 0.18f;
    public float bobSpeed = 1.6f;
    public Transform visual;
    public Light glow;
    public AudioClip pickupSound;
    public float soundVolume = 0.8f;
    private PhotonView pv;
    private Collider[] colliders;
    private bool taken;
    private float phase;
    private float glowBase;
    private Vector3 visualBase;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        colliders = GetComponentsInChildren<Collider>(true);
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null) visualBase = visual.localPosition;
        if (glow != null) glowBase = glow.intensity;
        phase = Random.Range(0f, 10f);
    }
    private void Update()
    {
        if (taken || visual == null) return;
        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        float t = Time.time * bobSpeed + phase;
        visual.localPosition = visualBase + new Vector3(0f, Mathf.Sin(t) * bobHeight, 0f);
        if (glow != null) glow.intensity = glowBase * (0.75f + 0.25f * Mathf.Sin(t * 2.1f));
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!autoPickup || taken || other == null) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;
        PhotonView owner = health.GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && owner != null && !owner.IsMine) return;
        Claim();
    }
    public void Interact()
    {
        if (taken) return;
        Claim();
    }
    private void Claim()
    {
        if (taken) return;
        if (PhotonNetwork.InRoom && pv != null && pv.ViewID != 0)
        {
            pv.RPC(nameof(RPC_Take), RpcTarget.AllBufferedViaServer, PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }
        RPC_Take(-1);
    }
    [PunRPC]
    private void RPC_Take(int actor)
    {
        if (taken) return;
        taken = true;
        bool mine = actor < 0;
        if (!mine && PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null) mine = PhotonNetwork.LocalPlayer.ActorNumber == actor;
        if (mine)
        {
            PwPoints.Add(Mathf.Max(1, value));
            PwPointsHud.Flash(Mathf.Max(1, value));
            if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }
        if (glow != null) glow.enabled = false;
        if (visual != null) visual.gameObject.SetActive(false);
        enabled = false;
    }
}
