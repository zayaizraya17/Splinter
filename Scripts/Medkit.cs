using UnityEngine;

public class Medkit : MonoBehaviour
{
    public int healAmount = 50;
    public float pickupRange = 2f;

    void Update()
    {
        // Автоматический подбор если игрок близко
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("АПТЕЧКА: Игрок с тегом Player не найден!");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= pickupRange)
        {
            Debug.Log("АПТЕЧКА: Игрок достаточно близко, подбираем!");
            Pickup(player);
        }
    }

    void Pickup(GameObject player)
    {
        Debug.Log("АПТЕЧКА: Вызван метод Pickup()");

        PlayerController playerController = player.GetComponent<PlayerController>();
        EffectManager effects = player.GetComponent<EffectManager>();

        if (playerController == null)
        {
            Debug.LogError("АПТЕЧКА: На игроке нет компонента PlayerController!");
            return;
        }

        // === Сообщение если здоровье больше 80 ===
        if (playerController.currentHealth > 80)
        {
            Debug.Log("$#!@");
        }

        // Лечение
        playerController.Heal(healAmount);

        // === Остановка кровотечения ===
        if (effects != null)
        {
            effects.StopBleeding();
        }

        Debug.Log("АПТЕЧКА: Подобрана!");
        Destroy(gameObject);
    }
}