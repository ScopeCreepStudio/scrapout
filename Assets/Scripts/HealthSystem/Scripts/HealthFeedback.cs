using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthFeedback : MonoBehaviour
{
    [Header("Hurt Sounds")]
    [SerializeField] AudioClip[] hurtSounds;
    [SerializeField] float hurtSoundVolume = 0.8f;
    [SerializeField] AudioSource hurtSoundSource;
    [SerializeField] float hurtSoundCooldown = 0.2f; // Minimum time between hurt sounds

    [Header("Heartbeat")]
    [SerializeField] AudioClip heartbeatClip;
    [SerializeField] AudioSource heartbeatSource;
    [SerializeField] float heartbeatMinPitch = 0.8f;
    [SerializeField] float heartbeatMaxPitch = 1.5f;
    [SerializeField] float healthThresholdToStartHeartbeat = 0.4f; // Start at 40% health
    [SerializeField] float minTimeBetweenBeats = 0.3f;
    [SerializeField] float maxTimeBetweenBeats = 1.5f;

    [Header("Blood Vignette")]
    [SerializeField] Image bloodVignetteImage;
    [SerializeField] float vignetteFlashAlpha = 0.6f;
    [SerializeField] float vignetteFadeDuration = 0.5f;
    [SerializeField] float vignetteHealthAlpha = 0.3f; // Max alpha when at 0 health

    Health health;
    Coroutine heartbeatRoutine;
    Coroutine vignetteFadeRoutine;
    float lastHeartbeatTime;
    float lastHurtSoundTime;

    void Start()
    {
        health = GetComponent<Health>();
        if (health == null)
            health = FindObjectOfType<Health>();

        // Get or create audio sources on the player
        if (hurtSoundSource == null)
        {
            hurtSoundSource = GetComponent<AudioSource>();
            if (hurtSoundSource == null)
            {
                hurtSoundSource = gameObject.AddComponent<AudioSource>();
                hurtSoundSource.spatialBlend = 0f;
            }
            hurtSoundSource.volume = hurtSoundVolume;
        }

        if (heartbeatSource == null)
        {
            // Try to find a second audio source, or create one
            AudioSource[] sources = GetComponents<AudioSource>();
            heartbeatSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            heartbeatSource.spatialBlend = 0f;
            heartbeatSource.volume = 0.6f;
        }

        // Setup vignette if not assigned
        if (bloodVignetteImage == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject vignetteObj = new GameObject("BloodVignette");
                vignetteObj.transform.SetParent(canvas.transform, false);

                bloodVignetteImage = vignetteObj.AddComponent<Image>();
                bloodVignetteImage.color = new Color(1f, 0f, 0f, 0f); // Red, transparent
                
                RectTransform rect = bloodVignetteImage.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // Set sort order to be on top
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    canvas.sortingOrder = 100;
            }
        }

        if (bloodVignetteImage != null)
        {
            Color startColor = bloodVignetteImage.color;
            startColor.a = 0f;
            bloodVignetteImage.color = startColor;
        }

        // Subscribe to health events
        if (health != null)
            health.OnDamageTaken.AddListener(OnHealthDamaged);

        // Start heartbeat management
        if (heartbeatRoutine == null)
            heartbeatRoutine = StartCoroutine(ManageHeartbeat());
    }

    void OnDestroy()
    {
        if (health != null && health.OnDamageTaken != null)
            health.OnDamageTaken.RemoveListener(OnHealthDamaged);
    }

    void OnHealthDamaged(float damage, float newHealth)
    {
        // Play random hurt sound (with cooldown)
        if (hurtSounds != null && hurtSounds.Length > 0 && hurtSoundSource != null && damage > 0f)
        {
            if (Time.time - lastHurtSoundTime >= hurtSoundCooldown)
            {
                AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
                if (clip != null)
                    hurtSoundSource.PlayOneShot(clip, hurtSoundVolume);
                lastHurtSoundTime = Time.time;
            }
        }

        // Flash blood vignette
        if (bloodVignetteImage != null)
        {
            if (vignetteFadeRoutine != null)
                StopCoroutine(vignetteFadeRoutine);
            vignetteFadeRoutine = StartCoroutine(FlashVignette());
        }
    }

    IEnumerator FlashVignette()
    {
        // Quick flash to high alpha
        Color color = bloodVignetteImage.color;
        color.a = vignetteFlashAlpha;
        bloodVignetteImage.color = color;

        yield return new WaitForSeconds(0.1f);

        // Fade out over vignetteFadeDuration
        float elapsed = 0f;
        while (elapsed < vignetteFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vignetteFadeDuration;

            // Calculate target alpha based on current health
            float healthPercent = health != null ? health.GetHealth() / health.GetMaxHealth() : 1f;
            float targetAlpha = Mathf.Lerp(vignetteHealthAlpha, 0f, healthPercent);

            color = bloodVignetteImage.color;
            color.a = Mathf.Lerp(vignetteFlashAlpha, targetAlpha, t);
            bloodVignetteImage.color = color;

            yield return null;
        }

        vignetteFadeRoutine = null;
    }

    IEnumerator ManageHeartbeat()
    {
        while (true)
        {
            if (health != null && heartbeatSource != null && heartbeatClip != null)
            {
                float maxHealth = health.GetMaxHealth();
                float currentHealth = health.GetHealth();
                float healthPercent = currentHealth / maxHealth;

                // Only play heartbeat if health is low enough
                if (healthPercent <= healthThresholdToStartHeartbeat)
                {
                    // Calculate pitch: lower health = faster heartbeat
                    float healthRatio = healthPercent / healthThresholdToStartHeartbeat;
                    heartbeatSource.pitch = Mathf.Lerp(heartbeatMaxPitch, heartbeatMinPitch, healthRatio);

                    // Calculate time between beats: lower health = faster beats
                    float timeBetweenBeats = Mathf.Lerp(minTimeBetweenBeats, maxTimeBetweenBeats, healthRatio);

                    // Play heartbeat
                    if (Time.time - lastHeartbeatTime >= timeBetweenBeats)
                    {
                        heartbeatSource.PlayOneShot(heartbeatClip, 0.6f);
                        lastHeartbeatTime = Time.time;
                    }
                }
                else if (heartbeatSource.isPlaying)
                {
                    // Stop heartbeat if health is above threshold
                    heartbeatSource.Stop();
                }
            }

            // Update vignette alpha based on health
            if (health != null && bloodVignetteImage != null && vignetteFadeRoutine == null)
            {
                float healthPercent = health.GetHealth() / health.GetMaxHealth();
                float targetAlpha = Mathf.Lerp(vignetteHealthAlpha, 0f, healthPercent);

                Color color = bloodVignetteImage.color;
                color.a = targetAlpha;
                bloodVignetteImage.color = color;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}
