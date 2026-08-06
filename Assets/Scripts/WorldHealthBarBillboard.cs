using UnityEngine;

public class WorldHealthBarBillboard : MonoBehaviour
{
    [SerializeField] private Transform billboardRoot;

    private void LateUpdate()
    {
        Camera activeCamera = Camera.main;
        Transform root = billboardRoot != null ? billboardRoot : transform;
        if (activeCamera == null || root == null)
            return;

        Vector3 direction = root.position - activeCamera.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
