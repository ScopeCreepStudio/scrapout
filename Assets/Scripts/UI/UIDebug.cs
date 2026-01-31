using UnityEngine;

public class UIDebug : MonoBehaviour
{
    [SerializeField] PlayerController player;
    Rigidbody rb;

    void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (player != null)
            rb = player.GetComponent<Rigidbody>();
    }

    void OnGUI()
    {
        if (player == null) return;

        string movementState = "standing";
        if (player.isSliding) movementState = "sliding";
        else if (player.isCrouching) movementState = "crouching";
        else if (player.isSprinting) movementState = "sprinting";

        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;

        GUI.Label(new Rect(10, 10, 400, 20), $"Movement: {movementState}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Can Jump: {player.CanJump}");
        GUI.Label(new Rect(10, 50, 400, 20), $"Velocity: {velocity}");
    }
}
