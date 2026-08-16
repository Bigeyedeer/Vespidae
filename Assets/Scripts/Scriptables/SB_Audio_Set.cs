using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The events the game can make a noise about. Adding one here is all that is needed to expose a new
/// slot on the audio set - nothing is hardcoded to a filename.
/// </summary>
public enum GameSound
{
    None,
    UiClick,
    UiOpenPanel,
    UiClosePanel,
    TrainingStarted,
    TrainingComplete,
    WaspDispatched,
    WaspRecalled,
    CombatStarted,
    CombatWon,
    CombatLost,
    HexScouted,
    HexCaptured,
    HexLost,
    HexClaimCountdown,
    FogRevealed,
    CodexUnlocked,
    MatchWon,
    MatchLost
}

/// <summary>
/// One sound, with the small amount of variation that stops a repeated cue turning into a rattle.
/// Several clips means one is chosen at random; the pitch jitter does the rest.
/// </summary>
[Serializable]
public class GameSoundEntry
{
    [SerializeField] private GameSound sound;
    [SerializeField] private List<AudioClip> clips = new List<AudioClip>();
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Random pitch spread either side of normal.")]
    private float pitchJitter = 0.06f;
    [SerializeField, Min(0f), Tooltip("Shortest gap before this sound may play again, so a burst of " +
                                      "events does not stack into noise.")]
    private float minimumInterval = 0.04f;

    public GameSound Sound => sound;
    public float Volume => volume;
    public float PitchJitter => pitchJitter;
    public float MinimumInterval => minimumInterval;
    public bool HasClips => clips != null && clips.Count > 0;

    public AudioClip PickClip()
    {
        if (!HasClips)
            return null;

        // Deliberately allows a repeat; a strict no-repeat rule reads as a pattern of its own.
        return clips[UnityEngine.Random.Range(0, clips.Count)];
    }
}

/// <summary>
/// The project's sound bank. Slots can sit empty - the director simply plays nothing - so this can be
/// wired into the game before any audio has been recorded.
/// </summary>
[CreateAssetMenu(fileName = "SO_AudioSet", menuName = "Vespidae Wars/Audio Set")]
public class SB_Audio_Set : ScriptableObject
{
    [SerializeField] private List<GameSoundEntry> entries = new List<GameSoundEntry>();

    [Header("Music")]
    [SerializeField, Tooltip("Looped under the match. Leave empty for silence.")]
    private AudioClip matchMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Ambience")]
    [SerializeField, Tooltip("Looped world bed, separate from music so the two can sit at different " +
                             "levels. Meant to stay well under everything else.")]
    private AudioClip ambientLoop;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.15f;

    public AudioClip MatchMusic => matchMusic;
    public float MusicVolume => musicVolume;
    public AudioClip AmbientLoop => ambientLoop;
    public float AmbientVolume => ambientVolume;
    public IReadOnlyList<GameSoundEntry> Entries => entries;

    public GameSoundEntry Find(GameSound sound)
    {
        for (int index = 0; index < entries.Count; index++)
        {
            if (entries[index] != null && entries[index].Sound == sound)
                return entries[index];
        }

        return null;
    }

#if UNITY_EDITOR
    /// <summary>Creates an empty slot for every sound the game can raise, ready for clips.</summary>
    public void PopulateEmptySlotsForEditor()
    {
        foreach (GameSound sound in Enum.GetValues(typeof(GameSound)))
        {
            if (sound == GameSound.None || Find(sound) != null)
                continue;

            GameSoundEntry entry = new GameSoundEntry();
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(this);
            entries.Add(entry);
            UnityEditor.SerializedProperty list = serialized.FindProperty("entries");
            serialized.Update();
            list.GetArrayElementAtIndex(entries.Count - 1).FindPropertyRelative("sound").enumValueIndex = (int)sound;
            serialized.ApplyModifiedProperties();
        }
    }
#endif
}
