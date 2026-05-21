using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Настройки оружия")]
    public Transform muzzlePoint;
    public float weaponRange = 100f;
    public Camera playerCamera;

    [Header("Система оружия")]
    public int currentWeaponIndex = 0;
    public string[] weaponNames = { "Пистолет", "Дробовик", "Нож" };

    [Header("Пистолет")]
    public int pistolDamage = 20;
    public int pistolMaxAmmo = 10;
    public int pistolCurrentAmmo;
    public float pistolEnergyCost = 5f;
    public float pistolFireRate = 0.2f;
    private float pistolNextFireTime = 0f;

    [Header("Дробовик")]
    public int shotgunDamage = 50;
    public int shotgunMaxAmmo = 5;
    public int shotgunCurrentAmmo;
    public float shotgunEnergyCost = 15f;
    public float shotgunFireRate = 1f;
    private float shotgunNextFireTime = 0f;
    public int shotgunPellets = 5;

    [Header("Нож")]
    public int knifeDamage = 10;
    public float knifeEnergyCost = 10f;
    public float knifeFireRate = 0.5f;
    private float knifeNextFireTime = 0f;
    public float knifeRange = 2f;

    [Header("Ссылки")]
    public PlayerController playerController;
    public JsonDatabaseManager databaseManager;

    private bool isReloading = false;
    private float lastReloadAttemptTime = 0f;

    void Start()
    {
        pistolCurrentAmmo = pistolMaxAmmo;
        shotgunCurrentAmmo = shotgunMaxAmmo;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        // === ИНИЦИАЛИЗАЦИЯ DATABASE MANAGER ===
        if (databaseManager == null)
            databaseManager = FindFirstObjectByType<JsonDatabaseManager>();
        // =======================================

        Debug.Log($"Оружие: {weaponNames[currentWeaponIndex]}");
    }

    void Update()
    {
        // === ГЛАВНАЯ ПРОВЕРКА: Если меню открыто - НЕ СТРЕЛЯТЬ ===
        if (MenuManager.IsMenuOpen)
        {
            return;
        }

        // Теперь можно стрелять
        if (Input.GetKey(KeyCode.LeftShift))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                SwitchWeapon(1);
            }
            else if (scroll < 0f)
            {
                SwitchWeapon(-1);
            }
        }

        if (Input.GetMouseButtonDown(0) && !isReloading)
        {
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Reload();
        }
    }

    void SwitchWeapon(int direction)
    {
        currentWeaponIndex += direction;

        if (currentWeaponIndex < 0)
            currentWeaponIndex = weaponNames.Length - 1;
        if (currentWeaponIndex >= weaponNames.Length)
            currentWeaponIndex = 0;

        Debug.Log($"Оружие: {weaponNames[currentWeaponIndex]}");
        isReloading = false;
    }

    void Shoot()
    {
        float currentTime = Time.time;

        // Проверка энергии
        if (playerController != null)
        {
            float energyCost = GetEnergyCost();
            if (playerController.currentStamina < energyCost)
            {
                Debug.Log("Недостаточно энергии!");
                return;
            }
            // ТРАТИМ энергию
            playerController.currentStamina -= energyCost;
            Debug.Log($"Потрачена энергия: {energyCost}, осталось: {playerController.currentStamina:F1}");
        }
        else
        {
            Debug.LogError("WeaponManager: playerController = null!");
        }

        switch (currentWeaponIndex)
        {
            case 0:
                if (currentTime >= pistolNextFireTime)
                {
                    if (pistolCurrentAmmo <= 0)
                    {
                        Debug.Log("Пусто! Перезарядись (C)");
                        return;
                    }
                    pistolCurrentAmmo--;
                    playerController.currentStamina -= pistolEnergyCost;
                    pistolNextFireTime = currentTime + pistolFireRate;
                    FirePistol();
                }
                break;

            case 1: // Дробовик
                if (currentTime >= shotgunNextFireTime)
                {
                    // ПРОВЕРКА ПАТРОНОВ ПЕРЕД ВСЕМ
                    if (shotgunCurrentAmmo <= 0)
                    {
                        Debug.Log("Пусто! Перезарядись (C)");
                        return; // НЕМЕДЛЕННЫЙ ВЫХОД
                    }

                    // ТРАТИМ ПАТРОН И ЭНЕРГИЮ
                    shotgunCurrentAmmo--;
                    playerController.currentStamina -= shotgunEnergyCost;

                    // ОБНОВЛЯЕМ ТАЙМЕР
                    shotgunNextFireTime = currentTime + shotgunFireRate;

                    // СТРЕЛЯЕМ
                    FireShotgun();
                }
                break;

            case 2:
                if (currentTime >= knifeNextFireTime)
                {
                    playerController.currentStamina -= knifeEnergyCost;
                    knifeNextFireTime = currentTime + knifeFireRate;
                    FireKnife();
                }
                break;
        }

        Debug.Log($"Оружие: {weaponNames[currentWeaponIndex]} | Патроны: {GetCurrentAmmo()}");
    }

    void FirePistol()
    {
        int damageBonus = GetDamageBonus();

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, weaponRange))
        {
            Debug.Log($"Пистолет попал в: {hit.transform.name}");
            Enemy enemy = hit.transform.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(pistolDamage + damageBonus, gameObject);

                // === ОБНОВЛЕНИЕ СТАТИСТИКИ УРОНА ===
                UpdateDamageStatistics(pistolDamage + damageBonus);
            }
        }
    }

    void FireShotgun()
    {
        // Дробовик стреляет несколькими дробинками с разбросом
        // Но патрон тратится ТОЛЬКО ОДИН РАЗ за выстрел!
        int damageBonus = GetDamageBonus();
        bool hitSomething = false;

        for (int i = 0; i < shotgunPellets; i++)
        {
            Vector3 spread = new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0
            );

            Vector3 direction = (playerCamera.transform.forward + spread).normalized;

            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, direction, out hit, weaponRange))
            {
                hitSomething = true;

                Enemy enemy = hit.transform.GetComponent<Enemy>();
                if (enemy != null && !enemy.alreadyHitThisShot)
                {
                    enemy.alreadyHitThisShot = true;
                    enemy.TakeDamage(shotgunDamage + damageBonus, gameObject);

                    // === ОБНОВЛЕНИЕ СТАТИСТИКИ УРОНА ===
                    UpdateDamageStatistics(shotgunDamage + damageBonus);

                    Invoke(nameof(ResetEnemyHitFlags), 0.1f);
                }
            }
        }

        if (hitSomething)
            Debug.Log("Дробовик выстрелил! (попал)");
        else
            Debug.Log("Дробовик выстрелил! (промах)");
    }

    // Метод для сброса флагов у врагов
    void ResetEnemyHitFlags()
    {
        Enemy[] allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in allEnemies)
        {
            enemy.alreadyHitThisShot = false;
        }
    }

    void FireKnife()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, knifeRange))
        {
            int damageBonus = GetDamageBonus();
            Debug.Log($"Нож попал в: {hit.transform.name}");
            Enemy enemy = hit.transform.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(knifeDamage + damageBonus, gameObject);

                // === ОБНОВЛЕНИЕ СТАТИСТИКИ УРОНА ===
                UpdateDamageStatistics(knifeDamage + damageBonus);
            }
        }
        else
        {
            Debug.Log("Нож промахнулся (слишком далеко)");
        }
    }

    // === МЕТОД ВЫНЕСЕН ОТДЕЛЬНО ===
    void UpdateDamageStatistics(int damage)
    {
        if (databaseManager != null && databaseManager.IsLoggedIn)
        {
            databaseManager.UpdateDamageStatistics(databaseManager.CurrentUser.Id, damage);
        }
    }


    void Reload()
    {
        if (isReloading) return;

        float currentTime = Time.time;

        if (currentTime - lastReloadAttemptTime < 2f)
        {
            if (GetCurrentAmmo() == GetMaxAmmo())
            {
                Debug.Log("зачем?");
                return;
            }
        }

        lastReloadAttemptTime = currentTime;

        if (GetCurrentAmmo() == GetMaxAmmo())
        {
            Debug.Log("патроны еще не закончились");
            return;
        }

        isReloading = true;
        Debug.Log("Перезарядка...");
        Invoke(nameof(FinishReload), 2f);
    }

    void FinishReload()
    {
        switch (currentWeaponIndex)
        {
            case 0:
                pistolCurrentAmmo = pistolMaxAmmo;
                break;
            case 1:
                shotgunCurrentAmmo = shotgunMaxAmmo;
                break;
        }
        isReloading = false;
        Debug.Log($"Перезаряжено! {GetCurrentAmmo()}/{GetMaxAmmo()}");
    }

    public int GetCurrentAmmo()
    {
        switch (currentWeaponIndex)
        {
            case 0: return pistolCurrentAmmo;
            case 1: return shotgunCurrentAmmo;
            case 2: return 999;
            default: return 0;
        }
    }

    int GetMaxAmmo()
    {
        switch (currentWeaponIndex)
        {
            case 0: return pistolMaxAmmo;
            case 1: return shotgunMaxAmmo;
            case 2: return 999;
            default: return 0;
        }
    }

    int GetDamageBonus()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            EffectManager effects = playerObject.GetComponent<EffectManager>();
            if (effects != null && effects.HasNoEnemiesBuff())
            {
                return 5; // Бонус +5 урона
            }
        }
        return 0;
    }

    float GetEnergyCost()
    {
        switch (currentWeaponIndex)
        {
            case 0: return pistolEnergyCost;
            case 1: return shotgunEnergyCost;
            case 2: return knifeEnergyCost;
            default: return 0;
        }
    }
}