using System.Reflection;
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

            GunStats stats = gun.GetCurrentStats();
            FieldInfo[] fields = typeof(GunStats).GetFields(BindingFlags.Public | BindingFlags.Instance);

            float y = 100f;
            for (int i = 0; i < fields.Length; i++)
            {
                object value = fields[i].GetValue(stats);
                GUI.Label(new Rect(10, y, 400, 20), $"{fields[i].Name}: {value}");
                y += 20f;
            }
        }
    }
}

