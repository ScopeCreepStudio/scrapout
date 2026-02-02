using UnityEngine;

public class GunTest : MonoBehaviour
{
    public GunAssembler gun;
    public GunPart barrel;
    public GunPart stock;
    public GunPart magazine;

    void Start()
    {
        gun.EquipPart(barrel);
        gun.EquipPart(stock);
        gun.EquipPart(magazine);
    }
}
