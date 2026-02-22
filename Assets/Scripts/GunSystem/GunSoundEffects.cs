using UnityEngine;

public class GunSoundEffects : MonoBehaviour
{
    [Header("Impact Sounds")]
    [SerializeField] AudioClip dirtImpact;
    [SerializeField] AudioClip fleshImpact;
    [SerializeField] AudioClip groundImpact;
    [SerializeField] AudioClip concreteImpact;
    [SerializeField] AudioClip metalImpact;

    [Header("Hit Feedback")]
    [SerializeField] AudioClip hitmarkerSound;

    [Header("Volume")]
    [SerializeField][Range(0, 1)] float impactVolume = 0.7f;
    [SerializeField][Range(0, 1)] float hitmarkerVolume = 0.8f;

    [Header("Pitch Variation")]
    [SerializeField] float impactPitchMin = 0.95f;
    [SerializeField] float impactPitchMax = 1.05f;
    [SerializeField] float hitmarkerPitchMin = 0.9f;
    [SerializeField] float hitmarkerPitchMax = 1.1f;

    [Header("3D Audio")]
    [SerializeField] float maxDistance = 50f;
    [SerializeField] float spatialBlend = 1f; // 0 = 2D, 1 = 3D

    private GunAssembler gunAssembler;

    private void Start()
    {
        gunAssembler = GetComponent<GunAssembler>();
        
        if (gunAssembler != null)
        {
            gunAssembler.HitConfirmed += OnHitConfirmed;
        }
        else
        {
            Debug.LogWarning("GunSoundEffects: GunAssembler not found on this object!");
        }
    }

    private void OnDestroy()
    {
        if (gunAssembler != null)
        {
            gunAssembler.HitConfirmed -= OnHitConfirmed;
        }
    }

    private void OnHitConfirmed(Collider hitCollider, RaycastHit hit)
    {
        PlayImpactSound(hitCollider, hit);
        PlayHitFeedback(hitCollider);
    }

    private void PlayImpactSound(Collider hitCollider, RaycastHit hit)
    {
        AudioClip impactClip = GetImpactClip(hitCollider.gameObject);
        if (impactClip != null)
        {
            PlaySoundAtPoint(impactClip, hit.point, impactVolume, impactPitchMin, impactPitchMax);
        }
    }

    private AudioClip GetImpactClip(GameObject hitObject)
    {
        // Check the object's tag
        string tag = hitObject.tag;

        return tag switch
        {
            "Dirt" => dirtImpact,
            "Flesh" => fleshImpact,
            "Ground" => groundImpact,
            "Concrete" => concreteImpact,
            "Metal" => metalImpact,
            _ => metalImpact // Default to metal
        };
    }

    private void PlayHitFeedback(Collider hitCollider)
    {
        // Play hitmarker if we hit an enemy
        Health health = hitCollider.GetComponent<Health>();
        if (health != null && hitmarkerSound != null)
        {
            PlaySoundAtPoint(hitmarkerSound, hitCollider.transform.position, hitmarkerVolume, hitmarkerPitchMin, hitmarkerPitchMax);
        }
    }

    private void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume, float pitchMin, float pitchMax)
    {
        GameObject tempAudio = new GameObject("ImpactSFX");
        tempAudio.transform.position = position;
        
        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.spatialBlend = spatialBlend;
        source.maxDistance = maxDistance;
        source.Play();
        
        Destroy(tempAudio, clip.length);
    }
}
