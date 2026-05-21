using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerController playerController;

    [Header("Эффект: Кровотечение")]
    public bool isBleeding = false;
    public float bleedDamagePerSecond = 1f;
    private float bleedTimer = 0f;

    [Header("Эффект: Я обязательно выживу")]
    private bool isLowHealthBuffActive = false;
    private float originalWalkSpeed;
    private float originalRunSpeed;

    [Header("Эффект: У меня нет врагов")]
    private bool isNoEnemiesBuffActive = false;
    private float noEnemiesTimer = 0f;
    private int recentKills = 0;
    private float killTimer = 0f;

    void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // Сохраняем базовые скорости
        originalWalkSpeed = playerController.walkSpeed;
        originalRunSpeed = playerController.runSpeed;
    }


    void Update()
    {
        // === ЭФФЕКТ: Я обязательно выживу ===
        CheckLowHealthBuff();

        // === ЭФФЕКТ: Кровотечение ===
        if (isBleeding)
        {
            bleedTimer += Time.deltaTime;
            if (bleedTimer >= 1f) // Каждую секунду
            {
                playerController.currentHealth -= bleedDamagePerSecond;
                bleedTimer = 0f;
                Debug.Log($"КРОВОТЕЧЕНИЕ: -1 HP (осталось: {playerController.currentHealth})");

                if (playerController.currentHealth <= 0)
                {
                    playerController.Die();
                }
            }
        }

        // === ЭФФЕКТ: У меня нет врагов ===
        UpdateNoEnemiesBuff();
    }

    // === МЕТОДЫ ДЛЯ ЭФФЕКТОВ ===

    void CheckLowHealthBuff()
    {
        if (playerController.currentHealth < 20 && !isLowHealthBuffActive)
        {
            // Включаем бафф
            ActivateLowHealthBuff();
        }
        else if (playerController.currentHealth >= 20 && isLowHealthBuffActive)
        {
            // Выключаем бафф
            DeactivateLowHealthBuff();
        }
    }

    void ActivateLowHealthBuff()
    {
        isLowHealthBuffActive = true;
        playerController.walkSpeed = originalWalkSpeed * 1.5f;
        playerController.runSpeed = originalRunSpeed * 1.5f;
        Debug.Log(" ЭФФЕКТ: Я обязательно выживу! (скорость +50%)");
    }

    void DeactivateLowHealthBuff()
    {
        isLowHealthBuffActive = false;
        playerController.walkSpeed = originalWalkSpeed;
        playerController.runSpeed = originalRunSpeed;
        Debug.Log(" ЭФФЕКТ снят: Я обязательно выживу");
    }

    // Метод для включения кровотечения (вызывается из Enemy.cs)
    public void StartBleeding()
    {
        if (!isBleeding)
        {
            isBleeding = true;
            bleedTimer = 0f;
            Debug.Log(" ЭФФЕКТ: КРОВОТЕЧЕНИЕ началось!");
        }
    }

    // Метод для остановки кровотечения (при использовании аптечки)
    public void StopBleeding()
    {
        if (isBleeding)
        {
            isBleeding = false;
            Debug.Log(" КРОВОТЕЧЕНИЕ остановлено!");
        }
    }

    // === ЭФФЕКТ: У меня нет врагов ===

    public void RegisterKill()
    {
        recentKills++;
        killTimer = 10f; // Сбрасываем таймер убийств

        if (recentKills >= 2 && !isNoEnemiesBuffActive)
        {
            ActivateNoEnemiesBuff();
        }
    }

    void ActivateNoEnemiesBuff()
    {
        isNoEnemiesBuffActive = true;
        Debug.Log(" ЭФФЕКТ: У меня нет врагов! (+5 урона на 20 сек)");
    }

    void UpdateNoEnemiesBuff()
    {
        if (isNoEnemiesBuffActive)
        {
            // Бафф длится 20 секунд
            // (упрощённо: просто выключаем через 20 сек после активации)
            // Для точности можно добавить таймер noEnemiesBuffTimer
        }

        if (killTimer > 0)
        {
            killTimer -= Time.deltaTime;
            if (killTimer <= 0)
            {
                recentKills = 0;
                if (isNoEnemiesBuffActive)
                {
                    isNoEnemiesBuffActive = false;
                    Debug.Log("ЭФФЕКТ снят: У меня нет врагов");
                }
            }
        }
    }

    // Публичный метод для получения бонуса к урону
    public bool HasNoEnemiesBuff()
    {
        return isNoEnemiesBuffActive;
    }
}