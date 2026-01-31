using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;                     // Pitch (head)
    public CinemachineCamera mainVirtualCamera;         // Player camera

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;
    public float cameraPitchLimit = 85f;
    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;
    public KeyCode interactKey = KeyCode.E;

    [Header("Zoom")]
    public float zoomFOV = 40f;
    public float zoomSpeed = 8f;

    [Header("Camera Switching")]
    public string switchableCameraTag = "SwitchableCamera";

    [Header("Remote Camera Orbit")]
    public float remoteOrbitSensitivity = 120f; // degrees per second per mouse unit
    public float remotePitchMin = -60f;
    public float remotePitchMax = 60f;

    CharacterController controller;
    Vector3 velocity;
    float pitch;

    float defaultFOV;
    float currentFOV;
    float fovVelocity;

    float remoteYaw;
    float remotePitch;

    CinemachineCamera currentCamera;
    CinemachineCamera previousCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraHolder == null && Camera.main != null)
            cameraHolder = Camera.main.transform;

        // Auto-find Cinemachine camera if missing
        if (mainVirtualCamera == null)
        {
            mainVirtualCamera = GetComponentInChildren<CinemachineCamera>();
            if (mainVirtualCamera == null)
                mainVirtualCamera = FindObjectOfType<CinemachineCamera>();
        }

        if (mainVirtualCamera == null)
        {
            Debug.LogError("FirstPersonController: No CinemachineCamera found!");
            enabled = false;
            return;
        }

        currentCamera = mainVirtualCamera;

        defaultFOV = currentCamera.Lens.FieldOfView;
        currentFOV = defaultFOV;

        // Force cursor lock with confined as fallback for editor
        Cursor.lockState = CursorLockMode.Locked;
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    void Update()
    {
        // Continuously enforce cursor lock during gameplay (needed for multi-monitor setups)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Confined;
        }
        if (Cursor.visible)
            Cursor.visible = false;

        if (Input.GetKeyDown(KeyCode.Escape))
            SwitchBackToMainCamera();

        if (currentCamera == mainVirtualCamera)
        {
            HandleMouseLook();
            HandleMovement();
        }
        else
        {
            HandleRemoteCameraOrbit();
        }
        
        HandleInteraction();
        HandleZoom();
    }

    // -------------------- ZOOM (from first script) --------------------
    void HandleZoom()
    {
        if (currentCamera == null) return;

        float targetFOV = Input.GetMouseButton(1) ? zoomFOV : defaultFOV;
        float smoothTime = Mathf.Max(0.01f, 1f / zoomSpeed);

        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, smoothTime);

        var lens = currentCamera.Lens;
        lens.FieldOfView = currentFOV;
        currentCamera.Lens = lens;
    }

    // -------------------- INTERACTION --------------------
    void HandleInteraction()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        // If already on a remote camera, pressing E returns to main camera
        if (currentCamera != mainVirtualCamera)
        {
            SwitchBackToMainCamera();
            return;
        }

        Transform cam = cameraHolder != null ? cameraHolder : Camera.main?.transform;
        if (cam == null) return;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, interactDistance, interactMask))
        {
            // Door interaction
            var door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                door.ToggleDoor();
                return;
            }

            // Camera switching
            CinemachineCamera camSwitch =
                hit.collider.GetComponent<CinemachineCamera>() ??
                hit.collider.GetComponentInParent<CinemachineCamera>() ??
                hit.collider.GetComponentInChildren<CinemachineCamera>();

            if (camSwitch != null || hit.collider.CompareTag(switchableCameraTag))
            {
                SwitchToCamera(camSwitch);
            }
        }
    }

    // -------------------- CAMERA SWITCHING --------------------
    public void SwitchToCamera(CinemachineCamera newCamera)
    {
        if (newCamera == null || newCamera == currentCamera) return;

        previousCamera = currentCamera;
        previousCamera.Priority = 10;

        newCamera.Priority = 20;
        currentCamera = newCamera;

        defaultFOV = currentCamera.Lens.FieldOfView;
        currentFOV = defaultFOV;
        fovVelocity = 0f;

        // Initialize orbit state from camera's current rotation
        var euler = currentCamera.transform.eulerAngles;
        remoteYaw = euler.y;
        remotePitch = NormalizeAngle(euler.x);
    }

    public void SwitchBackToMainCamera()
    {
        if (mainVirtualCamera == null) return;
        // Lower priority on current before switching back
        if (currentCamera != null && currentCamera != mainVirtualCamera)
            currentCamera.Priority = 10;

        SwitchToCamera(mainVirtualCamera);
    }

    // -------------------- MOUSE LOOK --------------------
    void HandleMouseLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);

        pitch -= my;
        pitch = Mathf.Clamp(pitch, -cameraPitchLimit, cameraPitchLimit);

        if (cameraHolder != null)
            cameraHolder.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    // -------------------- MOVEMENT --------------------
    void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 move = transform.TransformDirection(input) * speed;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;

        controller.Move((move + Vector3.up * velocity.y) * Time.deltaTime);
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // -------------------- REMOTE CAMERA ORBIT --------------------
    void HandleRemoteCameraOrbit()
    {
        if (currentCamera == null) return;

        float mx = Input.GetAxis("Mouse X") * remoteOrbitSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * remoteOrbitSensitivity * Time.deltaTime;

        remoteYaw += mx;
        remotePitch = Mathf.Clamp(remotePitch - my, remotePitchMin, remotePitchMax);

        currentCamera.transform.rotation = Quaternion.Euler(remotePitch, remoteYaw, 0f);
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
