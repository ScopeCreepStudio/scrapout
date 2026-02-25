using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles visual overlay and UI feedback for active status effects
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    [SerializeField] private StatusEffectManager effectManager;
    [SerializeField] private Image overlaySpriteImage;
    [SerializeField] private Transform effectIconContainer;
    [SerializeField] private GameObject statusEffectUIPrefab;

    private Dictionary<string, StatusEffectUIElement> uiElements = new();
    private StatusEffect currentSpriteEffect;
    private Coroutine fadeOutRoutine;
    private float fadeDuration = 0.3f;

    private void Start()
    {
        if (effectManager == null)
        {
            effectManager = GetComponent<StatusEffectManager>();
        }

        if (effectManager != null)
        {
            effectManager.EffectApplied += OnEffectApplied;
            effectManager.EffectRemoved += OnEffectRemoved;
            effectManager.EffectsChanged += OnEffectsChanged;
            effectManager.EffectsUpdated += OnEffectsUpdated;
        }
    }

    private void Update()
    {
        // Update timers every frame for smooth countdown
        if (effectManager != null)
        {
            var activeEffects = effectManager.GetActiveEffects();
            foreach (var kvp in activeEffects)
            {
                if (uiElements.TryGetValue(kvp.Key, out var uiElement))
                {
                    uiElement.UpdateDuration(kvp.Value.GetRemainingTime(), kvp.Value.statusEffect.duration);
                }
            }
        }
    }

    private void OnEffectApplied(StatusEffect effect)
    {
        UpdateOverlaySprite(effect);

        // Create UI element if prefab is assigned
        if (statusEffectUIPrefab != null && effectIconContainer != null)
        {
            CreateStatusEffectUIElement(effect);
        }
    }

    private void OnEffectRemoved(string effectName)
    {
        if (currentSpriteEffect != null && currentSpriteEffect.effectName == effectName)
        {
            FadeOutOverlaySprite();
        }

        if (uiElements.TryGetValue(effectName, out var uiElement))
        {
            Destroy(uiElement.gameObject);
            uiElements.Remove(effectName);
        }
    }

    private void OnEffectsChanged(Dictionary<string, ActiveStatusEffect> activeEffects)
    {
        // Update UI durations
        foreach (var kvp in activeEffects)
        {
            if (uiElements.TryGetValue(kvp.Key, out var uiElement))
            {
                uiElement.UpdateDuration(kvp.Value.GetRemainingTime(), kvp.Value.statusEffect.duration);
            }
        }
    }

    private void OnEffectsUpdated(Dictionary<string, ActiveStatusEffect> activeEffects)
    {
        // Update UI durations on every tick
        foreach (var kvp in activeEffects)
        {
            if (uiElements.TryGetValue(kvp.Key, out var uiElement))
            {
                uiElement.UpdateDuration(kvp.Value.GetRemainingTime(), kvp.Value.statusEffect.duration);
            }
        }
    }

    private void UpdateOverlaySprite(StatusEffect effect)
    {
        if (!effect.useOverlaySprite || effect.overlaySprite == null)
            return;

        currentSpriteEffect = effect;

        // Stop any existing fade-out
        if (fadeOutRoutine != null)
        {
            StopCoroutine(fadeOutRoutine);
        }

        if (overlaySpriteImage != null)
        {
            overlaySpriteImage.sprite = effect.overlaySprite;
            Color color = overlaySpriteImage.color;
            color.a = effect.overlayIntensity;
            overlaySpriteImage.color = color;
        }
    }

    private void FadeOutOverlaySprite()
    {
        if (fadeOutRoutine != null)
        {
            StopCoroutine(fadeOutRoutine);
        }
        fadeOutRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        if (overlaySpriteImage == null)
            yield break;

        float time = 0f;
        Color startColor = overlaySpriteImage.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            Color color = overlaySpriteImage.color;
            color.a = Mathf.Lerp(startColor.a, 0f, t);
            overlaySpriteImage.color = color;

            yield return null;
        }

        Color finalColor = overlaySpriteImage.color;
        finalColor.a = 0f;
        overlaySpriteImage.color = finalColor;
        currentSpriteEffect = null;
    }

    private void ClearOverlaySprite()
    {
        currentSpriteEffect = null;

        if (overlaySpriteImage != null)
        {
            Color color = overlaySpriteImage.color;
            color.a = 0f;
            overlaySpriteImage.color = color;
        }
    }

    private void CreateStatusEffectUIElement(StatusEffect effect)
    {
        if (uiElements.ContainsKey(effect.effectName)) return;

        GameObject uiGO = Instantiate(statusEffectUIPrefab, effectIconContainer);
        StatusEffectUIElement uiElement = uiGO.GetComponent<StatusEffectUIElement>();

        if (uiElement != null)
        {
            uiElement.Initialize(effect, effect.duration);
            uiElements[effect.effectName] = uiElement;
        }
    }

    private void OnDestroy()
    {
        if (effectManager != null)
        {
            effectManager.EffectApplied -= OnEffectApplied;
            effectManager.EffectRemoved -= OnEffectRemoved;
            effectManager.EffectsChanged -= OnEffectsChanged;
            effectManager.EffectsUpdated -= OnEffectsUpdated;
        }
    }
}
