using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Resources")]
    [SerializeField] private float protein;

    [Header("UI")]
    [SerializeField] private TMP_Text proteinText;

    public float Protein => protein;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddProtein(float amount)
    {
        protein += amount;

        Debug.Log($"Protein increased by {amount}. Total protein: {protein}");

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (proteinText != null)
            proteinText.text = $"Protein: {protein:0.0}";
    }
}