using UnityEngine;
using Photon.Pun;
public class FlashlightSway : MonoBehaviour
{
    public Camera aimCamera;
    public CharacterController controller;
    public float followSmooth = 9f;
    public float runFollowSmooth = 6f;
    public float refWalkSpeed = 3.5f;
    public float refRunSpeed = 6.5f;
    public float idleShakeDeg = 0.25f;
    public float walkShakeDeg = 0.6f;
    public float runShakeDeg = 1.6f;
    public float shakeRate = 1.3f;
    public float flickerChancePerSecond = 0.02f;
    public float flickerDuration = 0.12f;
    public float flickerMinFactor = 0.25f;
    private bool active;
    private Light spot;
    private float baseIntensity;
    private Quaternion originalLocal;
    private Quaternion smoothed;
    private bool hasSmoothed;
    private float noiseTime;
    private float flickerTimer;
    private void Start()
    {
        PhotonView pv = GetComponentInParent<PhotonView>();
        active = !PhotonNetwork.InRoom || pv == null || pv.IsMine;
        originalLocal = transform.localRotation;
        spot = GetComponent<Light>();
        if (spot != null)
        {
            baseIntensity = spot.intensity;
        }
        if (controller == null)
        {
            controller = GetComponentInParent<CharacterController>();
        }
        if (aimCamera == null && controller != null)
        {
            aimCamera = controller.GetComponentInChildren<Camera>(true);
        }
        noiseTime = Random.value * 100f;
    }
    private void OnDisable()
    {
        transform.localRotation = originalLocal;
        if (spot != null && baseIntensity > 0f)
        {
            spot.intensity = baseIntensity;
        }
        hasSmoothed = false;
        flickerTimer = 0f;
    }
    private void LateUpdate()
    {
        if (!active || aimCamera == null)
        {
            return;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        float speed = 0f;
        if (controller != null)
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            speed = v.magnitude;
        }
        float moveT = Mathf.Clamp01(speed / Mathf.Max(0.1f, refWalkSpeed));
        float runT = Mathf.Clamp01(Mathf.InverseLerp(refWalkSpeed, refRunSpeed, speed));
        Quaternion target = aimCamera.transform.rotation;
        if (!hasSmoothed)
        {
            smoothed = target;
            hasSmoothed = true;
        }
        float smooth = Mathf.Lerp(followSmooth, runFollowSmooth, runT);
        smoothed = Quaternion.Slerp(smoothed, target, 1f - Mathf.Exp(-smooth * dt));
        noiseTime += dt * shakeRate * (1f + runT * 2f);
        float amount = Mathf.Lerp(idleShakeDeg, walkShakeDeg, moveT);
        amount = Mathf.Lerp(amount, runShakeDeg, runT);
        float nx = (Mathf.PerlinNoise(noiseTime, 0.3f) - 0.5f) * 2f * amount;
        float ny = (Mathf.PerlinNoise(0.7f, noiseTime) - 0.5f) * 2f * amount;
        transform.rotation = smoothed * Quaternion.Euler(nx, ny, 0f);
        if (spot != null && baseIntensity > 0f)
        {
            if (flickerTimer > 0f)
            {
                flickerTimer -= dt;
                float k = Mathf.PerlinNoise(noiseTime * 25f, 1.7f);
                spot.intensity = baseIntensity * Mathf.Lerp(flickerMinFactor, 1f, k);
                if (flickerTimer <= 0f)
                {
                    spot.intensity = baseIntensity;
                }
            }
            else if (Random.value < flickerChancePerSecond * dt)
            {
                flickerTimer = flickerDuration;
            }
        }
    }
}
