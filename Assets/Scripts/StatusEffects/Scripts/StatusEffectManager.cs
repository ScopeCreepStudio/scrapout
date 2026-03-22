using UnityEngine;
using System;
using System.Collections.Generic;

/// Manages all active status effects on a character/player
public class StatusEffectManager : MonoBehaviour
{
    [SerializeField] private float statusEffectCheckInterval = 0.1f;

    private Dictionary<string, ActiveStatusEffect> activeEffects = new();
    private List<string> effectsToRemove = new();
    private float effectCheckTimer = 0f;
    private AudioSource audioSource;

    public event Action<StatusEffect> EffectApplied;
    public event Action<string> EffectRemoved;
    public event Action<Dictionary<string, ActiveStatusEffect>> EffectsChanged;
    public event Action<Dictionary<string, ActiveStatusEffect>> EffectsUpdated;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        // OnUpdate runs every frame so things like camera shake and movement
        // impairment are applied continuously, not in 0.1s chunks
        TickEffectUpdates(Time.deltaTime);

        // Expiry check only needs to run on the interval
        effectCheckTimer += Time.deltaTime;
        if (effectCheckTimer >= statusEffectCheckInterval)
        {
            effectCheckTimer = 0f;
            CheckForExpiredEffects();
        }
    }

    /// Apply a status effect to this character
    public void ApplyEffect(StatusEffect effect)
    {
        if (effect == null) return;

        string effectId = effect.effectName;

        // If same effect already active, reset its duration
        if (activeEffects.ContainsKey(effectId))
        {
            activeEffects[effectId].ResetDuration();
            return;
        }

        // Create new active effect instance
        ActiveStatusEffect activeEffect = new(effect);
        activeEffects[effectId] = activeEffect;

        // Call the effect's apply callback
        effect.OnApply(gameObject);

        // Play apply SFX
        if (effect.applySFX != null)
        {
            audioSource.PlayOneShot(effect.applySFX, effect.volume);
        }

        // Notify listeners
        EffectApplied?.Invoke(effect);
        EffectsChanged?.Invoke(activeEffects);

        Debug.Log($"Applied status effect: {effectId}");
    }

    /// Remove a specific status effect by name
    public void RemoveEffect(string effectName)
    {
        if (!activeEffects.ContainsKey(effectName)) return;

        StatusEffect effect = activeEffects[effectName].statusEffect;
        effect.OnRemove(gameObject);
        activeEffects.Remove(effectName);

        EffectRemoved?.Invoke(effectName);
        EffectsChanged?.Invoke(activeEffects);

        Debug.Log($"Removed status effect: {effectName}");
    }
    /// Get all currently active effects
    public Dictionary<string, ActiveStatusEffect> GetActiveEffects()
    {
        return new Dictionary<string, ActiveStatusEffect>(activeEffects);
    }
    /// Check if a specific effect is active
    public bool HasEffect(string effectName)
    {
        return activeEffects.ContainsKey(effectName);
    }

    /// <summary>
    /// Get remaining duration of an active effect
    /// </summary>
    public float GetEffectRemainingTime(string effectName)
    {
        if (activeEffects.TryGetValue(effectName, out var effect))
        {
            return effect.GetRemainingTime();
        }
        return -1f;
    }

    private void TickEffectUpdates(float deltaTime)
    {
        foreach (var kvp in activeEffects)
        {
            var activeEffect = kvp.Value;
            activeEffect.Update(deltaTime);

            // Call the effect's update callback every frame
            activeEffect.statusEffect.OnUpdate(gameObject, deltaTime);
        }

        if (activeEffects.Count > 0)
        {
            EffectsUpdated?.Invoke(activeEffects);
        }
    }

    private void CheckForExpiredEffects()
    {
        effectsToRemove.Clear();

        foreach (var kvp in activeEffects)
        {
            if (kvp.Value.HasExpired())
            {
                effectsToRemove.Add(kvp.Key);
            }
        }

        foreach (var effectName in effectsToRemove)
        {
            RemoveEffect(effectName);
        }
    }

    private void OnDestroy()
    {
        // Clean up all effects when object is destroyed
        var effectNames = new List<string>(activeEffects.Keys);
        foreach (var name in effectNames)
        {
            RemoveEffect(name);
        }
    }
}

/// Runtime instance of an active status effect
public class ActiveStatusEffect
{
    public StatusEffect statusEffect { get; private set; }
    private float elapsedTime = 0f;
    private float duration;

    public ActiveStatusEffect(StatusEffect effect)
    {
        statusEffect = effect;
        duration = effect.duration;
    }

    public void Update(float deltaTime)
    {
        elapsedTime += deltaTime;
    }

    public void ResetDuration()
    {
        elapsedTime = 0f;
    }

    public bool HasExpired()
    {
        if (statusEffect.isInfinite) return false;
        return elapsedTime >= duration;
    }

    public float GetRemainingTime()
    {
        if (statusEffect.isInfinite) return Mathf.Infinity;
        return Mathf.Max(0, duration - elapsedTime);
    }

    public float GetProgress()
    {
        if (statusEffect.isInfinite) return 1f;
        return Mathf.Clamp01(elapsedTime / duration);
    }
}