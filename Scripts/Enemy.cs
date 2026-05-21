using UnityEngine;

public class Enemy : MonoBehaviour
{
    [HideInInspector] public bool alreadyHitThisShot = false;

    [Header("Характеристики врага")]
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 20;
    public float speed = 1f;

    public enum EntityType { Peaceful, Neutral, Aggressive }

    [Header("Тип сущности")]
    public EntityType entityType;

    [Header("Атака")]
    public float attackCooldown = 5f;
    private float lastAttackTime = 0f;
    private bool canAttack = true;

    private Transform player;
    private bool isDead = false;
    private bool isChasing = false;
    private Vector3 lastKnownPosition;
    private float detectionRange = 10f;
    private float attackRange = 2f;

    // === ДОБАВЛЕНО: Ссылка на базу данных ===
    private JsonDatabaseManager databaseManager;

    void Start()
    {
        currentHealth = maxHealth;

        // === Инициализация базы данных ===
        databaseManager = FindFirstObjectByType<JsonDatabaseManager>();
        if (databaseManager == null)
        {
            Debug.LogWarning("⚠️ JsonDatabaseManager не найден! Статистика не будет сохраняться.");
        }

        // Ищем игрока по тегу
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Враг нашёл игрока!");
        }
        else
        {
            Debug.LogError("Враг НЕ нашёл игрока! Проверь тег Player на объекте игрока!");
        }
    }

    void Update()
    {
        // Если время остановлено (Time.timeScale = 0) - ничего не делаем
        if (Time.timeScale == 0)
            return;

        if (isDead) return;

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (entityType == EntityType.Aggressive)
            {
                if (distanceToPlayer <= detectionRange)
                {
                    ChasePlayer();
                }
            }
            else if (entityType == EntityType.Neutral && isChasing)
            {
                if (distanceToPlayer <= detectionRange)
                {
                    ChasePlayer();
                }
                else
                {
                    isChasing = false;
                }
            }
        }
    }

    // === ИСПРАВЛЕНО: Добавлена проверка databaseManager ===
    void UpdateStatisticsOnKill(GameObject killer)
    {
        if (databaseManager == null)
        {
            Debug.LogWarning("⚠️ DatabaseManager не найден!");
            return;
        }

        if (!databaseManager.IsLoggedIn || databaseManager.CurrentUser == null)
        {
            Debug.LogWarning("⚠️ Пользователь не вошёл в систему!");
            return;
        }

        // === ИСПОЛЬЗУЕМ ID ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ НАПРЯМУЮ ===
        int userId = databaseManager.CurrentUser.Id;

        databaseManager.UpdateKillStatistics(userId);
        Debug.Log($"✅ Статистика убийства обновлена для игрока {userId} (Login: {databaseManager.CurrentUser.Login})");
    }


    public void TakeDamage(float damage, GameObject damageSource)
    {
        currentHealth -= (int)damage; // === ИСПРАВЛЕНО: явное преобразование float -> int ===

        if (currentHealth <= 0)
        {
            Die();

            // === ОБНОВЛЕНИЕ СТАТИСТИКИ ===
            UpdateStatisticsOnKill(damageSource);
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            // Двигаемся к игроку
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Не двигаемся по Y

            transform.position += direction * speed * Time.deltaTime;

            // Поворачиваемся к игроку
            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);
        }
        else
        {
            // Атакуем игрока (только если кулдаун прошёл)
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        // Проверяем кулдаун
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return; // Ещё рано атаковать
        }

        Debug.Log("Враг атакует игрока!");

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("Враг не может найти игрока для атаки!");
            return;
        }

        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        EffectManager effects = playerObject.GetComponent<EffectManager>();

        if (playerController == null)
        {
            Debug.LogError("На игроке нет PlayerController!");
            return;
        }

        // Наносим урон
        playerController.TakeDamage(damage);
        Debug.Log($"Враг нанёс {damage} урона игроку! (HP: {playerController.currentHealth})");

        // Эффект кровотечения от Демона (30% шанс)
        if (gameObject.name.ToLower().Contains("demon") || entityType == EntityType.Aggressive)
        {
            if (Random.value < 0.3f && effects != null)
            {
                effects.StartBleeding();
                Debug.Log("Враг наложил кровотечение!");
            }
        }

        // Обновляем время последней атаки
        lastAttackTime = Time.time;
        Debug.Log($"Следующая атака через {attackCooldown} сек");
    }


    void Die()
    {
        isDead = true;
        Debug.Log("Враг погиб!");

        // Сообщаем EffectManager об убийстве
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            EffectManager effects = playerObject.GetComponent<EffectManager>();
            if (effects != null)
            {
                effects.RegisterKill();
            }
        }

        Destroy(gameObject, 2f);
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}