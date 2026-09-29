using UnityEngine;
using Photon.Pun;
public class PlayerInteraction : MonoBehaviour
{
    public Transform playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactableLayer;
    public GameObject interactButtonUI;
    public float facingDotThreshold = 0.3f;
    public float interactCooldown = 0.4f;
    private const float ScanInterval = 0.1f;
    private const float BehindLimit = -0.2f;
    private IInteractable currentInteractable;
    private float cooldownTimer;
    private float nextScan;
    private Transform body;
    private void Start()
    {
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (pv != null && PhotonNetwork.InRoom && !pv.IsMine)
        {
            enabled = false;
            return;
        }
        CharacterController cc = GetComponentInParent<CharacterController>();
        body = cc != null ? cc.transform : transform.root;
        SetButtonState(false);
    }
    private void OnDisable()
    {
        currentInteractable = null;
    }
    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + ScanInterval;
            Scan();
        }
        SetButtonState(currentInteractable != null);
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInteractButtonPressed();
        }
    }
    private void Scan()
    {
        if (body == null)
        {
            currentInteractable = null;
            return;
        }
        Vector3 center = body.position + Vector3.up;
        int mask = interactableLayer.value != 0 ? interactableLayer.value : ~0;
        Collider[] hits = Physics.OverlapSphere(center, interactDistance, mask, QueryTriggerInteraction.Collide);
        Vector3 forward = body.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        IInteractable best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null || c.transform.IsChildOf(body))
            {
                continue;
            }
            IInteractable target = c.GetComponentInParent<IInteractable>();
            if (target == null)
            {
                continue;
            }
            MeshCollider mc = c as MeshCollider;
            Vector3 point = mc != null && !mc.convex ? c.bounds.ClosestPoint(center) : c.ClosestPoint(center);
            Vector3 to = point - center;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > interactDistance)
            {
                continue;
            }
            float dot = dist > 0.05f ? Vector3.Dot(forward, to / dist) : 1f;
            if (dot < BehindLimit)
            {
                continue;
            }
            float score = dist - dot * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = target;
            }
        }
        currentInteractable = best;
    }
    private void SetButtonState(bool state)
    {
        if (interactButtonUI != null && interactButtonUI.activeSelf != state)
        {
            interactButtonUI.SetActive(state);
        }
    }
    public void OnInteractButtonPressed()
    {
        if (currentInteractable != null && cooldownTimer <= 0f)
        {
            cooldownTimer = interactCooldown;
            currentInteractable.Interact();
        }
    }
}
public interface IInteractable
{
    void Interact();
}
