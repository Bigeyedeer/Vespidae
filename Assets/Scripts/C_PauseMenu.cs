using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The pause screen, its controls list and its options, all living in the scene rather than being
/// built at runtime.
///
/// Being authored in the scene is the point: the pause menu is the one screen every player sees, and
/// it could not be laid out or previewed while it only existed during play. Everything here is wired
/// by name so the art can be rearranged without touching code.
///
/// Volume goes through <see cref="AudioDirector"/> rather than mixer parameters, because exposed
/// mixer parameters cannot be authored from script and would have to be set up by hand first.
/// </summary>
public class C_PauseMenu : MonoBehaviour
{
    private const string MasterKey = "vespidae.volume.master";
    private const string MusicKey = "vespidae.volume.music";
    private const string SfxKey = "vespidae.volume.sfx";
    private const string ZoomKey = "Vespidae.ScrollWheelZoomSpeed";

    [Header("Panels")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Controls")]
    [SerializeField] private TMP_Text keybindText;
    [SerializeField] private Button controlsBackButton;

    [Header("Options")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button optionsBackButton;

    [SerializeField, TextArea(8, 20)]
    private string keybinds =
        "Left Click  -  Select hex\n" +
        "Shift + Left Click  -  Add wasp to selection\n" +
        "Left Drag  -  Box select wasps\n" +
        "Right Click  -  Move selected wasps\n" +
        "Double Right Click  -  Clear selection\n" +
        "1 - 5  -  Select control group\n" +
        "Ctrl + 1 - 5  -  Assign control group\n" +
        "Middle Drag  -  Pan camera\n" +
        "Scroll Wheel  -  Zoom\n" +
        "H  -  Toggle map-only view\n" +
        "Esc  -  Pause / resume";

    [Header("Camera")]
    [SerializeField, Tooltip("Optional. Scroll wheel zoom speed, carried over from the old pause menu.")]
    private Slider zoomSpeedSlider;
    [SerializeField] private TMP_Text zoomSpeedValue;

    /// <summary>The authored pause screen, so the old runtime builder can stand aside for it.</summary>
    public static C_PauseMenu Instance { get; private set; }

    public bool IsPaused => pauseRoot != null && pauseRoot.activeSelf;

    private void OnEnable()
    {
        Instance = this;
    }

    private void Awake()
    {
        Instance = this;
        BindButtons();
        LoadVolumes();
        if (keybindText != null)
            keybindText.text = keybinds;
        Close();
    }

    private void BindButtons()
    {
        Bind(resumeButton, Close);
        Bind(controlsButton, ShowControls);
        Bind(optionsButton, ShowOptions);
        Bind(quitButton, QuitToMenu);
        Bind(controlsBackButton, ShowMenu);
        Bind(optionsBackButton, ShowMenu);

        BindSlider(masterSlider, value => { ApplyMaster(value); PlayerPrefs.SetFloat(MasterKey, value); });
        BindSlider(musicSlider, value => { ApplyMusic(value); PlayerPrefs.SetFloat(MusicKey, value); });
        BindSlider(sfxSlider, value => { ApplySfx(value); PlayerPrefs.SetFloat(SfxKey, value); });

        // Carried over from the old pause screen rather than dropped, since it was the one setting
        // that menu actually owned.
        if (zoomSpeedSlider != null)
        {
            zoomSpeedSlider.minValue = 0.005f;
            zoomSpeedSlider.maxValue = 0.08f;
            zoomSpeedSlider.onValueChanged.RemoveListener(ApplyZoomSpeed);
            zoomSpeedSlider.onValueChanged.AddListener(ApplyZoomSpeed);
            zoomSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(ZoomKey, 0.02f));
            ApplyZoomSpeed(zoomSpeedSlider.value);
        }
    }

    /// <summary>
    /// There are two audio systems in this project: AudioDirector drives the gameplay cues, and
    /// Herbert's AudioSettingsManager drives the button hover and click sounds through his own
    /// SoundManager. A single slider has to reach both, or turning SFX down silences the map while
    /// the buttons keep clicking at full volume.
    /// </summary>
    private static AudioSettingsManager HerbertAudio
    {
        get
        {
            if (herbertAudio == null)
                herbertAudio = FindFirstObjectByType<AudioSettingsManager>();
            return herbertAudio;
        }
    }

    private static AudioSettingsManager herbertAudio;

    private static void ApplyMaster(float value)
    {
        AudioDirector.Instance?.SetMasterVolume(value);
        if (HerbertAudio != null)
            HerbertAudio.SetMasterVolume(value);
    }

    private static void ApplyMusic(float value)
    {
        AudioDirector.Instance?.SetMusicVolume(value);
        if (HerbertAudio != null)
            HerbertAudio.SetMusicVolume(value);
    }

    private static void ApplySfx(float value)
    {
        AudioDirector.Instance?.SetSfxVolume(value);
        if (HerbertAudio != null)
            HerbertAudio.SetSFXVolume(value);
    }

    private void ApplyZoomSpeed(float speed)
    {
        C_MainWorldOverlayNavigation nav = C_MainWorldOverlayNavigation.Instance;
        if (nav != null)
            nav.SetScrollWheelZoomSpeed(speed);
        else
            PlayerPrefs.SetFloat(ZoomKey, speed);

        if (zoomSpeedValue != null)
            zoomSpeedValue.text = speed.ToString("0.000");
    }

    /// <summary>
    /// Binds the action to this button and every Button nested inside it.
    ///
    /// Herbert's button prefabs carry their own Button on the skin child, and that child sits on top
    /// in the raycast order - so a listener on the outer object alone never fires. Binding the whole
    /// chain means the press works wherever it actually lands.
    /// </summary>
    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        foreach (Button nested in button.GetComponentsInChildren<Button>(true))
        {
            nested.onClick.RemoveListener(action);
            nested.onClick.AddListener(action);
        }
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.RemoveListener(action);
        slider.onValueChanged.AddListener(action);
    }

