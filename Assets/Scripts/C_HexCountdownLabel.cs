using TMPro;
using UnityEngine;

/// <summary>
/// Floats a countdown above a hex while something timed is happening on it.
///
/// Scouting and enemy claims both run on a clock, and until now the only feedback was a number
/// tucked away in a corner panel. Putting it over the tile itself means the player watches the thing
/// they are waiting on rather than hunting for a readout, and can see at a glance which of several
/// hexes is about to resolve.
///
/// The label builds itself, so hexes need nothing authored on them beyond this component.
/// </summary>
[RequireComponent(typeof(HexTile))]
[DisallowMultipleComponent]
public class C_HexCountdownLabel : MonoBehaviour
{
    [SerializeField] private HexTile hexTile;

    [Header("Placement")]
    [SerializeField, Tooltip("Height above the tile's top face.")]
    private float heightAboveTile = 1.1f;
    [SerializeField, Min(0.001f), Tooltip("World size of one text unit. Small, because this is world space.")]
    private float characterScale = 0.012f;

    [Header("Appearance")]
    [SerializeField] private float fontSize = 34f;
    [SerializeField, Tooltip("Colour while the player is scouting.")]
    private Color scoutingColour = new Color(1f, 0.87f, 0.35f, 1f);
    [SerializeField, Tooltip("Colour while an invasive is claiming this tile.")]
    private Color claimColour = new Color(0.92f, 0.29f, 0.24f, 1f);

    private TMP_Text label;
    private Transform labelRoot;
    private Camera cameraCache;
    private int lastWholeSeconds = -1;

    private void Awake()
    {
        if (hexTile == null)
            hexTile = GetComponent<HexTile>();

        BuildLabel();
        SetVisible(false);
    }

    private void BuildLabel()
    {
        GameObject root = new GameObject("CountdownLabel", typeof(RectTransform), typeof(Canvas));
        labelRoot = root.transform;
        labelRoot.SetParent(transform, false);

        // Sit above the tile's rendered top rather than its pivot, so tall props do not swallow it.
        float top = heightAboveTile;
        Renderer tileRenderer = GetComponentInChildren<Renderer>();
        if (tileRenderer != null)
            top = tileRenderer.bounds.max.y - transform.position.y + heightAboveTile;
        labelRoot.localPosition = new Vector3(0f, top, 0f);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Under the wasp role icons, so a countdown never hides a unit.
        canvas.overrideSorting = true;
        canvas.sortingOrder = 40;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400f, 120f);
        rootRect.localScale = Vector3.one * characterScale;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(labelRoot, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.GetComponent<TextMeshProUGUI>();
        // Borrow whatever the rest of the HUD uses, cached across all hexes so this is one search
        // rather than forty-two. Without it these render in TMP's default face and look imported.
        TMP_FontAsset shared = ResolveSharedFont();
        if (shared != null)
            label.font = shared;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.fontStyle = FontStyles.Bold;

        SetLayerRecursively(root, gameObject.layer);
    }

    private void LateUpdate()
    {
        if (hexTile == null || label == null)
            return;

        bool scouting = hexTile.IsScouting;
        bool claiming = hexTile.ClaimTimeRemaining > 0f;

        if (!scouting && !claiming)
        {
            SetVisible(false);
            lastWholeSeconds = -1;
            return;
        }

        float remaining = scouting ? hexTile.ScoutingTimeRemaining : hexTile.ClaimTimeRemaining;
        int whole = Mathf.CeilToInt(Mathf.Max(0f, remaining));

        // Only rebuild the text when the second actually changes - TMP regenerates its mesh on every
        // assignment, and this runs on every hex every frame.
        if (whole != lastWholeSeconds)
        {
            lastWholeSeconds = whole;
            label.text = whole + "s";
        }

        label.color = scouting ? scoutingColour : claimColour;
        SetVisible(true);
        FaceCamera();
    }

    private void FaceCamera()
    {
        if (cameraCache == null || !cameraCache.isActiveAndEnabled)
            cameraCache = ResolveCamera();

        if (cameraCache == null)
            return;

        labelRoot.rotation = Quaternion.LookRotation(
            labelRoot.position - cameraCache.transform.position,
            cameraCache.transform.up);
    }

    private static TMP_FontAsset sharedFont;
    private static bool sharedFontSearched;

    private static TMP_FontAsset ResolveSharedFont()
    {
        if (sharedFontSearched)
            return sharedFont;

        sharedFontSearched = true;
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text == null || text.font == null)
                continue;

            sharedFont = text.font;
            break;
        }

        return sharedFont;
    }

    private static Camera ResolveCamera()
    {
        Camera best = Camera.main;
        if (best != null && best.isActiveAndEnabled)
            return best;

        // The match swaps to an untagged close-up camera, so Camera.main is null much of the time.
        Camera[] cameras = Camera.allCameras;
        for (int index = 0; index < cameras.Length; index++)
        {
            Camera candidate = cameras[index];
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.targetTexture != null)
                continue;

            if (best == null || candidate.depth > best.depth)
                best = candidate;
        }

        return best;
    }

    private void SetVisible(bool visible)
    {
        if (labelRoot != null && labelRoot.gameObject.activeSelf != visible)
            labelRoot.gameObject.SetActive(visible);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
