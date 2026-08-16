using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The wasp identification card.
///
/// The card is deliberately blank when the player first opens it on a species they have not fought.
/// Every few combat engagements with that faction, <see cref="SpeciesCodex"/> releases one more entry
/// from the species asset, starting with the name itself. Locked rows keep their label and show what
/// it would cost to fill them in, so the player learns which features to compare rather than being
/// handed the answer.
///
/// Each faction tracks separately, so a species the player has never met stays a blank card while the
/// one they have been fighting fills in.
/// </summary>
public class WaspInfoPanel : MonoBehaviour
{
    private const string UnknownTitle = "Unidentified specimen";
    private const string UnknownSubtitle = "No field data recorded";
    private const string UnknownChip = "Unknown";
    private const int DetailRowCount = 5;

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
    [SerializeField] private Button verifyButton;
    [SerializeField] private Button flagButton;

    [Header("Identification")]
    [SerializeField, Range(0f, 0.5f), Tooltip("Biodiversity lost when the player flags a native species " +
                                              "as invasive. This is the cost of acting before identifying.")]
    private float misidentificationPenalty = 0.08f;

    [Header("Locked Presentation")]
    [SerializeField, Tooltip("Alpha applied to a detail row and its divider while it is still locked.")]
    private float lockedRowAlpha = 0.35f;

    private WaspInfo selectedWasp;
    [SerializeField, Tooltip("Optional. Found automatically. Renders the selected wasp into the ID card.")]
    private C_SpecimenViewport specimenViewport;

    private readonly Graphic[] detailRows = new Graphic[DetailRowCount];
    private readonly Graphic[] detailLines = new Graphic[DetailRowCount];
    private RectTransform confidenceBarRect;
    private RectTransform confidenceBarBackground;
    private RectTransform aggressionBarRect;
    private float aggressionBarMaximumWidth;
    private bool resolved;

    private void Awake()
    {
        ResolveReferences();
        BindCloseButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindCloseButtons();

        if (SpeciesCodex.Instance != null)
            SpeciesCodex.Instance.CodexAdvanced += HandleCodexAdvanced;
    }

    private void OnDisable()
    {
        if (SpeciesCodex.Instance != null)
            SpeciesCodex.Instance.CodexAdvanced -= HandleCodexAdvanced;
    }

