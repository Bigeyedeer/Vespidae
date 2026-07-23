using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class C_WaspSelection_Menu : MonoBehaviour
{
    [Header("Species Data")]
    [SerializeField] private SB_Wasps_Info[] selectableWasps;
    [SerializeField] private SB_PlayerSelection_State selectionState;

    [Header("Navigation")]
    [SerializeField] private C_MainMenu_Ctrl mainMenuController;
    [SerializeField] private string mainWorldSceneName = "wasp RTS Lvl";

    [Header("Species Cards")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private C_WaspSelectionCard cardPrefab;

    [Header("Text and Controls")]
    [SerializeField] private TMP_Text speciesHeader;
    [SerializeField] private TMP_Text detailsTitle;
    [SerializeField] private TMP_Text detailsDescription;
    [SerializeField] private TMP_Text detailsBenefit;
    [SerializeField] private Button confirmButton;

    private readonly List<C_WaspSelectionCard> cards = new List<C_WaspSelectionCard>();
    private SB_Wasps_Info pendingWasp;
    private bool cardsBuilt;

    private void OnEnable()
    {
        EnsureCardsBuilt();
        RefreshSelectionDisplay();
    }

    public void SelectWasp(SB_Wasps_Info wasp, C_WaspSelectionCard selectedCard)
    {
        if (wasp == null)
        {
            return;
        }

        pendingWasp = wasp;

        foreach (C_WaspSelectionCard card in cards)
        {
            card.SetSelected(card == selectedCard);
        }

        RefreshSelectionDisplay();
    }

    public void ConfirmSelection()
    {
        if (pendingWasp == null || selectionState == null)
        {
            Debug.LogWarning("Wasp species selection cannot be confirmed because its data or runtime state is missing.");
            return;
        }

        // Colony functions are selected later in the gameplay flow. Scout is
        // only the temporary runtime default needed by the existing HUD/state.
        selectionState.SetSelection(pendingWasp, WaspFunction.Scout);
        SceneManager.LoadScene(mainWorldSceneName);
    }

    public void ReturnToMainMenu()
    {
        mainMenuController?.ReturnToMainMenu();
    }

    private void EnsureCardsBuilt()
    {
        if (cardsBuilt)
        {
            return;
        }

        cardsBuilt = true;

        if (selectableWasps == null || selectableWasps.Length == 0 || cardContainer == null || cardPrefab == null)
        {
            Debug.LogError("Wasp species selection is missing its species list, card container, or card prefab reference.");
            return;
        }

        if (speciesHeader != null)
        {
            speciesHeader.text = "Select a native species to command";
        }

        foreach (SB_Wasps_Info wasp in selectableWasps)
        {
            if (wasp == null || !wasp.IsPlayable || wasp.Classification != WaspClassification.Native)
            {
                continue;
            }

            C_WaspSelectionCard card = Instantiate(cardPrefab, cardContainer);
            card.BindSpecies(wasp, SelectWasp);
            card.SetSelected(false);
            cards.Add(card);
        }
    }

    private void RefreshSelectionDisplay()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = pendingWasp != null;
        }

        if (pendingWasp == null)
        {
            if (detailsTitle != null)
            {
                detailsTitle.text = "Choose a native wasp species";
            }

            if (detailsDescription != null)
            {
                detailsDescription.text = "Select the native species you want to command in the opening scenario.";
            }

            if (detailsBenefit != null)
            {
                detailsBenefit.text = "Species behaviour and habitat details will appear here.";
            }

            return;
        }

        if (detailsTitle != null)
        {
            detailsTitle.text = $"{pendingWasp.CommonName}\n<i>{pendingWasp.ScientificName}</i>";
        }

        if (detailsDescription != null)
        {
            detailsDescription.text = pendingWasp.GameplaySummary;
        }

        if (detailsBenefit != null)
        {
            detailsBenefit.text = $"{pendingWasp.Classification} species\nNest: {pendingWasp.NestType}\nHabitat: {pendingWasp.HabitatCue}";
        }
    }
}
