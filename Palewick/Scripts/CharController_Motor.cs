using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
[RequireComponent(typeof(CharacterController))]
public class CharController_Motor : MonoBehaviourPun
{
    public float moveSpeed = 3.5f;
    public float sprintSpeed = 6.5f;
    public float acceleration = 8f;
    public float deceleration = 12f;
    public float rotationSpeed = 100f;
    public float gravity = -19.62f;
    public float touchLookSensitivity = 0.15f;
    public Joystick moveJoystick;
    public Animator animator;
    public CameraViewSwitcher cameraSwitcher;
    public float freeLookReturnSpeed = 8f;
    public float rotationAccel = 8f;
    public float pitchAccel = 12f;
    public float analogDeadzone = 0.12f;
    public float jumpHeight = 1.1f;
    public float jumpCooldown = 0.35f;
    public float jumpBufferTime = 0.2f;
    [HideInInspector] public bool isSprinting;
    private CharacterController controller;
    private float verticalVelocity;
    private float moveInput;
    private float strafeInput;
    private float yawInput;
    private float pitchInput;
    private float smoothYaw;
    private float smoothPitch;
    private float analogMagnitude;
    private int lookTouchId = -1;
    private float cameraPitch;
    private float cameraYawOffset;
    private bool isMoving;
    private EventSystem currentEventSystem;
    private Vector3 currentVelocity;
    private float jumpRequestTime = -10f;
    private float lastJumpTime = -10f;
    public float CameraYawOffset
    {
        get { return cameraYawOffset; }
    }
    public float CameraPitch
    {
        get { return cameraPitch; }
    }
    public Vector3 CurrentVelocity
    {
        get { return currentVelocity; }
    }
    public float MoveSpeed
    {
        get { return moveSpeed; }
    }
    public bool IsGrounded
    {
        get { return controller != null && controller.isGrounded; }
    }
    public bool IsLocal
    {
        get { return photonView == null || !PhotonNetwork.InRoom || photonView.IsMine; }
    }
    public void Jump()
    {
        if (!IsLocal)
        {
            return;
        }
        jumpRequestTime = Time.time;
        Debug.Log("PW_JUMP pressed grounded=" + IsOnGround());
    }
    private bool IsOnGround()
    {
        if (controller == null)
        {
            return false;
        }
        if (controller.isGrounded)
        {
            return true;
        }
        float scale = Mathf.Abs(transform.lossyScale.y);
        float r = controller.radius * scale * 0.9f;
        Vector3 origin = transform.TransformPoint(controller.center);
        float down = Mathf.Max(0.05f, controller.height * scale * 0.5f - r) + controller.skinWidth + 0.2f;
        RaycastHit hit;
        return verticalVelocity <= 0.5f && Physics.SphereCast(origin, r, Vector3.down, out hit, down, ~0, QueryTriggerInteraction.Ignore);
    }
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        touchLookSensitivity = PlayerPrefs.GetFloat("TouchSensitivity", 0.15f);
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (cameraSwitcher == null)
        {
            cameraSwitcher = GetComponent<CameraViewSwitcher>();
        }
        if (moveJoystick == null)
        {
            moveJoystick = Object.FindAnyObjectByType<FixedJoystick>();
        }
        currentEventSystem = EventSystem.current;
    }
    private void Update()
    {
        if (photonView != null && PhotonNetwork.InRoom && !photonView.IsMine)
        {
            return;
        }
        yawInput = 0f;
        pitchInput = 0f;
        float rawMove = 0f;
        float rawStrafe = 0f;
        if (moveJoystick != null)
        {
            rawMove = moveJoystick.Vertical;
            rawStrafe = moveJoystick.Horizontal;
        }
        if (Input.touchCount > 0)
        {
            HandleMobileLook();
        }
        else
        {
            HandlePCInput();
            if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f)
            {
                rawMove = Input.GetAxisRaw("Vertical");
                rawStrafe = Input.GetAxisRaw("Horizontal");
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
        }
        Vector2 analog = new Vector2(rawStrafe, rawMove);
        analogMagnitude = analog.magnitude;
        if (analogMagnitude > 1f)
        {
            analog = analog.normalized;
            analogMagnitude = 1f;
        }
        isMoving = analogMagnitude > analogDeadzone;
        if (!isMoving)
        {
            isSprinting = false;
            analogMagnitude = 0f;
        }
        moveInput = analog.y;
        strafeInput = analog.x;
        RotatePlayerAndCamera();
        UpdateAnimator();
    }
    private void FixedUpdate()
    {
        if (photonView != null && PhotonNetwork.InRoom && !photonView.IsMine)
        {
            return;
        }
        MovePlayer();
    }
    private void HandlePCInput()
    {
        yawInput = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        pitchInput = -Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
    }
    private void HandleMobileLook()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            bool isRightSide = touch.position.x > Screen.width / 2f;
            if (touch.phase == TouchPhase.Began)
            {
                if (currentEventSystem != null && currentEventSystem.IsPointerOverGameObject(touch.fingerId))
                {
                    continue;
                }
                if (isRightSide && lookTouchId == -1)
                {
                    lookTouchId = touch.fingerId;
                }
            }
            if (touch.fingerId != lookTouchId)
            {
                continue;
            }
            if (touch.phase == TouchPhase.Moved)
            {
                yawInput = touch.deltaPosition.x * touchLookSensitivity;
                pitchInput = -touch.deltaPosition.y * touchLookSensitivity;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lookTouchId = -1;
            }
        }
    }
    private void MovePlayer()
    {
        if (controller == null)
        {
            return;
        }
        float targetSpeed = isMoving ? (isSprinting ? sprintSpeed : moveSpeed) * analogMagnitude : 0f;
        Vector3 inputDir = (transform.forward * moveInput + transform.right * strafeInput).normalized;
        Vector3 targetVelocity = Vector3.zero;
        if (isMoving)
        {
            targetVelocity = inputDir * targetSpeed;
        }
        float accelRate = isMoving ? acceleration : deceleration;
        currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetVelocity.x, accelRate * Time.fixedDeltaTime);
        currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, targetVelocity.z, accelRate * Time.fixedDeltaTime);
        bool grounded = IsOnGround();
        if (grounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            bool wantsJump = Time.time - jumpRequestTime <= jumpBufferTime;
            if (wantsJump && Time.time - lastJumpTime >= jumpCooldown)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpTime = Time.time;
                jumpRequestTime = -10f;
                Debug.Log("PW_JUMP go");
            }
        }
        else
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
        }
        Vector3 finalMove = currentVelocity;
        finalMove.y = verticalVelocity;
        controller.Move(finalMove * Time.fixedDeltaTime);
    }
    private void RotatePlayerAndCamera()
    {
        smoothYaw = Mathf.Lerp(smoothYaw, yawInput, rotationAccel * Time.deltaTime);
        smoothPitch = Mathf.Lerp(smoothPitch, pitchInput, pitchAccel * Time.deltaTime);
        bool isThirdPerson = cameraSwitcher != null && cameraSwitcher.IsThirdPerson;
        if (isThirdPerson)
        {
            if (!isMoving)
            {
                cameraYawOffset += smoothYaw;
            }
            else
            {
                transform.Rotate(0f, smoothYaw, 0f);
                if (Mathf.Abs(cameraYawOffset) > 0.01f)
                {
                    float returnStep = cameraYawOffset * Time.deltaTime * freeLookReturnSpeed;
                    transform.Rotate(0f, returnStep, 0f);
                    cameraYawOffset -= returnStep;
                }
            }
        }
        else
        {
            transform.Rotate(0f, smoothYaw, 0f);
            cameraYawOffset = 0f;
        }
        cameraPitch += smoothPitch;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
    }
    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }
        Vector2 horizontalVel = new Vector2(currentVelocity.x, currentVelocity.z);
        float currentMag = horizontalVel.magnitude;
        float speedValue = 0f;
        if (currentMag > 0.1f)
        {
            speedValue = (currentMag > moveSpeed + 0.1f) ? 1f : 0.5f;
        }
        animator.SetFloat("Speed", speedValue, 0.15f, Time.deltaTime);
    }
}
