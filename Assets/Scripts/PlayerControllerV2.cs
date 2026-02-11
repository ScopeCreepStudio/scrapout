using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PlayerControllerV2 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpForce = 6f;

    [Header("Sliding")]
    [SerializeField] float slideBoostMultiplier = 0.3f;
    [SerializeField] float slideSlopeAcceleration = 5f;
    [SerializeField] float slideSlopeSpeedBonus = 1.2f;
    [SerializeField] float slideJumpBoostMultiplier = 1.1f;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float cameraPitchLimit = 85f;

    [Header("Camera FOV")]
    [SerializeField] float fovLerpSpeed = 8f;
    [SerializeField] float fovMinSpeed = 5f;
    [SerializeField] float fovMaxSpeed = 15f;
    [SerializeField] float fovMaxIncrease = 20f;

    [Header("References")]
    [SerializeField] CharacterController controller;
    [SerializeField] Transform cameraHolder;
    [SerializeField] CinemachineCamera virtualCamera;

    [Header("Input Actions")]
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference lookAction;
    [SerializeField] InputActionReference sprintAction;
    [SerializeField] InputActionReference jumpAction;
    [SerializeField] InputActionReference crouchAction;

    // Derived constants (computed once in Start from the core values above)
    float crouchSpeed, crouchHeight, slideHeight;
    float slideStartSpeed, minSlideSpeed, slideMaxSlopeAngle;
    float slideDeceleration, slideUphillDeceleration;
    float groundAcceleration, groundFriction;
    float airAcceleration, airMaxSpeed, airSpeedCap;
    float sprintAcceleration, coyoteTime, slideCooldown;
    float turnSpeedThreshold, turnSpeedLoss;
    float crouchTransitionSpeed, groundStickForce;

    // Runtime state
    float pitch, yaw, lastYaw;
    float verticalVelocity, movementSpeed;
    float controllerStartHeight, baseFov;
    float lastGroundedTime, currentMoveMaxSpeed;
    float slideCooldownTimer;
    float lastSlopeAngle, lastSlopeTime;
    Vector3 horizontalVelocity, cameraStartPos;
    Vector3 slideDirection, lastSlopeDir;
    bool isCrouching, isSliding;

    // Cached per-frame (single raycast shared by all systems)
    bool frameGrounded, frameHasSlope;
    Vector3 frameSlopeDir, frameGroundNormal;
    float frameSlopeAngle;

    public float MovementSpeed => movementSpeed;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (controller != null)
            controllerStartHeight = controller.height;

        // Derive all tuning constants from core speed values
        crouchSpeed       = walkSpeed * 0.6f;
        crouchHeight      = controllerStartHeight * 0.15f; // crouch to ~85% of standing
        slideHeight       = controllerStartHeight * 0.5f;  // slide to ~50% of standing
        slideStartSpeed   = sprintSpeed;
        minSlideSpeed     = walkSpeed;
        slideMaxSlopeAngle = 45f;
        slideDeceleration = 3f;
        slideUphillDeceleration = 15f;
        groundAcceleration = 35f;
        groundFriction    = 8f;
        airAcceleration   = 20f;
        airMaxSpeed       = walkSpeed + 2f;
        airSpeedCap       = sprintSpeed + 1f;
        sprintAcceleration = 12f;
        coyoteTime        = 0.12f;
        slideCooldown     = 0.5f;
        turnSpeedThreshold = 240f;
        turnSpeedLoss     = 0.25f;
        crouchTransitionSpeed = 8f;
        groundStickForce  = 6f;

        SetupCameraHolder();

        if (virtualCamera != null)
        {
            if (virtualCamera.Follow == null) virtualCamera.Follow = cameraHolder;
            if (virtualCamera.LookAt == null) virtualCamera.LookAt = cameraHolder;
            baseFov = virtualCamera.Lens.FieldOfView;
        }

        if (cameraHolder != null)
            cameraStartPos = cameraHolder.localPosition;

        yaw = transform.eulerAngles.y;
        lastYaw = yaw;
        currentMoveMaxSpeed = walkSpeed;
    }

    void SetupCameraHolder()
    {
        if (cameraHolder == null)
            cameraHolder = transform.Find("CameraHolder");

        if (cameraHolder == null)
        {
            var holder = new GameObject("CameraHolder");
            holder.transform.SetParent(transform);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            cameraHolder = holder.transform;
        }
    }

    void OnEnable()  => SetActionsEnabled(true);
    void OnDisable() => SetActionsEnabled(false);

    void SetActionsEnabled(bool enabled)
    {
        InputActionReference[] actions = { moveAction, lookAction, sprintAction, jumpAction, crouchAction };
        foreach (var a in actions)
        {
            if (a == null) continue;
            if (enabled) a.action.Enable(); else a.action.Disable();
        }
    }

    void Update()
    {
        CacheFrameState();
        HandleMouseLook();
        HandleJump();
        HandleSlide();
        HandleCrouch();
        HandleMovement();
        UpdateHeightAndCamera();
        HandleCameraFov();
    }

    // ───────── Per-frame cache (one raycast for everything) ─────────

    void CacheFrameState()
    {
        if (controller == null) return;

        frameGrounded = controller.isGrounded;
        frameHasSlope = false;
        frameGroundNormal = Vector3.up;
        frameSlopeDir = Vector3.zero;
        frameSlopeAngle = 0f;

        float rayDist = (controllerStartHeight * 0.5f) + 0.3f;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, rayDist))
            return;

        Vector3 normal = hit.normal;
        if (Vector3.Dot(normal, Vector3.up) < 0f) normal = -normal;
        frameGroundNormal = normal;

        float angle = Vector3.Angle(normal, Vector3.up);
        if (angle <= 0.01f || angle > slideMaxSlopeAngle) return;

        Vector3 downSlope = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        if (downSlope.sqrMagnitude < 0.001f) return;

        frameHasSlope = true;
        frameSlopeDir = downSlope;
        frameSlopeAngle = angle;
        lastSlopeDir = downSlope;
        lastSlopeAngle = angle;
        lastSlopeTime = Time.time;
    }

    bool TryGetSlopeWithMemory(out Vector3 dir, out float angle)
    {
        if (frameHasSlope) { dir = frameSlopeDir; angle = frameSlopeAngle; return true; }
        if (Time.time - lastSlopeTime <= 0.2f) { dir = lastSlopeDir; angle = lastSlopeAngle; return true; }
        dir = Vector3.zero; angle = 0f; return false;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.zero;
    }

    // ───────── Input helpers ─────────

    Vector2 ReadVec2(InputActionReference a) => a != null ? a.action.ReadValue<Vector2>() : Vector2.zero;
    bool Held(InputActionReference a)    => a != null && a.action.ReadValue<float>() > 0.1f;
    bool Pressed(InputActionReference a) => a != null && a.action.WasPressedThisFrame();

    // ───────── Mouse Look ─────────

    void HandleMouseLook()
    {
        Vector2 look = ReadVec2(lookAction);
        yaw += look.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -cameraPitchLimit, cameraPitchLimit);

        // Turn momentum loss (ground only — preserve air momentum)
        if (frameGrounded && horizontalVelocity.sqrMagnitude > 0.001f)
        {
            float yawSpeed = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) / Mathf.Max(Time.deltaTime, 0.0001f);
            if (yawSpeed > turnSpeedThreshold)
            {
                float t = Mathf.InverseLerp(turnSpeedThreshold, turnSpeedThreshold * 2f, yawSpeed);
                horizontalVelocity *= 1f - Mathf.Lerp(0f, turnSpeedLoss, t);
            }
        }
        lastYaw = yaw;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraHolder != null)
            cameraHolder.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    // ───────── Movement ─────────

    void HandleMovement()
    {
        if (controller == null) return;

        if (frameGrounded) lastGroundedTime = Time.time;

        Vector2 input = ReadVec2(moveAction);
        bool hasMoveInput = input.sqrMagnitude > 0.01f;
        bool forwardOnly = input.y > 0.1f && Mathf.Abs(input.x) < 0.1f;
        IsSprinting = Held(sprintAction) && forwardOnly && !isCrouching && !isSliding;

        if (isSliding)
            UpdateSlideMovement();
        else
            UpdateNormalMovement(input.x, input.y, hasMoveInput);

        movementSpeed = horizontalVelocity.magnitude;

        if (frameGrounded && verticalVelocity < 0f)
            verticalVelocity = -groundStickForce;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    void UpdateSlideMovement()
    {
        bool onDownSlope = false, goingUphill = false;

        if (frameHasSlope)
        {
            Vector3 flatSlope = Flatten(frameSlopeDir);
            float dot = Vector3.Dot(horizontalVelocity.normalized, flatSlope);

            if (dot < -0.1f)
            {
                // Uphill — strong deceleration
                goingUphill = true;
                horizontalVelocity = slideDirection * Vector3.Dot(horizontalVelocity, slideDirection);

                float factor = Mathf.InverseLerp(0f, slideMaxSlopeAngle, frameSlopeAngle);
                float decel = slideUphillDeceleration * (1f + factor);
                float speed = horizontalVelocity.magnitude;
                horizontalVelocity = speed > 0f
                    ? horizontalVelocity.normalized * Mathf.Max(speed - decel * Time.deltaTime, 0f)
                    : Vector3.zero;
            }
            else
            {
                // Downhill — build speed
                onDownSlope = true;
                slideDirection = Vector3.Slerp(slideDirection, flatSlope, 8f * Time.deltaTime).normalized;

                float factor = Mathf.InverseLerp(0f, slideMaxSlopeAngle, frameSlopeAngle);
                float speed = horizontalVelocity.magnitude;
                float accel = slideSlopeAcceleration * factor + speed * slideSlopeSpeedBonus * factor;
                horizontalVelocity += slideDirection * (accel * Time.deltaTime);
            }
        }
        else
        {
            slideDirection = Flatten(slideDirection);
        }

        // Constrain to slide direction
        if (!goingUphill)
        {
            float fwd = Vector3.Dot(horizontalVelocity, slideDirection);
            if (!onDownSlope) fwd = Mathf.Max(fwd, 0f);
            horizontalVelocity = slideDirection * fwd;
        }

        // Flat ground deceleration
        if (!onDownSlope && !goingUphill)
        {
            float speed = horizontalVelocity.magnitude;
            if (speed > 0f)
                horizontalVelocity = horizontalVelocity.normalized * Mathf.Max(speed - slideDeceleration * Time.deltaTime, 0f);
        }
    }

    void UpdateNormalMovement(float h, float v, bool hasMoveInput)
    {
        float target = IsSprinting ? sprintSpeed : (isCrouching ? crouchSpeed : walkSpeed);
        currentMoveMaxSpeed = Mathf.MoveTowards(currentMoveMaxSpeed, target, sprintAcceleration * Time.deltaTime);

        Vector3 wishDir = transform.forward * v + transform.right * h;
        float wishMag = Mathf.Clamp01(wishDir.magnitude);
        if (wishMag > 0f) wishDir.Normalize();

        if (frameGrounded)
        {
            ApplyFriction(hasMoveInput ? groundFriction : groundFriction * 2f);
            horizontalVelocity = Vector3.ProjectOnPlane(horizontalVelocity, frameGroundNormal);
            Accelerate(wishDir, currentMoveMaxSpeed * wishMag, groundAcceleration);
        }
        else
        {
            float speedBefore = horizontalVelocity.magnitude;
            Accelerate(wishDir, airMaxSpeed * wishMag, airAcceleration * 0.4f);
            float speedAfter = horizontalVelocity.magnitude;

            // Never reduce existing momentum — only cap new speed from air strafing
            if (speedAfter > airSpeedCap && speedAfter > speedBefore)
                horizontalVelocity = horizontalVelocity.normalized * Mathf.Max(speedBefore, airSpeedCap);
        }
    }

    // ───────── Jump ─────────

    void HandleJump()
    {
        if (controller == null) return;

        bool canJump = frameGrounded || Time.time - lastGroundedTime <= coyoteTime;
        if (!Held(jumpAction) || !canJump) return;

        verticalVelocity = jumpForce;
        if (isSliding)
        {
            isSliding = false;
            horizontalVelocity *= slideJumpBoostMultiplier;
        }
    }

    // ───────── Slide ─────────

    void HandleSlide()
    {
        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        bool crouchHeld = Held(crouchAction);
        bool onSlope = TryGetSlopeWithMemory(out Vector3 slopeDir, out _) && frameSlopeAngle > 0.01f;

        // Start slide
        if (Pressed(crouchAction) && frameGrounded && !isSliding)
        {
            bool hasSpeed = horizontalVelocity.magnitude >= slideStartSpeed && slideCooldownTimer <= 0f && !isCrouching;

            bool tryingUphill = false;
            if (onSlope && horizontalVelocity.sqrMagnitude > 0.01f)
                tryingUphill = Vector3.Dot(horizontalVelocity.normalized, Flatten(slopeDir)) < -0.1f;

            if ((hasSpeed || onSlope) && !tryingUphill)
            {
                isSliding = true;
                slideCooldownTimer = slideCooldown;
                isCrouching = false;
                slideDirection = onSlope ? Flatten(slopeDir) : transform.forward;

                if (horizontalVelocity.magnitude > 0f)
                    horizontalVelocity += horizontalVelocity.normalized * (horizontalVelocity.magnitude * slideBoostMultiplier);
            }
        }

        // End slide
        if (isSliding && (!crouchHeld || horizontalVelocity.magnitude < minSlideSpeed))
            isSliding = false;
    }

    // ───────── Crouch ─────────

    void HandleCrouch()
    {
        bool crouchHeld = Held(crouchAction);
        if (isCrouching && !crouchHeld && !isSliding && CanStandUp())
            isCrouching = false;
        else if (crouchHeld && !isSliding)
            isCrouching = true;
    }

    // ───────── Height & Camera (single source of truth) ─────────

    void UpdateHeightAndCamera()
    {
        if (controller == null || cameraHolder == null) return;

        float targetHeight, cameraDrop;
        if (isSliding)      { targetHeight = slideHeight; cameraDrop = (controllerStartHeight - slideHeight) * 0.5f; }
        else if (isCrouching) { targetHeight = controllerStartHeight - crouchHeight; cameraDrop = crouchHeight; }
        else                  { targetHeight = controllerStartHeight; cameraDrop = 0f; }

        float t = Time.deltaTime * crouchTransitionSpeed;
        controller.height = Mathf.Lerp(controller.height, targetHeight, t);

        Vector3 camTarget = cameraStartPos;
        camTarget.y -= cameraDrop;
        cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, camTarget, t);
    }

    // ───────── FOV ─────────

    void HandleCameraFov()
    {
        if (virtualCamera == null) return;

        float speedT = Mathf.InverseLerp(fovMinSpeed, fovMaxSpeed, movementSpeed);
        float target = baseFov + fovMaxIncrease * speedT;
        virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, target, Time.deltaTime * fovLerpSpeed);
    }

    // ───────── Helpers ─────────

    bool CanStandUp()
    {
        if (controller == null) return true;
        float r = controller.radius;
        Vector3 bot = transform.position + Vector3.up * r;
        Vector3 top = transform.position + Vector3.up * (controllerStartHeight - r);

        foreach (Collider c in Physics.OverlapCapsule(bot, top, r))
            if (c.gameObject != gameObject) return false;
        return true;
    }

    void ApplyFriction(float friction)
    {
        float speed = horizontalVelocity.magnitude;
        if (speed <= 0f) return;
        horizontalVelocity *= Mathf.Max(speed - speed * friction * Time.deltaTime, 0f) / speed;
    }

    void Accelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        if (wishSpeed <= 0f) return;
        float addSpeed = wishSpeed - Vector3.Dot(horizontalVelocity, wishDir);
        if (addSpeed <= 0f) return;
        horizontalVelocity += wishDir * Mathf.Min(accel * Time.deltaTime * wishSpeed, addSpeed);
    }

    // ───────── Debug ─────────

    void OnDrawGizmos()
    {
        if (controller == null) return;
        float r = controller.radius;
        Vector3 bot = transform.position + Vector3.up * r;
        Vector3 top = transform.position + Vector3.up * (controller.height - r);

        Gizmos.color = CanStandUp() ? Color.green : Color.red;
        Gizmos.DrawLine(bot, top);
        Gizmos.DrawWireSphere(bot, r);
        Gizmos.DrawWireSphere(top, r);
    }
}
