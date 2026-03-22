using UnityEngine;

/// <summary>
/// Burning status effect - sets the target ablaze, dealing damage over time.
/// Damage intensifies over the first few seconds before fading out.
/// </summary>
[CreateAssetMenu(fileName = "Burning", menuName = "Status Effects/Burning")]
public class BurningEffect : StatusEffect
{
    [Header("Burning Specific")]
    [SerializeField] private float tickRate = 0.5f;       // How often to apply damage
    [SerializeField] private float burnIntensity = 1.5f;  // Damage multiplier at peak burn
    [SerializeField] private float rampUpTime = 2f;       // Seconds before burn reaches peak intensity

    private float lastTickTime = 0f;
    private float elapsedTime = 0f;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(effectName))
        {
            effectName = "Burning";
            description = "Flames engulf the target, dealing escalating fire damage over time.";
            duration = 6f;
            damagePerSecond = 8f;
            overlayColor = new Color(1f, 0.4f, 0f, 0.4f); // Orange fiery overlay
            overlayIntensity = 0.5f;
            causesMovementImpairment = false;
            movementSpeedMultiplier = 1f;
        }
    }

    public override void OnApply(GameObject target)
    {
        base.OnApply(target);
        lastTickTime = 0f;
        elapsedTime = 0f;
        Debug.Log($"{target.name} is on fire!");
    }

    public override void OnUpdate(GameObject target, float deltaTime)
    {
        elapsedTime += deltaTime;
        lastTickTime += deltaTime;

        if (lastTickTime >= tickRate)
        {
            Health health = target.GetComponent<Health>();
            if (health != null)
            {
                // Ramp up damage from 1x to burnIntensity over rampUpTime, then hold at peak
                float rampProgress = Mathf.Clamp01(elapsedTime / rampUpTime);
                float currentMultiplier = Mathf.Lerp(1f, burnIntensity, rampProgress);

                health.TakeDamage(damagePerSecond * tickRate * currentMultiplier);
            }

            lastTickTime = 0f;
        }
    }

    public override void OnRemove(GameObject target)
    {
        base.OnRemove(target);
        Debug.Log($"The flames on {target.name} have been extinguished!");
    }
}