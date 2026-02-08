using UnityEngine;

[CreateAssetMenu(fileName = "Grip", menuName = "Gun Parts/Grip")]
public class GripPart : GunPart
{
    private void OnEnable()
    {
        partType = GunPartType.Grip;
    }
}