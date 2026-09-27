using UnityEngine;
public class GTA6CameraEffects : MonoBehaviour
{
    public Transform playerRoot;
    public CharacterController controller;
    public float walkBobSpeed = 12f;
    public float walkBobAmount = 0.04f;
    public float runBobSpeed = 16f;
    public float runBobAmount = 0.08f;
    public float swayAmount = 1.5f;
    public float swaySmoothness = 6f;
    private float timer;
    private Vector3 bobOffset;
    private float currentRoll;
    private Vector3 appliedOffset;
    private float appliedRoll;
    private Vector3 lastWrittenPos;
    private Quaternion lastWrittenRot;
    private bool hasWritten;
    private float lastYaw;
    private bool hasYaw;
    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<CharacterController>();
        }
        if (playerRoot == null)
        {
            playerRoot = controller != null ? controller.transform : transform.root;
        }
    }
    private void OnDisable()
    {
        RemoveApplied();
        timer = 0f;
        bobOffset = Vector3.zero;
        currentRoll = 0f;
        hasYaw = false;
    }
    private void RemoveApplied()
    {
        if (hasWritten)
        {
            if (transform.localPosition == lastWrittenPos)
            {
                transform.localPosition -= appliedOffset;
            }
            if (transform.localRotation == lastWrittenRot)
            {
                transform.localRotation = transform.localRotation * Quaternion.Euler(0f, 0f, -appliedRoll);
            }
        }
        appliedOffset = Vector3.zero;
        appliedRoll = 0f;
        hasWritten = false;
    }
    private void LateUpdate()
    {
        RemoveApplied();
        if (controller == null)
        {
            return;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;
        if (speed > 0.1f && controller.isGrounded)
        {
            bool isRunning = speed > 4.5f;
            float currentBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float currentBobAmount = isRunning ? runBobAmount : walkBobAmount;
            timer += dt * currentBobSpeed;
            bobOffset = new Vector3(Mathf.Cos(timer * 0.5f) * currentBobAmount * 0.6f, Mathf.Sin(timer) * currentBobAmount, 0f);
        }
        else
        {
            timer = 0f;
            bobOffset = Vector3.Lerp(bobOffset, Vector3.zero, Mathf.Clamp01(dt * 8f));
        }
        float yaw = playerRoot != null ? playerRoot.eulerAngles.y : 0f;
        float yawSpeed = hasYaw ? Mathf.DeltaAngle(lastYaw, yaw) / dt : 0f;
        lastYaw = yaw;
        hasYaw = true;
        float targetRoll = -Mathf.Clamp(yawSpeed / 120f, -2f, 2f) * swayAmount;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Mathf.Clamp01(dt * swaySmoothness));
        transform.localPosition += bobOffset;
        transform.localRotation = transform.localRotation * Quaternion.Euler(0f, 0f, currentRoll);
        appliedOffset = bobOffset;
        appliedRoll = currentRoll;
        lastWrittenPos = transform.localPosition;
        lastWrittenRot = transform.localRotation;
        hasWritten = true;
    }
}
