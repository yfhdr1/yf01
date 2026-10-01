using UnityEngine;
using Photon.Pun;
public class CameraViewSwitcher : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject characterMesh;
    public CharController_Motor playerMotor;
    public bool IsThirdPerson;
    public float fpHeight = 1.7f;
    public float tpDistance = 3.5f;
    public float tpHeight = 1.5f;
    public float tpSmoothSpeed = 15f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.3f;
    public float minDistance = 0.5f;
    public float fpFov = 60f;
    public float tpFov = 70f;
    public float fovSmoothSpeed = 10f;
    public float bobFrequency = 1.8f;
    public float bobHorizontal = 0.04f;
    public float bobVertical = 0.05f;
    public float bobSpeedThreshold = 0.5f;
    public float bobSmoothSpeed = 8f;
    private readonly RaycastHit[] hits = new RaycastHit[10];
    private float bobTimer;
    private float bobAmount;
    private float targetFov;
    private Vector3 cameraPosition;
    private void Start()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && pv != null && !pv.IsMine)
        {
            enabled = false;
            return;
        }
        if (mainCamera == null) mainCamera = Camera.main;
        if (playerMotor == null) playerMotor = GetComponent<CharController_Motor>();
        targetFov = fpFov;
        cameraPosition = transform.position + Vector3.up * fpHeight;
        SetFirstPerson();
    }
    public void ToggleView()
    {
        if (IsThirdPerson) SetFirstPerson();
        else SetThirdPerson();
    }
    private void SetFirstPerson()
    {
        IsThirdPerson = false;
        targetFov = fpFov;
        if (characterMesh != null) characterMesh.SetActive(false);
    }
    private void SetThirdPerson()
    {
        IsThirdPerson = true;
        targetFov = tpFov;
        if (characterMesh != null) characterMesh.SetActive(true);
    }
    private void LateUpdate()
    {
        if (mainCamera == null || playerMotor == null) return;
        float xRot = Mathf.Clamp(playerMotor.CameraPitch, minPitch, maxPitch);
        float yOffset = playerMotor.CameraYawOffset;
        Quaternion rotation = Quaternion.Euler(xRot, transform.eulerAngles.y + yOffset, 0f);
        if (!Mathf.Approximately(mainCamera.fieldOfView, targetFov))
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFov, Time.deltaTime * fovSmoothSpeed);
        }
        if (!IsThirdPerson)
        {
            Vector3 fpPos = transform.position + (Vector3.up * fpHeight);
            Vector2 horizontalVel = new Vector2(playerMotor.CurrentVelocity.x, playerMotor.CurrentVelocity.z);
            float speed = horizontalVel.magnitude;
            float targetBob = (speed > bobSpeedThreshold && playerMotor.IsGrounded) ? 1f : 0f;
            bobAmount = Mathf.Lerp(bobAmount, targetBob, Time.deltaTime * bobSmoothSpeed);
            if (bobAmount > 0.01f)
            {
                float moveSpeedFactor = playerMotor.MoveSpeed > 0f ? (speed / playerMotor.MoveSpeed) : 1f;
                bobTimer += Time.deltaTime * bobFrequency * moveSpeedFactor;
                fpPos += (Vector3.up * Mathf.Sin(bobTimer * 2f) * bobVertical * bobAmount);
                fpPos += (transform.right * Mathf.Cos(bobTimer) * bobHorizontal * bobAmount);
            }
            cameraPosition = Vector3.Lerp(cameraPosition, fpPos, Time.deltaTime * 20f);
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.rotation = rotation;
        }
        else
        {
            Vector3 pivot = transform.position + (Vector3.up * tpHeight);
            Vector3 desiredPos = pivot - (rotation * Vector3.forward * tpDistance);
            float finalDist = tpDistance;
            Vector3 dir = (desiredPos - pivot).normalized;
            int hitCount = Physics.SphereCastNonAlloc(pivot, collisionRadius, dir, hits, tpDistance, collisionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].transform != null && hits[i].transform.root != transform.root)
                {
                    if (hits[i].distance < finalDist) finalDist = hits[i].distance;
                }
            }
            finalDist = Mathf.Max(minDistance, finalDist);
            Vector3 finalPos = pivot - (rotation * Vector3.forward * finalDist);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, finalPos, Time.deltaTime * tpSmoothSpeed);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, rotation, Time.deltaTime * tpSmoothSpeed);
            cameraPosition = mainCamera.transform.position;
        }
    }
}
