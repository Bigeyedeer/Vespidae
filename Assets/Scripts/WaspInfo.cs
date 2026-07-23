using UnityEngine;

public class WaspInfo : MonoBehaviour
{
    [Header("Species Information")]
    [SerializeField] private string commonName = "Cape Paper Wasp";
    [SerializeField] private string scientificName = "Polistes marginalis";

    [TextArea(3, 6)]
    [SerializeField] private string description =
        "A native South African wasp species that plays an important role in controlling insect populations.";

    [TextArea(2, 4)]
    [SerializeField] private string ecologicalRole =
        "Predates on caterpillars and other small insects, helping maintain ecological balance.";

    [Header("Classification")]
    [SerializeField] private bool isNative = true;

    [Header("Camera Focus")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private Transform lookPoint;

    public string CommonName => commonName;
    public string ScientificName => scientificName;
    public string Description => description;
    public string EcologicalRole => ecologicalRole;
    public bool IsNative => isNative;

    public Transform CameraPoint => cameraPoint;

    public Vector3 LookPosition
    {
        get
        {
            if (lookPoint != null)
                return lookPoint.position;

            return transform.position;
        }
    }
}
