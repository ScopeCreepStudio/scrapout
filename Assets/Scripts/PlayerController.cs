using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

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


    [Header("Ground Checker")]
    [SerializeField] float pHeight;
    [SerializeField] float floorDrag;
    [SerializeField] LayerMask isGround;
    

    [Header("References")]
    [SerializeField] Transform direction;
    [SerializeField] CameraController cameraControl;

    [Header("KeyBinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;

    //Private Floats
    float horizontalInput;
    float verticalInput;
    Rigidbody rb;
    Vector3 movementDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        canJump = true;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //Jumping
        if (Input.GetKey(jumpKey) && checkGround() && canJump)
        {
            canJump = false;
            Jumping();

            //Without this, the jump will be inconsistent with the jump force value.
            Invoke(nameof(ResetJump), 0.75f);
        }

        //Sprinting
        if (Input.GetKey(sprintKey))
        {
            isSprinting = true;
        }
        else isSprinting = false;

        if (checkGround()) { rb.linearDamping = floorDrag; }
        else { rb.linearDamping = 0f; }

    }


    void FixedUpdate()
    {
        movementDirection = direction.forward * verticalInput + direction.right * horizontalInput;

        float currentMaxSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (checkGround())
        {
            rb.AddForce(movementDirection.normalized * currentMaxSpeed * 10f, ForceMode.Force);
        }
        if (!checkGround())
        {
            rb.AddForce(movementDirection.normalized * currentMaxSpeed * 10f * airSpeed, ForceMode.Force);
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
        if (Physics.Raycast(transform.position, Vector3.down, pHeight, isGround))
        {
            return true;
        }
        else { return false; }
    }


    private void SpeedController(float currentMaxSpeed)
    {
        Vector3 velLimit = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (velLimit.magnitude > currentMaxSpeed)
        {
            Vector3 finalLimit = velLimit.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(finalLimit.x, rb.linearVelocity.y, finalLimit.z);

            if (!isSprinting) { Debug.Log("Limited Velocity for walking"); }
            else { Debug.Log("Limited Velocity for running"); }
        }
    }

    private void OnDrawGizmos()
    {
    }
}
