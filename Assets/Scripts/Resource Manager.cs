using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public event Action ResourcesChanged;

    [Header("Resources")]
    [FormerlySerializedAs("sugar")]
    [SerializeField, Min(0f)] private float nectar;
    [FormerlySerializedAs("protein")]
    [SerializeField, Min(0f)] private float prey;
    [SerializeField, Min(0f)] private float fibre;

    [Header("Starting Resources")]
    [SerializeField] private bool resetResourcesOnStart = true;
    [SerializeField, Min(0f)] private float startingNectar = 150f;
    [SerializeField, Min(0f)] private float startingPrey = 150f;
    [SerializeField, Min(0f)] private float startingFibre = 300f;

    [Header("UI")]
    [SerializeField] private C_MainWorldHUD hud;

    public float Nectar => nectar;
    public float Prey => prey;
    public float Fibre => fibre;

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
        HiveManagement.GetOrCreate();
        hud = ResolveHud();
        if (resetResourcesOnStart)
            SetResources(startingNectar, startingPrey, startingFibre);
        else
            NotifyChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddNectar(float amount)
    {
        nectar = Mathf.Max(0f, nectar + amount);
        NotifyChanged();
    }

    public void AddPrey(float amount)
    {
        prey = Mathf.Max(0f, prey + amount);
        NotifyChanged();
    }

    public void AddFibre(float amount)
    {
        fibre = Mathf.Max(0f, fibre + amount);
        NotifyChanged();
    }

    public void AddResources(float nectarAmount, float preyAmount, float fibreAmount)
    {
        nectar = Mathf.Max(0f, nectar + Mathf.Max(0f, nectarAmount));
        prey = Mathf.Max(0f, prey + Mathf.Max(0f, preyAmount));
        fibre = Mathf.Max(0f, fibre + Mathf.Max(0f, fibreAmount));
        NotifyChanged();
    }

    public bool CanAfford(float nectarCost, float preyCost, float fibreCost)
    {
        return nectar >= Mathf.Max(0f, nectarCost) &&
               prey >= Mathf.Max(0f, preyCost) &&
               fibre >= Mathf.Max(0f, fibreCost);
    }

    public bool TrySpend(float nectarCost, float preyCost, float fibreCost)
    {
        nectarCost = Mathf.Max(0f, nectarCost);
        preyCost = Mathf.Max(0f, preyCost);
        fibreCost = Mathf.Max(0f, fibreCost);

        if (!CanAfford(nectarCost, preyCost, fibreCost))
            return false;

        nectar -= nectarCost;
        prey -= preyCost;
        fibre -= fibreCost;
        NotifyChanged();
        return true;
    }

    public void SetResources(float newNectar, float newPrey, float newFibre)
    {
        nectar = Mathf.Max(0f, newNectar);
        prey = Mathf.Max(0f, newPrey);
        fibre = Mathf.Max(0f, newFibre);
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        if (HiveManagement.Instance != null)
            HiveManagement.Instance.RecalculateFromResources();

        hud = ResolveHud();
        hud?.RefreshAll();
        ResourcesChanged?.Invoke();
    }

    private C_MainWorldHUD ResolveHud()
    {
        if (hud != null)
            return hud;

        return C_MainWorldHUD.GetOrCreate();
    }
}
