using UnityEngine;

[CreateAssetMenu(fileName = "Magazine", menuName = "Gun Parts/Magazine")]
public class MagazinePart : GunPart
{
    private void OnEnable()
    {
        partType = GunPartType.Magazine;
    }
}
