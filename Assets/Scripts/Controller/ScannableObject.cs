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

    

    [Header("Category")]
    public ScanType scanType;

    [Header("UI Color")]
    public Color uiColor;

    [Header("Radial Progress Image")]
    public Image radialTarget;

    [Header("Notification Image")]
    public Image notificationImage;

    [Header("Scan Context Text")]
    [TextArea(2,4)]
    public string[] scanMessages;

    [Header("Panel Screen")]
    public GameObject InfoPanel;

    [Header("Scanning")]
    public int requiredScans = 1;
    [HideInInspector] public int completedScans = 0;
    public bool hasBeenScanned;
}

public enum ScanType
{
    Insect,
    Plant,
    Protein,
    Mineral,
    Other
}