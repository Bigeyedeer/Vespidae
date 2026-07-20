using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class C_MainWorldNavigation : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SB_PlayerSelection_State selectionState;

    [Header("Panels")]
    [SerializeField] private GameObject waspInfoPanel;
    [SerializeField] private GameObject skillsPanel;

    [Header("Wasp information")]
    [SerializeField] private TMP_Text waspInfoTitle;
    [SerializeField] private TMP_Text waspInfoSubtitle;
    [SerializeField] private TMP_Text waspInfoBody;
    [SerializeField] private TMP_Text waspInfoNodeDetails;
    [SerializeField] private Image waspInfoPortrait;

    [Header("Skills")]
    [SerializeField] private TMP_Text selectedSkillDetails;

    private C_MainWorldHexNode selectedHex;
    private C_MainWorldHexNode hoveredHex;
    private Button selectedSkillButton;

    private void Awake()
    {
        if (selectionState == null)
        {
            Debug.LogWarning("MainWorld navigation has no selection state. The fallback species will be used where available.");
        }

        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }

        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }

        BindSkillButtons();
    }

    public void SelectHex(C_MainWorldHexNode hex)
    {
        if (hex == null)
        {
            return;
        }

        if (selectedHex != null && selectedHex != hex)
        {
            selectedHex.SetHighlighted(false);
        }

        selectedHex = hex;
        selectedHex.SetHighlighted(true);
        PopulateWaspInfo(hex);

        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }

        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(true);
        }
    }

    public void SetHoveredHex(C_MainWorldHexNode hex)
    {
        hoveredHex = hex;
    }

    public void ClearHoveredHex(C_MainWorldHexNode hex)
    {
        if (hoveredHex == hex)
        {
            hoveredHex = null;
        }
    }

    public void OpenSkills()
    {
        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }

        if (skillsPanel != null)
        {
            skillsPanel.SetActive(true);
        }
    }

    public void CloseSkills()
    {
        if (skillsPanel != null)
        {
            skillsPanel.SetActive(false);
        }
    }

    public void CloseWaspInfo()
    {
        if (waspInfoPanel != null)
        {
            waspInfoPanel.SetActive(false);
        }

        if (selectedHex != null)
        {
            selectedHex.SetHighlighted(false);
        }
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void SelectSkillCard(Button card)
    {
        if (card == null)
        {
            return;
        }

        if (selectedSkillButton != null)
        {
            selectedSkillButton.interactable = true;
        }

        selectedSkillButton = card;
        selectedSkillButton.interactable = false;

        TMP_Text title = card.GetComponentInChildren<TMP_Text>();
        if (selectedSkillDetails != null)
        {
            string skillName = title != null ? title.text : "Selected skill";
            selectedSkillDetails.text = $"{skillName}\nNavigation preview only — this skill is not activated yet.";
        }
    }

    private void BindSkillButtons()
    {
        if (skillsPanel == null)
        {
            return;
        }

        Button[] buttons = skillsPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            Button capturedButton = button;
            capturedButton.onClick.AddListener(() => SelectSkillCard(capturedButton));
        }
    }

    private void PopulateWaspInfo(C_MainWorldHexNode hex)
    {
        SB_Wasps_Info wasp = selectionState != null ? selectionState.SelectedWasp : null;

        if (wasp == null)
        {
            if (waspInfoTitle != null) waspInfoTitle.text = "Wasp information";
            if (waspInfoSubtitle != null) waspInfoSubtitle.text = "No species data is available";
            if (waspInfoBody != null) waspInfoBody.text = "Select a species from the Menu before entering MainWorld.";
            if (waspInfoNodeDetails != null) waspInfoNodeDetails.text = FormatNodeDetails(hex);
            return;
        }

        if (waspInfoTitle != null) waspInfoTitle.text = wasp.CommonName;
        if (waspInfoSubtitle != null) waspInfoSubtitle.text = $"{wasp.ScientificName}  ·  {wasp.Classification}";
        if (waspInfoBody != null)
        {
            waspInfoBody.text =
                $"Nest type\n{wasp.NestType}\n\n" +
                $"Habitat\n{wasp.HabitatCue}\n\n" +
                $"Threat response\n{wasp.ThreatResponse}\n\n" +
                $"Species summary\n{wasp.GameplaySummary}";
        }

        if (waspInfoNodeDetails != null)
        {
            waspInfoNodeDetails.text = FormatNodeDetails(hex);
        }

        if (waspInfoPortrait != null)
        {
            waspInfoPortrait.sprite = wasp.Portrait;
            waspInfoPortrait.color = wasp.Portrait != null ? Color.white : new Color(wasp.AccentColor.r, wasp.AccentColor.g, wasp.AccentColor.b, 0.85f);
        }
    }

    private string FormatNodeDetails(C_MainWorldHexNode hex)
    {
        return $"RIDGE HEX · {hex.NodeStatus}\n\n" +
               $"Habitat        {hex.HabitatName}\n" +
               $"Resources      {hex.ResourceSummary}\n" +
               $"Nest cue       {hex.NestSummary}";
    }
}
