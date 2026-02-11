using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PlayerControllerV2 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float crouchSpeed = 3f;
    [SerializeField] float groundAcceleration = 35f;
    [SerializeField] float groundFriction = 8f;
    [SerializeField] float airAcceleration = 20f;
    [SerializeField] float airMaxSpeed = 7f;
    [SerializeField][Range(0f, 1f)] float airControl = 0.4f;
    [SerializeField] float airSpeedCap = 9f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpForce = 6f;

    [Header("Crouching")]
    [SerializeField] float crouchHeight = 0.8f;
    [SerializeField] float crouchTransitionSpeed = 8f;

    [Header("Sliding")]
    [SerializeField] float minSlideSpeed = 5f;
    [SerializeField] float slideCooldown = 0.5f;
    [SerializeField] float slideHeight = 0.5f;
    [SerializeField] float slideFriction = 2f;
    [SerializeField] float slideDeceleration = 3f;
    [SerializeField] float slideBoostMultiplier = 0.5f;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float cameraPitchLimit = 85f;

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

    float pitch;
    float yaw;
    float verticalVelocity;
    float movementSpeed;
    Vector3 horizontalVelocity;
    bool isCrouching;
    float controllerStartHeight;
    Vector3 cameraStartPos;
    bool isSliding;
    float slideCooldownTimer;
    Vector3 slideDirection;

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

        if (cameraHolder == null)
        {
            Transform found = transform.Find("CameraHolder");
            if (found != null)
                cameraHolder = found;
        }

        if (cameraHolder == null)
        {
            GameObject holder = new GameObject("CameraHolder");
            holder.transform.SetParent(transform);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            cameraHolder = holder.transform;
        }

        if (virtualCamera != null)
        {
            if (virtualCamera.Follow == null)
                virtualCamera.Follow = cameraHolder;
            if (virtualCamera.LookAt == null)
                virtualCamera.LookAt = cameraHolder;
        }

        if (cameraHolder != null)
            cameraStartPos = cameraHolder.localPosition;

        yaw = transform.eulerAngles.y;
    }

    void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        sprintAction?.action.Enable();
        jumpAction?.action.Enable();
        crouchAction?.action.Enable();
    }

    void OnDisable()
    {
        moveAction?.action.Disable();
        lookAction?.action.Disable();
        sprintAction?.action.Disable();
        jumpAction?.action.Disable();
        crouchAction?.action.Disable();
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();
        HandleSlide();
        HandleCrouch();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        Vector2 look = lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        float mx = look.x * mouseSensitivity;
        float my = look.y * mouseSensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -cameraPitchLimit, cameraPitchLimit);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraHolder != null)
            cameraHolder.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        if (controller == null)
            return;

        Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        float h = moveInput.x;
        float v = moveInput.y;

        IsSprinting = sprintAction != null && sprintAction.action.ReadValue<float>() > 0.1f;
        
        // Handle slide deceleration (skip normal friction/acceleration while sliding)
        if (isSliding)
        {
            // Apply only forward input while sliding, using locked slide direction
            Vector3 wishDir = slideDirection * Mathf.Max(v, 0f);
            float wishMag = Mathf.Clamp01(wishDir.magnitude);
            if (wishMag > 0f)
                wishDir.Normalize();

            // Only accelerate in forward direction, no friction
            Accelerate(wishDir, walkSpeed * 1.2f * wishMag, groundAcceleration * 0.5f);
            
            // Force velocity to only move along slide direction (prevents mouse look from adding speed)
            float forwardSpeed = Vector3.Dot(horizontalVelocity, slideDirection);
            horizontalVelocity = slideDirection * forwardSpeed;
            
            // Apply slide deceleration to reduce speed over time
            float speed = horizontalVelocity.magnitude;
            if (speed > 0f)
            {
                float newSpeed = Mathf.Max(speed - slideDeceleration * Time.deltaTime, 0f);
                horizontalVelocity = horizontalVelocity.normalized * newSpeed;
            }
        }
        else
        {
            // Normal movement when not sliding
            float maxSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            if (isCrouching)
                maxSpeed = crouchSpeed;

            Vector3 wishDir = (transform.forward * v + transform.right * h);
            float wishMag = Mathf.Clamp01(wishDir.magnitude);
            if (wishMag > 0f)
                wishDir.Normalize();

            if (controller.isGrounded)
            {
                ApplyFriction(groundFriction);
                Accelerate(wishDir, maxSpeed * wishMag, groundAcceleration);
            }
            else
            {
                Accelerate(wishDir, airMaxSpeed * wishMag, airAcceleration * airControl);
                if (horizontalVelocity.magnitude > airSpeedCap)
                    horizontalVelocity = horizontalVelocity.normalized * airSpeedCap;
            }
        }

        movementSpeed = horizontalVelocity.magnitude;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (controller == null)
            return;

        bool jumpHeld = jumpAction != null && jumpAction.action.ReadValue<float>() > 0.1f;
        if (jumpHeld && controller.isGrounded)
            verticalVelocity = jumpForce;
    }

    void HandleSlide()
    {
        if (slideCooldownTimer > 0)
            slideCooldownTimer -= Time.deltaTime;

        bool crouchHeld = crouchAction != null && crouchAction.action.ReadValue<float>() > 0.1f;

        // Try to initiate slide
        bool slidePressed = crouchAction != null && crouchAction.action.WasPressedThisFrame();
        if (slidePressed && controller.isGrounded && horizontalVelocity.magnitude >= minSlideSpeed && slideCooldownTimer <= 0 && !isCrouching)
        {
            isSliding = true;
            slideCooldownTimer = slideCooldown;
            isCrouching = false;
            
            // Lock the slide direction to prevent mouse rotation from affecting momentum
            slideDirection = transform.forward;
            
            // Give speed-based boost in the direction we're moving
            if (horizontalVelocity.magnitude > 0f)
            {
                float speedBoost = horizontalVelocity.magnitude * slideBoostMultiplier;
                horizontalVelocity += horizontalVelocity.normalized * speedBoost;
            }
        }

        // End slide if button released OR speed drops below minimum
        if (isSliding)
        {
            if (!crouchHeld || horizontalVelocity.magnitude < minSlideSpeed)
            {
                isSliding = false;
            }
        }

        // Update controller and camera height during slide
        if (controller != null && cameraHolder != null)
        {
            float targetHeight = isSliding ? slideHeight : (isCrouching ? controllerStartHeight - crouchHeight : controllerStartHeight);
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            float cameraDropAmount = isSliding ? (controllerStartHeight - slideHeight) : (isCrouching ? crouchHeight : 0f);
            Vector3 targetCameraPos = cameraStartPos;
            targetCameraPos.y -= cameraDropAmount;
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, targetCameraPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    void HandleCrouch()
    {
        bool crouchHeld = crouchAction != null && crouchAction.action.ReadValue<float>() > 0.1f;
        
        // Prevent standing if something is above or if sliding
        if (isCrouching && !crouchHeld && !isSliding && CanStandUp())
            isCrouching = false;
        else if (crouchHeld && !isSliding)
            isCrouching = true;

        if (controller == null || cameraHolder == null)
            return;

        // Height transitions are now handled in HandleSlide() as well
        // This prevents conflicts between crouch and slide
        if (!isSliding)
        {
            float targetHeight = isCrouching ? controllerStartHeight - crouchHeight : controllerStartHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            float cameraDropAmount = isCrouching ? crouchHeight : 0f;
            Vector3 targetCameraPos = cameraStartPos;
            targetCameraPos.y -= cameraDropAmount;
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, targetCameraPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    bool CanStandUp()
    {
        if (controller == null)
            return true;

        // Use overlap capsule to check if standing height is blocked
        float standHeight = controllerStartHeight;
        float radius = controller.radius;
        
        // Position capsule from feet to full standing height
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standHeight - radius);
        
        // Check for any colliders in the standing area (excluding self)
        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius);
        
        foreach (Collider hit in hits)
        {
            if (hit.gameObject != gameObject)
                return false;
        }
        
        return true;
    }

    void OnDrawGizmos()
    {
        if (controller == null)
            return;

        float standHeight = controller.height;
        float radius = controller.radius;
        
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standHeight - radius);

        Gizmos.color = CanStandUp() ? Color.green : Color.red;
        Gizmos.DrawLine(bottom, top);
        Gizmos.DrawWireSphere(bottom, radius);
        Gizmos.DrawWireSphere(top, radius);
    }

    void ApplyFriction(float friction)
    {
        float speed = horizontalVelocity.magnitude;
        if (speed <= 0f)
            return;

        float drop = speed * friction * Time.deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        horizontalVelocity *= newSpeed / speed;
    }

    void Accelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        if (wishSpeed <= 0f)
            return;

        float currentSpeed = Vector3.Dot(horizontalVelocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f)
            return;

        float accelSpeed = accel * Time.deltaTime * wishSpeed;
        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        horizontalVelocity += wishDir * accelSpeed;
    }
}
