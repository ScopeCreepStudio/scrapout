using UnityEngine;

public class CameraController : MonoBehaviour
{

    [Header("Field of View")]
    [SerializeField] float defaultFOV;
    [SerializeField] float sprintFOV;
    [SerializeField, Range(0, 0.1f)] float sprintFOVSpeedMultiplier;
    float currentFOV;
    float xRot;
    float yRot;

    [Header("References")]
    [Space(5)]
    [SerializeField] Transform direction;
    [SerializeField] PlayerController playerController;

    [Header("Settings")]
    public bool SprintFOVToggle;
    public float senY;
    public float senX;



    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //Make changes later for the settings menu (TEMP)
        currentFOV = defaultFOV;
    }

    void Update()
    {
        gameObject.GetComponent<Camera>().fieldOfView = currentFOV;
        //Get the mouse values
        float mouseX = Input.GetAxisRaw("Mouse X") * senX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * senY;


        yRot += mouseX;
        xRot -= mouseY;

        //Prevent 360 up and down movement
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        //Rotate the camera
        transform.rotation = Quaternion.Euler(xRot, yRot, 0);

        //Rotate the direction (Needed for PlayerController)
        direction.rotation = Quaternion.Euler(0, yRot, 0);

        Sprinting();
    }

    private void Sprinting()
    {
        if (SprintFOVToggle)
        {
            //If sprinting is true, targetFOV = sprintFOV, else default.
            float targetFOV = playerController.isSprinting ? sprintFOV : defaultFOV;

            //Smoothly change FOV
            currentFOV = Mathf.MoveTowards(currentFOV, targetFOV, sprintFOVSpeedMultiplier);
        }

    }
}
