using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space tug-of-war bar shown above a hex only while a fight is happening there.
/// The green fill is the player's side: full means the invaders are beaten, empty means the
/// player's attackers are. Follows the same pattern as the wasp and hive health bars.
/// </summary>
[RequireComponent(typeof(HexTile))]
public class HexCombatBar : MonoBehaviour
{
    [SerializeField] private HexTile hexTile;
    [SerializeField] private HexCombatController combatController;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private Image fill;
    [SerializeField] private Image background;

    [Header("Appearance")]
    [SerializeField] private Color playerColour = new Color(0.30f, 0.78f, 0.35f, 1f);
    [SerializeField] private Color losingColour = new Color(0.82f, 0.27f, 0.22f, 1f);
    [SerializeField, Min(0f), Tooltip("How quickly the bar slides toward the true value.")]
    private float smoothing = 6f;

    private float displayedBalance = 0.5f;

    private void Awake()
    {
        if (hexTile == null)
            hexTile = GetComponent<HexTile>();
        if (combatController == null)
            combatController = GetComponent<HexCombatController>();

        displayedBalance = 0.5f;
        ApplyVisuals(false);
    }

    private void LateUpdate()
    {
        if (combatController == null)
            return;

        bool fighting = combatController.HasActiveBattle;
        if (!fighting)
        {
            // Reset so the next fight starts from an even bar rather than the last result.
            displayedBalance = 0.5f;
            ApplyVisuals(false);
            return;
        }

        float target = combatController.BattleBalance;
        displayedBalance = smoothing <= 0f
            ? target
            : Mathf.Lerp(displayedBalance, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        ApplyVisuals(true);
    }

    private void ApplyVisuals(bool visible)
    {
        if (barRoot != null && barRoot.activeSelf != visible)
            barRoot.SetActive(visible);

        if (!visible || fill == null)
            return;

        fill.fillAmount = Mathf.Clamp01(displayedBalance);
        // Tint toward red as the player loses ground, so the state reads at a glance.
        fill.color = Color.Lerp(losingColour, playerColour, Mathf.Clamp01(displayedBalance));
    }

    /// <summary>Wiring hook for the editor setup tool.</summary>
    public void Configure(GameObject root, Image fillImage, Image backgroundImage)
    {
        barRoot = root;
        fill = fillImage;
        background = backgroundImage;
    }
}
