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

    private C_MainWorldCameraFocus cameraFocus;
    private bool subscribed;

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

    public void Refresh()
    {
        if (iconImage == null)
            return;

        Sprite icon = waspInfo != null ? waspInfo.RoleIcon : null;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

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
