using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    [Header("Tutorial Arrow")]
    [SerializeField] private GameObject tutorialArrow;
    [SerializeField] private RectTransform tutorialArrowRect;
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private Camera worldCamera;

    [Header("Arrow Animation")]
    [SerializeField] private float arrowFloatDistance = 10f;
    [SerializeField] private float arrowFloatSpeed = 3f;

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

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (tutorialCanvas == null &&
            tutorialPanel != null)
        {
            tutorialCanvas =
                tutorialPanel.GetComponentInParent<Canvas>();
        }

        if (tutorialArrowRect == null &&
            tutorialArrow != null)
        {
            tutorialArrowRect =
                tutorialArrow.GetComponent<RectTransform>();
        }
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

    private void Update()
    {
        UpdateArrowTracking();

        if (!tutorialActive)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        TutorialStep currentStep =
            GetCurrentStep();

        if (currentStep == null)
            return;

        if (!currentStep.requiresManualContinue)
            return;

        // Clicking UI should not skip the tutorial bubble.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        AdvanceToNextStep();
    }

    private void ConfigureTutorialPortraitRendering()
    {
        int tutorialLayer =
            LayerMask.NameToLayer("TutorialPortrait");

        if (tutorialLayer < 0)
        {
            Debug.LogWarning(
                "TutorialPortrait layer is missing."
            );

            return;
        }

        int tutorialMask =
            1 << tutorialLayer;

        Transform[] sceneTransforms =
            FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.name ==
                "Tutorial Pip Display")
            {
                SetLayerRecursively(
                    sceneTransform,
                    tutorialLayer
                );

                break;
            }
        }

        Camera[] sceneCameras =
            FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Camera sceneCamera in sceneCameras)
        {
            if (sceneCamera.name ==
                "Tutorial Pip Camera")
            {
                sceneCamera.cullingMask =
                    tutorialMask;
            }
            else
            {
                sceneCamera.cullingMask &=
                    ~tutorialMask;
            }
        }
    }

    private static void SetLayerRecursively(
        Transform root,
        int layer
    )
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursively(
                child,
                layer
            );
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

        return PlayerPrefs.GetInt(
            TutorialCompletedKey,
            0
        ) == 0;
    }

    public void StartTutorial()
    {
        if (steps == null ||
            steps.Length == 0)
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

        TutorialStep currentStep =
            GetCurrentStep();

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
        if (!tutorialActive ||
            !allowTutorialSkip)
        {
            return;
        }

        CompleteTutorial();
    }

    public void CompleteTutorial()
    {
        tutorialActive = false;

        if (rememberCompletion)
        {
            PlayerPrefs.SetInt(
                TutorialCompletedKey,
                1
            );

            PlayerPrefs.Save();
        }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);

        Debug.Log(
            "Tutorial completed."
        );
    }

    public void DisableTutorial()
    {
        tutorialActive = false;
        currentStepIndex = -1;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);
    }

    public void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(
            TutorialCompletedKey
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Tutorial completion progress was reset."
        );
    }

    private void ShowCurrentStep()
    {
        TutorialStep step =
            GetCurrentStep();

        if (step == null)
            return;

        if (titleText != null)
            titleText.text =
                step.title;

        if (descriptionText != null)
            descriptionText.text =
                step.description;

        UpdateTutorialArrow(step);

        if (continueButton != null)
        {
            bool showContinue =
                step.showContinueButton &&
                step.requiresManualContinue;

            continueButton.gameObject.SetActive(
                showContinue
            );

            continueButton.interactable =
                showContinue;
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

    private void UpdateTutorialArrow(
        TutorialStep step
    )
    {
        if (tutorialArrow == null ||
            step == null)
        {
            return;
        }

        if (!step.showArrow)
        {
            tutorialArrow.SetActive(false);
            return;
        }

        if (tutorialArrowRect == null)
        {
            tutorialArrowRect =
                tutorialArrow.GetComponent<RectTransform>();
        }

        if (tutorialArrowRect == null)
            return;

        tutorialArrowRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                step.arrowRotation
            );

        tutorialArrow.SetActive(true);
    }

    private void UpdateArrowTracking()
    {
        if (!tutorialActive ||
            tutorialArrow == null ||
            tutorialArrowRect == null)
        {
            return;
        }

        TutorialStep step = GetCurrentStep();

        if (step == null ||
            !step.showArrow)
        {
            if (tutorialArrow.activeSelf)
                tutorialArrow.SetActive(false);

            return;
        }

        if (step.arrowWorldTarget == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;
        
        Vector3 screenPosition =
            worldCamera.WorldToScreenPoint(
                step.arrowWorldTarget.position
            );
        
        if (screenPosition.z <= 0f)
        {
            tutorialArrow.SetActive(false);
            return;
        }

        if (!tutorialArrow.activeSelf)
            tutorialArrow.SetActive(true);
        
        RectTransform arrowParent =
            tutorialArrowRect.parent as RectTransform;

        if (arrowParent == null)
        {
            Debug.LogWarning(
                "Tutorial Arrow needs a RectTransform parent."
            );

            return;
        }

        Camera uiCamera = null;

        if (tutorialCanvas != null &&
            tutorialCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = tutorialCanvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arrowParent,
                screenPosition,
                uiCamera,
                out Vector2 localPosition))
        {
            float floatOffset =
                Mathf.Sin(
                    Time.unscaledTime * arrowFloatSpeed
                ) * arrowFloatDistance;

            tutorialArrowRect.anchoredPosition =
                localPosition +
                step.arrowOffset +
                Vector2.up * floatOffset;
        }
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