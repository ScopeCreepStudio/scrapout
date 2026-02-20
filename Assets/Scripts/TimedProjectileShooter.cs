using UnityEngine;

public class TimedProjectileShooter : MonoBehaviour
{
    [SerializeField] private SimpleProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 3f;
    [SerializeField] private float projectileSpeed = 2f;
    [SerializeField] private float projectileDamage = 5f;
    [SerializeField] private string targetTag = "Player";

    private float nextFireTime;

    private void OnEnable()
    {
        nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);
    }

    private void Update()
    {
        if (projectilePrefab == null) return;

        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);
        }
    }

    private void Fire()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        SimpleProjectile projectile = Instantiate(projectilePrefab, origin.position, origin.rotation);
        projectile.Configure(projectileSpeed, projectileDamage, targetTag);
    }
}
