<<<<<<< HEAD
﻿using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Характеристики Босса")]
    public int maxHealth = 500;
    public int currentHealth;
    public float speed = 1.5f;

    [Header("Атаки Босса")]
    public int meleeDamage = 30;        // Урон ближней атаки
    public int heavyDamage = 50;        // Урон мощной атаки
    public int rangedDamage = 25;       // Урон дальнобойной атаки
    public float meleeRange = 3f;       // Радиус ближней атаки
    public float rangedRange = 20f;     // Дальность дальнобойной атаки

    [Header("Тайминги атак")]
    public float meleeCooldown = 2f;    // Кулдаун ближней атаки
    public float heavyCooldown = 5f;    // Кулдаун мощной атаки
    public float rangedCooldown = 3f;   // Кулдаун дальнобойной атаки

    private float meleeNextAttackTime = 0f;
    private float heavyNextAttackTime = 0f;
    private float rangedNextAttackTime = 0f;

    public enum BossState { Idle, Chase, AttackMelee, AttackHeavy, AttackRanged }

    [Header("Состояния")]
    public BossState currentState = BossState.Idle;

    private Transform player;
    private bool isDead = false;
    private float detectionRange = 15f;
    private float attackRange = 5f;
    private Vector3 lastKnownPosition;
    private bool isChasing = false;

    [Header("Эффекты")]
    public GameObject rangedProjectilePrefab;  // Префаб снаряда для дальнобойной атаки
    public Transform projectileSpawnPoint;     // Точка spawns снаряда

    void Start()
    {
        currentHealth = maxHealth;

        // Ищем игрока
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Босс нашёл игрока!");
        }
        else
        {
            Debug.LogError("Босс НЕ нашёл игрока! Проверь тег Player!");
        }

        // Если нет точки spawns для снаряда, создаём её
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
        if (Time.timeScale == 0 || isDead) return;

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool canSeePlayer = CheckLineOfSight(player.position);

            // Логика обнаружения игрока
            if (canSeePlayer && distanceToPlayer <= detectionRange)
            {
                lastKnownPosition = player.position;
                isChasing = true;
                currentState = BossState.Chase;

                // Выбор атаки в зависимости от дистанции
                if (distanceToPlayer <= meleeRange)
                {
                    // Ближняя атака или мощная атака
                    TryPerformAttack(distanceToPlayer);
                }
                else if (distanceToPlayer <= rangedRange)
                {
                    // Дальнобойная атака
                    TryPerformRangedAttack();
                }
                else
                {
                    // Приближаемся к игроку
                    ChasePlayer();
                }
            }
            else if (isChasing)
            {
                // Игрок потерян из виду, идём к последней известной позиции
                MoveToLastKnownPosition();
            }
            else
            {
                currentState = BossState.Idle;
            }
        }
    }

    bool CheckLineOfSight(Vector3 targetPosition)
    {
        RaycastHit hit;
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (Physics.Raycast(transform.position, direction, out hit, distance))
        {
            if (hit.transform != player)
            {
                return false;
            }
        }
        return true;
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            // Проверяем, нет ли стены на пути
            Vector3 movePosition = transform.position + direction * speed * Time.deltaTime;
            if (!Physics.CheckSphere(movePosition, 0.5f))
            {
                transform.position = movePosition;
            }


            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);

            currentState = BossState.Chase;
        }
    }

    void MoveToLastKnownPosition()
    {
        float distanceToLastPos = Vector3.Distance(transform.position, lastKnownPosition);

        if (distanceToLastPos > 1f)
        {
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            direction.y = 0;

            // Проверяем, нет ли стены на пути
            Vector3 movePosition = transform.position + direction * speed * Time.deltaTime;
            if (!Physics.CheckSphere(movePosition, 0.5f))
            {
                transform.position = movePosition;
            }

            Vector3 lookPos = new Vector3(lastKnownPosition.x, transform.position.y, lastKnownPosition.z);
            transform.LookAt(lookPos);
        }
        else
        {
            isChasing = false;
            currentState = BossState.Idle;
            Debug.Log("Босс потерял игрока и вернулся в патрулирование");
        }
    }

    void TryPerformAttack(float distanceToPlayer)
    {
        float currentTime = Time.time;

        // Приоритет атак: мощная -> ближняя
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

        // Проверяем попадание
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= meleeRange)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(meleeDamage);
                Debug.Log($"Босс нанёс {meleeDamage} урона игроку (ближняя атака)!");
            }
        }

        meleeNextAttackTime = Time.time + meleeCooldown;
    }

    void PerformHeavyAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: МОЩНАЯ АТАКА!");
        currentState = BossState.AttackHeavy;

        // Мощная атака имеет больший радиус
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= meleeRange * 1.5f)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(heavyDamage);
                Debug.Log($"Босс нанёс {heavyDamage} урона игроку (мощная атака)!");

                // Эффект отбрасывания
                Vector3 knockbackDirection = (player.position - transform.position).normalized;
                CharacterController playerCC = player.GetComponent<CharacterController>();
                if (playerCC != null)
                {
                    playerCC.Move(knockbackDirection * 5f);
                }
            }
        }

        heavyNextAttackTime = Time.time + heavyCooldown;
    }

    void PerformRangedAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: Дальнобойная атака!");
        currentState = BossState.AttackRanged;

        // Создаём снаряд
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
                // Если нет скрипта снаряда, просто двигаем его к игроку
                StartCoroutine(MoveProjectileToPlayer(projectile));
            }
        }
        else
        {
            // Альтернатива: мгновенная атака лучом
            RaycastHit hit;
            Vector3 direction = (player.position - transform.position).normalized;

            if (Physics.Raycast(transform.position, direction, out hit, rangedRange))
            {
                if (hit.transform == player)
                {
                    PlayerController playerCtrl = player.GetComponent<PlayerController>();
                    if (playerCtrl != null)
                    {
                        playerCtrl.TakeDamage(rangedDamage);
                        Debug.Log($"Босс нанёс {rangedDamage} урона игроку (дальнобойная атака)!");
                    }
                }
            }
        }

        rangedNextAttackTime = Time.time + rangedCooldown;
    }

    System.Collections.IEnumerator MoveProjectileToPlayer(GameObject projectile)
    {
        float speed = 10f;
        while (projectile != null && player != null)
        {
            Vector3 direction = (player.position - projectile.transform.position).normalized;
            projectile.transform.position += direction * speed * Time.deltaTime;

            // Проверяем расстояние до игрока
            if (Vector3.Distance(projectile.transform.position, player.position) < 1f)
            {
                PlayerController playerCtrl = player.GetComponent<PlayerController>();
                if (playerCtrl != null)
                {
                    playerCtrl.TakeDamage(rangedDamage);
                    Debug.Log($"Босс нанёс {rangedDamage} урона игроку (снаряд)!");
                }
                Destroy(projectile);
                break;
            }

            yield return null;
        }
    }

    public void TakeDamage(float damage, GameObject damageSource)
    {
        currentHealth -= (int)damage;
        Debug.Log($"Босс получил урон: {damage}. Здоровье: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("БОСС ПОГИБ!");

        // Обновляем статистику убийства
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            EffectManager effects = playerObject.GetComponent<EffectManager>();
            if (effects != null)
            {
                effects.RegisterKill();
            }

            // Обновляем статистику в базе данных
            JsonDatabaseManager dbManager = FindFirstObjectByType<JsonDatabaseManager>();
            if (dbManager != null && dbManager.IsLoggedIn)
            {
                dbManager.UpdateKillStatistics(dbManager.CurrentUser.Id);
            }
        }

        // Эффект смерти (можно добавить частицы, звук и т.д.)
        Debug.Log("=== БОСС ПОВЕРЖЕН! ===");

        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Радиус ближней атаки
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Радиус дальнобойной атаки
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangedRange);
    }
}

