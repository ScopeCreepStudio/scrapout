using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual UI element for a status effect (icon + duration)
/// </summary>
public class StatusEffectUIElement : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [Header("Intro Animation")]
    [SerializeField] private RectTransform slideTarget;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float slideDistance = 140f;
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform cachedRect;
    private Coroutine slideRoutine;

    public void Initialize(StatusEffect effect, float duration)
    {
        EnsureReferences();

        if (iconImage != null && effect.icon != null)
        {
            iconImage.sprite = effect.icon;
        }

        UpdateTimerText(duration);

        PlayIntro();
    }

    public void UpdateDuration(float remainingTime, float maxDuration)
    {
        UpdateTimerText(remainingTime);
    }

    private void UpdateTimerText(float remainingTime)
    {
        if (timerText == null) return;

        if (float.IsInfinity(remainingTime))
        {
            timerText.text = "--";
            return;
        }

        float clamped = Mathf.Max(0f, remainingTime);
        timerText.text = $"{clamped:0.0}s";
    }

    private void EnsureReferences()
    {
        if (cachedRect == null)
            cachedRect = GetComponent<RectTransform>();
        if (slideTarget == null)
            slideTarget = cachedRect;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void PlayIntro()
    {
        if (slideRoutine != null)
            StopCoroutine(slideRoutine);

        slideRoutine = StartCoroutine(SlideInRoutine());
    }

    private IEnumerator SlideInRoutine()
    {
        // Wait for layout to settle so we can capture the final position
        yield return null;

        if (slideTarget == null)
            yield break;

        Vector2 endPos = slideTarget.anchoredPosition;
        Vector2 startPos = endPos + Vector2.left * Mathf.Max(0f, slideDistance);

        slideTarget.anchoredPosition = startPos;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        float time = 0f;
        float duration = Mathf.Max(0.01f, slideDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float eased = slideCurve != null ? slideCurve.Evaluate(t) : t;

            slideTarget.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        slideTarget.anchoredPosition = endPos;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}
