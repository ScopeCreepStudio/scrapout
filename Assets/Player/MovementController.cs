using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] CharacterController characterController;
    [SerializeField] Transform cameraTransform;
    [SerializeField] CameraController cameraController;

    [Header("Input Actions")]
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference sprintAction;
    [SerializeField] InputActionReference crouchAction;

    [Header("Animation Blend")]
    [SerializeField] float blendSmoothTime = 0.1f;
    [SerializeField] float walkAnimatorSpeed = 1f;
    [SerializeField] float sprintAnimatorSpeed = 1.25f;
    [SerializeField] float crouchAnimatorSpeed = 0.8f;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 6f;
    [SerializeField] float sprintSpeed = 10f;
    [SerializeField] float crouchSpeed = 3f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float gravity = -20f;

    [Header("IK (Head/Torso Look)")]
    [SerializeField] float ikHeadWeight = 0.5f;
    [SerializeField] float ikBodyWeight = 0.3f;
    [SerializeField] float bodyRotationThreshold = 45f;
    [SerializeField] float bodyRotationLerpSpeed = 5f;

    Vector2 moveInput;
    float currentBlendX, currentBlendY;
    float blendVelocityX, blendVelocityY;
    bool isSprinting;
    bool isCrouching;
    string debugStatus;
    bool hasLoggedMissingCrouchBinding;
    float verticalVelocity;
    Vector3 lastLookDirection = Vector3.forward;

    void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }

        if (sprintAction != null)
        {
            sprintAction.action.Enable();
        }

        if (crouchAction != null)
        {
            crouchAction.action.Enable();
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 500f, 90f), debugStatus);
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }

        if (sprintAction != null)
        {
            sprintAction.action.Disable();
        }

        if (crouchAction != null)
        {
            crouchAction.action.Disable();
        }
    }

    void Update()
    {
        // Read movement input
        moveInput = moveAction.action.ReadValue<Vector2>();
        isSprinting = sprintAction != null && sprintAction.action.IsPressed();
        isCrouching = crouchAction != null && crouchAction.action.IsPressed();

        if (crouchAction == null && !hasLoggedMissingCrouchBinding)
        {
            Debug.LogWarning("MovementController: crouchAction is not assigned.");
            hasLoggedMissingCrouchBinding = true;
        }

        if (isCrouching)
        {
            isSprinting = false;
        }

        // Normalize input for proper blending
        Vector2 normalizedInput = moveInput.normalized;

        // Smooth blend values for smoother animation transitions
        currentBlendX = Mathf.SmoothDamp(currentBlendX, normalizedInput.x, ref blendVelocityX, blendSmoothTime);
        currentBlendY = Mathf.SmoothDamp(currentBlendY, normalizedInput.y, ref blendVelocityY, blendSmoothTime);

        // Handle movement
        HandleMovement();

        // Handle IK look direction for first person
        if (cameraController != null && cameraController.IsFirstPerson && animator != null)
        {
            UpdateIKLookDirection();
        }

        debugStatus = $"Move: {moveInput}\nSprint: {isSprinting}\nCrouch: {isCrouching}\nBlendX: {currentBlendX:F2} BlendY: {currentBlendY:F2}";

        // Set animator parameters
        if (animator != null)
        {
            animator.SetFloat("MovementX", currentBlendX);
            animator.SetFloat("MovementY", currentBlendY);
            animator.SetBool("IsSprinting", isSprinting);
            animator.SetBool("IsCrouching", isCrouching);
            animator.speed = isCrouching ? crouchAnimatorSpeed : isSprinting ? sprintAnimatorSpeed : walkAnimatorSpeed;
        }
    }

    void UpdateIKLookDirection()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // Get camera forward direction
        Vector3 cameraForward = cameraTransform.forward;
        lastLookDirection = Vector3.Lerp(lastLookDirection, cameraForward, Time.deltaTime * 5f);

        // Only rotate body if moving AND looking too far to the side
        if (moveInput.sqrMagnitude > 0.01f)
        {
            float angleToCamera = Vector3.SignedAngle(transform.forward, cameraForward, Vector3.up);

            // If angle exceeds threshold, rotate the body (legs) to follow camera
            if (Mathf.Abs(angleToCamera) > bodyRotationThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(cameraForward.x, 0, cameraForward.z).normalized);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, bodyRotationLerpSpeed * Time.deltaTime);
            }
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || cameraTransform == null)
        {
            return;
        }

        if (cameraController != null && !cameraController.IsFirstPerson)
        {
            return;
        }

        // Set look at position far ahead based on camera direction
        Vector3 lookAtPosition = transform.position + lastLookDirection * 100f;
        animator.SetLookAtPosition(lookAtPosition);
        animator.SetLookAtWeight(ikHeadWeight, ikBodyWeight, 1f);
    }

    void HandleMovement()
    {
        if (characterController == null)
        {
            return;
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                return;
            }
        }

        // Cache camera direction at frame start to avoid arc movement during body rotation
        Vector3 cachedCameraForward = cameraTransform.forward;
        Vector3 cachedCameraRight = cameraTransform.right;

        // Determine current speed
        float currentSpeed = isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed;

        // Camera-relative movement using cached direction
        Vector3 forward = cachedCameraForward;
        Vector3 right = cachedCameraRight;

        // Flatten vectors to prevent vertical bias
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // Apply gravity
        if (!characterController.isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -0.5f;
        }

        // Build final velocity
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        // Move character
        characterController.Move(velocity * Time.deltaTime);

        // Rotate player to face movement direction (only in third-person)
        if (moveDirection.sqrMagnitude > 0.01f && cameraController != null && !cameraController.IsFirstPerson)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
