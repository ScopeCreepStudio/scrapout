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
    [SerializeField] float groundAcceleration = 35f;
    [SerializeField] float groundFriction = 8f;
    [SerializeField] float groundStickForce = 6f;
    [SerializeField] float sprintAcceleration = 12f;
    [SerializeField] float coyoteTime = 0.12f;

    [Header("Air Movement")]
    [SerializeField] float airAcceleration = 20f;
    [SerializeField] float airMaxSpeed = 7f;

    [Header("Crouching")]
    [SerializeField] float crouchSpeed = 3f;
    [SerializeField] float crouchHeight = 1.4f;
    [SerializeField] float crouchTransitionSpeed = 8f;

    [Header("Wall Running")]
    [SerializeField] float wallRunGravity = -5f;
    [SerializeField] float wallRunTiltAngle = 15f;
    [SerializeField] float wallRunTiltSpeed = 8f;
    [SerializeField] float wallDetectionDistance = 0.7f;
    [SerializeField] float wallJumpForce = 8f;
    [SerializeField] float wallJumpSideForce = 6f;

    [Header("Wall Jump Chain (Experimenting)")]
    [SerializeField] float wallJumpChainBoost = 1.15f;
    [SerializeField] float wallJumpMaxChainMultiplier = 1.6f;
    [SerializeField] float wallJumpChainResetTime = 1.2f;

    [SerializeField] LayerMask wallMask = ~0;


    [Header("Sliding")]
    [SerializeField] float slideBoostMultiplier = 0.3f;
    [SerializeField] float slideSlopeAcceleration = 1f;
    [SerializeField] float slideSlopeSpeedBonus = 0.5f;
    [SerializeField] float slideJumpBoostMultiplier = 1.1f;
    [SerializeField] float slideHeight = 1f;
    [SerializeField] float slideStartSpeed = 8f;
    [SerializeField] float minSlideSpeed = 5f;
    [SerializeField] float slideMaxSlopeAngle = 45f;
    [SerializeField] float slideDeceleration = 3f;
    [SerializeField] float slideUphillDeceleration = 15f;
    [SerializeField] float slideCooldown = 0.5f;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float cameraPitchLimit = 85f;
    [SerializeField] float turnSpeedThreshold = 240f;
    [SerializeField] float turnSpeedLoss = 0.25f;

    [Header("Jump")]
    [SerializeField] bool holdToJump = true;

    [Header("Shooting")]
    [SerializeField] GunAssembler gun;

    [Header("Recoil")]
    [SerializeField] float recoilPitchKick = 2f;
    [SerializeField] float recoilReturnSpeed = 18f;
    [SerializeField] float recoilMaxPitch = 12f;

    [Header("Hover Outline")]
    [SerializeField] string gunPartTag = "GunPart";
    [SerializeField] float hoverOutlineDistance = 3f;
    [SerializeField] LayerMask hoverOutlineMask = ~0;
    [SerializeField] Color hoverOutlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] float hoverOutlineWidth = 2f;

    [Header("Camera FOV")]
    [SerializeField] float fovLerpSpeed = 8f;
    [SerializeField] float fovMinSpeed = 5f;
    [SerializeField] float fovMaxSpeed = 15f;
    [SerializeField] float fovMaxIncrease = 20f;

    [Header("References")]
    [SerializeField] CharacterController controller;
    [SerializeField] Transform cameraHolder;
    [SerializeField] CinemachineCamera virtualCamera;
    [SerializeField] StatusEffectManager statusEffectManager;

    [Header("Input Actions")]
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference lookAction;
    [SerializeField] InputActionReference sprintAction;
    [SerializeField] InputActionReference jumpAction;
    [SerializeField] InputActionReference crouchAction;
    [SerializeField] InputActionReference fireAction;
    [SerializeField] InputActionReference reloadAction;
    [SerializeField] InputActionReference adsAction;



    // Runtime state
    float pitch, yaw, lastYaw;
    float verticalVelocity, movementSpeed;
    float controllerStartHeight, baseFov;
    float lastGroundedTime, currentMoveMaxSpeed;
    float slideCooldownTimer;
    float lastSlopeAngle, lastSlopeTime;
    Vector3 horizontalVelocity, cameraStartPos;
    Vector3 slideDirection, lastSlopeDir;
    bool isCrouching, isSliding, justJumped, isWallRunning;
    float slideEndTime = -1f;
    float slideJumpMinSpeed;
    float lastJumpPressTime = -1f;
    float recoilOffset;
    Outline currentOutline;
    bool currentOutlineAdded;
    float currentCameraTilt;

    //Experimental
    int wallJumpChainCount;
    float lastWallJumpTime;


    Vector3 wallNormal;
    float wallRunStartTime;

    // Cached per-frame (single raycast shared by all systems)
    bool frameGrounded, frameHasSlope;
    Vector3 frameSlopeDir, frameGroundNormal;
    float frameSlopeAngle;

    public float MovementSpeed => movementSpeed;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;
    public bool IsGrounded => frameGrounded;
    public InputActionReference AdsAction => adsAction;

    public bool IsWallRunning => isWallRunning;
    // Set by status effects (pitch, yaw) in degrees — applied each frame in HandleMouseLook
    public Vector2 ShakeOffset { get; set; }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (controller != null)
            controllerStartHeight = controller.height;

        if (statusEffectManager == null)
            statusEffectManager = GetComponent<StatusEffectManager>();

        SetupCameraHolder();

        if (virtualCamera != null)
        {
            if (virtualCamera.Follow == null) virtualCamera.Follow = cameraHolder;
            if (virtualCamera.LookAt == null) virtualCamera.LookAt = cameraHolder;
            baseFov = virtualCamera.Lens.FieldOfView;
        }

        if (cameraHolder != null)
            cameraStartPos = cameraHolder.localPosition;

        if (gun == null)
            gun = GetComponentInChildren<GunAssembler>();

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

    void OnEnable() => SetActionsEnabled(true);
    void OnDisable() => SetActionsEnabled(false);

    void SetActionsEnabled(bool enabled)
    {
        foreach (var a in new[] { moveAction, lookAction, sprintAction, jumpAction, crouchAction, fireAction, reloadAction, adsAction })
        {
            if (a == null) continue;
            if (enabled) a.action.Enable(); else a.action.Disable();
        }
    }

    void Update()
    {
        justJumped = false;
        CacheFrameState();
        HandleHoverOutline();
        HandleShooting();
        HandleMouseLook();
        HandleJump();
        HandleSlide();
        HandleCrouch();
        HandleWallRun();
        HandleWallRunTilt();
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
    bool Held(InputActionReference a) => a != null && a.action.ReadValue<float>() > 0.1f;
    bool Pressed(InputActionReference a) => a != null && a.action.WasPressedThisFrame();

    // ───────── Mouse Look ─────────

    void HandleMouseLook()
    {
        Vector2 look = ReadVec2(lookAction);
        yaw += look.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -cameraPitchLimit, cameraPitchLimit);

        if (recoilOffset != 0f)
            recoilOffset = Mathf.MoveTowards(recoilOffset, 0f, recoilReturnSpeed * Time.deltaTime);

        float finalPitch = Mathf.Clamp(pitch + recoilOffset, -cameraPitchLimit, cameraPitchLimit);

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

        transform.rotation = Quaternion.Euler(0f, yaw + ShakeOffset.y, 0f);
        if (cameraHolder != null)
            cameraHolder.localEulerAngles = new Vector3(finalPitch + ShakeOffset.x, 0f, currentCameraTilt);
    }

    // ───────── Shooting & Recoil ─────────

    void HandleShooting()
    {
        if (gun == null) return;

        if (Held(fireAction))
        {
            Transform origin = cameraHolder != null ? cameraHolder : transform;
            if (gun.Shoot(origin.position, origin.forward))
                ApplyRecoil();
        }

        if (Pressed(reloadAction))
            gun.Reload();
    }

    void ApplyRecoil()
    {
        if (recoilPitchKick <= 0f) return;
        recoilOffset = Mathf.Max(recoilOffset - recoilPitchKick, -Mathf.Abs(recoilMaxPitch));
    }

    // ───────── Hover Outline ─────────

    void HandleHoverOutline()
    {
        Transform origin = cameraHolder != null ? cameraHolder : transform;
        if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, hoverOutlineDistance, hoverOutlineMask, QueryTriggerInteraction.Collide))
        {
            ClearCurrentOutline();
            return;
        }

        if (!hit.collider.CompareTag(gunPartTag))
        {
            ClearCurrentOutline();
            return;
        }

        Outline outline = hit.collider.GetComponentInParent<Outline>();
        bool added = false;
        if (outline == null)
        {
            outline = hit.collider.gameObject.AddComponent<Outline>();
            added = true;
        }

        if (outline != currentOutline)
        {
            ClearCurrentOutline();
            currentOutline = outline;
            currentOutlineAdded = added;
        }

        currentOutline.OutlineMode = Outline.Mode.OutlineAll;
        currentOutline.OutlineColor = hoverOutlineColor;
        currentOutline.OutlineWidth = hoverOutlineWidth;
        currentOutline.enabled = true;
    }

    void ClearCurrentOutline()
    {
        if (currentOutline == null) return;

        if (currentOutlineAdded)
            Destroy(currentOutline);
        else
            currentOutline.enabled = false;

        currentOutline = null;
        currentOutlineAdded = false;
    }

    // ───────── Movement ─────────

    void HandleMovement()
    {
        if (controller == null) return;

        if (frameGrounded)
        {
            wallJumpChainCount = 0;
            lastGroundedTime = Time.time;
            if (verticalVelocity <= 0f) slideJumpMinSpeed = 0f; // clear on landing
        }

        // If fully stunned (movementSpeedMultiplier == 0), kill velocity and skip movement
        if (IsStunned())
        {
            horizontalVelocity = Vector3.zero;
            isSliding = false;
            movementSpeed = 0f;
            if (frameGrounded && verticalVelocity < 0f)
                verticalVelocity = -groundStickForce;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            return;
        }

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
        float speedMultiplier = GetMovementSpeedMultiplier();
        float target = IsSprinting ? sprintSpeed : (isCrouching ? crouchSpeed : walkSpeed);
        target *= speedMultiplier;
        currentMoveMaxSpeed = Mathf.MoveTowards(currentMoveMaxSpeed, target, sprintAcceleration * Time.deltaTime);

        Vector3 wishDir = transform.forward * v + transform.right * h;
        float wishMag = Mathf.Clamp01(wishDir.magnitude);
        if (wishMag > 0f) wishDir.Normalize();

        if (frameGrounded && !justJumped && verticalVelocity <= 0f)
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

            // Don't gain speed in air beyond entry speed or ground max
            // (preserves slide-jump momentum while preventing walk-jump speed gain)
            float maxAllowed = Mathf.Max(speedBefore, currentMoveMaxSpeed);
            if (speedAfter > maxAllowed)
                horizontalVelocity = horizontalVelocity.normalized * maxAllowed;

            // Protect slide-jump speed floor
            if (slideJumpMinSpeed > 0f)
            {
                float mag = horizontalVelocity.magnitude;
                if (mag > 0.001f && mag < slideJumpMinSpeed)
                    horizontalVelocity = horizontalVelocity.normalized * slideJumpMinSpeed;
            }
        }
    }

    // ───────── Jump ─────────

    void HandleJump()
    {
        if (controller == null) return;

        //Wall Jump, Overrides default jump when on wall.
        if (isWallRunning && Pressed(jumpAction))
        {
            isWallRunning = false;

            verticalVelocity = wallJumpForce;

            float currentSpeed = horizontalVelocity.magnitude;

            // Direction away from wall + slight upward bias
            Vector3 pushDir = (wallNormal + Vector3.up * 0.5f).normalized;

            // Preserve forward momentum without stacking the extra speed.
            Vector3 preservedMomentum = horizontalVelocity;
            Vector3 newVelocity = preservedMomentum + pushDir * wallJumpSideForce;

            float maxAllowedSpeed = Mathf.Max(currentMoveMaxSpeed, airMaxSpeed) * 1.1f;

            if (newVelocity.magnitude > maxAllowedSpeed)
                newVelocity = newVelocity.normalized * maxAllowedSpeed;

            horizontalVelocity = newVelocity;

            justJumped = true;
            return;
        }

        //EXPERIMENTAL
        //if (isWallRunning && Pressed(jumpAction))
        //{
        //    isWallRunning = false;

        //    verticalVelocity = wallJumpForce;

        //    if (Time.time - lastWallJumpTime > wallJumpChainResetTime)
        //        wallJumpChainCount = 0;

        //    wallJumpChainCount++;
        //    lastWallJumpTime = Time.time;

        //    float chainMultiplier = Mathf.Pow(wallJumpChainBoost, wallJumpChainCount);
        //    chainMultiplier = Mathf.Min(chainMultiplier, wallJumpMaxChainMultiplier);

        //    float currentSpeed = horizontalVelocity.magnitude;

        //    Vector3 pushDir = (wallNormal + Vector3.up * 0.5f).normalized;

        //    Vector3 newVelocity =
        //        horizontalVelocity +
        //        pushDir * wallJumpSideForce * chainMultiplier;

        //    float maxAllowedSpeed = Mathf.Max(currentMoveMaxSpeed, airMaxSpeed) * wallJumpMaxChainMultiplier;

        //    if (newVelocity.magnitude > maxAllowedSpeed)
        //        newVelocity = newVelocity.normalized * maxAllowedSpeed;

        //    horizontalVelocity = newVelocity;

        //    justJumped = true;
        //    return;
        //}

        // Buffer jump input so it's not lost if grounding flickers
        // Hold to keep the buffer alive (optional)
        if (holdToJump)
        {
            if (Held(jumpAction))
                lastJumpPressTime = Time.time;
        }
        else
        {
            if (Pressed(jumpAction))
                lastJumpPressTime = Time.time;
        }

        bool hasJumpInput = Time.time - lastJumpPressTime < 0.12f;
        bool canJump = frameGrounded || Time.time - lastGroundedTime <= coyoteTime;
        if (!hasJumpInput || !canJump) return;

        lastJumpPressTime = -1f; // consume the buffered press

        justJumped = true;
        verticalVelocity = jumpForce;

        // Slide-jump: boost if currently sliding OR slide ended very recently
        bool slideJump = isSliding || (Time.time - slideEndTime < 0.15f);
        if (slideJump)
        {
            isSliding = false;
            verticalVelocity *= slideJumpBoostMultiplier;
            horizontalVelocity *= slideJumpBoostMultiplier;
            slideJumpMinSpeed = horizontalVelocity.magnitude;
        }
    }

    // ───────── Slide ─────────

    void HandleSlide()
    {
        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        bool crouchHeld = Held(crouchAction);
        bool onSlope = TryGetSlopeWithMemory(out Vector3 slopeDir, out float slopeAngle) && slopeAngle > 0.01f;

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
        {
            slideEndTime = Time.time;
            isSliding = false;
        }
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

    // ───────── WallRunning/Jumping ─────────

    bool CheckForWall(out Vector3 normal)
    {
        normal = Vector3.zero;

        Vector3 origin = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(origin, transform.right, out RaycastHit rightHit, wallDetectionDistance, wallMask))
        {
            normal = rightHit.normal;
            return true;
        }

        if (Physics.Raycast(origin, -transform.right, out RaycastHit leftHit, wallDetectionDistance, wallMask))
        {
            normal = leftHit.normal;
            return true;
        }

        return false;
    }

    void HandleWallRunTilt()
    {
        if (cameraHolder == null) return;

        float targetTilt = 0f;

        if (isWallRunning)
        {
            float side = Vector3.Dot(wallNormal, transform.right);

            if (side > 0f)
                targetTilt = -wallRunTiltAngle; // right wall
            else
                targetTilt = wallRunTiltAngle;  // left wall
        }

        currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, Time.deltaTime * wallRunTiltSpeed);
    }

    void HandleWallRun()
    {
        // Stop if grounded
        if (frameGrounded)
        {
            StopWallRun();
            return;
        }

        // If already wallrunning, just make sure the wall still exists
        if (isWallRunning)
        {
            if (!CheckForWall(out Vector3 detectedNormal))
            {
                StopWallRun();
                return;
            }

            wallNormal = detectedNormal;

            verticalVelocity = wallRunGravity;


            Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

            if (Vector3.Dot(wallForward, transform.forward) < 0f)
                wallForward = -wallForward;

            float currentSpeed = horizontalVelocity.magnitude;
            float minWallSpeed = walkSpeed;

            horizontalVelocity = wallForward.normalized * Mathf.Max(currentSpeed, minWallSpeed);

            return;
        }

        // START wallrun (must press or hold jump while airborne)
        if (!frameGrounded && Held(jumpAction))
        {
            if (CheckForWall(out Vector3 detectedNormal))
            {
                if (verticalVelocity <= 0f)
                {
                    isWallRunning = true;
                    wallNormal = detectedNormal;
                    wallRunStartTime = Time.time;
                }
            }
        }
    }

    void StopWallRun()
    {
        isWallRunning = false;
    }

    // ───────── Height & Camera (single source of truth) ─────────

    void UpdateHeightAndCamera()
    {
        if (controller == null || cameraHolder == null) return;

        float targetHeight, cameraDrop;
        if (isSliding) { targetHeight = slideHeight; cameraDrop = (controllerStartHeight - slideHeight) * 0.5f; }
        else if (isCrouching)
        {
            float clampedCrouch = Mathf.Clamp(crouchHeight, 0.2f, controllerStartHeight);
            targetHeight = clampedCrouch;
            cameraDrop = controllerStartHeight - clampedCrouch;
        }
        else { targetHeight = controllerStartHeight; cameraDrop = 0f; }

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

        // Apply ADS zoom if available
        GunAnimationController gunAnimController = gun.GetComponent<GunAnimationController>();
        if (gunAnimController != null)
        {
            target -= gunAnimController.CurrentAdsFov;
        }

        // Apply status effect FOV modifier (slowness usually means reduced FOV)
        // Skip when fully stunned — a stun shouldn't collapse the FOV
        if (!IsStunned())
        {
            float fovMultiplier = GetMovementSpeedMultiplier();
            target *= fovMultiplier;
        }

        virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, target, Time.deltaTime * fovLerpSpeed);
    }

    // ───────── Helpers ─────────

    float GetMovementSpeedMultiplier()
    {
        if (statusEffectManager == null) return 1f;

        var activeEffects = statusEffectManager.GetActiveEffects();
        if (activeEffects.Count == 0) return 1f;

        float slowest = 1f;
        foreach (var effect in activeEffects.Values)
        {
            if (effect.statusEffect.causesMovementImpairment)
            {
                slowest = Mathf.Min(slowest, effect.statusEffect.movementSpeedMultiplier);
            }
        }

        return slowest;
    }

    bool IsStunned()
    {
        if (statusEffectManager == null) return false;

        var activeEffects = statusEffectManager.GetActiveEffects();
        foreach (var effect in activeEffects.Values)
        {
            if (effect.statusEffect.causesMovementImpairment &&
                effect.statusEffect.movementSpeedMultiplier <= 0f)
                return true;
        }
        return false;
    }

    static readonly Collider[] standUpBuffer = new Collider[8];

    bool CanStandUp()
    {
        if (controller == null) return true;
        float r = controller.radius;
        Vector3 bot = transform.position + Vector3.up * r;
        Vector3 top = transform.position + Vector3.up * (controllerStartHeight - r);

        int count = Physics.OverlapCapsuleNonAlloc(bot, top, r, standUpBuffer);
        for (int i = 0; i < count; i++)
            if (standUpBuffer[i].gameObject != gameObject) return false;
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
