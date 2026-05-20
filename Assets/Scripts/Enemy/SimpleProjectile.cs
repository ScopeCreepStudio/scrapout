using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private string targetTag = "Player";

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = false;
    }

    private void OnEnable()
    {
        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        rb.linearVelocity = transform.forward * speed;
    }

    public void Configure(float newSpeed, float newDamage, string newTargetTag)
    {
        if (newSpeed > 0f) speed = newSpeed;
        if (newDamage > 0f) damage = newDamage;
        if (!string.IsNullOrEmpty(newTargetTag)) targetTag = newTargetTag;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (other == null) return;

        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            return;

        Health health = other.GetComponent<Health>();
        if (health != null)
            health.TakeDamage(damage);

        Destroy(gameObject);
    }
}
