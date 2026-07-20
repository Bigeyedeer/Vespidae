using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class C_MainMenu_Ctrl : MonoBehaviour
{
    private const string MasterVolumeKey = "VespidaeWars.MasterVolume";
    private const string FullscreenKey = "VespidaeWars.Fullscreen";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject waspSelectionPanel;

    [Header("Default Buttons")]
    [SerializeField] private Button mainMenuDefaultButton;
    [SerializeField] private Button optionsDefaultButton;
    [SerializeField] private Button selectionDefaultButton;

    [Header("Options")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Shared State")]
    [SerializeField] private SB_PlayerSelection_State selectionState;

    private void Awake()
    {
        selectionState?.ResetSelection();
        LoadOptions();
        ShowOnly(mainMenuPanel);
    }

    private void Start()
    {
        SelectButton(mainMenuDefaultButton);
    }

    public void OpenWaspSelection()
    {
        ShowOnly(waspSelectionPanel);
        SelectButton(selectionDefaultButton);
    }

    public void OpenOptions()
    {
        ShowOnly(optionsPanel);
        SelectButton(optionsDefaultButton);
    }

    public void ReturnToMainMenu()
    {
        ShowOnly(mainMenuPanel);
        SelectButton(mainMenuDefaultButton);
    }

    public void SetMasterVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        AudioListener.volume = clampedVolume;
        PlayerPrefs.SetFloat(MasterVolumeKey, clampedVolume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadOptions()
    {
        float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        AudioListener.volume = savedVolume;
        Screen.fullScreen = savedFullscreen;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(savedVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        }
    }

    private void ShowOnly(GameObject activePanel)
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(activePanel == optionsPanel);
        }

        if (waspSelectionPanel != null)
        {
            waspSelectionPanel.SetActive(activePanel == waspSelectionPanel);
        }
    }

    private static void SelectButton(Button button)
    {
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
