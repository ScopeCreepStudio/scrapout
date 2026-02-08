using UnityEngine;

public class GunPartPickup : MonoBehaviour
{
    [SerializeField] private GunPart part;
    [SerializeField] private bool autoEquip;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        GunPartInventory inventory = other.GetComponentInParent<GunPartInventory>();
        if (inventory == null) return;

        bool added = inventory.AddPart(part);
        if (!added) return;

        if (autoEquip)
        {
            GunAssembler assembler = other.GetComponentInParent<GunAssembler>();
            if (assembler != null && part != null)
            {
                assembler.EquipPart(part);
            }
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
