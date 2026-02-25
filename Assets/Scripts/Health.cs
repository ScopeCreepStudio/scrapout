using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] Canvas worldCanvas;
    [SerializeField] GameObject hitTemplatePrefab;
    float currentHealth;

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

        if (currentHealth <= 0)
        {
            Die();
        }
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
