using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GunAnimationController : MonoBehaviour
{
    [Header("Weapon Sway")]
    [SerializeField] float maxSwayPositionAmount = 0.3f;
    [SerializeField] float maxSwayRotationAmount = 3f;
    [SerializeField] float swayLerpSpeed = 8f;
    [SerializeField] float movementSwayMultiplier = 1.5f;
    [SerializeField] float lookSwayMultiplier = 2f;

    [Header("Gun Bobbing")]
    [SerializeField] bool enableGunBobbing = true;
    
    [Header("Bobbing - Walking")]
    [SerializeField] float walkBobbingAmount = 0.05f;
    [SerializeField] float walkBobbingSpeed = 5f;
    
    [Header("Bobbing - Sprinting")]
    [SerializeField] float sprintBobbingAmount = 0.08f;
    [SerializeField] float sprintBobbingSpeed = 7f;
    
    [Header("Bobbing - Crouching")]
    [SerializeField] float crouchBobbingAmount = 0.02f;
    [SerializeField] float crouchBobbingSpeed = 3f;
    
    [Header("Bobbing - In Air")]
    [SerializeField] float airBobbingAmount = 0f;

    [Header("Crouch")]
    [SerializeField] float crouchPositionOffset = -0.15f;
    [SerializeField] float crouchLerpSpeed = 8f;

    [Header("ADS (Aim Down Sights)")]
    [SerializeField] bool holdToAds = true;
    [SerializeField] Vector3 adsPosition = new Vector3(0.2f, -0.1f, 0.3f);
    [SerializeField] Quaternion adsRotation = Quaternion.identity;
    [SerializeField] float adsLerpSpeed = 8f;
    [SerializeField] float adsFov = 30f;
    [SerializeField] float adsFovLerpSpeed = 6f;

    [Header("Recoil")]
    [SerializeField] Vector3 recoilPosition = new Vector3(0, -0.1f, -0.2f);
    [SerializeField] Vector3 recoilRotation = new Vector3(5f, 0, 0);
    [SerializeField] float recoilDuration = 0.1f;
    [SerializeField] float recoilRecoveryDuration = 0.3f;
    [SerializeField] AnimationCurve recoilFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] AnimationCurve recoveryFalloff = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private GunAssembler gunAssembler;
    private PlayerControllerV2 playerController;
    private Transform cameraHolder;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 currentSwayOffset = Vector3.zero;
    private Quaternion currentSwayRotation = Quaternion.identity;
    private Vector3 targetSwayOffset = Vector3.zero;
    private Quaternion targetSwayRotation = Quaternion.identity;
    private Quaternion lastCameraRotation = Quaternion.identity;
    private Coroutine recoilRoutine;
    private bool isRecoiling = false;
    private bool isAds = false;
    private float currentAdsFov = 0f;
    private float bobbingTime = 0f;
    private float currentCrouchOffset = 0f;

    public bool IsADS => isAds;
    public float CurrentAdsFov => currentAdsFov;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        // Get references
        gunAssembler = GetComponent<GunAssembler>();
        
        // Find PlayerControllerV2 - search up the hierarchy
        playerController = GetComponentInParent<PlayerControllerV2>();
        
        // If not found in parents, search the entire object tree
        if (playerController == null)
        {
            Transform current = transform;
            while (current != null)
            {
                playerController = current.GetComponent<PlayerControllerV2>();
                if (playerController != null) break;
                current = current.parent;
            }
        }
        
        // If still not found, try finding in scene
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerControllerV2>();
        }

        // Find camera holder - it should be a child of the player
        if (playerController != null)
        {
            cameraHolder = playerController.transform.Find("CameraHolder");
            if (cameraHolder == null)
            {
                // Search children recursively
                cameraHolder = FindTransformRecursive(playerController.transform, "CameraHolder");
            }
        }

        if (cameraHolder == null)
        {
            cameraHolder = transform.root.Find("CameraHolder");
        }

        if (cameraHolder != null)
        {
            lastCameraRotation = cameraHolder.rotation;
            Debug.Log("GunAnimationController: CameraHolder found at " + cameraHolder.name);
        }
        else
        {
            Debug.LogWarning("GunAnimationController: CameraHolder not found!");
        }

        // Subscribe to GunAssembler events
        if (gunAssembler != null)
        {
            gunAssembler.HitConfirmed += OnShoot;
            Debug.Log("GunAnimationController: Subscribed to GunAssembler");
        }
        else
        {
            Debug.LogWarning("GunAnimationController: GunAssembler not found on this object!");
        }

        if (playerController == null)
        {
            Debug.LogWarning("GunAnimationController: PlayerControllerV2 not found!");
        }
        else
        {
            Debug.Log("GunAnimationController: PlayerControllerV2 found");
        }
    }

    private Transform FindTransformRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        
        foreach (Transform child in parent)
        {
            Transform found = FindTransformRecursive(child, name);
            if (found != null) return found;
        }
        
        return null;
    }

    private void OnDestroy()
    {
        if (gunAssembler != null)
        {
            gunAssembler.HitConfirmed -= OnShoot;
        }
    }

    private void OnShoot(Collider hitCollider, RaycastHit hit)
    {
        TriggerRecoil();
    }

    private void Update()
    {
        HandleADS();
        HandleCrouch();

        if (!isRecoiling)
        {
            UpdateSway();
        }

        ApplyTransform();
    }

    private void HandleADS()
    {
        // Handle ADS input based on mode
        if (playerController != null)
        {
            InputActionReference adsAction = playerController.AdsAction;
            
            if (adsAction != null)
            {
                if (holdToAds)
                {
                    // Hold mode: ADS is active while button pressed
                    isAds = adsAction.action.ReadValue<float>() > 0.1f;
                }
                else
                {
                    // Toggle mode: ADS toggles on press
                    if (adsAction.action.WasPressedThisFrame())
                    {
                        isAds = !isAds;
                    }
                }
            }
        }

        // Lerp FOV based on ADS state - smooth animation both ways
        float targetFov = isAds ? adsFov : 0f;
        currentAdsFov = Mathf.Lerp(currentAdsFov, targetFov, Time.deltaTime * adsFovLerpSpeed);
    }

    private void HandleCrouch()
    {
        // Calculate target crouch offset based on player state
        float targetCrouchOffset = 0f;
        if (playerController != null && playerController.IsCrouching)
        {
            targetCrouchOffset = crouchPositionOffset;
        }

        // Smoothly lerp to target crouch offset
        currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetCrouchOffset, Time.deltaTime * crouchLerpSpeed);
    }

    private void UpdateSway()
    {
        float movementIntensity = 0f;
        float yawIntensity = 0f;
        float pitchIntensity = 0f;

        // Calculate movement-based sway
        if (playerController != null)
        {
            movementIntensity = Mathf.Clamp01(playerController.MovementSpeed / 8f);
        }

        // Calculate yaw and pitch from camera rotation delta
        if (cameraHolder != null)
        {
            // Get rotation delta this frame
            Quaternion rotationDelta = cameraHolder.rotation * Quaternion.Inverse(lastCameraRotation);
            lastCameraRotation = cameraHolder.rotation;

            // Extract pitch and yaw from the delta rotation
            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

            // Yaw is rotation around Y axis (horizontal look)
            if (Mathf.Abs(axis.y) > 0.5f)
            {
                yawIntensity = Mathf.Clamp(angle * (axis.y > 0 ? 1 : -1) / 15f, -1f, 1f);
            }

            // Pitch is rotation around X axis (vertical look)
            if (Mathf.Abs(axis.x) > 0.5f)
            {
                pitchIntensity = Mathf.Clamp01(Mathf.Abs(angle) / 30f);
            }
        }

        // Calculate dynamic bobbing based on player state
        float bobbingOffset = 0f;
        if (enableGunBobbing && movementIntensity > 0.01f && playerController != null)
        {
            // No bobbing while sliding, jumping, or in ADS
            if (!playerController.IsSliding && playerController.IsGrounded && !isAds)
            {
                bobbingTime += Time.deltaTime;
                
                // Determine bobbing parameters based on player state
                float bobbingAmount = walkBobbingAmount;
                float bobbingSpeed = walkBobbingSpeed;
                
                // Sprinting: more intense, faster bobbing
                if (playerController.IsSprinting)
                {
                    bobbingAmount = sprintBobbingAmount;
                    bobbingSpeed = sprintBobbingSpeed;
                }
                // Crouching: subtle, slow bobbing
                else if (playerController.IsCrouching)
                {
                    bobbingAmount = crouchBobbingAmount;
                    bobbingSpeed = crouchBobbingSpeed;
                    movementIntensity *= 0.5f; // Reduce overall movement intensity when crouching
                }
                
                bobbingOffset = Mathf.Sin(bobbingTime * bobbingSpeed) * bobbingAmount * movementIntensity;
            }
            else if (!playerController.IsSliding && playerController.IsCrouching && !isAds)
            {
                // Subtle bobbing even when not moving but crouched
                bobbingTime += Time.deltaTime;
                bobbingOffset = Mathf.Sin(bobbingTime * crouchBobbingSpeed) * (crouchBobbingAmount * 0.3f);
            }
            else
            {
                bobbingTime = 0f;
            }
        }
        else
        {
            bobbingTime = 0f;
        }

        // Calculate combined sway target
        if (cameraHolder != null)
        {
            // Position sway - moves gun based on movement and vertical looking
            float posSwayX = 0f; // No horizontal movement from yaw
            float posSwayY = movementIntensity * maxSwayPositionAmount * movementSwayMultiplier + 
                             pitchIntensity * maxSwayPositionAmount * 0.5f +
                             bobbingOffset; // Add bobbing

            targetSwayOffset = new Vector3(posSwayX, posSwayY, 0);

            // Rotation sway - calculate as individual quaternion rotations and combine
            Quaternion rotX = Quaternion.AngleAxis(movementIntensity * maxSwayRotationAmount, Vector3.right);
            Quaternion rotY = Quaternion.AngleAxis(pitchIntensity * maxSwayRotationAmount * 0.5f, Vector3.up);
            Quaternion rotZ = Quaternion.AngleAxis(yawIntensity * maxSwayRotationAmount * lookSwayMultiplier, Vector3.forward);

            // Combine all rotations
            targetSwayRotation = rotZ * rotY * rotX;
        }

        // Smoothly lerp to target sway
        currentSwayOffset = Vector3.Lerp(currentSwayOffset, targetSwayOffset, Time.deltaTime * swayLerpSpeed);
        currentSwayRotation = Quaternion.Lerp(currentSwayRotation, targetSwayRotation, Time.deltaTime * swayLerpSpeed);
    }

    private void TriggerRecoil()
    {
        if (recoilRoutine != null)
            StopCoroutine(recoilRoutine);

        recoilRoutine = StartCoroutine(RecoilRoutine());
    }

    private IEnumerator RecoilRoutine()
    {
        isRecoiling = true;
        float elapsed = 0f;

        // Recoil phase - kick back
        while (elapsed < recoilDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoilDuration;
            float falloff = recoilFalloff.Evaluate(t);

            currentSwayOffset = Vector3.Lerp(Vector3.zero, recoilPosition, falloff);
            currentSwayRotation = Quaternion.Euler(Vector3.Lerp(Vector3.zero, recoilRotation, falloff));

            yield return null;
        }

        // Recovery phase - return to sway
        elapsed = 0f;
        while (elapsed < recoilRecoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoilRecoveryDuration;
            float falloff = recoveryFalloff.Evaluate(t);

            currentSwayOffset = Vector3.Lerp(recoilPosition, targetSwayOffset, falloff);
            currentSwayRotation = Quaternion.Lerp(Quaternion.Euler(recoilRotation), targetSwayRotation, falloff);

            yield return null;
        }

        isRecoiling = false;
        recoilRoutine = null;
    }

    private void ApplyTransform()
    {
        // Apply sway offset and rotation
        Vector3 swayPos = initialPosition + currentSwayOffset;
        Quaternion swayRot = initialRotation * currentSwayRotation;

        // Apply smooth crouch offset
        swayPos.y += currentCrouchOffset;

        // Apply ADS offset based on current FOV zoom (lerps smoothly both ways)
        float adsAmount = Mathf.Clamp01(currentAdsFov / adsFov); // 0 to 1 based on current zoom
        swayPos = Vector3.Lerp(swayPos, initialPosition + adsPosition, adsAmount);
        swayRot = Quaternion.Lerp(swayRot, initialRotation * adsRotation, adsAmount);

        transform.localPosition = swayPos;
        transform.localRotation = swayRot;
    }
}

