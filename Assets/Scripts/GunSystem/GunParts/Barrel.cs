using UnityEngine;

[CreateAssetMenu(fileName = "Barrel", menuName = "Gun Parts/Barrel")]
public class BarrelPart : GunPart
{
    private void OnEnable()
    {
        partType = GunPartType.Barrel;
    }
}
