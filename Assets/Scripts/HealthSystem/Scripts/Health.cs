using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] Canvas worldCanvas;
    [SerializeField] GameObject hitTemplatePrefab;
    float currentHealth;

    // Event fired when damage is taken
    public UnityEvent<float, float> OnDamageTaken = new UnityEvent<float, float>();

    void Start()
    {
        currentHealth = maxHealth;

        if (worldCanvas == null)
            worldCanvas = FindObjectOfType<Canvas>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}");

        ShowDamageNumber(damage);
        OnDamageTaken.Invoke(damage, currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        Debug.Log($"{gameObject.name} healed for {amount}. Health: {currentHealth}");
        OnDamageTaken.Invoke(-amount, currentHealth); // Invoke with negative damage for consistency
    }

    void ShowDamageNumber(float damage)
    {
        if (hitTemplatePrefab == null || worldCanvas == null)
        {
            Debug.Log("Missing hitTemplatePrefab or Canvas");
            return;
        }

        // Spawn slightly above the object
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject damageNumberGO = Instantiate(hitTemplatePrefab, worldCanvas.transform);
        damageNumberGO.transform.position = spawnPos;
        damageNumberGO.transform.localPosition = new Vector3(1f, 0f, 0f);

        DamagePopup damageNumber = damageNumberGO.GetComponent<DamagePopup>();
        if (damageNumber != null)
        {
            damageNumber.SetText(damage);
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        Destroy(gameObject);
    }

    public float GetHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
}
