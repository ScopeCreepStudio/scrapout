using UnityEngine;

[CreateAssetMenu(fileName = "Optic", menuName = "Gun Parts/Optic")]
public class OpticPart : GunPart
{
    private void OnEnable()
    {
        partType = GunPartType.Optic;
    }
}