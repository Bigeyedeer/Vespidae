using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

public class CameraLockOn : MonoBehaviour
{
    private StarterAssetsInputs _Input;  // StarterAssetsInputs reference
    public ThirdPersonController thirdPersonController;
    //public GameObject followCamera;
    //public GameObject lockCamera;
    public ScanningManager scanningManager;

    [Tooltip("How high above the target you will look at")]
    public float lookatOffset = 1.2f;

    [Tooltip("Current Lookat Targer")]
    public Transform CurrentTarget { get; private set; }
    public bool isLockedOn { get; set; }

    [SerializeField] private float lockDistance = 20f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _Input = GetComponent<StarterAssetsInputs>();
        //lockCamera.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        LockOn();
    }

    private void LockOn()
    {
        if (!_Input.lockOn)
            return;

        _Input.lockOn = false;

        Debug.Log($"Before: isLockedOn = {isLockedOn}");

        if (isLockedOn)
        {
            Debug.Log("Unlocking");

            scanningManager.CancelScan();

            ForceUnlock();
            thirdPersonController.SyncCameraRotation();
        }
        else
        {
            Debug.Log("Searching for target...");

            CurrentTarget = FindNearestTarget();

            if (CurrentTarget == null)
            {
                Debug.Log("No target found.");
                return;
            }

            Debug.Log("Locked onto " + CurrentTarget.name);

            isLockedOn = true;
            scanningManager.BeginScan(CurrentTarget.GetComponent<ScannableObject>());
        }

        Debug.Log($"After: isLockedOn = {isLockedOn}");
    }

    private Transform FindNearestTarget()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");

        Transform nearest = null;

        float closestDistance = Mathf.Infinity;

        foreach (GameObject target in targets)
        {
            float distance = Vector3.Distance(
                transform.position,
                target.transform.position);

            if (distance < lockDistance && distance < closestDistance)
            {
                closestDistance = distance;
                nearest = target.transform;
            }
        }

        return nearest;
    }

    public void ForceUnlock()
    {
        scanningManager.CancelScan();

        CurrentTarget = null;
        isLockedOn = false;

    }
}
