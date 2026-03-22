using UnityEngine;

public class HealthPackSpawner : MonoBehaviour
{
    [SerializeField] GameObject healthPackPrefab;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float spawnInterval = 10f;
    [SerializeField] bool spawnOnStart = true;

    float timeSinceLastSpawn = 0f;
    GameObject activeHealthPack;

    void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (spawnOnStart && healthPackPrefab != null)
        {
            SpawnHealthPack();
        }
    }

    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnInterval && activeHealthPack == null && healthPackPrefab != null)
        {
            SpawnHealthPack();
            timeSinceLastSpawn = 0f;
        }
    }

    void SpawnHealthPack()
    {
        activeHealthPack = Instantiate(healthPackPrefab, spawnPoint.position, Quaternion.identity);

        // Track when this pack is destroyed
        HealthPackTracker tracker = activeHealthPack.GetComponent<HealthPackTracker>();
        if (tracker == null)
            tracker = activeHealthPack.AddComponent<HealthPackTracker>();
        
        tracker.OnDestroyed += OnHealthPackDestroyed;
    }

    void OnHealthPackDestroyed()
    {
        activeHealthPack = null;
    }
}

// Helper component to track when a health pack is destroyed
public class HealthPackTracker : MonoBehaviour
{
    public System.Action OnDestroyed;

    void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }
}
