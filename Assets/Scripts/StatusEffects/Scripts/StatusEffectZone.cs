using UnityEngine;
using System.Collections.Generic;

/// Trigger zone that applies a status effect to entities that enter it
public class StatusEffectZone : MonoBehaviour
{
    [SerializeField] private StatusEffect statusEffect;
    [SerializeField] private float reapplicationCooldown = 1f;
    [SerializeField] private bool destroyAfterApply = false;
    [Header("Application Rules")]
    [SerializeField] private bool applyOnEnter = true;
    [SerializeField] private bool applyWhileMoving = false;
    [SerializeField] private float minMoveSpeed = 0.1f;

    private Collider triggerCollider;
    private Dictionary<Collider, float> lastApplyTime = new();
    private Dictionary<Collider, Vector3> lastPositions = new();

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("StatusEffectZone requires a Collider component set as trigger!", gameObject);
            enabled = false;
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("StatusEffectZone collider is not set as trigger. Enabling it.", gameObject);
            triggerCollider.isTrigger = true;
        }

        if (statusEffect == null)
        {
            Debug.LogError("StatusEffectZone has no status effect assigned!", gameObject);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log($"Trigger entered by: {collision.gameObject.name}");

        if (statusEffect == null) return;

        // Use GetComponentInParent so the manager is found even if the
        // collider that entered is a child of the player root GameObject
        StatusEffectManager effectManager = collision.GetComponentInParent<StatusEffectManager>();
        if (effectManager == null) return;

        if (applyWhileMoving && !IsMoving(collision))
        {
            lastPositions[collision] = collision.transform.position;
            return;
        }

        // Check cooldown for this specific collision object
        if (lastApplyTime.TryGetValue(collision, out float lastTime))
        {
            if (Time.time - lastTime < reapplicationCooldown)
            {
                return;
            }
        }

        if (applyOnEnter)
        {
            // Apply the effect
            effectManager.ApplyEffect(statusEffect);
            lastApplyTime[collision] = Time.time;
            Debug.Log($"Applied {statusEffect.effectName} to {collision.gameObject.name}");
        }

        if (destroyAfterApply)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        if (!applyWhileMoving) return;
        if (statusEffect == null) return;

        // Use GetComponentInParent so the manager is found even if the
        // collider that entered is a child of the player root GameObject
        StatusEffectManager effectManager = collision.GetComponentInParent<StatusEffectManager>();
        if (effectManager == null) return;

        if (!IsMoving(collision)) return;

        if (lastApplyTime.TryGetValue(collision, out float lastTime))
        {
            if (Time.time - lastTime < reapplicationCooldown)
                return;
        }

        effectManager.ApplyEffect(statusEffect);
        lastApplyTime[collision] = Time.time;
    }

    private void OnTriggerExit(Collider collision)
    {
        lastApplyTime.Remove(collision);
        lastPositions.Remove(collision);
    }

    private bool IsMoving(Collider collision)
    {
        if (collision == null) return false;

        if (collision.attachedRigidbody != null)
        {
            return collision.attachedRigidbody.linearVelocity.magnitude >= minMoveSpeed;
        }

        CharacterController controller = collision.GetComponent<CharacterController>();
        if (controller != null)
        {
            return controller.velocity.magnitude >= minMoveSpeed;
        }

        Vector3 current = collision.transform.position;
        if (!lastPositions.TryGetValue(collision, out Vector3 last))
        {
            lastPositions[collision] = current;
            return false;
        }

        float speed = (current - last).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPositions[collision] = current;
        return speed >= minMoveSpeed;
    }

    public void SetStatusEffect(StatusEffect newEffect)
    {
        statusEffect = newEffect;
    }
}