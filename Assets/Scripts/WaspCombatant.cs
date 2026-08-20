using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaspCombatant : MonoBehaviour
{
    [SerializeField] private WaspInfo waspInfo;
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private WaspRoleIconBillboard roleIconBillboard;

    private float currentHealth;
    private float attackCooldownRemaining;
    private bool initialized;
    private bool alive = true;
    private bool enemy;

    public WaspInfo WaspInfo => waspInfo;
    public WaspFunction Function => waspInfo != null ? waspInfo.FunctionRole : WaspFunction.Scout;
    public bool IsAttacker => Function == WaspFunction.Guard;
    public bool IsEnemy => enemy;
    public bool IsAlive => alive;
    public float CurrentHealth => currentHealth;
    public float MaximumHealth => Mathf.Max(1f, waspInfo != null ? waspInfo.MaximumHealth : 100f);
    public float AttackDamage => Mathf.Max(0f, waspInfo != null ? waspInfo.AttackDamage : 10f);
    public float Defence => Mathf.Max(0f, waspInfo != null ? waspInfo.DefenceMultiplier : 1f);
    public float AttacksPerSecond => Mathf.Max(0.05f, waspInfo != null ? waspInfo.AttackSpeedMultiplier : 1f);
    public event Action<WaspCombatant> Died;

    private void Awake()
    {
        if (waspInfo == null)
            waspInfo = GetComponent<WaspInfo>();

        if (roleIconBillboard == null)
            roleIconBillboard = GetComponentInChildren<WaspRoleIconBillboard>(true);

        RefreshVisuals();
    }

    public void Initialize(bool isEnemy)
    {
        enemy = isEnemy;
        initialized = true;
        alive = true;
        currentHealth = MaximumHealth;
        attackCooldownRemaining = 0f;
        RefreshVisuals();
    }

    public void RefreshStats(bool restoreHealth)
    {
        if (!initialized)
            return;

        currentHealth = restoreHealth
            ? MaximumHealth
            : Mathf.Clamp(currentHealth, 0f, MaximumHealth);
        RefreshVisuals();
    }

    public bool TickAttack(WaspCombatant target, float deltaTime)
    {
        if (!alive || !IsAttacker || target == null || !target.IsAlive)
            return false;

        attackCooldownRemaining -= Mathf.Max(0f, deltaTime);
        if (attackCooldownRemaining > 0f)
            return false;

        attackCooldownRemaining = 1f / AttacksPerSecond;
        LungeAt(target.transform.position);
        target.TakeDamage(AttackDamage);
        return true;
    }

    public void TickAttack(HiveCombatant target, float deltaTime)
    {
        if (!alive || !IsAttacker || target == null || !target.IsAlive)
            return;

        attackCooldownRemaining -= Mathf.Max(0f, deltaTime);
        if (attackCooldownRemaining > 0f)
            return;

        attackCooldownRemaining = 1f / AttacksPerSecond;
        LungeAt(target.transform.position);
        target.TakeDamage(AttackDamage);
    }

    /// <summary>
    /// Throws this wasp at what it just hit. Resolved damage is unchanged - this is presentation only,
    /// attached on first use so no prefab has to carry the component up front.
    /// </summary>
    private void LungeAt(Vector3 targetPosition)
    {
        if (lunge == null)
            lunge = C_CombatLunge.Attach(this);

        if (lunge != null)
            lunge.Strike(targetPosition);
    }

    private C_CombatLunge lunge;

    public void ResetAttackCooldown()
    {
        attackCooldownRemaining = 0f;
    }

    public void TakeDamage(float rawDamage)
    {
        if (!alive)
            return;

        float damage = Mathf.Max(1f, rawDamage - Defence);
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshVisuals();
        if (currentHealth > 0f)
            return;

        alive = false;
        Died?.Invoke(this);
    }

    public void Eliminate()
    {
        WaspControl friendly = GetComponent<WaspControl>();
        if (friendly != null)
        {
            friendly.DestroyFromCombat();
            return;
        }

        EnemyWaspControl hostile = GetComponent<EnemyWaspControl>();
        if (hostile != null)
        {
            hostile.DestroyFromCombat();
            return;
        }

        Destroy(gameObject);
    }

    private void RefreshVisuals()
    {
        bool visible = initialized && alive && IsAttacker;
        if (healthBarRoot != null)
            healthBarRoot.SetActive(visible);

        float normalized = MaximumHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / MaximumHealth);
        if (healthFill != null)
            healthFill.fillAmount = normalized;
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(MaximumHealth)}";
    }
}
