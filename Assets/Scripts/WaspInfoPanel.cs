using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WaspInfoPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private Graphic commonNameText;
    [SerializeField] private Graphic scientificNameText;
    [SerializeField] private Graphic statusText;
    [SerializeField] private Graphic descriptionText;
    [SerializeField] private Graphic ecologicalRoleText;
    [SerializeField] private Graphic aggressionText;
    [SerializeField] private Graphic classificationText;
    [SerializeField] private Graphic confidenceText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image confidenceBar;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button closeButton;

    private WaspInfo selectedWasp;

    private void Awake()
    {
        BindCloseButtons();
    }

    private void OnEnable()
    {
        BindCloseButtons();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (IsPointerOverButton(returnButton, mousePosition) ||
            IsPointerOverButton(closeButton, mousePosition))
        {
            Close();
        }
    }

    public void Open(WaspInfo wasp)
    {
        if (wasp == null)
            return;

        selectedWasp = wasp;

        gameObject.SetActive(true);

        SetText(commonNameText, wasp.CommonName);
        SetText(scientificNameText, wasp.ScientificName);

        string classification = wasp.IsNative
            ? "Native Species"
            : "Invasive Species";

        SetText(statusText, classification);
        SetText(descriptionText, wasp.Description);
        SetText(ecologicalRoleText, wasp.EcologicalRole);
        SetText(aggressionText, $"Ecological role       {wasp.EcologicalRole}");
        SetText(classificationText, wasp.IsNative ? "Native" : "Invasive");
        SetText(confidenceText, "ID confidence                                      100%");

        if (portraitImage != null)
        {
            Sprite portrait = wasp.SpeciesInfo != null ? wasp.SpeciesInfo.Portrait : null;
            if (portrait != null)
                portraitImage.sprite = portrait;

            portraitImage.enabled = true;
        }

        if (confidenceBar != null)
        {
            confidenceBar.fillAmount = 1f;
        }
    }

    public void Close()
    {
        selectedWasp = null;
        gameObject.SetActive(false);
    }

    private void BindCloseButtons()
    {
        if (returnButton == null)
            returnButton = FindButton("WaspInfo_Return");

        if (closeButton == null)
            closeButton = FindButton("WaspInfo_Close");

        BindCloseButton(returnButton);
        BindCloseButton(closeButton);
    }

    private void BindCloseButton(Button button)
    {
        if (button == null)
            return;

        button.interactable = true;
        EnsureButtonHitArea(button);
        button.onClick.RemoveListener(Close);
        button.onClick.AddListener(Close);
    }

    private void EnsureButtonHitArea(Button button)
    {
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.enabled = true;
            buttonImage.raycastTarget = true;
            buttonImage.canvasRenderer.cullTransparentMesh = false;
            Color color = buttonImage.color;
            color.a = 0.01f;
            buttonImage.color = color;
        }

        Transform existing = button.transform.Find("InputHitArea");
        GameObject hitArea = existing != null ? existing.gameObject : new GameObject("InputHitArea", typeof(RectTransform), typeof(Image));
        hitArea.transform.SetParent(button.transform, false);
        hitArea.transform.SetAsLastSibling();

        RectTransform hitRect = hitArea.GetComponent<RectTransform>();
        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.offsetMin = Vector2.zero;
        hitRect.offsetMax = Vector2.zero;
        hitRect.localScale = Vector3.one;

        Image hitImage = hitArea.GetComponent<Image>();
        hitImage.raycastTarget = true;
        hitImage.canvasRenderer.cullTransparentMesh = false;
        hitImage.color = new Color(1f, 1f, 1f, 0.01f);
    }

    private bool IsPointerOverButton(Button button, Vector2 screenPosition)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            return false;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
            return false;

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera))
            return true;

        if (canvas == null)
            return false;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null || Screen.width <= 0 || Screen.height <= 0)
            return false;

        Vector2 scaledPosition = new Vector2(
            screenPosition.x * canvasRect.rect.width / Screen.width,
            screenPosition.y * canvasRect.rect.height / Screen.height);

        return RectTransformUtility.RectangleContainsScreenPoint(rect, scaledPosition, camera);
    }

    private Button FindButton(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name != objectName)
                continue;

            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;

            button = child.GetComponentInChildren<Button>(true);
            if (button != null)
                return button;

            return child.GetComponentInParent<Button>(true);
        }

        return null;
    }

    private static void SetText(Graphic target, string value)
    {
        if (target == null)
            return;

        TMP_Text tmp = target as TMP_Text;
        if (tmp != null)
        {
            tmp.text = value ?? string.Empty;
            return;
        }

        Text legacyText = target as Text;
        if (legacyText != null)
            legacyText.text = value ?? string.Empty;
    }
}
