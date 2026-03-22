using UnityEngine;

/// Electrocuted status effect - stuns the player and violently shakes their camera.
/// Hooks directly into PlayerControllerV2's ShakeOffset so it doesn't fight the look system.
[CreateAssetMenu(fileName = "Electrocuted", menuName = "Status Effects/Electrocuted")]
public class ElectrocutedEffect : StatusEffect
{
    [Header("Electrocuted Specific")]
    [SerializeField] private float tickRate = 0.3f;
    [SerializeField] private float cameraShakeIntensity = 8f;  // Degrees of rotation
    [SerializeField] private float cameraShakeSpeed = 25f;     // How fast it flicks

    private float lastTickTime = 0f;
    private float shakeTimer = 0f;
    private PlayerControllerV2 playerController;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(effectName))
        {
            effectName = "Electrocuted";
            description = "A violent surge of electricity locks your muscles and scrambles your senses.";
            duration = 3f;
            damagePerSecond = 6f;
            overlayColor = new Color(0.5f, 0.9f, 1f, 0.35f);
            overlayIntensity = 0.6f;
            causesMovementImpairment = true;
            movementSpeedMultiplier = 0f; // Fully stunned
        }
    }

    public override void OnApply(GameObject target)
    {
        base.OnApply(target);
        lastTickTime = 0f;
        shakeTimer = 0f;
        playerController = target.GetComponent<PlayerControllerV2>();
        Debug.Log($"{target.name} is being electrocuted!");
    }

    public override void OnUpdate(GameObject target, float deltaTime)
    {
        // Damage tick
        lastTickTime += deltaTime;
        if (lastTickTime >= tickRate)
        {
            Health health = target.GetComponent<Health>();
            if (health != null)
                health.TakeDamage(damagePerSecond * tickRate);
            lastTickTime = 0f;
        }

        // Camera shake — write into PlayerControllerV2's shake offset
        if (playerController != null)
        {
            shakeTimer += deltaTime * cameraShakeSpeed;

            float pitchShake = Mathf.Sin(shakeTimer * 1.3f) * cameraShakeIntensity;
            float yawShake = Mathf.Sin(shakeTimer * 1.7f) * cameraShakeIntensity;

            playerController.ShakeOffset = new Vector2(pitchShake, yawShake);
        }
    }

    public override void OnRemove(GameObject target)
    {
        if (playerController != null)
            playerController.ShakeOffset = Vector2.zero;

        playerController = null;

        base.OnRemove(target);
        Debug.Log($"{target.name} recovered from the electric shock!");
    }
}