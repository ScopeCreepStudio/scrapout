using UnityEngine;

public class GunTest : MonoBehaviour
{
    public GunAssembler gun;
    public GunPart barrel;
    public GunPart stock;
    public GunPart magazine;
    public GunPart optic;
    public GunPart grip;

    void Start()
    {
        if (gun == null) return;

        if (barrel != null) gun.EquipPart(barrel);
        if (stock != null) gun.EquipPart(stock);
        if (magazine != null) gun.EquipPart(magazine);
        if (optic != null) gun.EquipPart(optic);
        if (grip != null) gun.EquipPart(grip);
    }
}
