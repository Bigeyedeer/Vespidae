using UnityEngine;
using UnityEngine.AI;

public enum WaspWorkforceState
{
    Idle,
    Travelling,
    Stationed
}

public class WaspControl : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private WaspRoleIconBillboard roleIconBillboard;
    [SerializeField] private WaspCombatant combatant;
    [SerializeField] private GameObject selectionIndicatorRoot;
    [SerializeField] private WaspFunction assignedFunction = WaspFunction.Scout;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField, Min(0.1f)] private float flightHeight = 0.35f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 8f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.25f;
    [SerializeField, Min(1f)] private float turnSpeed = 12f;
    [SerializeField, Min(0.1f)] private float baseAgentSpeed = 3.5f;

    private Vector3 destination;
    private bool hasDestination;
    private bool selected;
    private C_Friendly_Hive_Orc homeHive;
    private HexTile targetHex;
    private HexTile stationedHex;
    private WaspWorkforceState workforceState = WaspWorkforceState.Idle;
    private GameObject navigationProxy;
    private bool returningToHive;
    private Vector3 homePosition;
    private Vector3 stationaryNavPosition;
    private bool hasStationaryPosition;

    public WaspInfo WaspInfo => waspInfo;
    public SB_Wasps_Info SpeciesInfo => waspInfo != null ? waspInfo.SpeciesInfo : null;
    public WaspFunction AssignedFunction => assignedFunction;
    public bool IsSelected => selected;
    public bool HasDestination => hasDestination;
    public Vector3 Destination => destination;
    public C_Friendly_Hive_Orc HomeHive => homeHive;
    public HexTile TargetHex => targetHex;
    public HexTile StationedHex => stationedHex;
    public WaspWorkforceState WorkforceState => workforceState;
    public bool IsAvailable => workforceState == WaspWorkforceState.Idle;
    public NavMeshAgent NavigationAgent => navMeshAgent;
    public WaspCombatant Combatant => combatant;
    public bool IsAlive => combatant == null || combatant.IsAlive;
    public bool IsCombatLocked => stationedHex != null &&
                                  stationedHex.CombatController != null &&
                                  stationedHex.CombatController.IsCombatantEngaged(combatant);

    private void Awake()
    {
        if (waspInfo == null)
            waspInfo = GetComponent<WaspInfo>();

        if (roleIconBillboard == null)
            roleIconBillboard = GetComponentInChildren<WaspRoleIconBillboard>(true);

        if (combatant == null)
            combatant = GetComponent<WaspCombatant>();

        roleIconBillboard?.Initialize(waspInfo);

        if (selectionIndicatorRoot != null)
            selectionIndicatorRoot.SetActive(false);

        if (navMeshAgent == null)
            navMeshAgent = GetComponentInChildren<NavMeshAgent>(true);

        if (navMeshAgent != null)
            navMeshAgent.enabled = false;

        CreateNavigationProxy();
        ConfigureAgent();
    }

    private void Update()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            return;

        RefreshAgentSpeed();

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
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (workforceState == WaspWorkforceState.Travelling &&
            !navMeshAgent.pathPending &&
            navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
            navMeshAgent.remainingDistance <= Mathf.Max(arrivalDistance, navMeshAgent.stoppingDistance))
        {
            CompleteArrival();
        }
    }

    public void SetSelected(bool value)
    {
        selected = value;
        if (selectionIndicatorRoot != null)
            selectionIndicatorRoot.SetActive(value);
    }

    public void SetDestination(Vector3 worldPosition)
    {
        if (!TrySamplePosition(worldPosition, out NavMeshHit hit))
            return;

        destination = hit.position;
        hasDestination = TrySetPath(destination);
    }

    public void ClearDestination()
    {
        hasDestination = false;
    }

    public void SetAssignedFunction(WaspFunction function)
    {
        assignedFunction = function;
        waspInfo?.SetRuntimeAssignment(null, function);
    }

    public bool InitializeFriendlyWasp(
        C_Friendly_Hive_Orc hive,
        WaspFunction function,
        SB_Wasps_Info species)
    {
        homeHive = hive;
        assignedFunction = function;
        waspInfo?.SetRuntimeAssignment(species, function);
        combatant?.Initialize(false);
        workforceState = WaspWorkforceState.Idle;
        targetHex = null;
        stationedHex = null;
        hasDestination = false;
        returningToHive = false;
        homePosition = transform.position;
        bool placed = PlaceOnNavMesh(homePosition);
        if (placed)
        {
            homePosition = navMeshAgent.nextPosition;
            SetStationaryPosition(homePosition);
        }

        return placed;
    }

    public bool DispatchToHex(HexTile hex)
    {
        return DispatchToHex(hex, hex != null ? hex.transform.position : Vector3.zero);
    }

    public bool DispatchToHex(HexTile hex, Vector3 targetPosition)
    {
        if (hex == null || !IsAvailable)
            return false;

        if (!EnsureAgentOnNavMesh() || !TrySamplePosition(targetPosition, out NavMeshHit hit))
            return false;

        if (stationedHex != null)
            stationedHex.UnregisterFriendlyWasp(this);

        targetHex = hex;
        stationedHex = null;
        returningToHive = false;
        hasStationaryPosition = false;
        destination = hit.position;
        hasDestination = TrySetPath(destination);
        if (!hasDestination)
        {
            targetHex = null;
            return false;
        }

        workforceState = WaspWorkforceState.Travelling;
        HiveManagement.Instance?.NotifyWorkforceChanged();
        return true;
    }

    public bool TryIssueGuardMoveOrder(HexTile hex, Vector3 targetPosition)
    {
        if (hex == null || assignedFunction != WaspFunction.Guard || !IsAlive || IsCombatLocked)
            return false;

        if (!EnsureAgentOnNavMesh() || !TrySamplePosition(targetPosition, out NavMeshHit hit))
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!navMeshAgent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        HexTile previousStationedHex = stationedHex;
        previousStationedHex?.UnregisterFriendlyWasp(this);
        targetHex = hex;
        stationedHex = null;
        returningToHive = false;
        hasStationaryPosition = false;
        destination = hit.position;
        hasDestination = navMeshAgent.SetPath(path);
        if (!hasDestination)
        {
            targetHex = null;
            stationedHex = previousStationedHex;
            if (previousStationedHex != null)
                previousStationedHex.RegisterFriendlyWasp(this);
            return false;
        }

        workforceState = WaspWorkforceState.Travelling;
        HiveManagement.Instance?.NotifyWorkforceChanged();
        return true;
    }

    public void ReturnToIdle()
    {
        if (stationedHex != null)
            stationedHex.UnregisterFriendlyWasp(this);

        targetHex = null;
        stationedHex = null;
        hasDestination = false;
        returningToHive = false;
        workforceState = WaspWorkforceState.Idle;
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();

        HiveManagement.Instance?.NotifyWorkforceChanged();
    }

    public bool ReturnToHomeHive()
    {
        if (homeHive == null || workforceState != WaspWorkforceState.Stationed)
            return false;

        if (!EnsureAgentOnNavMesh() || !TrySamplePosition(homePosition, out NavMeshHit hit))
            return false;

        HexTile previousHex = stationedHex;
        stationedHex = null;
        targetHex = null;
        destination = hit.position;
        hasDestination = TrySetPath(destination);
        if (!hasDestination)
        {
            stationedHex = previousHex;
            return false;
        }

        previousHex?.UnregisterFriendlyWasp(this);
        returningToHive = true;
        hasStationaryPosition = false;
        workforceState = WaspWorkforceState.Travelling;
        HiveManagement.Instance?.NotifyWorkforceChanged();
        return true;
    }

    private void ConfigureAgent()
    {
        if (navMeshAgent == null)
            return;

        navMeshAgent.radius = 0.15f;
        navMeshAgent.height = 0.3f;
        RefreshAgentSpeed();
        navMeshAgent.acceleration = 8f;
        navMeshAgent.angularSpeed = 240f;
        navMeshAgent.stoppingDistance = 0.2f;
        navMeshAgent.baseOffset = 0f;
        navMeshAgent.autoRepath = true;
        navMeshAgent.autoBraking = true;
        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
    }

    private void CreateNavigationProxy()
    {
        navigationProxy = new GameObject($"{name}_NavAgent");
        navigationProxy.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        navigationProxy.transform.position = transform.position;
        navigationProxy.transform.rotation = transform.rotation;
        navigationProxy.transform.localScale = Vector3.one;
        navMeshAgent = navigationProxy.AddComponent<NavMeshAgent>();
        navMeshAgent.enabled = false;
    }

    private bool PlaceOnNavMesh(Vector3 worldPosition)
    {
        if (navMeshAgent == null || !TrySamplePosition(worldPosition, out NavMeshHit hit))
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

    private bool EnsureAgentOnNavMesh()
    {
        if (navMeshAgent == null)
            return false;

        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            return true;

        return PlaceOnNavMesh(transform.position);
    }

    private bool TrySamplePosition(Vector3 worldPosition, out NavMeshHit hit)
    {
        return NavMesh.SamplePosition(worldPosition, out hit, navMeshSampleRadius, NavMesh.AllAreas);
    }

    private bool TrySetPath(Vector3 worldPosition)
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!navMeshAgent.CalculatePath(worldPosition, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        return navMeshAgent.SetPath(path);
    }

    private void CompleteArrival()
    {
        navMeshAgent.ResetPath();
        hasDestination = false;

        if (returningToHive)
        {
            returningToHive = false;
            targetHex = null;
            stationedHex = null;
            workforceState = WaspWorkforceState.Idle;
            SetStationaryPosition(navMeshAgent.nextPosition);
            HiveManagement.Instance?.NotifyWorkforceChanged();
            return;
        }

        stationedHex = targetHex;
        targetHex = null;
        workforceState = WaspWorkforceState.Stationed;
        SetStationaryPosition(navMeshAgent.nextPosition);
        stationedHex?.RegisterFriendlyWasp(this);
        HiveManagement.Instance?.NotifyWorkforceChanged();
    }

    private void SetStationaryPosition(Vector3 position)
    {
        stationaryNavPosition = position;
        hasStationaryPosition = true;
    }

    private void OnDestroy()
    {
        selected = false;
        stationedHex?.UnregisterFriendlyWasp(this);
        HiveManagement.Instance?.UnregisterFriendlyWasp(this);
        if (navigationProxy != null)
            Destroy(navigationProxy);
    }

    public void DestroyFromCombat()
    {
        if (stationedHex != null)
            stationedHex.UnregisterFriendlyWasp(this);
        HiveManagement.Instance?.UnregisterFriendlyWasp(this);
        Destroy(gameObject);
    }

    private void RefreshAgentSpeed()
    {
        if (navMeshAgent == null)
            return;

        float multiplier = waspInfo != null ? Mathf.Max(0.1f, waspInfo.MovementSpeedMultiplier) : 1f;
        navMeshAgent.speed = baseAgentSpeed * multiplier;
    }
}
