using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Jump & Land SFX")]
    [SerializeField] AudioClip jumpClip;
    [SerializeField] AudioClip landClip;
    [SerializeField] AudioClip slideClip;

    [Header("Footsteps")]
    [SerializeField] AudioClip[] carpetFootsteps;
    [SerializeField] AudioClip[] dirtFootsteps;
    [SerializeField] AudioClip[] floorFootsteps;
    [SerializeField] AudioClip[] gravelFootsteps;
    [SerializeField] AudioClip[] snowFootsteps;
    [SerializeField] AudioClip[] tilesFootsteps;
    [SerializeField] AudioClip[] waterFootsteps;
    [SerializeField] AudioClip[] woodFootsteps;

    [Header("Volume")]
    [SerializeField][Range(0, 1)] float jumpVolume = 0.7f;
    [SerializeField][Range(0, 1)] float landVolume = 0.6f;
    [SerializeField][Range(0, 1)] float slideVolume = 0.8f;
    [SerializeField][Range(0, 1)] float footstepVolume = 0.5f;

    [Header("Pitch Variation")]
    [SerializeField] float jumpPitchMin = 0.9f;
    [SerializeField] float jumpPitchMax = 1.1f;
    [SerializeField] float landPitchMin = 0.85f;
    [SerializeField] float landPitchMax = 1.15f;
    [SerializeField] float slidePitchMin = 0.95f;
    [SerializeField] float slidePitchMax = 1.05f;
    [SerializeField] float footstepPitchMin = 0.9f;
    [SerializeField] float footstepPitchMax = 1.1f;

    [Header("3D Audio")]
    [SerializeField] float maxDistance = 50f;
    [SerializeField] float spatialBlend = 1f; // 0 = 2D, 1 = 3D

    public static AudioManager instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayJumpSound(Vector3 position)
    {
        if (jumpClip != null)
            PlaySoundAtPoint(jumpClip, position, jumpVolume, jumpPitchMin, jumpPitchMax);
    }

    public void PlayLandSound(Vector3 position)
    {
        if (landClip != null)
            PlaySoundAtPoint(landClip, position, landVolume, landPitchMin, landPitchMax);
    }

    public void PlaySlideSound(Vector3 position)
    {
        if (slideClip != null)
            PlaySoundAtPoint(slideClip, position, slideVolume, slidePitchMin, slidePitchMax);
    }

    public void PlayFootstep(Vector3 position, string surfaceTag)
    {
        AudioClip[] footstepArray = GetFootstepArray(surfaceTag);
        if (footstepArray != null && footstepArray.Length > 0)
        {
            AudioClip randomClip = footstepArray[Random.Range(0, footstepArray.Length)];
            PlaySoundAtPoint(randomClip, position, footstepVolume, footstepPitchMin, footstepPitchMax);
        }
    }

    AudioClip[] GetFootstepArray(string surfaceTag)
    {
        return surfaceTag switch
        {
            "Carpet" => carpetFootsteps,
            "Dirt" => dirtFootsteps,
            "Gravel" => gravelFootsteps,
            "Snow" => snowFootsteps,
            "Tiles" => tilesFootsteps,
            "Water" => waterFootsteps,
            "Wood" => woodFootsteps,
            _ => floorFootsteps // Default
        };
    }

    void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume, float pitchMin, float pitchMax)
    {
        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = position;
        //TODO; pooling, add the uhhh AudioSource component to an existing object instead of creating new one each time!!!!
        
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
