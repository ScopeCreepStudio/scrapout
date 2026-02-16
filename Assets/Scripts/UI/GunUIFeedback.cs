using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunUIFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunAssembler gun;
    [SerializeField] private PlayerControllerV2 player;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private Transform speedometerNeedle;
    [SerializeField] private Graphic[] speedometerGraphics;
    [SerializeField] private RectTransform[] speedometerShakeTargets;
    [SerializeField] private GameObject hitmarker;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayer;

    [Header("Speedometer")]
    [SerializeField] private float speedometerMaxSpeed = 12f;
    [SerializeField] private float speedometerMinAngle = 180f;
    [SerializeField] private float speedometerMaxAngle = -80f;
    [SerializeField] private float speedometerLerpSpeed = 12f;
    [SerializeField] private Color speedometerSlowColor = Color.white;
    [SerializeField] private Color speedometerFastColor = Color.red;
    [SerializeField] private float speedometerFastThreshold = 0.85f;
    [SerializeField] private float speedometerShakeAmplitude = 6f;
    [SerializeField] private float speedometerShakeFrequency = 20f;

    [Header("Hitmarker")]
    [SerializeField] private float hitmarkerDuration = 0.05f;
    [SerializeField] private float hitmarkerFadeDuration = 0.15f;

    private CanvasGroup hitmarkerGroup;

    private Coroutine hitmarkerRoutine;
    private float currentNeedleAngle;
    private Vector3[] speedometerBaseLocalPos;

    private void Awake()
    {
        if (gun == null)
            gun = FindObjectOfType<GunAssembler>();

        if (player == null)
            player = FindObjectOfType<PlayerControllerV2>();

        if (hitmarker != null)
        {
            hitmarkerGroup = hitmarker.GetComponent<CanvasGroup>();
            if (hitmarkerGroup == null)
                hitmarkerGroup = hitmarker.AddComponent<CanvasGroup>();

            hitmarkerGroup.alpha = 0f;
            hitmarker.SetActive(false);
        }

        if (speedometerNeedle != null)
            currentNeedleAngle = speedometerNeedle.localEulerAngles.z;

        if (speedometerShakeTargets != null && speedometerShakeTargets.Length > 0)
        {
            speedometerBaseLocalPos = new Vector3[speedometerShakeTargets.Length];
            for (int i = 0; i < speedometerShakeTargets.Length; i++)
            {
                RectTransform target = speedometerShakeTargets[i];
                speedometerBaseLocalPos[i] = target != null ? target.localPosition : Vector3.zero;
            }
        }
    }

    private void OnEnable()
    {
        if (gun != null)
        {
            gun.AmmoChanged += HandleAmmoChanged;
            gun.HitConfirmed += HandleHitConfirmed;
        }

        if (gun != null)
            HandleAmmoChanged(gun.CurrentAmmo, gun.MaxAmmo);
    }

    private void OnDisable()
    {
        if (gun != null)
        {
            gun.AmmoChanged -= HandleAmmoChanged;
            gun.HitConfirmed -= HandleHitConfirmed;
        }
    }

    private void Update()
    {
        UpdateSpeedUI();
    }

    private void HandleAmmoChanged(int current, int max)
    {
        if (ammoText == null) return;
        ammoText.text = current + " / " + max;
    }

    private void UpdateSpeedUI()
    {
        if (player == null) return;

        float speed = player.MovementSpeed;
        if (speedText != null)
            speedText.text = speed.ToString("0.0");

        if (speedometerNeedle == null) return;

        float t = Mathf.InverseLerp(0f, Mathf.Max(0.01f, speedometerMaxSpeed), speed);
        float targetAngle = Mathf.Lerp(speedometerMinAngle, speedometerMaxAngle, t);
        currentNeedleAngle = Mathf.LerpAngle(currentNeedleAngle, targetAngle, Time.deltaTime * speedometerLerpSpeed);

        Vector3 angles = speedometerNeedle.localEulerAngles;
        angles.z = currentNeedleAngle;
        speedometerNeedle.localEulerAngles = angles;

        UpdateSpeedometerColor(t);
        UpdateSpeedometerShake(t);
    }

    private void UpdateSpeedometerColor(float normalizedSpeed)
    {
        if (speedometerGraphics == null || speedometerGraphics.Length == 0) return;

        Color target = Color.Lerp(speedometerSlowColor, speedometerFastColor, normalizedSpeed);
        for (int i = 0; i < speedometerGraphics.Length; i++)
        {
            if (speedometerGraphics[i] == null) continue;
            speedometerGraphics[i].color = target;
        }
    }

    private void UpdateSpeedometerShake(float normalizedSpeed)
    {
        if (speedometerShakeTargets == null || speedometerShakeTargets.Length == 0 || speedometerBaseLocalPos == null)
            return;

        float intensity = normalizedSpeed < speedometerFastThreshold
            ? 0f
            : Mathf.InverseLerp(speedometerFastThreshold, 1f, normalizedSpeed);

        float shake = speedometerShakeAmplitude * intensity;
        float time = Time.time * speedometerShakeFrequency;

        for (int i = 0; i < speedometerShakeTargets.Length; i++)
        {
            RectTransform target = speedometerShakeTargets[i];
            if (target == null) continue;

            Vector3 basePos = speedometerBaseLocalPos[i];
            if (intensity <= 0f)
            {
                target.localPosition = basePos;
                continue;
            }

            float phase = i * 0.37f;
            Vector3 offset = new Vector3(
                Mathf.Sin(time + phase),
                Mathf.Cos(time * 1.37f + phase),
                0f
            ) * shake;

            target.localPosition = basePos + offset;
        }
    }

    private void HandleHitConfirmed(Collider hitCollider)
    {
        if (hitmarker == null || hitCollider == null) return;

        if (!IsEnemyHit(hitCollider))
            return;

        if (hitmarkerRoutine != null)
            StopCoroutine(hitmarkerRoutine);

        hitmarkerRoutine = StartCoroutine(HitmarkerRoutine());
    }

    private IEnumerator HitmarkerRoutine()
    {
        hitmarker.SetActive(true);
        if (hitmarkerGroup != null)
            hitmarkerGroup.alpha = 1f;

        yield return new WaitForSeconds(hitmarkerDuration);

        if (hitmarkerGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < hitmarkerFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, hitmarkerFadeDuration));
                hitmarkerGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            hitmarkerGroup.alpha = 0f;
        }

        hitmarker.SetActive(false);
        hitmarkerRoutine = null;
    }

    private bool IsEnemyHit(Collider hitCollider)
    {
        bool tagMatch = !string.IsNullOrEmpty(enemyTag) && hitCollider.CompareTag(enemyTag);
        bool layerMatch = enemyLayer != 0 && ((enemyLayer.value & (1 << hitCollider.gameObject.layer)) != 0);
        return tagMatch || layerMatch;
    }
}
