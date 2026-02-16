using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GunAssembler : MonoBehaviour
{
    [Header("Attachment Points")]
    public Transform barrelPoint;
    public Transform stockPoint;
    public Transform magazinePoint;
    public Transform opticPoint;
    public Transform gripPoint;

    [Header("Base Stats")]
    [SerializeField] GunStats baseStats;

    [Header("Shooting")]
    [SerializeField] float shootRange = 100f;
    [SerializeField] LayerMask damageLayer;
    [SerializeField] GameObject bulletHolePrefab;
    [SerializeField] float bulletHoleOffset = 0.01f;
    [SerializeField, Range(1, 200)] int maxBulletHoles = 50;
    [SerializeField] Transform bulletHoleParent;
    [SerializeField] AudioSource fireAudio;
    [SerializeField] ParticleSystem fireVfx;
    [SerializeField] TrailRenderer tracerPrefab;
    [SerializeField] float tracerDuration = 0.05f;
    [SerializeField] float tracerSpeed = 200f;

    private Dictionary<GunPartType, GameObject> currentModels = new();
    private List<GunPart> equippedParts = new();
    private readonly Queue<GameObject> bulletHolePool = new();
    private float lastShootTime;
    private Transform currentBodyRoot;
    private Transform firePoint;
    private int currentAmmo;
    private bool isReloading;
    private Coroutine reloadRoutine;

    public event Action<int, int> AmmoChanged;
    public event Action ReloadStarted;
    public event Action ReloadFinished;
    public event Action<Collider> HitConfirmed;

    private void Start()
    {
        SetAmmoToMax();
        ResolveBulletHoleParent();
    }

    public void EquipPart(GunPart part)
    {
        if (part == null || part.modelPrefab == null) return;

        Transform attachPoint = GetAttachPoint(part.partType);
        if (attachPoint == null) return;

        // Remove old model
        if (currentModels.ContainsKey(part.partType))
        {
            Destroy(currentModels[part.partType]);
        }

        // Spawn new model
        GameObject model = Instantiate(part.modelPrefab, attachPoint);

        // Align part using its own attachment point (e.g., BarrelPoint)
        Transform partAttachPoint = FindPartAttachPoint(model.transform, part.partType);
        if (partAttachPoint != null)
        {
            // Match rotation then position so the part's attach point lines up to the gun's attach point
            model.transform.rotation = attachPoint.rotation * Quaternion.Inverse(partAttachPoint.localRotation);
            Vector3 offset = attachPoint.position - partAttachPoint.position;
            model.transform.position += offset;
        }
        else
        {
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }

        currentModels[part.partType] = model;

        if (part.partType == GunPartType.GunBody)
        {
            currentBodyRoot = model.transform;
        }

        if (part.partType == GunPartType.Barrel)
        {
            CacheFirePointFromBarrel(model.transform);
        }

        // Replace part data
        equippedParts.RemoveAll(p => p.partType == part.partType);
        equippedParts.Add(part);

        UpdateAmmoCapacity();
    }

    private Transform FindPartAttachPoint(Transform partRoot, GunPartType type)
    {
        if (partRoot == null) return null;

        string specificName = type switch
        {
            GunPartType.Barrel => "BarrelPoint",
            GunPartType.Stock => "StockPoint",
            GunPartType.Magazine => "MagazinePoint",
            GunPartType.Optic => "OpticPoint",
            GunPartType.Grip => "GripPoint",
            _ => "AttachPoint"
        };

        Transform found = partRoot.Find(specificName);
        if (found != null) return found;

        // Fallbacks
        found = partRoot.Find("AttachPoint");
        if (found != null) return found;

        foreach (Transform child in partRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == specificName || child.name == "AttachPoint")
                return child;
        }

        return null;
    }

    private void CacheFirePointFromBarrel(Transform barrelRoot)
    {
        if (barrelRoot == null) return;

        Transform found = barrelRoot.Find("FirePoint");
        if (found == null)
        {
            foreach (Transform child in barrelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "FirePoint")
                {
                    found = child;
                    break;
                }
            }
        }

        if (found != null)
        {
            firePoint = found;
            if (fireAudio == null)
                fireAudio = firePoint.GetComponentInChildren<AudioSource>(true);
            if (fireVfx == null)
                fireVfx = firePoint.GetComponentInChildren<ParticleSystem>(true);

            if (fireAudio == null)
                fireAudio = barrelRoot.GetComponentInChildren<AudioSource>(true);
            if (fireVfx == null)
                fireVfx = barrelRoot.GetComponentInChildren<ParticleSystem>(true);

            Debug.Log($"FirePoint found on barrel: {firePoint.name}");
            if (fireAudio == null)
                Debug.LogWarning("FirePoint found but no AudioSource attached.");
            if (fireVfx == null)
                Debug.LogWarning("FirePoint found but no ParticleSystem found in children.");
        }
        else
        {
            Debug.LogWarning("FirePoint not found on barrel. Expected a child named 'FirePoint'.");
        }
    }

    Transform GetAttachPoint(GunPartType type)
    {
        if (type == GunPartType.GunBody)
            return transform;

        Transform bodyPoint = GetBodyAttachPoint(type);
        if (bodyPoint != null)
            return bodyPoint;

        return type switch
        {
            GunPartType.Barrel => barrelPoint,
            GunPartType.Stock => stockPoint,
            GunPartType.Magazine => magazinePoint,
            GunPartType.Optic => opticPoint,
            GunPartType.Grip => gripPoint,
            _ => null
        };
    }

    private Transform GetBodyAttachPoint(GunPartType type)
    {
        if (currentBodyRoot == null) return null;

        string pointName = type switch
        {
            GunPartType.Barrel => "BarrelPoint",
            GunPartType.Stock => "StockPoint",
            GunPartType.Magazine => "MagazinePoint",
            GunPartType.Optic => "OpticPoint",
            GunPartType.Grip => "GripPoint",
            _ => null
        };

        if (string.IsNullOrEmpty(pointName)) return null;

        Transform found = currentBodyRoot.Find(pointName);
        if (found != null) return found;

        foreach (Transform child in currentBodyRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == pointName)
                return child;
        }

        return null;
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
            finalStats.range += part.rangeModifier;
            finalStats.ammoCapacity += part.ammoCapacityModifier;
            finalStats.reloadSpeed += part.reloadSpeedModifier;
        }

        return finalStats;
    }

    public GunStats GetCurrentStats()
    {
        return CalculateStats(baseStats);
    }

    public int CurrentAmmo => currentAmmo;

    public int MaxAmmo
    {
        get
        {
            GunStats stats = CalculateStats(baseStats);
            return Mathf.Max(1, stats.ammoCapacity);
        }
    }

    public bool Shoot(Vector3 shootFromPosition, Vector3 shootDirection)
    {
        GunStats finalStats = CalculateStats(baseStats);

        if (isReloading)
        {
            return false;
        }

        if (currentAmmo <= 0)
        {
            Reload();
            return false;
        }

        // Check fire rate
        float timeSinceLastShot = Time.time - lastShootTime;
        float fireRateCooldown = 1f / Mathf.Max(0.01f, finalStats.fireRate);
        
        if (timeSinceLastShot < fireRateCooldown)
        {
            return false;
        }

        lastShootTime = Time.time;
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        NotifyAmmoChanged();

        if (firePoint == null || fireVfx == null || fireAudio == null)
        {
            TryResolveFireComponents();
        }

        if (fireVfx != null)
        {
            fireVfx.Play(true);
            Debug.Log($"FireVFX played: {fireVfx.name}");
        }
        else
        {
            Debug.LogWarning("FireVFX is null on shoot. Check FirePoint ParticleSystem reference.");
        }

        if (fireAudio != null)
        {
            if (fireAudio.clip != null)
            {
                fireAudio.PlayOneShot(fireAudio.clip);
                Debug.Log($"FireAudio PlayOneShot: {fireAudio.clip.name}");
            }
            else
            {
                fireAudio.Play();
                Debug.Log($"FireAudio played: {fireAudio.name}");
            }
        }
        else
        {
            Debug.LogWarning("FireAudio is null on shoot. Check FirePoint AudioSource reference.");
        }

        // Use all layers if damageLayer is set to Nothing
        int layerMask = damageLayer == 0 ? ~0 : damageLayer;

        Debug.Log($"Raycast: from {shootFromPosition}, direction {shootDirection}, range {shootRange}");

        float range = finalStats.range > 0f ? finalStats.range : shootRange;

        Vector3 tracerStart = firePoint != null ? firePoint.position : shootFromPosition;
        Vector3 tracerEnd = shootFromPosition + shootDirection * range;

        // Raycast - straight from camera, no spread
        if (Physics.Raycast(shootFromPosition, shootDirection, out RaycastHit hit, range, layerMask))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            Debug.DrawLine(shootFromPosition, hit.point, Color.red, 999999f);

            tracerEnd = hit.point;

            HitConfirmed?.Invoke(hit.collider);

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
                Vector3 holePos = hit.point + hit.normal * bulletHoleOffset;
                SpawnBulletHole(holePos, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            Debug.Log("Raycast did not hit anything");
            Debug.DrawLine(shootFromPosition, shootFromPosition + shootDirection * range, Color.green, 999999f);
        }

        SpawnTracer(tracerStart, tracerEnd);
        return true;
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (tracerPrefab == null) return;

        TrailRenderer tracer = Instantiate(tracerPrefab, start, Quaternion.identity);
        StartCoroutine(AnimateTracer(tracer, end));
    }

    private IEnumerator AnimateTracer(TrailRenderer tracer, Vector3 end)
    {
        if (tracer == null) yield break;

        Vector3 start = tracer.transform.position;
        float distance = Vector3.Distance(start, end);
        float travelTime = Mathf.Max(0.01f, distance / Mathf.Max(1f, tracerSpeed));
        float time = 0f;

        while (time < travelTime && tracer != null)
        {
            time += Time.deltaTime;
            float t = time / travelTime;
            tracer.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (tracer != null)
            tracer.transform.position = end;

        yield return new WaitForSeconds(tracerDuration);
        if (tracer != null)
            Destroy(tracer.gameObject);
    }

    private void TryResolveFireComponents()
    {
        Transform barrelRoot = null;
        if (currentModels.TryGetValue(GunPartType.Barrel, out GameObject barrelModel) && barrelModel != null)
            barrelRoot = barrelModel.transform;

        if (firePoint == null && barrelRoot != null)
            CacheFirePointFromBarrel(barrelRoot);

        if (firePoint == null && currentBodyRoot != null)
            firePoint = FindFirePointRecursive(currentBodyRoot);

        if (firePoint == null)
            firePoint = FindFirePointRecursive(transform);

        if (firePoint != null)
        {
            if (fireAudio == null)
                fireAudio = firePoint.GetComponentInChildren<AudioSource>(true);
            if (fireVfx == null)
                fireVfx = firePoint.GetComponentInChildren<ParticleSystem>(true);
        }

        if (fireAudio == null)
        {
            if (barrelRoot != null)
                fireAudio = barrelRoot.GetComponentInChildren<AudioSource>(true);
            if (fireAudio == null && currentBodyRoot != null)
                fireAudio = currentBodyRoot.GetComponentInChildren<AudioSource>(true);
            if (fireAudio == null)
                fireAudio = GetComponentInChildren<AudioSource>(true);
        }

        if (fireVfx == null)
        {
            if (barrelRoot != null)
                fireVfx = barrelRoot.GetComponentInChildren<ParticleSystem>(true);
            if (fireVfx == null && currentBodyRoot != null)
                fireVfx = currentBodyRoot.GetComponentInChildren<ParticleSystem>(true);
            if (fireVfx == null)
                fireVfx = GetComponentInChildren<ParticleSystem>(true);
        }

        if (firePoint == null)
            Debug.LogWarning("FirePoint not found anywhere in gun hierarchy.");
        if (fireAudio == null)
            Debug.LogWarning("FireAudio not found anywhere in gun hierarchy.");
        if (fireVfx == null)
            Debug.LogWarning("FireVFX not found anywhere in gun hierarchy.");
    }

    private Transform FindFirePointRecursive(Transform root)
    {
        if (root == null) return null;
        if (root.name == "FirePoint") return root;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "FirePoint")
                return child;
        }

        return null;
    }

    public void Reload()
    {
        if (isReloading) return;
        if (currentAmmo >= MaxAmmo) return;

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        ReloadStarted?.Invoke();

        float duration = Mathf.Max(0.05f, GetReloadDurationSeconds());
        yield return new WaitForSeconds(duration);

        currentAmmo = MaxAmmo;
        NotifyAmmoChanged();
        isReloading = false;
        ReloadFinished?.Invoke();
        reloadRoutine = null;
    }

    private float GetReloadDurationSeconds()
    {
        GunStats stats = CalculateStats(baseStats);
        return stats.reloadSpeed;
    }

    private void UpdateAmmoCapacity()
    {
        int maxAmmo = MaxAmmo;
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        if (currentAmmo == 0)
            currentAmmo = maxAmmo;
        NotifyAmmoChanged();
    }

    private void SetAmmoToMax()
    {
        currentAmmo = MaxAmmo;
        NotifyAmmoChanged();
    }

    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(currentAmmo, MaxAmmo);
    }

    private void ResolveBulletHoleParent()
    {
        if (bulletHoleParent != null) return;
        GameObject found = GameObject.Find("bulletholepooling");
        if (found != null)
            bulletHoleParent = found.transform;
    }

    private void SpawnBulletHole(Vector3 position, Quaternion rotation)
    {
        if (bulletHolePrefab == null) return;

        int poolLimit = Mathf.Max(1, maxBulletHoles);
        GameObject hole = null;

        if (bulletHolePool.Count >= poolLimit)
        {
            // Reuse the oldest still-alive entry
            while (bulletHolePool.Count > 0 && hole == null)
                hole = bulletHolePool.Dequeue();
        }

        if (hole == null)
            hole = Instantiate(bulletHolePrefab);

        if (bulletHoleParent == null)
            ResolveBulletHoleParent();

        if (bulletHoleParent != null)
            hole.transform.SetParent(bulletHoleParent, true);

        hole.transform.SetPositionAndRotation(position, rotation);
        hole.SetActive(true);
        bulletHolePool.Enqueue(hole);
    }
}
