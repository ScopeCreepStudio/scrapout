using UnityEngine;
using System.Collections.Generic;

public class GunAssembler : MonoBehaviour
{
    [Header("Attachment Points")]
    public Transform barrelPoint;
    public Transform stockPoint;
    public Transform magazinePoint;
    public Transform sightPoint;

    [Header("Base Stats")]
    [SerializeField] GunStats baseStats;

    [Header("Shooting")]
    [SerializeField] float shootRange = 100f;
    [SerializeField] LayerMask damageLayer;
    [SerializeField] GameObject bulletHolePrefab;

    private Dictionary<GunPartType, GameObject> currentModels = new();
    private List<GunPart> equippedParts = new();
    private float lastShootTime;

    public void EquipPart(GunPart part)
    {
        Transform attachPoint = GetAttachPoint(part.partType);
        if (attachPoint == null) return;

        // Remove old model
        if (currentModels.ContainsKey(part.partType))
        {
            Destroy(currentModels[part.partType]);
        }

        // Spawn new model
        GameObject model = Instantiate(part.modelPrefab, attachPoint);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        currentModels[part.partType] = model;

        // Replace part data
        equippedParts.RemoveAll(p => p.partType == part.partType);
        equippedParts.Add(part);
    }

    Transform GetAttachPoint(GunPartType type)
    {
        return type switch
        {
            GunPartType.Barrel => barrelPoint,
            GunPartType.Stock => stockPoint,
            GunPartType.Magazine => magazinePoint,
            GunPartType.Sight => sightPoint,
            _ => null
        };
    }

    public GunStats CalculateStats(GunStats baseStats)
    {
        GunStats finalStats = baseStats;

        foreach (var part in equippedParts)
        {
            finalStats.damage += part.damageModifier;
            finalStats.fireRate += part.fireRateModifier;
            finalStats.recoil += part.recoilModifier;
            finalStats.accuracy += part.accuracyModifier;
        }

        return finalStats;
    }

    public GunStats GetCurrentStats()
    {
        return CalculateStats(baseStats);
    }

    public void Shoot(Vector3 shootFromPosition, Vector3 shootDirection)
    {
        GunStats finalStats = CalculateStats(baseStats);

        // Check fire rate
        float timeSinceLastShot = Time.time - lastShootTime;
        float fireRateCooldown = 1f / finalStats.fireRate;
        
        if (timeSinceLastShot < fireRateCooldown)
        {
            return;
        }

        lastShootTime = Time.time;

        // Use all layers if damageLayer is set to Nothing
        int layerMask = damageLayer == 0 ? ~0 : damageLayer;

        Debug.Log($"Raycast: from {shootFromPosition}, direction {shootDirection}, range {shootRange}");

        // Raycast - straight from camera, no spread
        if (Physics.Raycast(shootFromPosition, shootDirection, out RaycastHit hit, shootRange, layerMask))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            Debug.DrawLine(shootFromPosition, hit.point, Color.red, 999999f);

            // Deal damage
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(finalStats.damage);
                Debug.Log($"Dealt {finalStats.damage} damage to {hit.collider.gameObject.name}");
            }
            else
            {
                Debug.Log($"Hit {hit.collider.gameObject.name} but no Health component found");
            }

            // Spawn bullet hole
            if (bulletHolePrefab != null)
            {
                Instantiate(bulletHolePrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            Debug.Log("Raycast did not hit anything");
            Debug.DrawLine(shootFromPosition, shootFromPosition + shootDirection * shootRange, Color.green, 999999f);
        }
    }
}
