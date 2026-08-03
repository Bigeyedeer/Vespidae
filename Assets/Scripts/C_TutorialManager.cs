using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class C_TutorialManager : MonoBehaviour
{
    private const string TutorialCompletedKey =
        "VespidaeWars.TutorialCompleted";

    [Header("Tutorial Settings")]
    [SerializeField] private bool tutorialEnabled = true;
    [SerializeField] private bool allowTutorialSkip = true;
    [SerializeField] private bool rememberCompletion = true;

    [Header("Tutorial Steps")]
    [SerializeField] private TutorialStep[] steps;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipButton;

    [Header("Debug")]
    [SerializeField] private bool alwaysShowTutorialInEditor = true;

    private int currentStepIndex = -1;
    private bool tutorialActive;

    public bool TutorialActive => tutorialActive;
    public int CurrentStepIndex => currentStepIndex;

    private void Awake()
    {
        ConfigureTutorialPortraitRendering();
        BindButtons();
    }

    private void ConfigureTutorialPortraitRendering()
    {
        int tutorialLayer = LayerMask.NameToLayer("TutorialPortrait");

        if (tutorialLayer < 0)
        {
            Debug.LogWarning("TutorialPortrait layer is missing.");
            return;
        }

        int tutorialMask = 1 << tutorialLayer;
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.name == "Tutorial Pip Display")
            {
                SetLayerRecursively(sceneTransform, tutorialLayer);
                break;
            }
        }

        Camera[] sceneCameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Camera sceneCamera in sceneCameras)
        {
            if (sceneCamera.name == "Tutorial Pip Camera")
                sceneCamera.cullingMask = tutorialMask;
            else
                sceneCamera.cullingMask &= ~tutorialMask;
        }
    }

    private static void SetLayerRecursively(
        Transform root,
        int layer
    )
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }

    private void Start()
    {
        if (ShouldStartTutorial())
        {
            StartTutorial();
        }
        else
        {
            DisableTutorial();
        }
    }

    private bool ShouldStartTutorial()
    {
        if (!tutorialEnabled)
            return false;

#if UNITY_EDITOR
        if (alwaysShowTutorialInEditor)
            return true;
#endif

        if (!rememberCompletion)
            return true;

        return PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 0;
    }

    public void StartTutorial()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning(
                "Tutorial cannot start because no steps are assigned."
            );

            DisableTutorial();
            return;
        }

        tutorialActive = true;
        currentStepIndex = 0;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        ShowCurrentStep();
    }

    public void ContinueTutorial()
    {
        if (!tutorialActive)
            return;

        TutorialStep currentStep = GetCurrentStep();

        if (currentStep != null &&
            !currentStep.requiresManualContinue)
        {
            return;
        }

        AdvanceToNextStep();
    }

    public void AdvanceToNextStep()
    {
        if (!tutorialActive)
            return;

        currentStepIndex++;

        if (currentStepIndex >= steps.Length)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
    }

    public void SkipTutorial()
    {
        if (!tutorialActive || !allowTutorialSkip)
            return;

        CompleteTutorial();
    }

    public void CompleteTutorial()
    {
        tutorialActive = false;

        if (rememberCompletion)
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        Debug.Log("Tutorial completed.");
    }

    public void DisableTutorial()
    {
        tutorialActive = false;
        currentStepIndex = -1;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    public void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(TutorialCompletedKey);
        PlayerPrefs.Save();

        Debug.Log("Tutorial completion progress was reset.");
    }

    private void ShowCurrentStep()
    {
        TutorialStep step = GetCurrentStep();

        if (step == null)
            return;

        if (titleText != null)
            titleText.text = step.title;

        if (descriptionText != null)
            descriptionText.text = step.description;

        if (continueButton != null)
        {
            bool showContinue =
                step.showContinueButton &&
                step.requiresManualContinue;

            continueButton.gameObject.SetActive(showContinue);
            continueButton.interactable = showContinue;
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(
                allowTutorialSkip
            );
        }

        Debug.Log(
            $"Tutorial step {currentStepIndex + 1}: {step.title}"
        );
    }

    private TutorialStep GetCurrentStep()
    {
        if (steps == null ||
            currentStepIndex < 0 ||
            currentStepIndex >= steps.Length)
        {
            return null;
        }

        return steps[currentStepIndex];
    }

    private void BindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                ContinueTutorial
            );

            continueButton.onClick.AddListener(
                ContinueTutorial
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                SkipTutorial
            );

            skipButton.onClick.AddListener(
                SkipTutorial
            );
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                ContinueTutorial
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                SkipTutorial
            );
        }
    }
}
