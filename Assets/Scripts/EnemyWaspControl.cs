using UnityEngine;

public class EnemyWaspControl : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private WaspRoleIconBillboard roleIconBillboard;
    [SerializeField] private WaspScopeRole faction = WaspScopeRole.PrimaryInvasive;
    [SerializeField] private WaspFunction assignedFunction = WaspFunction.Scout;
    [SerializeField] private bool deriveFactionFromSpecies = true;
    [SerializeField, Min(0f)] private float threatLevel = 1f;
    [SerializeField] private UnityEngine.AI.NavMeshAgent navMeshAgent;
    [SerializeField, Min(0.1f)] private float flightHeight = 0.35f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 8f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.25f;
    [SerializeField, Min(1f)] private float turnSpeed = 12f;

    private Vector3 destination;
    private bool hasDestination;
    private bool alerted;

    private C_Enemy_Hive_Orc homeHive;

    private HexTile targetHex;
    private HexTile stationedHex;
    private HexTile lastVisitedHex;

    private WaspWorkforceState workforceState = WaspWorkforceState.Idle;

    private GameObject navigationProxy;

    private Vector3 stationaryNavPosition;
    private bool hasStationaryPosition;

    public WaspInfo WaspInfo => waspInfo;
    public SB_Wasps_Info SpeciesInfo => waspInfo != null ? waspInfo.SpeciesInfo : null;
    public WaspScopeRole Faction => ResolveFaction();
    public WaspFunction AssignedFunction => assignedFunction;
    public C_Enemy_Hive_Orc HomeHive => homeHive;
    public HexTile TargetHex => targetHex;
    public HexTile StationedHex => stationedHex;
    public HexTile LastVisitedHex => lastVisitedHex;
    
    public WaspWorkforceState WorkforceState => workforceState;
    public float ThreatLevel => threatLevel;
    public bool IsAlerted => alerted;
    public bool HasDestination => hasDestination;
    public Vector3 Destination => destination;
   

    private void Awake()    
    {
        if (waspInfo == null)
            waspInfo = GetComponent<WaspInfo>();

        if (roleIconBillboard == null)
            roleIconBillboard = GetComponentInChildren<WaspRoleIconBillboard>(true);

        roleIconBillboard?.Initialize(waspInfo);
        CreateNavigationProxy();
        ConfigureAgent();

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

    public bool SetDestination(Vector3 worldPosition)
    {
        if (!EnsureAgentOnNavMesh())
            return false;

        if (!TrySamplePosition(worldPosition, out UnityEngine.AI.NavMeshHit hit))
            return false;

        destination = hit.position;

        hasDestination = TrySetPath(destination);

        if (!hasDestination)
            return false;
        Debug.Log(
            $"Enemy path set: {hasDestination}, OnNavMesh: {navMeshAgent.isOnNavMesh}");
        return true;
    }
    
    public bool DispatchToHex(HexTile hex)
    {
        if (hex == null)
            return false;

        if (!EnsureAgentOnNavMesh() ||
            !TrySamplePosition(hex.transform.position, out UnityEngine.AI.NavMeshHit hit))
        {
            return false;
        }

        if (stationedHex != null)
            stationedHex.UnregisterEnemyWasp(this);

        targetHex = hex;
        stationedHex = null;
        hasStationaryPosition = false;

        destination = hit.position;
        hasDestination = TrySetPath(destination);

        if (!hasDestination)
        {
            targetHex = null;
            return false;
        }

        workforceState = WaspWorkforceState.Travelling;

        return true;
    }

    private bool TrySetPath(Vector3 worldPosition)
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            return false;

        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();

        if (!navMeshAgent.CalculatePath(worldPosition, path) ||
            path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        return navMeshAgent.SetPath(path);
    }
    
    private bool EnsureAgentOnNavMesh()
    {
        if (navMeshAgent == null)
            return false;

        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            return true;

        return PlaceOnNavMesh(transform.position);
    }
    
    private bool PlaceOnNavMesh(Vector3 worldPosition)
    {
        if (navMeshAgent == null || !TrySamplePosition(worldPosition, out UnityEngine.AI.NavMeshHit hit))
            return false;

        bool wasEnabled = navMeshAgent.enabled;
        if (wasEnabled)
            navMeshAgent.enabled = false;

        transform.position = hit.position;
        navMeshAgent.enabled = true;
        ConfigureAgent();

        if (!navMeshAgent.Warp(hit.position))
        {
            navMeshAgent.enabled = false;
            return false;
        }

        navMeshAgent.nextPosition = hit.position;
        transform.position = hit.position + Vector3.up * flightHeight;

        return navMeshAgent.isOnNavMesh;
    }
    
    private void ConfigureAgent()
    {
        if (navMeshAgent == null)
            return;

        navMeshAgent.radius = 0.15f;
        navMeshAgent.height = 0.3f;
        navMeshAgent.speed = 3.5f;
        navMeshAgent.acceleration = 8f;
        navMeshAgent.angularSpeed = 240f;
        navMeshAgent.stoppingDistance = 0.2f;
        navMeshAgent.baseOffset = 0f;
        navMeshAgent.autoRepath = true;
        navMeshAgent.autoBraking = true;
        navMeshAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
    }
    
    private void CreateNavigationProxy()
    {
        GameObject proxy = new GameObject($"{name}_NavAgent");
        proxy.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        proxy.transform.position = transform.position;
        proxy.transform.rotation = transform.rotation;

        navMeshAgent = proxy.AddComponent<UnityEngine.AI.NavMeshAgent>();
        navMeshAgent.enabled = false;
    }
    
    private bool TrySamplePosition(Vector3 worldPosition, out UnityEngine.AI.NavMeshHit hit)
    {
        return UnityEngine.AI.NavMesh.SamplePosition(worldPosition, out hit, navMeshSampleRadius, UnityEngine.AI.NavMesh.AllAreas);
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
        if (!PlaceOnNavMesh(transform.position))
        {
            Debug.LogError($"{name} failed to place on NavMesh.");
        }
    }

    public void SetFaction(WaspScopeRole value)
    {
        faction = value;
        deriveFactionFromSpecies = false;
        EnemyHiveControl.Instance?.RefreshRegistration(this);
    }
    
    private void Update()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            return;
        
        if (workforceState != WaspWorkforceState.Travelling && hasStationaryPosition)
        {
            if ((navMeshAgent.nextPosition - stationaryNavPosition).sqrMagnitude > 0.0001f)
                navMeshAgent.Warp(stationaryNavPosition);

            transform.position = stationaryNavPosition + Vector3.up * flightHeight;
            return;
        }
        
        transform.position = navMeshAgent.nextPosition + Vector3.up * flightHeight;

        Vector3 velocity = navMeshAgent.desiredVelocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(velocity.normalized, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }
        
        if (workforceState == WaspWorkforceState.Travelling &&
            !navMeshAgent.pathPending &&
            navMeshAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete &&
            navMeshAgent.remainingDistance <= Mathf.Max(arrivalDistance, navMeshAgent.stoppingDistance))
        {
            CompleteArrival();
        }
    }

    private void CompleteArrival()
    {
        navMeshAgent.ResetPath();

        hasDestination = false;

        stationedHex = targetHex;
        lastVisitedHex = stationedHex;
        targetHex = null;

        workforceState = WaspWorkforceState.Stationed;

        SetStationaryPosition(navMeshAgent.nextPosition);

        if (stationedHex != null)
            stationedHex.RegisterEnemyWasp(this);
    }
    
    private void SetStationaryPosition(Vector3 position)
    {
        stationaryNavPosition = position;
        hasStationaryPosition = true;
    }
    
    private WaspScopeRole ResolveFaction()
    {
        if (deriveFactionFromSpecies && SpeciesInfo != null)
            return SpeciesInfo.ScopeRole;

        return faction;
    }
}
