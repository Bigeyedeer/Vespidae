using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// One place the whole game asks for sound.
///
/// Callers say what happened - <c>AudioDirector.Play(GameSound.HexCaptured)</c> - not which file to
/// play. That keeps gameplay code free of audio detail and means the sound bank can be swapped or
/// left empty without touching anything else. Every slot may be empty: with no clip assigned the call
/// is a no-op, so this can be wired in before any audio exists.
///
/// Playback uses a small pool of sources rather than one, so overlapping cues do not cut each other
/// off, and a per-sound cooldown stops a burst of events stacking into a rattle.
/// </summary>
[DefaultExecutionOrder(-200)]
public class AudioDirector : MonoBehaviour
{
    public static AudioDirector Instance { get; private set; }

    [Header("Bank")]
    [SerializeField, Tooltip("The clips this game can play. Slots may be left empty.")]
    private SB_Audio_Set audioSet;

    [Header("Mixer")]
    [SerializeField, Tooltip("Optional. Routes playback so volume can be balanced per group.")]
    private AudioMixerGroup effectsGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Playback")]
    [SerializeField, Range(1, 16), Tooltip("How many sounds can overlap before the oldest is reused.")]
    private int voiceCount = 8;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Tooltip("Play the match music on start, if the set has any.")]
    private bool playMusicOnStart = true;

    private readonly List<AudioSource> voices = new List<AudioSource>();
    private readonly Dictionary<GameSound, float> lastPlayed = new Dictionary<GameSound, float>();
    private AudioSource musicSource;
    private AudioSource ambientSource;
    private int nextVoice;

    public bool HasBank => audioSet != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildVoices();
    }

    private void Start()
    {
        if (playMusicOnStart)
        {
            PlayMusic();
            PlayAmbience();
        }
    }

    public void PlayAmbience()
    {
        if (audioSet == null || ambientSource == null || audioSet.AmbientLoop == null)
            return;

        ambientSource.clip = audioSet.AmbientLoop;
        ambientSource.volume = audioSet.AmbientVolume * masterVolume;
        ambientSource.Play();
    }

    public void StopAmbience()
    {
        if (ambientSource != null)
            ambientSource.Stop();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildVoices()
    {
        for (int index = 0; index < voiceCount; index++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;      // UI and feedback cues are not positional
            source.outputAudioMixerGroup = effectsGroup;
            voices.Add(source);
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.outputAudioMixerGroup = musicGroup;

        // Ambience gets its own source so the world bed and the music can sit at different levels
        // and be stopped independently.
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.outputAudioMixerGroup = effectsGroup;
    }

    /// <summary>Plays the cue for an event. Does nothing if no clip is assigned to it.</summary>
    public static void Play(GameSound sound)
    {
        if (Instance != null)
            Instance.PlaySound(sound);
    }

    public void PlaySound(GameSound sound)
    {
        if (sound == GameSound.None || audioSet == null || voices.Count == 0)
            return;

        GameSoundEntry entry = audioSet.Find(sound);
        if (entry == null || !entry.HasClips)
            return;

        // Cooldown, so a frame that raises the same event several times still reads as one sound.
        if (lastPlayed.TryGetValue(sound, out float last) && Time.unscaledTime - last < entry.MinimumInterval)
            return;
        lastPlayed[sound] = Time.unscaledTime;

        AudioClip clip = entry.PickClip();
        if (clip == null)
            return;

        AudioSource source = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Count;

        source.outputAudioMixerGroup = IsUiSound(sound) ? (uiGroup != null ? uiGroup : effectsGroup) : effectsGroup;
        source.clip = clip;
        source.volume = entry.Volume * masterVolume;
        source.pitch = 1f + Random.Range(-entry.PitchJitter, entry.PitchJitter);
        source.Play();
    }

    private static bool IsUiSound(GameSound sound)
    {
        return sound == GameSound.UiClick || sound == GameSound.UiOpenPanel || sound == GameSound.UiClosePanel;
    }

    public void PlayMusic()
    {
        if (audioSet == null || musicSource == null || audioSet.MatchMusic == null)
            return;

        musicSource.clip = audioSet.MatchMusic;
        musicSource.volume = audioSet.MusicVolume * masterVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        if (musicSource != null && audioSet != null)
            musicSource.volume = audioSet.MusicVolume * masterVolume;
    }

    [ContextMenu("Report which sounds have clips")]
    private void DebugReportBank()
    {
        if (audioSet == null)
        {
            Debug.LogWarning("No audio set assigned.", this);
            return;
        }

        int filled = 0, total = 0;
        foreach (GameSound sound in System.Enum.GetValues(typeof(GameSound)))
        {
            if (sound == GameSound.None)
                continue;

            total++;
            GameSoundEntry entry = audioSet.Find(sound);
            if (entry != null && entry.HasClips)
                filled++;
        }

        Debug.Log($"Audio bank: {filled} of {total} sounds have clips.", this);
    }
}
