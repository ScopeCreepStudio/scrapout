using UnityEngine;

public class UIDebug : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] GunAssembler gun;
    Rigidbody rb;

    void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (player != null)
            rb = player.GetComponent<Rigidbody>();

        if (gun == null)
            gun = FindObjectOfType<GunAssembler>();
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

        if (gun != null)
        {
            GUI.Label(new Rect(10, 80, 400, 20), "--- Gun Stats ---");
            GUI.Label(new Rect(10, 100, 400, 20), $"Damage: {gun.GetCurrentStats().damage}");
            GUI.Label(new Rect(10, 120, 400, 20), $"Fire Rate: {gun.GetCurrentStats().fireRate}");
            GUI.Label(new Rect(10, 140, 400, 20), $"Accuracy: {gun.GetCurrentStats().accuracy}");
            GUI.Label(new Rect(10, 160, 400, 20), $"Recoil: {gun.GetCurrentStats().recoil}");
        }
    }
}

