using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-match screen. Listens to <see cref="GameStateManager"/> and shows a win or loss card
/// with a short summary of how the match finished.
/// </summary>
public class C_MatchResultScreen : MonoBehaviour
{
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button menuButton;

    [Header("Appearance")]
    [SerializeField] private Color victoryColour = new Color(0.45f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color defeatColour = new Color(0.88f, 0.35f, 0.30f, 1f);

    private bool shown;

    private void Start()
    {
        if (screenRoot != null)
            screenRoot.SetActive(false);

        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.MatchEnded -= HandleMatchEnded;
    }

    private void Subscribe()
    {
        if (GameStateManager.Instance == null)
            return;

        GameStateManager.Instance.MatchEnded -= HandleMatchEnded;
        GameStateManager.Instance.MatchEnded += HandleMatchEnded;

        // The match may already be decided if this object was enabled late.
        if (GameStateManager.Instance.IsMatchOver)
            HandleMatchEnded(GameStateManager.Instance.Outcome);
    }

    private void HandleMatchEnded(GameOutcome outcome)
    {
        if (shown)
            return;

        shown = true;
        Show(outcome);
    }

    public void Show(GameOutcome outcome)
    {
        if (screenRoot != null)
            screenRoot.SetActive(true);

        bool victory = outcome == GameOutcome.Victory;

        if (titleText != null)
        {
            titleText.text = victory ? "THE VALLEY HOLDS" : "THE COLONY HAS FALLEN";
            titleText.color = victory ? victoryColour : defeatColour;
        }

        if (summaryText != null)
            summaryText.text = BuildSummary(victory);

        // Freeze the world behind the card.
        Time.timeScale = 0f;

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    private string BuildSummary(bool victory)
    {
        GameStateManager state = GameStateManager.Instance;
        if (state == null)
            return string.Empty;

        float territory = state.GetPlayerTerritoryPercentage();
        int capitulated = state.CapitulatedFactions.Count;

        if (victory)
        {
            return capitulated > 0
                ? $"You drove out {capitulated} invasive faction{(capitulated == 1 ? "" : "s")}.\n" +
                  $"Native territory held: {territory:0}%"
                : $"Native territory held: {territory:0}%";
        }

        return "Your last nest was destroyed.\n" +
               $"Native territory held at the end: {territory:0}%";
    }

    private void ReturnToMenu()
    {
        // Always restore time before leaving, or the menu scene loads frozen.
        Time.timeScale = 1f;
        C_MainWorldOverlayNavigation.Instance?.QuitToMenu();
    }

    /// <summary>Wiring hook for the editor setup tool.</summary>
    public void Configure(GameObject root, TMP_Text title, TMP_Text summary, Button button)
    {
        screenRoot = root;
        titleText = title;
        summaryText = summary;
        menuButton = button;
    }
}
