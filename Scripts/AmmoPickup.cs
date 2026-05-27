using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Настройки патрона")]
    public AmmoType ammoType = AmmoType.Pistol;
    public int ammoAmount = 5;
    public float pickupRange = 2f;

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("ПАТРОНЫ: Игрок с тегом Player не найден!");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= pickupRange)
        {
            Pickup(player);
        }
    }

    void Pickup(GameObject player)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError("ПАТРОНЫ: На игроке нет компонента PlayerInventory!");
            return;
        }

        string ammoName = ammoType == AmmoType.Pistol ? "пистолета" : "дробовика";
        Debug.Log($"ПАТРОНЫ: Подобраны патроны для {ammoName} в количестве {ammoAmount} шт.");

        inventory.AddAmmo(ammoType, ammoAmount);

        Destroy(gameObject);
    }
}