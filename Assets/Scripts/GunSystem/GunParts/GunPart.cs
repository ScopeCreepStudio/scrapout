using UnityEngine;

public abstract class GunPart : ScriptableObject
{
    public string partName;
    public GunPartType partType;
    public GameObject modelPrefab;

    [Header("Stat Modifiers")]
    public float damageModifier;
    public float fireRateModifier;
    public float recoilModifier;
    public float accuracyModifier;
    public float rangeModifier;
    public int ammoCapacityModifier;
}
