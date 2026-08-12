using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Default UI Clips")]
    [SerializeField] private AudioClip defaultClickSound;
    [SerializeField] private AudioClip defaultHoverSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayClickSound(AudioClip overrideClip = null)
    {
        AudioClip clipToPlay = overrideClip != null ? overrideClip : defaultClickSound;
        PlaySFX(clipToPlay);
    }

    public void PlayHoverSound(AudioClip overrideClip = null)
    {
        AudioClip clipToPlay = overrideClip != null ? overrideClip : defaultHoverSound;
        PlaySFX(clipToPlay);
    }
}