using UnityEngine;

/// <summary>
/// Base class for all status effects. Extend this to create new status effects.
/// Use as a ScriptableObject for easy expansion and configuration.
/// </summary>
public class StatusEffect : ScriptableObject
{
    [Header("Identity")]
    public string effectName = "Status Effect";
    [TextArea(2, 4)]
    public string description = "Describes what this status effect does";
    public Sprite icon;

    [Header("Duration")]
    public float duration = 5f;
    public bool isInfinite = false;

    [Header("Visual Feedback")]
    public Color overlayColor = new Color(1, 1, 1, 0.2f);
    [Range(0, 1)]
    public float overlayIntensity = 0.3f;
    public Sprite overlaySprite;
    public bool useOverlaySprite = false;

    [Header("Audio Feedback")]
    public AudioClip applySFX;
    public AudioClip loopingSFX;
    [Range(0, 1)]
    public float volume = 1f;

    [Header("Effect Parameters")]
    public float damagePerSecond = 1f;
    public bool causesMovementImpairment = false;
    [Range(0, 1)]
    public float movementSpeedMultiplier = 1f;

    /// <summary>
    /// Called when the effect is first applied to a target
    /// </summary>
    public virtual void OnApply(GameObject target)
    {
        Debug.Log($"Status effect '{effectName}' applied to {target.name}");
    }

    /// <summary>
    /// Called every frame while the effect is active
    /// </summary>
    public virtual void OnUpdate(GameObject target, float deltaTime)
    {
        // Override in subclasses to implement custom behavior
        if (damagePerSecond > 0)
        {
            Health health = target.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damagePerSecond * deltaTime);
            }
        }
    }

    /// <summary>
    /// Called when the effect expires or is removed
    /// </summary>
    public virtual void OnRemove(GameObject target)
    {
        Debug.Log($"Status effect '{effectName}' removed from {target.name}");
    }
}
