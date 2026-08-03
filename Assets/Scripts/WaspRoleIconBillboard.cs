using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WaspRoleIconBillboard : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector2 iconSize = new Vector2(56f, 56f);
    [SerializeField, Min(0.001f)] private float worldScale = 0.01f;
    [SerializeField] private Color friendlyBackground = new Color(0.05f, 0.22f, 0.12f, 0.88f);
    [SerializeField] private Color enemyBackground = new Color(0.32f, 0.06f, 0.06f, 0.88f);
    [SerializeField] private Color borderColor = new Color(0.92f, 0.92f, 0.82f, 0.9f);

    private RectTransform billboardRoot;
    private Image backgroundImage;
    private Image iconImage;
    private C_MainWorldCameraFocus cameraFocus;
    private bool subscribed;

    public static WaspRoleIconBillboard Ensure(GameObject host, WaspInfo info)
    {
        if (host == null || info == null)
            return null;

        WaspRoleIconBillboard billboard = host.GetComponent<WaspRoleIconBillboard>();
        if (billboard == null)
            billboard = host.AddComponent<WaspRoleIconBillboard>();

        billboard.Initialize(info);
        return billboard;
    }

    private void Awake()
    {
        if (waspInfo == null)
            waspInfo = GetComponent<WaspInfo>();

        BuildVisuals();
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

        billboardRoot.rotation = Quaternion.LookRotation(
            billboardRoot.position - activeCamera.transform.position,
            activeCamera.transform.up);
    }

    public void Initialize(WaspInfo info)
    {
        if (waspInfo != info)
        {
            Unsubscribe();
            waspInfo = info;
        }

        BuildVisuals();
        Subscribe();
        Refresh();
    }

    public void Refresh()
    {
        if (iconImage == null || backgroundImage == null)
            return;

        Sprite icon = waspInfo != null ? waspInfo.RoleIcon : null;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        backgroundImage.color = GetComponent<WaspControl>() != null
            ? friendlyBackground
            : enemyBackground;
    }

    private void BuildVisuals()
    {
        if (billboardRoot != null)
            return;

        Transform existing = transform.Find("Wasp Role Icon");
        if (existing != null)
        {
            billboardRoot = existing as RectTransform;
            backgroundImage = existing.GetComponent<Image>();
            Transform iconTransform = existing.Find("Icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();

            if (billboardRoot != null && backgroundImage != null && iconImage != null)
                return;
        }

        GameObject rootObject = new GameObject(
            "Wasp Role Icon",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(Outline));
        rootObject.layer = gameObject.layer;
        rootObject.transform.SetParent(transform, false);

        billboardRoot = rootObject.GetComponent<RectTransform>();
        billboardRoot.localPosition = localOffset;
        billboardRoot.localScale = Vector3.one * worldScale;
        billboardRoot.sizeDelta = iconSize;

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        backgroundImage = rootObject.GetComponent<Image>();
        backgroundImage.raycastTarget = false;

        Outline outline = rootObject.GetComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.layer = gameObject.layer;
        iconObject.transform.SetParent(rootObject.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);

        iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
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