// Скрипт для снаряда дальнобойной атаки
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

            // Поворачиваем снаряд по направлению движения
            transform.LookAt(target.position);
        }
        else
        {
            // Если цель потеряна, летим вперёд
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        // Проверяем столкновение с игроком
        if (target != null && Vector3.Distance(transform.position, target.position) < 1f)
        {
            PlayerController playerCtrl = target.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
=======
﻿using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Характеристики Босса")]
    public int maxHealth = 500;
    public int currentHealth;
    public float speed = 1.5f;

    [Header("Атаки Босса")]
    public int meleeDamage = 30;        // Урон ближней атаки
    public int heavyDamage = 50;        // Урон мощной атаки
    public int rangedDamage = 25;       // Урон дальнобойной атаки
    public float meleeRange = 3f;       // Радиус ближней атаки
    public float rangedRange = 20f;     // Дальность дальнобойной атаки

    [Header("Тайминги атак")]
    public float meleeCooldown = 2f;    // Кулдаун ближней атаки
    public float heavyCooldown = 5f;    // Кулдаун мощной атаки
    public float rangedCooldown = 3f;   // Кулдаун дальнобойной атаки

    private float meleeNextAttackTime = 0f;
    private float heavyNextAttackTime = 0f;
    private float rangedNextAttackTime = 0f;

    public enum BossState { Idle, Chase, AttackMelee, AttackHeavy, AttackRanged }

    [Header("Состояния")]
    public BossState currentState = BossState.Idle;

    private Transform player;
    private bool isDead = false;
    private float detectionRange = 15f;
    private float attackRange = 5f;
    private Vector3 lastKnownPosition;
    private bool isChasing = false;

    [Header("Эффекты")]
    public GameObject rangedProjectilePrefab;  // Префаб снаряда для дальнобойной атаки
    public Transform projectileSpawnPoint;     // Точка spawns снаряда

    void Start()
    {
        currentHealth = maxHealth;

        // Ищем игрока
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Босс нашёл игрока!");
        }
        else
        {
            Debug.LogError("Босс НЕ нашёл игрока! Проверь тег Player!");
        }

        // Если нет точки spawns для снаряда, создаём её
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
        if (Time.timeScale == 0 || isDead) return;

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool canSeePlayer = CheckLineOfSight(player.position);

            // Логика обнаружения игрока
            if (canSeePlayer && distanceToPlayer <= detectionRange)
            {
                lastKnownPosition = player.position;
                isChasing = true;
                currentState = BossState.Chase;

                // Выбор атаки в зависимости от дистанции
                if (distanceToPlayer <= meleeRange)
                {
                    // Ближняя атака или мощная атака
                    TryPerformAttack(distanceToPlayer);
                }
                else if (distanceToPlayer <= rangedRange)
                {
                    // Дальнобойная атака
                    TryPerformRangedAttack();
                }
                else
                {
                    // Приближаемся к игроку
                    ChasePlayer();
                }
            }
            else if (isChasing)
            {
                // Игрок потерян из виду, идём к последней известной позиции
                MoveToLastKnownPosition();
            }
            else
            {
                currentState = BossState.Idle;
            }
        }
    }

    bool CheckLineOfSight(Vector3 targetPosition)
    {
        RaycastHit hit;
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (Physics.Raycast(transform.position, direction, out hit, distance))
        {
            if (hit.transform != player)
            {
                return false;
            }
        }
        return true;
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            // Проверяем, нет ли стены на пути
            Vector3 movePosition = transform.position + direction * speed * Time.deltaTime;
            if (!Physics.CheckSphere(movePosition, 0.5f))
            {
                transform.position = movePosition;
            }


            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);

            currentState = BossState.Chase;
        }
    }

    void MoveToLastKnownPosition()
    {
        float distanceToLastPos = Vector3.Distance(transform.position, lastKnownPosition);

        if (distanceToLastPos > 1f)
        {
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            direction.y = 0;

            // Проверяем, нет ли стены на пути
            Vector3 movePosition = transform.position + direction * speed * Time.deltaTime;
            if (!Physics.CheckSphere(movePosition, 0.5f))
            {
                transform.position = movePosition;
            }

            Vector3 lookPos = new Vector3(lastKnownPosition.x, transform.position.y, lastKnownPosition.z);
            transform.LookAt(lookPos);
        }
        else
        {
            isChasing = false;
            currentState = BossState.Idle;
            Debug.Log("Босс потерял игрока и вернулся в патрулирование");
        }
    }

    void TryPerformAttack(float distanceToPlayer)
    {
        float currentTime = Time.time;

        // Приоритет атак: мощная -> ближняя
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

        // Проверяем попадание
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= meleeRange)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(meleeDamage);
                Debug.Log($"Босс нанёс {meleeDamage} урона игроку (ближняя атака)!");
            }
        }

        meleeNextAttackTime = Time.time + meleeCooldown;
    }

    void PerformHeavyAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: МОЩНАЯ АТАКА!");
        currentState = BossState.AttackHeavy;

        // Мощная атака имеет больший радиус
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= meleeRange * 1.5f)
        {
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(heavyDamage);
                Debug.Log($"Босс нанёс {heavyDamage} урона игроку (мощная атака)!");

                // Эффект отбрасывания
                Vector3 knockbackDirection = (player.position - transform.position).normalized;
                CharacterController playerCC = player.GetComponent<CharacterController>();
                if (playerCC != null)
                {
                    playerCC.Move(knockbackDirection * 5f);
                }
            }
        }

        heavyNextAttackTime = Time.time + heavyCooldown;
    }

    void PerformRangedAttack()
    {
        if (player == null) return;

        Debug.Log("БОСС: Дальнобойная атака!");
        currentState = BossState.AttackRanged;

        // Создаём снаряд
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
                // Если нет скрипта снаряда, просто двигаем его к игроку
                StartCoroutine(MoveProjectileToPlayer(projectile));
            }
        }
        else
        {
            // Альтернатива: мгновенная атака лучом
            RaycastHit hit;
            Vector3 direction = (player.position - transform.position).normalized;

            if (Physics.Raycast(transform.position, direction, out hit, rangedRange))
            {
                if (hit.transform == player)
                {
                    PlayerController playerCtrl = player.GetComponent<PlayerController>();
                    if (playerCtrl != null)
                    {
                        playerCtrl.TakeDamage(rangedDamage);
                        Debug.Log($"Босс нанёс {rangedDamage} урона игроку (дальнобойная атака)!");
                    }
                }
            }
        }

        rangedNextAttackTime = Time.time + rangedCooldown;
    }

    System.Collections.IEnumerator MoveProjectileToPlayer(GameObject projectile)
    {
        float speed = 10f;
        while (projectile != null && player != null)
        {
            Vector3 direction = (player.position - projectile.transform.position).normalized;
            projectile.transform.position += direction * speed * Time.deltaTime;

            // Проверяем расстояние до игрока
            if (Vector3.Distance(projectile.transform.position, player.position) < 1f)
            {
                PlayerController playerCtrl = player.GetComponent<PlayerController>();
                if (playerCtrl != null)
                {
                    playerCtrl.TakeDamage(rangedDamage);
                    Debug.Log($"Босс нанёс {rangedDamage} урона игроку (снаряд)!");
                }
                Destroy(projectile);
                break;
            }

            yield return null;
        }
    }

    public void TakeDamage(float damage, GameObject damageSource)
    {
        currentHealth -= (int)damage;
        Debug.Log($"Босс получил урон: {damage}. Здоровье: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("БОСС ПОГИБ!");

        // Обновляем статистику убийства
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            EffectManager effects = playerObject.GetComponent<EffectManager>();
            if (effects != null)
            {
                effects.RegisterKill();
            }

            // Обновляем статистику в базе данных
            JsonDatabaseManager dbManager = FindFirstObjectByType<JsonDatabaseManager>();
            if (dbManager != null && dbManager.IsLoggedIn)
            {
                dbManager.UpdateKillStatistics(dbManager.CurrentUser.Id);
            }
        }

        // Эффект смерти (можно добавить частицы, звук и т.д.)
        Debug.Log("=== БОСС ПОВЕРЖЕН! ===");

        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Радиус ближней атаки
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Радиус дальнобойной атаки
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangedRange);
    }
}

// Скрипт для снаряда дальнобойной атаки
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

            // Поворачиваем снаряд по направлению движения
            transform.LookAt(target.position);
        }
        else
        {
            // Если цель потеряна, летим вперёд
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        // Проверяем столкновение с игроком
        if (target != null && Vector3.Distance(transform.position, target.position) < 1f)
        {
            PlayerController playerCtrl = target.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }

}
