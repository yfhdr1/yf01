using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class FootstepSoundController : MonoBehaviour
{
    public AudioClip footstepLoopSound;
    public AudioSource audioSource;
    public float minMoveSpeed = 0.4f;
    public float sprintSpeed = 5f;
    public float walkPitch = 1f;
    public float sprintPitch = 1.25f;
    public float stopDelay = 0.15f;
    private CharacterController characterController;
    private Vector3 lastPosition;
    private float smoothSpeed;
    private float stopTimer;
    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.loop = true;
        audioSource.clip = footstepLoopSound;
        audioSource.volume = 1f;
        lastPosition = transform.position;
    }
    private void Update()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            return;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;
        smoothSpeed = Mathf.Lerp(smoothSpeed, delta.magnitude / dt, dt * 10f);
        bool grounded = characterController == null || !characterController.enabled || characterController.isGrounded;
        bool moving = smoothSpeed > minMoveSpeed && grounded;
        if (moving)
        {
            stopTimer = 0f;
        }
        else
        {
            stopTimer += dt;
        }
        if (moving || stopTimer < stopDelay)
        {
            if (!audioSource.isPlaying && moving)
            {
                audioSource.Play();
            }
            audioSource.pitch = smoothSpeed >= sprintSpeed ? sprintPitch : walkPitch;
        }
        else if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
