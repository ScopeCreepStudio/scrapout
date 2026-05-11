using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine Camera Roots")]
    [SerializeField] GameObject firstPersonCamera;
    [SerializeField] GameObject thirdPersonCamera;

    [Header("Start State")]
    [SerializeField] bool startInFirstPerson = true;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float pitchLimit = 85f;

    bool isFirstPerson;
    float pitch = 0f;

    public bool IsFirstPerson => isFirstPerson;

    void Start()
    {
        isFirstPerson = startInFirstPerson;
        ApplyCameraState();

        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFirstPerson = !isFirstPerson;
            ApplyCameraState();
        }

        // Handle mouse look for first person
        if (isFirstPerson)
        {
            HandleMouseLook();
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        // Standard FPS: Rotate the player root horizontally (yaw)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera vertically (pitch)
        if (firstPersonCamera != null)
        {
            firstPersonCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void ApplyCameraState()
    {
        if (firstPersonCamera != null)
        {
            firstPersonCamera.SetActive(isFirstPerson);
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.SetActive(!isFirstPerson);
        }
    }
}
