using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HiveCombatant : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximumHealth = 300f;
    [SerializeField, Min(0f)] private float defence;
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private C_Friendly_Hive_Orc friendlyHive;
    [SerializeField] private C_Enemy_Hive_Orc enemyHive;

    private HexTile ownerHex;
    private float currentHealth;
    private bool enemy;
    private bool alive = true;

    public HexTile OwnerHex => ownerHex;
    public bool IsEnemy => enemy;
    public bool IsAlive => alive;
    public float CurrentHealth => currentHealth;
    public float MaximumHealth => Mathf.Max(1f, maximumHealth);
    public event Action<HiveCombatant> Died;

    private void Awake()
    {
        if (friendlyHive == null)
            friendlyHive = GetComponent<C_Friendly_Hive_Orc>();
        if (enemyHive == null)
            enemyHive = GetComponent<C_Enemy_Hive_Orc>();
        RefreshVisuals();
    }

    public void Initialize(HexTile hex, bool isEnemy)
    {
        ownerHex = hex;
        enemy = isEnemy;
        alive = true;
        currentHealth = MaximumHealth;
        RefreshVisuals();
    }

    public void TakeDamage(float rawDamage)
    {
        if (!alive)
            return;

        float damage = Mathf.Max(1f, rawDamage - defence);
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshVisuals();
        ownerHex?.NotifyCombatInformationChanged();
        if (currentHealth > 0f)
            return;

        alive = false;
        Died?.Invoke(this);
    }

    public void Eliminate()
    {
        if (friendlyHive != null)
            HiveManagement.Instance?.HandleHiveDestroyed(friendlyHive);
        if (enemyHive != null)
            EnemyHiveControl.Instance?.HandleHiveDestroyed(enemyHive);

        Destroy(gameObject);
    }

    private void RefreshVisuals()
    {
        if (healthBarRoot != null)
            healthBarRoot.SetActive(alive && ownerHex != null);

        float normalized = MaximumHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / MaximumHealth);
        if (healthFill != null)
            healthFill.fillAmount = normalized;
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(MaximumHealth)}";
    }
}
