using UnityEngine;

public class HealthPack : MonoBehaviour
{
    [SerializeField] float healAmount = 25f;
    [SerializeField] AudioClip pickupSound;
    [SerializeField] float pickupSoundVolume = 0.8f;
    [SerializeField] ParticleSystem pickupEffect;

    [Header("Hover Animation")]
    [SerializeField] float hoverHeight = 0.5f;
    [SerializeField] float hoverSpeed = 2f;
    [SerializeField] float rotationSpeed = 120f;

    bool hasBeenPickedUp = false;
    Vector3 startPosition;
    float spawnTime;

    void Start()
    {
        startPosition = transform.position;
        spawnTime = Time.time;
    }

    void Update()
    {
        // Hovering movement (only goes up, doesn't dip down)
        float hoverOffset = (Mathf.Sin((Time.time - spawnTime) * hoverSpeed) + 1f) * 0.5f * hoverHeight;
        transform.position = startPosition + Vector3.up * hoverOffset;

        // Rotation (upside down spinning)
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenPickedUp)
            return;

        Health health = other.GetComponent<Health>();
        if (health == null)
            return;

        // Only pickup if not at full health
        if (health.GetHealth() >= health.GetMaxHealth())
            return;

        health.Heal(healAmount);
        PickUp();
    }

    void PickUp()
    {
        hasBeenPickedUp = true;

        // Play sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupSoundVolume);
        }

        // Play particle effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // Destroy this health pack
        Destroy(gameObject);
    }
}
