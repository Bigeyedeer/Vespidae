using UnityEngine;

public class EnemyWaspControl : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private WaspScopeRole faction = WaspScopeRole.PrimaryInvasive;
    [SerializeField] private WaspFunction assignedFunction = WaspFunction.Scout;
    [SerializeField] private bool deriveFactionFromSpecies = true;
    [SerializeField, Min(0f)] private float threatLevel = 1f;

    private Vector3 destination;
    private bool hasDestination;
    private bool alerted;
    private C_Enemy_Hive_Orc homeHive;

    public WaspInfo WaspInfo => waspInfo;
    public SB_Wasps_Info SpeciesInfo => waspInfo != null ? waspInfo.SpeciesInfo : null;
    public WaspScopeRole Faction => ResolveFaction();
    public WaspFunction AssignedFunction => assignedFunction;
    public C_Enemy_Hive_Orc HomeHive => homeHive;
    public float ThreatLevel => threatLevel;
    public bool IsAlerted => alerted;
    public bool HasDestination => hasDestination;
    public Vector3 Destination => destination;

    private void Awake()
    {
        if (waspInfo == null)
            waspInfo = GetComponent<WaspInfo>();

        if (deriveFactionFromSpecies && SpeciesInfo != null)
            faction = SpeciesInfo.ScopeRole;
    }

    private void OnEnable()
    {
        EnemyHiveControl.Instance?.Register(this);
    }

    private void OnDisable()
    {
        homeHive?.OwnerHex?.UnregisterEnemyWasp(this);
        EnemyHiveControl.Instance?.Unregister(this);
    }

    public void SetDestination(Vector3 worldPosition)
    {
        destination = worldPosition;
        hasDestination = true;
    }

    public void ClearDestination()
    {
        hasDestination = false;
    }

    public void SetAlerted(bool value)
    {
        alerted = value;
    }

    public void SetThreatLevel(float value)
    {
        threatLevel = Mathf.Max(0f, value);
    }

    public void InitializeEnemyWasp(C_Enemy_Hive_Orc hive, WaspFunction function)
    {
        homeHive = hive;
        assignedFunction = function;
        waspInfo?.SetRuntimeAssignment(null, function);
        homeHive?.OwnerHex?.RegisterEnemyWasp(this);
    }

    public void SetFaction(WaspScopeRole value)
    {
        faction = value;
        deriveFactionFromSpecies = false;
        EnemyHiveControl.Instance?.RefreshRegistration(this);
    }

    private WaspScopeRole ResolveFaction()
    {
        if (deriveFactionFromSpecies && SpeciesInfo != null)
            return SpeciesInfo.ScopeRole;

        return faction;
    }
}
