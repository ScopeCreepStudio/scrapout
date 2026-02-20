using System.Reflection;
using UnityEngine;

public class UIDebug : MonoBehaviour
{
    [SerializeField] GameObject playerObject;
    PlayerControllerV2 player;
    [SerializeField] GunAssembler gun;

    void Awake()
    {
        ResolvePlayer();

        if (gun == null)
            gun = FindObjectOfType<GunAssembler>();
    }

    void OnValidate()
    {
        ResolvePlayer();
    }

    void ResolvePlayer()
    {
        if (playerObject != null)
            player = playerObject.GetComponent<PlayerControllerV2>();

        if (player == null)
        {
            player = FindObjectOfType<PlayerControllerV2>();
            if (player != null)
                playerObject = player.gameObject;
        }
    }

    void OnGUI()
    {
        if (player == null) return;

        string movementState = "Standing";
        if (player.IsSliding) movementState = "Sliding";
        else if (player.IsSprinting) movementState = "Sprinting";
        else if (player.IsCrouching) movementState = "Crouching";

        GUI.Label(new Rect(10, 10, 400, 20), $"Current Movement: {movementState}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Movement Speed: {player.MovementSpeed:F2}");

        if (gun != null)
        {
            GUI.Label(new Rect(10, 100, 400, 20), "--- Gun Stats ---");

            GunStats stats = gun.GetCurrentStats();
            FieldInfo[] fields = typeof(GunStats).GetFields(BindingFlags.Public | BindingFlags.Instance);

            float y = 120f;
            for (int i = 0; i < fields.Length; i++)
            {
                object value = fields[i].GetValue(stats);
                GUI.Label(new Rect(10, y, 400, 20), $"{fields[i].Name}: {value}");
                y += 20f;
            }
        }
    }
}

