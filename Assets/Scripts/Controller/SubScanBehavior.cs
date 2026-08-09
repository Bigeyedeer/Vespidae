using UnityEngine;
using UnityEngine.UI;

public class SubScanBehavior : MonoBehaviour
{
    public Image radialImage;
    public ScannableObject scannableObject;
    public int requiredScans;
    private bool isScanned;
    void Start()
    {
        isScanned = false;
    }

    void Update()
    {
        
    }
}
