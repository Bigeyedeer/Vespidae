using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks")]
    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private int currentTrackIndex = -1;
    private Coroutine crossfadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create two AudioSource components for crossfading
        activeSource = gameObject.AddComponent<AudioSource>();
        inactiveSource = gameObject.AddComponent<AudioSource>();

        activeSource.loop = false;
        inactiveSource.loop = false;
    }

    private void Start()
    {
        if (playOnStart && playlist.Length > 0)
        {
            PlayNextRandomTrack();
        }
    }

    private void Update()
    {
        // Automatically crossfade to the next track when the current one nears completion
        if (activeSource.clip != null && !activeSource.isPlaying && playlist.Length > 0 && crossfadeCoroutine == null)
        {
            PlayNextRandomTrack();
        }
    }

    public void PlayNextRandomTrack()
    {
        if (playlist.Length == 0) return;

        int nextIndex = currentTrackIndex;
        if (playlist.Length > 1)
        {
            while (nextIndex == currentTrackIndex)
            {
                nextIndex = Random.Range(0, playlist.Length);
            }
        }
        else
        {
            nextIndex = 0;
        }

        currentTrackIndex = nextIndex;
        CrossfadeTo(playlist[currentTrackIndex]);
    }

    public void CrossfadeTo(AudioClip newClip)
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        // Swap active and inactive sources
        AudioSource oldSource = activeSource;
        activeSource = inactiveSource;
        inactiveSource = oldSource;

        activeSource.clip = newClip;
        activeSource.volume = 0f;
        activeSource.Play();

        float timer = 0f;
        float startVolume = inactiveSource.volume;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / fadeDuration;

            activeSource.volume = Mathf.Lerp(0f, 1f, progress);
            inactiveSource.volume = Mathf.Lerp(startVolume, 0f, progress);

            yield return null;
        }

        activeSource.volume = 1f;
        inactiveSource.volume = 0f;
        inactiveSource.Stop();

        crossfadeCoroutine = null;
    }
}