    private void HandleCodexAdvanced(WaspScopeRole faction)
    {
        // Refresh in place if the card is already open on the species that just advanced.
        if (selectedWasp != null && selectedWasp.SpeciesInfo != null && selectedWasp.SpeciesInfo.ScopeRole == faction)
            Render(selectedWasp);
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

    /// <summary>
    /// Finds the card's widgets by name. The panel was authored in the scene with none of these wired,
    /// so resolving by name keeps it working without twelve manual inspector assignments, and survives
    /// the card being rebuilt. Anything already assigned in the inspector wins.
    /// </summary>
    private void ResolveReferences()
    {
        if (resolved)
            return;

        resolved = true;

        if (commonNameText == null) commonNameText = FindGraphic("WaspInfo_Title");
        if (scientificNameText == null) scientificNameText = FindGraphic("WaspInfo_Subtitle");
        if (classificationText == null) classificationText = FindGraphic("WaspInfo_NativeChipText");
        if (statusText == null) statusText = FindGraphic("WaspInfo_Status");
        if (aggressionText == null) aggressionText = FindGraphic("WaspInfo_Aggression");
        if (confidenceText == null) confidenceText = FindGraphic("WaspInfo_IDConfidence");

        for (int i = 0; i < DetailRowCount; i++)
        {
            detailRows[i] = FindGraphic($"WaspInfo_Row_{i}");
            detailLines[i] = FindGraphic($"WaspInfo_Line_{i}");
        }

        confidenceBarRect = FindRect("WaspInfo_IDBarFill");
        confidenceBarBackground = FindRect("WaspInfo_IDBarBack");
        aggressionBarRect = FindRect("WaspInfo_AggressionBar");

        // The threat bar has no sibling track to measure, so its authored width is the maximum.
        if (aggressionBarRect != null && aggressionBarMaximumWidth <= 0f)
            aggressionBarMaximumWidth = aggressionBarRect.sizeDelta.x;

        if (confidenceBar == null && confidenceBarRect != null)
            confidenceBar = confidenceBarRect.GetComponent<Image>();
    }

    public void Open(WaspInfo wasp)
    {
        if (wasp == null)
            return;

        selectedWasp = wasp;
        gameObject.SetActive(true);
        ResolveReferences();
        Render(wasp);

        // Put a live, rotatable copy of this specimen in the card so the player can inspect the
        // morphology rather than read about it. This is never gated - looking at the animal is the
        // evidence the codex asks them to compare.
        ResolveViewport();
        if (specimenViewport != null)
            specimenViewport.ShowSpecimen(wasp);
    }

    private void Render(WaspInfo wasp)
    {
        SB_Wasps_Info species = wasp.SpeciesInfo;
        if (species == null)
            return;

        SpeciesCodex codex = SpeciesCodex.Instance;
        int unlocked = codex != null ? codex.UnlockedTierCount(species.ScopeRole) : 0;
        int remaining = codex != null ? codex.EngagementsUntilNextUnlock(species.ScopeRole) : 0;

        // Tier 0 is the identity itself, so below it the player has a specimen and no name for it.
        bool identified = unlocked > 0;
        SetText(commonNameText, identified ? species.CommonName : UnknownTitle);
        SetText(scientificNameText, identified ? species.ScientificName : UnknownSubtitle);
        SetText(classificationText, identified ? (species.Classification == WaspClassification.Native ? "Native" : "Invasive") : UnknownChip);
        SetText(statusText, identified ? "Identified" : $"{remaining} more encounters");

        RenderDetailRows(species, unlocked, remaining);
        RenderConfidence(species, unlocked);
        RenderThreat(species, unlocked);
    }

    /// <summary>
    /// Fills the five detail rows from the species codex. Entry 0 is the identity and is spent on the
    /// title, so the rows start at entry 1.
    /// </summary>
    private void RenderDetailRows(SB_Wasps_Info species, int unlocked, int remaining)
    {
        var entries = species.CodexEntries;
        for (int i = 0; i < DetailRowCount; i++)
        {
            Graphic row = detailRows[i];
            if (row == null)
                continue;

            int entryIndex = i + 1;
            bool hasEntry = entries != null && entryIndex < entries.Count;
            bool isUnlocked = hasEntry && unlocked > entryIndex;

            if (!hasEntry)
            {
                SetText(row, string.Empty);
                SetAlpha(row, 0f);
                SetAlpha(detailLines[i], 0f);
                continue;
            }

            WaspCodexEntry entry = entries[entryIndex];
            if (isUnlocked)
            {
                SetText(row, FormatRow(entry.Label, entry.Value));
                SetAlpha(row, 1f);
                SetAlpha(detailLines[i], 1f);
            }
            else
            {
                // Keep the label. Naming what is missing is what teaches the player which features
                // to compare; hiding the row entirely would teach nothing.
                // Entry e needs unlocked to reach e + 1, so the wait is the engagements left in the
                // current tier plus a full tier for every one after it.
                int perUnlock = SpeciesCodex.Instance != null ? SpeciesCodex.Instance.EngagementsPerUnlock : 3;
                int cost = (entryIndex - unlocked) * perUnlock + remaining;
                SetText(row, FormatRow(entry.Label, cost > 0 ? $"— {cost} more encounters —" : "— locked —"));
                SetAlpha(row, lockedRowAlpha);
                SetAlpha(detailLines[i], lockedRowAlpha);
            }
        }
    }

    private void RenderConfidence(SB_Wasps_Info species, int unlocked)
    {
        int total = species.CodexEntries != null ? species.CodexEntries.Count : 0;
        float progress = total > 0 ? Mathf.Clamp01(Mathf.Min(unlocked, total) / (float)total) : 0f;

        SetText(confidenceText, FormatRow("ID confidence", $"{progress * 100f:0}%"));

        if (confidenceBarRect != null)
        {
            // Measure the track rather than assuming a width, so resizing the card in the scene
            // cannot leave the fill over- or under-shooting it.
            float maximum = confidenceBarBackground != null ? confidenceBarBackground.sizeDelta.x : confidenceBarRect.sizeDelta.x;
            Vector2 size = confidenceBarRect.sizeDelta;
            size.x = maximum * progress;
            confidenceBarRect.sizeDelta = size;
        }
    }

    private void RenderThreat(SB_Wasps_Info species, int unlocked)
    {
        // Threat response is itself a codex entry, so the bar stays empty until that entry is earned.
        bool known = unlocked > 3;
        float level = known ? Mathf.Clamp01(species.ThreatLevel) : 0f;

        SetText(aggressionText, FormatRow("Threat response", known ? DescribeThreat(level) : "Unknown"));

        if (aggressionBarRect != null && aggressionBarMaximumWidth > 0f)
        {
            Vector2 size = aggressionBarRect.sizeDelta;
            size.x = aggressionBarMaximumWidth * level;
            aggressionBarRect.sizeDelta = size;
        }
    }

    private static string DescribeThreat(float level)
    {
        if (level >= 0.75f) return "High";
        if (level >= 0.45f) return "Moderate";
        if (level >= 0.2f) return "Defensive";
        return "Docile";
    }

    /// <summary>Matches the card's authored two-column look: label left, value right.</summary>
    private static string FormatRow(string label, string value)
    {
        return $"{label}\t{value}";
    }

    public void Close()
    {
        selectedWasp = null;

        ResolveViewport();
        if (specimenViewport != null)
            specimenViewport.ClearSpecimen();

        gameObject.SetActive(false);
    }

    private void ResolveViewport()
    {
        if (specimenViewport == null)
            specimenViewport = GetComponentInChildren<C_SpecimenViewport>(true);
    }

    private void BindCloseButtons()
    {
        if (returnButton == null)
            returnButton = FindButton("WaspInfo_Return");

        if (closeButton == null)
            closeButton = FindButton("WaspInfo_Close");

        BindCloseButton(returnButton);
        BindCloseButton(closeButton);
        BindVerdictButtons();
    }

    /// <summary>
    /// The two verdict buttons are the player's actual identification call, and the only place a
    /// wrong answer costs anything.
    ///
    /// Flagging a specimen means "this is invasive, act on it". Get that right and it is worth codex
    /// progress; get it wrong on a native and the colony pays for it in biodiversity. Verifying means
    /// "this one is native, leave it" - safe, but it teaches nothing if the specimen was invasive.
    /// </summary>
    private void BindVerdictButtons()
    {
        if (verifyButton == null)
            verifyButton = FindButton("WaspInfo_Verify");
        if (flagButton == null)
            flagButton = FindButton("WaspInfo_Flag");

        if (verifyButton != null)
        {
            verifyButton.onClick.RemoveListener(VerifyAsNative);
            verifyButton.onClick.AddListener(VerifyAsNative);
            EnsureButtonHitArea(verifyButton);
        }

        if (flagButton != null)
        {
            flagButton.onClick.RemoveListener(FlagAsInvasive);
            flagButton.onClick.AddListener(FlagAsInvasive);
            EnsureButtonHitArea(flagButton);
        }
    }

    private void VerifyAsNative()
    {
        SB_Wasps_Info species = selectedWasp != null ? selectedWasp.SpeciesInfo : null;
        if (species == null)
            return;

        bool correct = species.Classification == WaspClassification.Native;
        AudioDirector.Play(correct ? GameSound.CodexUnlocked : GameSound.UiClick);
        SetText(statusText, correct ? "Logged as native" : "Logged - but this one is invasive");
    }

    private void FlagAsInvasive()
    {
        SB_Wasps_Info species = selectedWasp != null ? selectedWasp.SpeciesInfo : null;
        if (species == null)
            return;

        if (species.Classification == WaspClassification.Invasive)
        {
            // A correct call is worth the same as meeting it in the field.
            SpeciesCodex.Instance?.RegisterEngagement(species.ScopeRole);
            AudioDirector.Play(GameSound.CodexUnlocked);
            SetText(statusText, "Flagged - correctly identified as invasive");
            Render(selectedWasp);
            return;
        }

        // Flagging a native is the mistake the whole game is about, so it costs real biodiversity.
        HiveManagement.Instance?.ApplyBiodiversityDamage(misidentificationPenalty);
        AudioDirector.Play(GameSound.CombatLost);
        SetText(statusText, "Flagged a native species - biodiversity harmed");
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

    private Graphic FindGraphic(string objectName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name != objectName)
                continue;

            TMP_Text tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
                return tmp;

            return child.GetComponent<Graphic>();
        }

        return null;
    }

    private RectTransform FindRect(string objectName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child as RectTransform;
        }

        return null;
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

    private static void SetAlpha(Graphic target, float alpha)
    {
        if (target == null)
            return;

        Color color = target.color;
        color.a = alpha;
        target.color = color;
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
