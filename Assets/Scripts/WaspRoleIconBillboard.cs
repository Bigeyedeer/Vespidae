using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WaspRoleIconBillboard : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private RectTransform billboardRoot;
    [SerializeField] private Canvas billboardCanvas;
    [SerializeField] private Image iconImage;

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
