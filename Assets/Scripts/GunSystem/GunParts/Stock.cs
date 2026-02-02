using UnityEngine;
[CreateAssetMenu(fileName = "Stock", menuName = "Gun Parts/Stock")]
public class StockPart : GunPart
{
    private void OnEnable()
    {
        partType = GunPartType.Stock;
    }
}
