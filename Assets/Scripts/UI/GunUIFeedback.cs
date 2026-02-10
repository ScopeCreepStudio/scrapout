using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunUIFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunAssembler gun;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private GameObject hitmarker;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayer;

    [Header("Hitmarker")]
    [SerializeField] private float hitmarkerDuration = 0.05f;
    [SerializeField] private float hitmarkerFadeDuration = 0.15f;

    private CanvasGroup hitmarkerGroup;

    private Coroutine hitmarkerRoutine;

    private void Awake()
    {
        if (gun == null)
            gun = FindObjectOfType<GunAssembler>();

        if (hitmarker != null)
        {
            hitmarkerGroup = hitmarker.GetComponent<CanvasGroup>();
            if (hitmarkerGroup == null)
                hitmarkerGroup = hitmarker.AddComponent<CanvasGroup>();

            hitmarkerGroup.alpha = 0f;
            hitmarker.SetActive(false);
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

    private void HandleAmmoChanged(int current, int max)
    {
        if (ammoText == null) return;
        ammoText.text = current + " / " + max;
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
