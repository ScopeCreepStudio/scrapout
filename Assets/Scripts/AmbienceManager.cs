using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbiencePlayer : MonoBehaviour
{
    [Header("Ambience Settings")]
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = false;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();

        source.clip = ambienceClip;
        source.loop = true;
        source.spatialBlend = 0f; // 2D sound
        source.volume = volume;
        source.playOnAwake = false;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (playOnStart && ambienceClip != null)
        {
            source.Play();
        }
    }
}
