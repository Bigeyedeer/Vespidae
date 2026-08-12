using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-corner warning listing hexes an invasive faction is currently claiming, with a countdown
/// per hex. Without this the player has no way to notice territory slipping away until it is gone.
/// </summary>
public class C_ClaimAlertPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text[] entryLabels = new TMP_Text[3];
    [SerializeField] private Image[] entryBars = new Image[3];
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.15f;

    [Header("Appearance")]
    [SerializeField] private Color warningColour = new Color(0.85f, 0.42f, 0.18f, 1f);
    [SerializeField] private Color criticalColour = new Color(0.86f, 0.22f, 0.18f, 1f);
    [SerializeField, Range(0f, 1f), Tooltip("Progress past which the countdown turns critical.")]
    private float criticalProgress = 0.7f;

    private readonly List<HexTile> claimed = new List<HexTile>();
    private float refreshTimer;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;
        Refresh();
    }

    private void Refresh()
    {
        CollectClaimedHexes();

        bool anyClaims = claimed.Count > 0;
        if (panelRoot != null && panelRoot.activeSelf != anyClaims)
            panelRoot.SetActive(anyClaims);

        if (!anyClaims)
            return;

        int slots = entryLabels != null ? entryLabels.Length : 0;
        for (int index = 0; index < slots; index++)
        {
            TMP_Text label = entryLabels[index];
            Image bar = index < (entryBars != null ? entryBars.Length : 0) ? entryBars[index] : null;

            if (index >= claimed.Count)
            {
                if (label != null) label.gameObject.SetActive(false);
                if (bar != null) bar.transform.parent.gameObject.SetActive(false);
                continue;
            }

            HexTile hex = claimed[index];
            float progress = hex.ClaimProgress;

            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = $"{hex.HexName}  —  {Mathf.CeilToInt(hex.ClaimTimeRemaining)}s";
                label.color = progress >= criticalProgress ? criticalColour : warningColour;
            }

            if (bar != null)
            {
                bar.transform.parent.gameObject.SetActive(true);
                bar.fillAmount = Mathf.Clamp01(progress);
                bar.color = progress >= criticalProgress ? criticalColour : warningColour;
            }
        }
    }

    private void CollectClaimedHexes()
    {
        claimed.Clear();
        foreach (HexTile hex in FindObjectsByType<HexTile>(FindObjectsSortMode.None))
        {
            if (hex != null && hex.IsBeingClaimed)
                claimed.Add(hex);
        }

        // Most urgent first, and never show more than the panel has slots for.
        claimed.Sort((left, right) => left.ClaimTimeRemaining.CompareTo(right.ClaimTimeRemaining));
        int capacity = entryLabels != null ? entryLabels.Length : 0;
        if (claimed.Count > capacity)
            claimed.RemoveRange(capacity, claimed.Count - capacity);
    }

    /// <summary>Wiring hook for the editor setup tool.</summary>
    public void Configure(GameObject root, TMP_Text[] labels, Image[] bars)
    {
        panelRoot = root;
        entryLabels = labels;
        entryBars = bars;
    }
}
