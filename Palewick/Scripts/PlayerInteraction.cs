using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
    private const float KeepTime = 0.5f;
    private IInteractable currentInteractable;
    private IInteractable shownInteractable;
    private float lastSeenTime = -10f;
    private float cooldownTimer;
    private float nextScan;
    private Transform body;
    private GameObject hookedButton;
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
        shownInteractable = null;
    }
    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        HookButton();
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + ScanInterval;
            Scan();
        }
        if (currentInteractable != null)
        {
            shownInteractable = currentInteractable;
            lastSeenTime = Time.time;
        }
        else if (Time.time - lastSeenTime > KeepTime)
        {
            shownInteractable = null;
        }
        SetButtonState(shownInteractable != null);
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInteractButtonPressed();
        }
    }
    private void HookButton()
    {
        if (interactButtonUI == null || hookedButton == interactButtonUI)
        {
            return;
        }
        hookedButton = interactButtonUI;
        Button b = interactButtonUI.GetComponent<Button>();
        if (b != null)
        {
            b.onClick.RemoveAllListeners();
        }
        InteractPress press = interactButtonUI.GetComponent<InteractPress>();
        if (press == null)
        {
            press = interactButtonUI.AddComponent<InteractPress>();
        }
        press.owner = this;
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
            if (!CanSee(center, point, c, target))
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
    private bool CanSee(Vector3 from, Vector3 to, Collider targetCollider, IInteractable target)
    {
        Vector3 dir = to - from;
        float len = dir.magnitude;
        if (len < 0.1f)
        {
            return true;
        }
        RaycastHit[] hits = Physics.RaycastAll(from, dir / len, len - 0.05f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider h = hits[i].collider;
            if (h == null || h == targetCollider || h.transform.IsChildOf(body))
            {
                continue;
            }
            if (h.GetComponentInParent<IInteractable>() == target)
            {
                continue;
            }
            return false;
        }
        return true;
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
        IInteractable target = currentInteractable != null ? currentInteractable : shownInteractable;
        if (target != null && cooldownTimer <= 0f)
        {
            cooldownTimer = interactCooldown;
            target.Interact();
        }
    }
}
public class InteractPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerInteraction owner;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null)
        {
            owner.OnInteractButtonPressed();
        }
        transform.localScale = new Vector3(0.9f, 0.9f, 1f);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
    private void OnDisable()
    {
        transform.localScale = Vector3.one;
    }
}
public interface IInteractable
{
    void Interact();
}
