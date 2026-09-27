using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class DoorController : MonoBehaviourPunCallbacks, IInteractable
{
    public float openAngle = 90f;
    public float openSpeed = 3f;
    public bool invertDirection;
    private bool isOpen;
    private bool isMoving;
    private float currentDirection = 1f;
    private Quaternion defaultRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;
    private void Awake()
    {
        defaultRotation = transform.localRotation;
        openRotation = defaultRotation * Quaternion.Euler(0f, openAngle, 0f);
        targetRotation = defaultRotation;
    }
    private void Update()
    {
        if (!isMoving)
        {
            return;
        }
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Mathf.Clamp01(Time.deltaTime * openSpeed));
        if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
        {
            transform.localRotation = targetRotation;
            isMoving = false;
        }
    }
    public void Interact()
    {
        bool wantOpen = !isOpen;
        float dir = currentDirection;
        if (wantOpen)
        {
            Camera mainCam = Camera.main;
            Transform referenceCam = mainCam != null ? mainCam.transform : transform;
            Vector3 toDoor = transform.position - referenceCam.position;
            toDoor.y = 0f;
            Vector3 doorAxis = transform.right;
            doorAxis.y = 0f;
            float side = Vector3.Dot(doorAxis.normalized, toDoor.normalized);
            dir = side > 0f ? 1f : -1f;
            if (invertDirection)
            {
                dir = -dir;
            }
        }
        if (photonView != null && PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_SetDoor), RpcTarget.AllViaServer, wantOpen, dir);
        }
        else
        {
            SetDoor(wantOpen, dir, false);
        }
    }
    [PunRPC]
    private void RPC_SetDoor(bool open, float dir)
    {
        SetDoor(open, dir, false);
    }
    [PunRPC]
    private void RPC_SyncDoor(bool open, float dir)
    {
        SetDoor(open, dir, true);
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || photonView == null || !isOpen) return;
        photonView.RPC(nameof(RPC_SyncDoor), newPlayer, isOpen, currentDirection);
    }
    private void SetDoor(bool open, float dir, bool instant)
    {
        if (open)
        {
            currentDirection = dir;
            openRotation = defaultRotation * Quaternion.Euler(0f, openAngle * dir, 0f);
        }
        isOpen = open;
        targetRotation = isOpen ? openRotation : defaultRotation;
        if (instant)
        {
            transform.localRotation = targetRotation;
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }
    }
}
