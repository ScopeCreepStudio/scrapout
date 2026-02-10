using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speed")]
    [SerializeField][Range(0, 20)] float walkSpeed;
    [SerializeField][Range(0, 10)] float sprintSpeed;
    [SerializeField][Range(0, 1)] float airSpeed;


    [Header("Sprinting")]
    [SerializeField] public bool isSprinting;

    [Header("Jumping")]
    [SerializeField] float jumpF;
    [SerializeField] bool canJump;

    [Header("Sliding")]
    [SerializeField] KeyCode slideKey = KeyCode.LeftControl;
    [SerializeField] float slideDuration = 0.5f;
    [SerializeField] float slideCooldown = 1f;
    [SerializeField] float slideSpeedMultiplier = 1.5f;
    [SerializeField] float slideDrag = 0.1f;
    float slideTimer;
    float slideCooldownTimer;
    public bool isSliding;

    [Header("Crouching")]
    [SerializeField] float crouchCameraHeight = 0.25f;
    [SerializeField] float slideCameraHeight = 0.35f;
    [SerializeField] float standingCameraHeight = 0.5f;
    [SerializeField] float crouchTransitionSpeed = 8f;
    [SerializeField] float minSlideSpeed = 5f;
    public bool isCrouching;
    Vector3 cameraHolderStartPos;

    [Header("Footsteps")]
    [SerializeField] float walkFootstepInterval = 0.5f;
    [SerializeField] float sprintFootstepInterval = 0.3f;
    float footstepTimer;

    [Header("Ground Checker")]
    [SerializeField] float pHeight;
    [SerializeField] float floorDrag;
    [SerializeField] LayerMask isGround;
    

    [Header("References")]
    [SerializeField] Transform direction;
    [SerializeField] Transform cameraHolder;
    [SerializeField] CinemachineCamera virtualCamera;
    [SerializeField] AudioManager audioManager;
    [SerializeField] GunAssembler gun;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float cameraPitchLimit = 85f;

    [Header("Camera FOV")]
    [SerializeField] float defaultFOV = 60f;
    [SerializeField] float sprintFOV = 70f;
    [SerializeField] float slideFOV = 75f;
    [SerializeField] float jumpFOV = 65f;
    [SerializeField] float fovChangeSpeed = 8f;
    [SerializeField] float minSprintFOVSpeed = 4.5f;
    float currentFOV;
    float fovVelocity;

    [Header("KeyBinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode reloadKey = KeyCode.R;

    //Camera rotation
    float pitch;
    float yaw;

    public bool CanJump => canJump;

    //Private Floats
    float horizontalInput;
    float verticalInput;
    Rigidbody rb;
    Vector3 movementDirection;
    bool wasGrounded;
    Vector3 groundNormal = Vector3.up;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        canJump = true;

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
            cameraHolderStartPos = cameraHolder.localPosition;

        if (virtualCamera != null)
            currentFOV = virtualCamera.Lens.FieldOfView;

        if (audioManager == null)
            audioManager = AudioManager.instance;

        wasGrounded = checkGround();
        yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //Handle mouse look
        HandleMouseLook();

        //Handle slide and crouch
        HandleSlide();

        //Handle camera FOV
        HandleFOV();

        //Handle footsteps
        HandleFootsteps();

        //Handle shooting
        if (Input.GetMouseButton(0))
        {
            gun?.Shoot(cameraHolder.position, cameraHolder.forward);
        }

        if (Input.GetKeyDown(reloadKey))
        {
            gun?.Reload();
        }

        //Jumping
        if (Input.GetKey(jumpKey) && checkGround() && canJump)
        {
            canJump = false;
            Jumping();
            audioManager?.PlayJumpSound(transform.position);

            //Without this, the jump will be inconsistent with the jump force value.
            Invoke(nameof(ResetJump), 0.75f);
        }

        //Sprinting - can't sprint while crouching or sliding
        if (Input.GetKey(sprintKey) && !isCrouching && !isSliding)
        {
            isSprinting = true;
        }
        else isSprinting = false;

        if (checkGround()) { rb.linearDamping = floorDrag; }
        else { rb.linearDamping = 0f; }

        //Handle landing sound
        if (wasGrounded == false && checkGround() == true)
        {
            audioManager?.PlayLandSound(transform.position);
        }
        wasGrounded = checkGround();

    }

    void HandleMouseLook()
    {
        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -cameraPitchLimit, cameraPitchLimit);
    }

    void LateUpdate()
    {
        if (cameraHolder != null)
            cameraHolder.localEulerAngles = new Vector3(pitch, 0f, 0f);

        if (direction != null)
            direction.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void HandleSlide()
    {
        //Update slide timer
        if (slideTimer > 0)
            slideTimer -= Time.deltaTime;
        else
            isSliding = false;

        //Update cooldown
        if (slideCooldownTimer > 0)
            slideCooldownTimer -= Time.deltaTime;

        //Handle slide initiation - only on key press, only while sprinting
        if (Input.GetKeyDown(slideKey) && checkGround() && isSprinting && slideCooldownTimer <= 0)
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideCooldownTimer = slideCooldown;
            isCrouching = false;
            audioManager?.PlaySlideSound(transform.position);
        }

        //Handle crouch hold (separate from slide) - only if not sliding
        if (!isSliding)
        {
            if (Input.GetKey(slideKey) && checkGround())
            {
                isCrouching = true;
            }
            else
            {
                isCrouching = false;
            }
        }
        else
        {
            //Force uncrouch while sliding
            isCrouching = false;
        }

        //Update camera height
        if (cameraHolder != null)
        {
            float targetHeight = standingCameraHeight;
            if (isSliding)
                targetHeight = slideCameraHeight;
            else if (isCrouching)
                targetHeight = crouchCameraHeight;
            Vector3 targetPos = cameraHolderStartPos;
            targetPos.y = targetHeight;
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, targetPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    void HandleFOV()
    {
        if (virtualCamera == null) return;

        float targetFOV = defaultFOV;

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        if (isSliding)
            targetFOV = slideFOV;
        else if (isSprinting && speed >= minSprintFOVSpeed)
            targetFOV = sprintFOV;
        else if (!checkGround())
            targetFOV = jumpFOV;

        float smoothTime = Mathf.Max(0.01f, 1f / fovChangeSpeed);
        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, smoothTime);

        var lens = virtualCamera.Lens;
        lens.FieldOfView = currentFOV;
        virtualCamera.Lens = lens;
    }

    void HandleFootsteps()
    {
        if (!checkGround() || isSliding) return;

        bool isMoving = horizontalInput != 0 || verticalInput != 0;
        if (!isMoving) return;

        if (rb != null)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.magnitude < 0.1f)
                return;
        }

        float interval = isSprinting ? sprintFootstepInterval : walkFootstepInterval;
        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            footstepTimer = interval;
            PlayFootstepSound();
        }
    }

    void PlayFootstepSound()
    {
        if (audioManager == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, pHeight + 0.1f, isGround))
        {
            string surfaceTag = hit.collider.tag;
            audioManager.PlayFootstep(transform.position, surfaceTag);
        }
    }


    void FixedUpdate()
    {
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        movementDirection = direction.forward * verticalInput + direction.right * horizontalInput;

        float currentMaxSpeed = isSprinting ? sprintSpeed : walkSpeed;
        
        //Apply slide speed multiplier
        if (isSliding)
            currentMaxSpeed *= slideSpeedMultiplier;

        if (checkGround())
        {
            Vector3 slopeMove = Vector3.ProjectOnPlane(movementDirection, groundNormal).normalized;
            if (slopeMove.sqrMagnitude > 0f)
                rb.AddForce(slopeMove * currentMaxSpeed * 10f, ForceMode.Force);
            rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }
        if (!checkGround())
        {
            rb.AddForce(movementDirection.normalized * currentMaxSpeed * 10f * airSpeed, ForceMode.Force);
        }

        // Reduce sliding when no input on ground
        if (checkGround() && movementDirection.sqrMagnitude <= 0.001f)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 damped = Vector3.Lerp(horizontalVel, Vector3.zero, Time.fixedDeltaTime * 12f);
            rb.linearVelocity = new Vector3(damped.x, rb.linearVelocity.y, damped.z);
        }

        SpeedController(currentMaxSpeed);
    }


    private void Jumping()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpF, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        canJump = true;
    }


    private bool checkGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, pHeight, isGround))
        {
            groundNormal = hit.normal;
            return true;
        }

        groundNormal = Vector3.up;
        return false;
    }


    private void SpeedController(float currentMaxSpeed)
    {
        Vector3 velLimit = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (velLimit.magnitude > currentMaxSpeed)
        {
            Vector3 finalLimit = velLimit.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(finalLimit.x, rb.linearVelocity.y, finalLimit.z);

            // if (!isSprinting) { Debug.Log("Limited Velocity for walking"); }
            // else { Debug.Log("Limited Velocity for running"); }
        }
    }

    private void OnDrawGizmos()
    {
    }
}
