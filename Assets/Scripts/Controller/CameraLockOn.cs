using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

public class CameraLockOn : MonoBehaviour
{
    private StarterAssetsInputs _Input;  // StarterAssetsInputs reference
    //public GameObject followCamera;
    //public GameObject lockCamera;

    [Tooltip("How high above the target you will look at")]
    public float lookatOffset = 1.2f;

    [Tooltip("Current Lookat Targer")]
    public Transform CurrentTarget { get; private set; }
    public bool isLockedOn { get; private set; }

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

        if (isLockedOn)
        {
            isLockedOn = false;
            CurrentTarget = null;
        }
        else
        {
            CurrentTarget = FindNearestTarget();

            if (CurrentTarget != null)
            {
                isLockedOn = true;
            }
        }

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
}
