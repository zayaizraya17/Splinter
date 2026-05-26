using TMPro;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [HideInInspector] public bool alreadyHitThisShot = false;

    [Header("Характеристики врага")]
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 20;
    public float speed = 1f;

    public enum EntityType { Neutral, Aggressive }

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

    // Ссылка на базу данных
    private JsonDatabaseManager databaseManager;

    // Метод для начала преследования (вызывается при атаке игрока)
    public void StartChasing()
    {
        isChasing = true;
        Debug.Log("Нейтральная сущность переходит в режим атаки!");
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Инициализация базы данных
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

            // Проверяем видимость игрока через стены
            bool canSeePlayer = CheckLineOfSight();

            if (entityType == EntityType.Aggressive)
            {
                if (distanceToPlayer <= detectionRange && canSeePlayer)
                {
                    ChasePlayer();
                }
                else if (distanceToPlayer <= detectionRange && !canSeePlayer)
                {
                    // Игрок в радиусе, но за стеной - запоминаем позицию и идем туда (опционально)
                    lastKnownPosition = player.position;
                    isChasing = false; // Или можно оставить true, чтобы идти к последней точке
                }
            }
            else if (entityType == EntityType.Neutral && isChasing)
            {
                if (distanceToPlayer <= detectionRange && canSeePlayer)
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

    /// <summary>
    /// Проверяет, есть ли прямая видимость до игрока (нет ли стен на пути)
    /// </summary>
    bool CheckLineOfSight()
    {
        if (player == null) return false;

        Vector3 direction = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        // Пускаем луч из позиции врага к игроку
        // LayerMask -1 (Default) проверяет все слои. Если стены на другом слое, укажите его явно.
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance))
        {
            // Если попали в игрока - значит видим его (стены не заблокировали)
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                // Отладочная линия (зеленая - видно)
                Debug.DrawLine(transform.position, hit.point, Color.green, 0.1f);
                return true;
            }
            // Если попали во что-то другое (стена, объект) - линия обзора заблокирована
            else
            {
                // Отладочная линия (красная - не видно)
                Debug.DrawLine(transform.position, hit.point, Color.red, 0.1f);
                return false;
            }
        }

        // Если луч ни во что не попал (игрок слишком далеко или баг), считаем что не видим
        return false;
    }

    // Обновление статистики при убийстве врага
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

        // Используем ID текущего пользователя напрямую
        int userId = databaseManager.CurrentUser.Id;

        databaseManager.UpdateKillStatistics(userId);
        Debug.Log($"✅ Статистика убийства обновлена для игрока {userId} (Login: {databaseManager.CurrentUser.Login})");
    }

    public void TakeDamage(float damage, GameObject damageSource)
    {
        currentHealth -= (int)damage;

        if (currentHealth <= 0)
        {
            Die();

            // Обновление статистики
            UpdateStatisticsOnKill(damageSource);
        }

        // Если это нейтральная сущность и она ещё не преследует игрока - начинаем преследование
        if (entityType == EntityType.Neutral && !isChasing)
        {
            isChasing = true;
            if (player != null)
            {
                lastKnownPosition = player.position;
            }
            Debug.Log("Нейтральная сущность атакована и переходит в режим атаки!");
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
