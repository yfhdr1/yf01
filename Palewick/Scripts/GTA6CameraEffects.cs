using UnityEngine;
public class GTA6CameraEffects : MonoBehaviour
{
    public Transform playerRoot;
    public CharacterController controller;
    public float refWalkSpeed = 3.5f;
    public float refRunSpeed = 6.5f;
    public float stepRateWalk = 1.9f;
    public float stepRateRun = 2.8f;
    public float bobUpWalk = 0.035f;
    public float bobUpRun = 0.065f;
    public float bobSideWalk = 0.018f;
    public float bobSideRun = 0.03f;
    public float bobRollWalk = 0.5f;
    public float bobRollRun = 1.1f;
    public float bobPitchWalk = 0.35f;
    public float bobPitchRun = 0.8f;
    public float strafeTiltDeg = 1.8f;
    public float turnSwayDeg = 1.2f;
    public float swaySmooth = 7f;
    public float breathUp = 0.006f;
    public float breathPitchDeg = 0.25f;
    public float breathRate = 0.25f;
    public float landKick = 0.05f;
    public float landMaxDip = 0.14f;
    public float landStiffness = 70f;
    public float landDamping = 11f;
    public float sprintFovKick = 5f;
    public float fovSmooth = 5f;
    private Camera cam;
    private float phase;
    private float bobWeight;
    private float breathTime;
    private float roll;
    private float lastYaw;
    private bool hasYaw;
    private bool wasGrounded = true;
    private float minAirVelocity;
    private float landPos;
    private float landVel;
    private float fovDelta;
    private Vector3 appliedOffset;
    private Quaternion appliedRot = Quaternion.identity;
    private float appliedFov;
    private Vector3 lastWrittenPos;
    private Quaternion lastWrittenRot;
    private float lastWrittenFov;
    private bool hasWritten;
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
        cam = GetComponent<Camera>();
    }
    private void OnDisable()
    {
        RemoveApplied();
        phase = 0f;
        bobWeight = 0f;
        roll = 0f;
        hasYaw = false;
        landPos = 0f;
        landVel = 0f;
        fovDelta = 0f;
        wasGrounded = true;
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
                transform.localRotation = transform.localRotation * Quaternion.Inverse(appliedRot);
            }
            if (cam != null && Mathf.Approximately(cam.fieldOfView, lastWrittenFov))
            {
                cam.fieldOfView -= appliedFov;
            }
        }
        appliedOffset = Vector3.zero;
        appliedRot = Quaternion.identity;
        appliedFov = 0f;
        hasWritten = false;
    }
    private void LateUpdate()
    {
        RemoveApplied();
        if (controller == null || !controller.enabled)
        {
            return;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        Vector3 velocity = controller.velocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        float speed = flat.magnitude;
        bool grounded = controller.isGrounded;
        float runT = Mathf.Clamp01(Mathf.InverseLerp(refWalkSpeed, refRunSpeed, speed));
        float moveT = Mathf.Clamp01(speed / Mathf.Max(0.1f, refWalkSpeed));
        float targetWeight = grounded && speed > 0.15f ? moveT : 0f;
        bobWeight = Mathf.Lerp(bobWeight, targetWeight, Mathf.Clamp01(dt * 8f));
        if (grounded && speed > 0.15f)
        {
            phase += dt * Mathf.Lerp(stepRateWalk, stepRateRun, runT) * Mathf.PI;
            if (phase > Mathf.PI * 2f)
            {
                phase -= Mathf.PI * 2f;
            }
        }
        float step = Mathf.Abs(Mathf.Sin(phase));
        float sway = Mathf.Cos(phase);
        float up = (step - 0.64f) * Mathf.Lerp(bobUpWalk, bobUpRun, runT) * bobWeight;
        float side = sway * Mathf.Lerp(bobSideWalk, bobSideRun, runT) * bobWeight;
        float bobRoll = sway * Mathf.Lerp(bobRollWalk, bobRollRun, runT) * bobWeight;
        float bobPitch = (0.64f - step) * Mathf.Lerp(bobPitchWalk, bobPitchRun, runT) * bobWeight;
        breathTime += dt * breathRate * Mathf.PI * 2f;
        float idle = 1f - bobWeight;
        float breath = Mathf.Sin(breathTime) * breathUp * idle;
        float breathPitch = Mathf.Sin(breathTime - 0.6f) * breathPitchDeg * idle;
        float yaw = playerRoot != null ? playerRoot.eulerAngles.y : 0f;
        float yawSpeed = hasYaw ? Mathf.DeltaAngle(lastYaw, yaw) / dt : 0f;
        lastYaw = yaw;
        hasYaw = true;
        float lateral = playerRoot != null ? Vector3.Dot(flat, playerRoot.right) : 0f;
        float targetRoll = -Mathf.Clamp(yawSpeed / 120f, -2f, 2f) * turnSwayDeg * 0.5f;
        targetRoll -= Mathf.Clamp(lateral / Mathf.Max(0.1f, refRunSpeed), -1f, 1f) * strafeTiltDeg;
        roll = Mathf.Lerp(roll, targetRoll, Mathf.Clamp01(dt * swaySmooth));
        if (!grounded)
        {
            minAirVelocity = Mathf.Min(minAirVelocity, velocity.y);
        }
        else if (!wasGrounded)
        {
            float fall = -minAirVelocity;
            if (fall > 2f)
            {
                landVel -= fall * landKick;
            }
            minAirVelocity = 0f;
        }
        else
        {
            minAirVelocity = 0f;
        }
        wasGrounded = grounded;
        float acc = -landStiffness * landPos - landDamping * landVel;
        landVel += acc * dt;
        landPos += landVel * dt;
        landPos = Mathf.Clamp(landPos, -landMaxDip, landMaxDip * 0.3f);
        float fovTarget = grounded && runT > 0.3f ? sprintFovKick * runT : 0f;
        fovDelta = Mathf.Lerp(fovDelta, fovTarget, Mathf.Clamp01(dt * fovSmooth));
        Vector3 offset = new Vector3(side, up + breath + landPos, 0f);
        Quaternion rot = Quaternion.Euler(bobPitch + breathPitch - landPos * 25f, 0f, bobRoll + roll);
        transform.localPosition += offset;
        transform.localRotation = transform.localRotation * rot;
        appliedOffset = offset;
        appliedRot = rot;
        lastWrittenPos = transform.localPosition;
        lastWrittenRot = transform.localRotation;
        if (cam != null && !cam.orthographic)
        {
            cam.fieldOfView += fovDelta;
            appliedFov = fovDelta;
            lastWrittenFov = cam.fieldOfView;
        }
        hasWritten = true;
    }
}