    /// <summary>Restores saved levels and pushes them to the director, so settings survive a restart.</summary>
    private void LoadVolumes()
    {
        float master = PlayerPrefs.GetFloat(MasterKey, 1f);
        float music = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfx = PlayerPrefs.GetFloat(SfxKey, 1f);

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

        ApplyMaster(master);
        ApplyMusic(music);
        ApplySfx(sfx);
    }

    public void Toggle()
    {
        if (IsPaused)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (pauseRoot == null)
            return;

        pauseRoot.SetActive(true);
        ShowMenu();
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        // Always restore time, even if the panels were never wired - a pause screen that cannot be
        // closed would otherwise leave the game frozen.
        Time.timeScale = 1f;
    }

    public void ShowMenu()
    {
        SetPanel(menuPanel, true);
        SetPanel(controlsPanel, false);
        SetPanel(optionsPanel, false);
    }

    public void ShowControls()
    {
        SetPanel(menuPanel, false);
        SetPanel(controlsPanel, true);
        SetPanel(optionsPanel, false);
    }

    public void ShowOptions()
    {
        // Reflect the live values in case anything changed them since the panel was last open.
        AudioDirector director = AudioDirector.Instance;
        if (director != null)
        {
            if (masterSlider != null) masterSlider.SetValueWithoutNotify(director.MasterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(director.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(director.SfxVolume);
        }

        SetPanel(menuPanel, false);
        SetPanel(controlsPanel, false);
        SetPanel(optionsPanel, true);
    }

    private static void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
            panel.SetActive(visible);
    }

    public void QuitToMenu()
    {
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        // Leaving the scene while paused must not carry a frozen clock into the next one.
        Time.timeScale = 1f;
        PlayerPrefs.Save();
    }
}
