using System.Collections.Generic;
using UnityEngine;

public class PlayerGunLoadout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunAssembler gun;
    [SerializeField] private GunPartInventory inventory;
    [SerializeField] private Transform gunHolder;

    [Header("Auto Equip")]
    [SerializeField] private bool autoEquipOnStart = true;

    private readonly Dictionary<GunPartType, GunPart> equipped = new();

    private void Awake()
    {
        if (gun == null)
            gun = GetComponentInChildren<GunAssembler>();

        if (inventory == null)
            inventory = GetComponent<GunPartInventory>();
    }

    private void Start()
    {
        AttachGunToHolder();

        if (autoEquipOnStart)
            EquipDefaultsFromInventory();
    }

    private void AttachGunToHolder()
    {
        if (gun == null || gunHolder == null) return;

        Transform gunRoot = gun.transform;
        gunRoot.SetParent(gunHolder, false);
        gunRoot.localPosition = Vector3.zero;
        gunRoot.localRotation = Quaternion.identity;
        gunRoot.localScale = Vector3.one;
    }

    public bool EquipPart(GunPart part)
    {
        if (part == null || gun == null) return false;

        if (inventory != null && !inventory.HasPart(part))
            return false;

        gun.EquipPart(part);
        equipped[part.partType] = part;
        return true;
    }

    public bool EquipFirstAvailable(GunPartType type)
    {
        if (inventory == null) return false;

        GunPart part = inventory.GetFirstPartByType(type);
        return EquipPart(part);
    }

    public void EquipDefaultsFromInventory()
    {
        EquipFirstAvailable(GunPartType.Barrel);
        EquipFirstAvailable(GunPartType.Stock);
        EquipFirstAvailable(GunPartType.Magazine);
        EquipFirstAvailable(GunPartType.Optic);
        EquipFirstAvailable(GunPartType.Grip);
    }

    public GunPart GetEquipped(GunPartType type)
    {
        return equipped.TryGetValue(type, out GunPart part) ? part : null;
    }
}