using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WaspRoleIconBillboard : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private RectTransform billboardRoot;
    [SerializeField] private Canvas billboardCanvas;
    [SerializeField] private Image iconImage;

    [Header("Faction Tint")]
    [SerializeField, Tooltip("Tint applied to the role icon for the player's own wasps.")]
    private Color nativeTint = new Color(1f, 0.87f, 0.35f, 1f);
    [SerializeField, Tooltip("Tint applied to invasive wasps so they read as hostile at a glance.")]
    private Color invasiveTint = new Color(0.92f, 0.29f, 0.24f, 1f);

    [Header("Fog")]
    [SerializeField, Tooltip("Hide this icon while the band its wasp stands in is still under fog. " +
                             "Without it the icon floats on top of the cloud and gives the position away.")]
    private bool hideUnderFog = true;
    [SerializeField, Min(0f), Tooltip("Seconds between fog visibility checks. The bands change rarely, " +
                                      "so this does not need to run every frame.")]
    private float fogCheckInterval = 0.35f;

    private C_MainWorldCameraFocus cameraFocus;
    private bool subscribed;
    private float fogCheckTimer;
    private bool hiddenByFog;

    private void Awake()
    {
        if (waspInfo == null)
            waspInfo = GetComponentInParent<WaspInfo>();

        if (billboardRoot == null)
            billboardRoot = transform as RectTransform;

        if (billboardCanvas == null)
            billboardCanvas = GetComponent<Canvas>();

        if (iconImage == null)
            iconImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        UpdateFogVisibility();

        Camera activeCamera = GetActiveCamera();
        if (billboardRoot == null || activeCamera == null)
            return;

        if (billboardCanvas != null)
            billboardCanvas.worldCamera = activeCamera;

        billboardRoot.rotation = Quaternion.LookRotation(
            activeCamera.transform.position - billboardRoot.position,
            activeCamera.transform.up);
    }

    public void Initialize(WaspInfo info)
    {
        if (waspInfo != info)
        {
            Unsubscribe();
            waspInfo = info;
        }

        Subscribe();
        Refresh();
    }

    /// <summary>
    /// Hides the icon while the band its wasp is standing in is still fogged.
    ///
    /// The icon lives on a world space canvas with overrideSorting, so it draws after the fog volume
    /// whatever render queue the fog uses. Fighting that with sorting alone would be fragile, and it
    /// would still be wrong: an icon visible through fog tells the player where an enemy is before
    /// they have uncovered that ground.
    /// </summary>
    private void UpdateFogVisibility()
    {
        if (!hideUnderFog || iconImage == null)
            return;

        fogCheckTimer -= Time.deltaTime;
        if (fogCheckTimer > 0f)
            return;

        fogCheckTimer = fogCheckInterval;

        RegionFogController fog = RegionFogController.Instance;
        if (fog == null)
        {
            hiddenByFog = false;
            return;
        }

        HexTile hex = ResolveHex();
        // A wasp between hexes keeps whatever visibility it last had rather than flickering.
        if (hex == null)
            return;

        bool shouldHide = !fog.IsHexVisible(hex);
        if (shouldHide == hiddenByFog)
            return;

        hiddenByFog = shouldHide;
        Refresh();
    }

    private HexTile ResolveHex()
    {
        WaspControl friendly = GetComponentInParent<WaspControl>();
        if (friendly != null)
            return friendly.StationedHex != null ? friendly.StationedHex : friendly.TargetHex;

        EnemyWaspControl enemy = GetComponentInParent<EnemyWaspControl>();
        if (enemy != null)
            return enemy.StationedHex != null ? enemy.StationedHex : enemy.TargetHex;

        return null;
    }

    public void Refresh()
    {
        if (iconImage == null)
            return;

        Sprite icon = waspInfo != null ? waspInfo.RoleIcon : null;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null && !hiddenByFog;

        // Same pictogram, different colour, so friend and foe are separable without reading it.
        bool invasive = waspInfo != null && !waspInfo.IsNative;
        iconImage.color = invasive ? invasiveTint : nativeTint;
    }

    private Camera GetActiveCamera()
    {
        if (cameraFocus == null)
            cameraFocus = FindFirstObjectByType<C_MainWorldCameraFocus>();

        Camera activeCamera = cameraFocus != null ? cameraFocus.ActiveCamera : null;
        if (activeCamera != null && activeCamera.isActiveAndEnabled)
            return activeCamera;

        Camera bestCamera = null;
        Camera[] cameras = Camera.allCameras;
        for (int index = 0; index < cameras.Length; index++)
        {
            Camera candidate = cameras[index];
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.targetTexture != null)
                continue;

            if (bestCamera == null || candidate.depth > bestCamera.depth)
                bestCamera = candidate;
        }

        return bestCamera != null ? bestCamera : Camera.main;
    }

    private void Subscribe()
    {
        if (subscribed || waspInfo == null)
            return;

        waspInfo.AssignmentChanged += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || waspInfo == null)
            return;

        waspInfo.AssignmentChanged -= Refresh;
        subscribed = false;
    }
}
