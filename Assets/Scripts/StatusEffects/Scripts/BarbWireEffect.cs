using UnityEngine;

/// <summary>
/// Barb Wire status effect - damages the player over time
/// Player takes moderate damage for a moderate duration
/// </summary>
[CreateAssetMenu(fileName = "BarbWire", menuName = "Status Effects/Barb Wire")]
public class BarbWireEffect : StatusEffect
{
    [Header("Barb Wire Specific")]
    [SerializeField] private float tickRate = 0.2f; // How often to apply damage
    private float lastTickTime = 0f;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(effectName))
        {
            effectName = "Barb Wire";
            description = "Sharp barbed wire cuts into your flesh, dealing continuous damage.";
            duration = 8f;
            damagePerSecond = 5f;
            overlayColor = new Color(0.8f, 0.1f, 0.1f, 0.4f); // Red-ish overlay
            overlayIntensity = 0.4f;
            causesMovementImpairment = true;
            movementSpeedMultiplier = 0.6f; // 60% speed
        }
    }

    public override void OnApply(GameObject target)
    {
        base.OnApply(target);
        lastTickTime = 0f;
        Debug.Log($"{effectName} is tearing into {target.name}!");
    }

    public override void OnUpdate(GameObject target, float deltaTime)
    {
        lastTickTime += deltaTime;

        if (lastTickTime >= tickRate)
        {
            Health health = target.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damagePerSecond * tickRate);
            }
            lastTickTime = 0f;
        }
    }

    public override void OnRemove(GameObject target)
    {
        base.OnRemove(target);
        Debug.Log($"{target.name} escaped the barbed wire!");
    }
}
