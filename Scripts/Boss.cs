using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Boss : MonoBehaviour
{
    [Header("Характеристики Босса")]
    public int maxHealth = 500;
    public int currentHealth;
    public float speed = 3.5f;

    [Header("Атаки Босса")]
    public int meleeDamage = 30;
    public int heavyDamage = 50;
    public int rangedDamage = 25;
    public float meleeRange = 3.5f;
    public float rangedRange = 20f;

    [Header("Тайминги атак")]
    public float meleeCooldown = 2f;
    public float heavyCooldown = 5f;
    public float rangedCooldown = 3f;

    private float meleeNextAttackTime = 0f;
    private float heavyNextAttackTime = 0f;
    private float rangedNextAttackTime = 0f;

    [Header("Дистанции для атак")]
    public float meleeFollowDistance = 2.5f;
    public float rangedRetreatDistance = 12f;

    public enum BossState { Idle, Chase, AttackMelee, AttackHeavy, AttackRanged }

    [Header("Состояния")]
    public BossState currentState = BossState.Idle;

    private Transform player;
    private bool isDead = false;
    private float detectionRange = 15f;
    private Vector3 lastKnownPosition;
    private bool isChasing = false;

    private CharacterController controller;

    [Header("Эффекты")]
    public GameObject rangedProjectilePrefab;
    public Transform projectileSpawnPoint;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("На Боссе отсутствует CharacterController!");
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Босс нашёл игрока!");
        }
        else
        {
            Debug.LogWarning("Босс НЕ нашёл игрока при старте! Проверь тег Player.");
        }

        if (projectileSpawnPoint == null)
        {
            GameObject spawnObj = new GameObject("ProjectileSpawnPoint");
            spawnObj.transform.parent = transform;
            spawnObj.transform.localPosition = new Vector3(0, 1.5f, 1f);
            projectileSpawnPoint = spawnObj.transform;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0 || isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = CheckLineOfSight(player.position);

        if (canSeePlayer && distanceToPlayer <= detectionRange)
        {
            lastKnownPosition = player.position;
            isChasing = true;

            if (distanceToPlayer <= meleeRange)
            {
                if (distanceToPlayer > meleeFollowDistance)
                {
                    MoveTowardsPlayer(distanceToPlayer);
                    currentState = BossState.Chase;
                }
                else
                {
                    LookAtPlayer();
                    currentState = BossState.AttackMelee;
                }
                TryPerformAttack(distanceToPlayer);
            }
            else if (distanceToPlayer <= rangedRange)
            {
                if (distanceToPlayer < rangedRetreatDistance)
                {
                    MoveAwayFromPlayer(distanceToPlayer);
                    currentState = BossState.Chase;
                }
                else if (distanceToPlayer > rangedRetreatDistance + 2f)
                {
                    MoveTowardsPlayer(distanceToPlayer);
                    currentState = BossState.Chase;
                }
                else
                {
                    LookAtPlayer();
                    currentState = BossState.AttackRanged;
                }
                TryPerformRangedAttack();
            }
            else
            {
                MoveTowardsPlayer(distanceToPlayer);
                currentState = BossState.Chase;
            }
        }
        else if (isChasing)
        {
            MoveToLastKnownPosition();
        }
        else
        {
            currentState = BossState.Idle;
        }
    }

    void MoveTowardsPlayer(float distance)
    {
        if (player == null || controller == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (!Physics.Raycast(transform.position, direction, 1.5f))
        {
            controller.Move(direction * speed * Time.deltaTime);
            LookAtPlayer();
        }
        else
        {
            Vector3 rightDir = Vector3.Cross(direction, Vector3.up).normalized;
            controller.Move(rightDir * speed * 0.5f * Time.deltaTime);
        }
    }

    void MoveAwayFromPlayer(float distance)
    {
        if (player == null || controller == null) return;

        Vector3 direction = (transform.position - player.position).normalized;
        direction.y = 0;

        if (!Physics.Raycast(transform.position, direction, 1.5f))
        {
            controller.Move(direction * speed * Time.deltaTime);
            LookAtPlayer();
        }
        else
        {
            Vector3 rightDir = Vector3.Cross(direction, Vector3.up).normalized;
            controller.Move(rightDir * speed * 0.5f * Time.deltaTime);
            LookAtPlayer();
        }
    }

    void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);
    }

    bool CheckLineOfSight(Vector3 targetPosition)
    {
        RaycastHit hit;
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        // Маска слоя игрока или просто проверка по тегу внутри хита
        if (Physics.Raycast(transform.position + Vector3.up * 1f, direction, out hit, distance))
        {
            if (hit.transform.CompareTag("Player") || hit.transform.IsChildOf(player))
            {
                return true;
            }
            Debug.DrawLine(transform.position + Vector3.up * 1f, hit.point, Color.red, 0.1f);
            return false;
        }
        return true;
    }

    void MoveToLastKnownPosition()
    {
        if (controller == null) return;

        float distanceToLastPos = Vector3.Distance(transform.position, lastKnownPosition);

        if (distanceToLastPos > 1f)
        {
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            direction.y = 0;
            controller.Move(direction * speed * Time.deltaTime);
            Vector3 lookPos = new Vector3(lastKnownPosition.x, transform.position.y, lastKnownPosition.z);
            transform.LookAt(lookPos);
        }
        else
        {
            isChasing = false;
            currentState = BossState.Idle;
        }
    }

    void TryPerformAttack(float distanceToPlayer)
    {
        float currentTime = Time.time;

        if (currentTime >= heavyNextAttackTime && Random.value < 0.3f)
        {
            PerformHeavyAttack();
        }
        else if (currentTime >= meleeNextAttackTime)
        {
            PerformMeleeAttack();
        }
    }

    void TryPerformRangedAttack()
    {
        float currentTime = Time.time;
        if (currentTime >= rangedNextAttackTime)
        {
            PerformRangedAttack();
        }
    }

    void PerformMeleeAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: Ближняя атака!");
        currentState = BossState.AttackMelee;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= meleeRange * 1.2f)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(meleeDamage);
                Debug.Log($"Босс нанёс {meleeDamage} урона (ближняя)!");
            }
        }
        meleeNextAttackTime = Time.time + meleeCooldown;
    }

    void PerformHeavyAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: МОЩНАЯ АТАКА!");
        currentState = BossState.AttackHeavy;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= meleeRange * 1.8f)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(heavyDamage);
                Debug.Log($"Босс нанёс {heavyDamage} урона (мощная)!");
            }
        }
        heavyNextAttackTime = Time.time + heavyCooldown;
    }

    void PerformRangedAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: Дальнобойная атака!");
        currentState = BossState.AttackRanged;

        if (rangedProjectilePrefab != null)
        {
            GameObject projectile = Instantiate(rangedProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            RangedProjectile projScript = projectile.GetComponent<RangedProjectile>();

            if (projScript != null)
            {
                projScript.SetTarget(player, rangedDamage);
            }
            else
            {
                StartCoroutine(MoveProjectileToPlayer(projectile));
            }
        }
        else
        {
            RaycastHit hit;
            Vector3 direction = (player.position - transform.position).normalized;
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, direction, out hit, rangedRange))
            {
                if (hit.transform.CompareTag("Player") || hit.transform.IsChildOf(player))
                {
                    PlayerController playerCtrl = player.GetComponent<PlayerController>();
                    if (playerCtrl != null) playerCtrl.TakeDamage(rangedDamage);
                }
            }
        }
        rangedNextAttackTime = Time.time + rangedCooldown;
    }

    System.Collections.IEnumerator MoveProjectileToPlayer(GameObject projectile)
    {
        float projSpeed = 10f;
        while (projectile != null && player != null)
        {
            Vector3 direction = (player.position - projectile.transform.position).normalized;
            projectile.transform.position += direction * projSpeed * Time.deltaTime;
            projectile.transform.LookAt(player.position);

            if (Vector3.Distance(projectile.transform.position, player.position) < 1f)
            {
                PlayerController playerCtrl = player.GetComponent<PlayerController>();
                if (playerCtrl != null) playerCtrl.TakeDamage(rangedDamage);
                Destroy(projectile);
                break;
            }
            yield return null;
        }
    }

 
    public void TakeDamage(float damage, GameObject damageSource)
    {
        currentHealth -= (int)damage;

        if (currentHealth <= 0)
        {
            Die();

        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("БОСС ПОГИБ!");

        // Находим игрока для статистики
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            EffectManager effects = playerObject.GetComponent<EffectManager>();
            if (effects != null) effects.RegisterKill();

            JsonDatabaseManager dbManager = FindFirstObjectByType<JsonDatabaseManager>();
            if (dbManager != null && dbManager.IsLoggedIn)
            {
                dbManager.UpdateKillStatistics(dbManager.CurrentUser.Id);
            }
        }

        // Отключаем коллайдеры и физику, чтобы босс не падал сквозь пол или не бился
        CharacterController cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangedRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, meleeFollowDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangedRetreatDistance);
    }
}

// Скрипт снаряда
public class RangedProjectile : MonoBehaviour
{
    private Transform target;
    private int damage;
    private float speed = 15f;
    private float lifetime = 5f;

    public void SetTarget(Transform target, int damage)
    {
        this.target = target;
        this.damage = damage;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.LookAt(target.position);
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        if (target != null && Vector3.Distance(transform.position, target.position) < 1f)
        {
            PlayerController playerCtrl = target.GetComponent<PlayerController>();
            if (playerCtrl != null) playerCtrl.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    // Добавляем обработку столкновений для снаряда, если он пролетит мимо цели но врежется в босса (на всякий случай)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playerCtrl = other.GetComponent<PlayerController>();
            if (playerCtrl != null) playerCtrl.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
