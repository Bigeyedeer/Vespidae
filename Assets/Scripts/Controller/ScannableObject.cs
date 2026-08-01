using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScannableObject : MonoBehaviour
{
    [Header("General")]
    public string objectName;
    public Image objectImage;

    [TextArea]
    public string description;

    [Header("Scan")]
    public bool hasBeenScanned;

    [Header("Category")]
    public ScanType scanType;

    [Header("UI Color")]
    public Color uiColor;

    [Header("Radial Progress Image")]
    public Image radialTarget;

    [Header("Notification Image")]
    public Image notificationImage;
}

public enum ScanType
{
    Insect,
    Plant,
    Protein,
    Mineral,
    Other
}