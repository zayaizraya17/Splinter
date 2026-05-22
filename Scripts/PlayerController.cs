using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [HideInInspector] public int userId = 0; 

    [Header("Настройки Движения")]
    public float walkSpeed = 1f;      // 1 м/сек
    public float runSpeed = 2f;       // 2 м/сек
    public float jumpHeight = 1.5f;   // Высота прыжка (подбери под себя)
    public float gravity = -9.81f;    // Сила гравитации

    [Header("Настройки Выносливости")]
    public float maxStamina = 100f;
    public float staminaCostRun = 5f; // 5 ед в секунду (50 за 10 сек)
    public float staminaCostJump = 5f;
    public float staminaRegen = 1f;  // Скорость восстановления
    public float currentStamina;

    [Header("Настройки Здоровья")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isBleeding = false; // Для эффекта кровотечения

    [Header("Компоненты")]
    public Transform cameraTransform; // Сюда перетащи камеру
    public float mouseSensitivity = 2f;


    [Header("Атака рукой")]
    public float handDamage = 5f;
    public float handRange = 2.5f;
    public float handAttackCooldown = 0.5f;
    private float handNextAttackTime = 0f;

    // Внутренние переменные
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float rotationY = 0f; // Для поворота камеры вверх/вниз (если нужно)

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        currentHealth = maxHealth;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Инициализация userId с повторными попытками
        Invoke(nameof(TryInitializeUserId), 0.1f);
    }

    void TryInitializeUserId()
    {
        // Если userId уже установлен - выходим
        if (userId != 0)
        {
            Debug.Log($"✅ userId уже установлен: {userId}");
            return;
        }

        var db = FindFirstObjectByType<JsonDatabaseManager>();

        if (db == null)
        {
            Debug.LogWarning("⚠️ JsonDatabaseManager не найден! Повторная попытка...");
            Invoke(nameof(TryInitializeUserId), 0.5f);
            return;
        }

        if (!db.IsLoggedIn)
        {
            Debug.LogWarning("⚠️ Пользователь не вошёл в систему! Повторная попытка...");
            Invoke(nameof(TryInitializeUserId), 0.5f);
            return;
        }

        if (db.CurrentUser == null)
        {
            Debug.LogWarning("⚠️ CurrentUser = null! Повторная попытка...");
            Invoke(nameof(TryInitializeUserId), 0.5f);
            return;
        }

        // Успех!
        userId = db.CurrentUser.Id;
        Debug.Log($"✅ userId установлен: {userId} (Login: {db.CurrentUser.Login})");
    }

    void Update()
    {
        // 1. Проверка земли
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Небольшое прижатие к земле
        }

        // 2. Ввод (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Логика Бега и Выносливости
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool canRun = currentStamina > 0;

        float currentSpeed = (wantsToRun && canRun) ? runSpeed : walkSpeed;

        // Трата выносливости при беге
        if (wantsToRun && canRun && (x != 0 || z != 0))
        {
            currentStamina -= staminaCostRun * Time.deltaTime;
            Debug.Log($"Стамина: {currentStamina:F1}"); // Для отладки
        }
        else if (currentStamina < maxStamina)
        {
            // Восстановление, если не бежим (1 ед в сек)
            currentStamina += staminaRegen * Time.deltaTime;
        }
        // Ограничиваем значения стамины от 0 до Max
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // 4. Движение
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 5. Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded && currentStamina >= staminaCostJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            currentStamina -= staminaCostJump;
        }

        // 6. Гравитация
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 7. Вращение игрока (Мышь)
        //  нельзя смотреть вверх/вниз, только поворачиваться телом
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;

        // Поворачиваем только тело игрока по Y (влево-вправо)
        transform.Rotate(0, mouseX, 0);

        // 8. Атака рукой (Правая кнопка мыши)
        if (Input.GetMouseButtonDown(1))
        {
            HandAttack();
        }

        // Если захочешь добавить небольшой наклон камеры вверх/вниз, раскомментируй ниже:
        /*
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, -30f, 30f); // Ограничение угла
        cameraTransform.localRotation = Quaternion.Euler(rotationY, 0, 0);
        */
    }

    void HandAttack()
    {
        // Проверка кулдауна
        if (Time.time < handNextAttackTime)
        {
            return;
        }

        Debug.Log("Игрок атакует рукой!");

        // Проверяем попадание в радиусе атаки рукой
        RaycastHit hit;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(transform.position, direction, out hit, handRange))
        {
            Debug.Log($"Атака рукой попала в: {hit.transform.name}");

            Enemy enemy = hit.transform.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Наносим урон врагу
                enemy.TakeDamage(handDamage, gameObject);
                Debug.Log($"Враг получил {handDamage} урона от руки!");

                // Если мирная сущность - заставляем убегать
                if (enemy.entityType == Enemy.EntityType.Peaceful)
                {
                    enemy.StartFleeing();
                }
            }
        }
        else
        {
            Debug.Log("Атака рукой промахнулась!");
        }

        // Обновляем время следующей атаки
        handNextAttackTime = Time.time + handAttackCooldown;
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Игрок получил урон: {damage}. Здоровье: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        // Сообщение если здоровье больше 80
        if (currentHealth > 80)
        {
            Debug.Log("$#!@");
        }

        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"Игрок вылечен: {currentHealth}/{maxHealth}");
    }

    public void Die()
    {
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log("ИГРОК ПОГИБ!");

        // === ОБНОВЛЕНИЕ СТАТИСТИКИ СМЕРТИ ===
        if (userId != 0)
        {
            var dbManager = FindFirstObjectByType<JsonDatabaseManager>();
            if (dbManager != null)
            {
                dbManager.UpdateDeathStatistics(userId);
                Debug.Log($"✅ Статистика смерти обновлена для игрока {userId}");
            }
        }

        // Находим DeathScreen и показываем экран смерти
        DeathScreen deathScreen = FindObjectOfType<DeathScreen>();
        if (deathScreen != null)
        {
            deathScreen.ShowDeathScreen();
        }
        else
        {
            Debug.LogError("DeathScreen не найден! Создай объект с этим скриптом!");
        }

        // Отключаем управление
        enabled = false; // Отключаем этот скрипт

        // Если есть CharacterController - отключаем его
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    void OnApplicationQuit()
    {
        // Сохраняем позицию игрока при выходе из игры
        SaveData saveData = new SaveData();
        saveData.playerPosition = transform.position;
        saveData.playerRotation = transform.rotation;
        saveData.playerHealth = currentHealth;
        saveData.playerStamina = currentStamina; 

        JsonDatabaseManager db = FindFirstObjectByType<JsonDatabaseManager>();
        if (db != null && db.IsLoggedIn && db.CurrentUser != null)
        {
            saveData.userId = db.CurrentUser.Id;
            db.SavePlayerProgress(saveData);
            Debug.Log($"✅ Позиция игрока сохранена: {transform.position}");
        }
    }
}