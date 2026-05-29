using UnityEngine;
using System;
using System.Collections.Generic;
public enum AmmoType
{
    Pistol,
    Shotgun
}


[System.Serializable]
public class InventorySlot
{
    public string itemName;
    public bool isEmpty => string.IsNullOrEmpty(itemName);

    public void Clear()
    {
        itemName = "";
    }

    public void SetItem(string name)
    {
        itemName = name;
    }
}

public class PlayerInventory : MonoBehaviour, ISaveable
{
    [Header("Патроны")]
    public AmmoData ammoData = new AmmoData();

    [Header("Слоты инвентаря (6 слотов)")]
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();

    [Header("Текущий выбранный слот")]
    public int currentSlotIndex = -1; // -1 означает "ничего в руках"

    [Header("Ссылки")]
    public WeaponManager weaponManager;
    public JsonDatabaseManager databaseManager;

    // Стартовые предметы
    void Start()
    {
        // Инициализируем 6 слотов
        if (inventorySlots == null || inventorySlots.Count != 6)
        {
            inventorySlots = new List<InventorySlot>();
            for (int i = 0; i < 6; i++)
            {
                inventorySlots.Add(new InventorySlot());
            }
        }

        // Даем стартовое оружие (Пистолет в слот 1, Нож в слот 2)
        if (inventorySlots[0].isEmpty)
            inventorySlots[0].SetItem("Пистолет");
        if (inventorySlots[1].isEmpty)
            inventorySlots[1].SetItem("Нож");
        if (inventorySlots[2].isEmpty)
            inventorySlots[2].SetItem("Дробовик");

        // Инициализация стартовых патронов (опционально)
        // ammoData.pistolAmmo = 12;
        // ammoData.shotgunAmmo = 10;

        // Находим ссылки
        if (weaponManager == null)
            weaponManager = GetComponentInParent<WeaponManager>();

        if (databaseManager == null)
            databaseManager = FindObjectOfType<JsonDatabaseManager>();

        Debug.Log($"Инвентарь инициализирован! Слоты: {inventorySlots.Count}");
        PrintInventory();
    }

    void Update()
    {
        // Переключение между слотами цифрами 1-6
        for (int i = 1; i <= 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown((KeyCode)(KeyCode.Keypad1 + i - 1)))
            {
                SelectSlot(i - 1);
            }
        }

        // Выбросить предмет (Q) - только если предмет в руках
        if (Input.GetKeyDown(KeyCode.Q) && currentSlotIndex >= 0 && currentSlotIndex < inventorySlots.Count)
        {
            DropCurrentItem();
        }

        // Подобрать предмет (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            PickupItem();
        }
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count)
        {
            Debug.Log($"Неверный индекс слота: {slotIndex}");
            return;
        }

        currentSlotIndex = slotIndex;
        InventorySlot slot = inventorySlots[slotIndex];

        if (slot.isEmpty)
        {
            Debug.Log($"Слот {slotIndex + 1}: Пусто");
            // Можно переключиться на "руки" или ничего
            if (weaponManager != null)
            {
                weaponManager.SwitchToHand();
            }
        }
        else
        {
            Debug.Log($"Слот {slotIndex + 1}: {slot.itemName}");
            if (weaponManager != null)
            {
                weaponManager.SwitchToWeapon(slot.itemName);
            }
        }
    }

    public void DropCurrentItem()
    {
        if (currentSlotIndex < 0 || currentSlotIndex >= inventorySlots.Count)
        {
            Debug.Log("Нечего выбрасывать!");
            return;
        }

        InventorySlot slot = inventorySlots[currentSlotIndex];

        if (slot.isEmpty)
        {
            Debug.Log("В руках пусто!");
            return;
        }

        string droppedItem = slot.itemName;
        slot.Clear();
        currentSlotIndex = -1;

        Debug.Log($"Выброшен предмет: {droppedItem}");

        // Здесь можно создать префаб предмета на земле
        // SpawnItemOnGround(droppedItem, transform.position);

        PrintInventory();

        // Переключаемся на руки
        if (weaponManager != null)
        {
            weaponManager.SwitchToHand();
        }
    }

    public void PickupItem()
    {
        // Ищем все предметы в радиусе подбора вокруг игрока
        float pickupRadius = 3f; // Радиус доступности для подбора

        // Используем позицию CharacterController для более точного определения центра игрока
        CharacterController controller = GetComponent<CharacterController>();
        Vector3 checkPosition = controller != null ? transform.position + Vector3.up * controller.height / 2 : transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(checkPosition, pickupRadius);

        Debug.Log($"Проверка подбора предметов в радиусе {pickupRadius}м. Найдено коллайдеров: {hitColliders.Length}");

        PickupableItem nearestPickupable = null;
        float nearestDistance = float.MaxValue;

        foreach (var collider in hitColliders)
        {
            Debug.Log($"Найден коллайдер: {collider.gameObject.name}, Layer: {collider.gameObject.layer}");

            PickupableItem pickupable = collider.GetComponent<PickupableItem>();
            if (pickupable != null)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                Debug.Log($"Предмет найден: {pickupable.itemName}, дистанция: {distance:F2}м");

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPickupable = pickupable;
                }
            }
        }


        if (nearestPickupable != null)
        {
            // Находим первый пустой слот для всех предметов (включая патроны)
            int emptySlot = FindEmptySlot();

            if (emptySlot == -1)
            {
                Debug.Log("Инвентарь полон! Нет свободных слотов.");
                return;
            }

            // Подбираем предмет (патроны тоже занимают слот до перезарядки)
            inventorySlots[emptySlot].SetItem(nearestPickupable.itemName);
            Debug.Log($"Подобран предмет: {nearestPickupable.itemName} в слот {emptySlot + 1} (дистанция: {nearestDistance:F2}м)");

            // Если это патроны, сразу добавляем их в запас
            if (nearestPickupable.itemName.Contains("Pistol") || nearestPickupable.itemName.Contains("pistol"))
            {
                AddAmmo(AmmoType.Pistol, 12);
            }
            else if (nearestPickupable.itemName.Contains("Shotgun") || nearestPickupable.itemName.Contains("shotgun"))
            {
                AddAmmo(AmmoType.Shotgun, 10);
            }
            // Удаляем предмет со сцены
            Destroy(nearestPickupable.gameObject);

            PrintInventory();
        }
        else
        {
            Debug.Log($"В радиусе {pickupRadius}м нет предметов для подбора!");
        }
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].isEmpty)
            {
                return i;
            }
        }
        return -1; // Нет пустых слотов
    }

    public bool HasItem(string itemName)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.itemName == itemName)
            {
                return true;
            }
        }
        return false;
    }

    public void AddItem(string itemName)
    {
        int emptySlot = FindEmptySlot();

        if (emptySlot == -1)
        {
            Debug.Log("Инвентарь полон!");
            return;
        }

        inventorySlots[emptySlot].SetItem(itemName);
        Debug.Log($"Добавлен предмет: {itemName} в слот {emptySlot + 1}");
        PrintInventory();
    }

    public void RemoveItem(string itemName)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.itemName == itemName)
            {
                slot.Clear();
                Debug.Log($"Удален предмет: {itemName}");
                PrintInventory();
                return;
            }
        }
        Debug.Log($"Предмет {itemName} не найден в инвентаре!");
    }

    public string GetCurrentItem()
    {
        if (currentSlotIndex < 0 || currentSlotIndex >= inventorySlots.Count)
        {
            return "";
        }

        return inventorySlots[currentSlotIndex].itemName;
    }

    void PrintInventory()
    {
        string inventoryStr = "Инвентарь: ";
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i == currentSlotIndex)
                inventoryStr += $"[{i + 1}:{(inventorySlots[i].isEmpty ? "Пусто" : inventorySlots[i].itemName)}] ";
            else
                inventoryStr += $"{i + 1}:{(inventorySlots[i].isEmpty ? "Пусто" : inventorySlots[i].itemName)} ";
        }
        Debug.Log(inventoryStr);
    }

    // === Методы для сохранения/загрузки ===

    public void SaveData()
    {
        if (databaseManager == null || !databaseManager.IsLoggedIn)
        {
            Debug.LogWarning("PlayerInventory: Нельзя сохранить - пользователь не вошёл!");
            return;
        }

        SaveData saveData = new SaveData();

        // Сохраняем патроны
        saveData.PistolAmmo = ammoData.pistolAmmo;
        saveData.ShotgunAmmo = ammoData.shotgunAmmo;

        // Сохраняем инвентарь
        saveData.InventoryItems = new List<string>();
        foreach (var slot in inventorySlots)
        {
            saveData.InventoryItems.Add(slot.itemName ?? "");
        }

        databaseManager.SavePlayerProgress(saveData);
        Debug.Log("PlayerInventory: Данные сохранены!");
    }

    public void LoadData()
    {
        if (databaseManager == null || !databaseManager.IsLoggedIn)
        {
            Debug.LogWarning("PlayerInventory: Нельзя загрузить - пользователь не вошёл!");
            return;
        }

        SaveData loadedSave = databaseManager.LoadPlayerProgress();

        if (loadedSave != null)
        {
            // Загружаем патроны
            ammoData.pistolAmmo = loadedSave.PistolAmmo;
            ammoData.shotgunAmmo = loadedSave.ShotgunAmmo;

            // Загружаем инвентарь
            if (loadedSave.InventoryItems != null && loadedSave.InventoryItems.Count > 0)
            {
                for (int i = 0; i < Math.Min(inventorySlots.Count, loadedSave.InventoryItems.Count); i++)
                {
                    string itemName = loadedSave.InventoryItems[i];
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        inventorySlots[i].SetItem(itemName);
                    }
                    else
                    {
                        inventorySlots[i].Clear();
                    }
                }
            }

            Debug.Log("PlayerInventory: Данные загружены!");
            PrintInventory();
        }
        else
        {
            Debug.Log("PlayerInventory: Нет сохранённых данных, используем значения по умолчанию.");
        }
    }
    public void AddAmmo(AmmoType type, int amount)
    {
        switch (type)
        {
            case AmmoType.Pistol:
                int newPistolAmmo = ammoData.pistolAmmo + amount;
                if (newPistolAmmo > ammoData.maxPistolAmmoCapacity)
                {
                    newPistolAmmo = ammoData.maxPistolAmmoCapacity;
                }
                ammoData.pistolAmmo = newPistolAmmo;
                Debug.Log($"Подобраны патроны для пистолета! Всего: {ammoData.pistolAmmo}/{ammoData.maxPistolAmmoCapacity}");
                break;

            case AmmoType.Shotgun:
                int newShotgunAmmo = ammoData.shotgunAmmo + amount;
                if (newShotgunAmmo > ammoData.maxShotgunAmmoCapacity)
                {
                    newShotgunAmmo = ammoData.maxShotgunAmmoCapacity;
                }
                ammoData.shotgunAmmo = newShotgunAmmo;
                Debug.Log($"Подобраны патроны для дробовика! Всего: {ammoData.shotgunAmmo}/{ammoData.maxShotgunAmmoCapacity}");
                break;
        }
    }

    public bool HasAmmo(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:
                return ammoData.pistolAmmo > 0;
            case AmmoType.Shotgun:
                return ammoData.shotgunAmmo > 0;
            default:
                return false;
        }
    }

    public bool ConsumeAmmo(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:
                if (ammoData.pistolAmmo > 0)
                {
                    ammoData.pistolAmmo--;
                    return true;
                }
                return false;

            case AmmoType.Shotgun:
                if (ammoData.shotgunAmmo > 0)
                {
                    ammoData.shotgunAmmo--;
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    public int GetAmmoCount(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:
                return ammoData.pistolAmmo;
            case AmmoType.Shotgun:
                return ammoData.shotgunAmmo;
            default:
                return 0;
        }
    }

    public int GetMaxAmmoCapacity(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:
                return ammoData.maxPistolAmmoCapacity;
            case AmmoType.Shotgun:
                return ammoData.maxShotgunAmmoCapacity;
            default:
                return 0;
        }
    }

    public void ReloadWeapon(AmmoType type, ref int currentAmmo, int maxAmmo)
    {
        int ammoNeeded = maxAmmo - currentAmmo;

        if (ammoNeeded <= 0)
        {
            Debug.Log("Магазин полон!");
            return;
        }

        int availableAmmo = GetAmmoCount(type);

        if (availableAmmo <= 0)
        {
            Debug.Log("Нет патронов в запасе!");
            return;
        }

        int ammoToReload = Mathf.Min(ammoNeeded, availableAmmo);

        // Тратим патроны из запаса
        switch (type)
        {
            case AmmoType.Pistol:
                ammoData.pistolAmmo -= ammoToReload;
                break;
            case AmmoType.Shotgun:
                ammoData.shotgunAmmo -= ammoToReload;
                break;
        }

        // Заряжаем оружие
        currentAmmo += ammoToReload;

        // Удаляем использованные патроны из инвентаря
        RemoveAmmoItemsFromInventory(type, ammoToReload);

        Debug.Log($"Перезарядка! Использовано патронов: {ammoToReload}, Осталось в запасе: {GetAmmoCount(type)}");
    }


    public void RemoveAmmoItemsFromInventory(AmmoType type, int amountUsed)
    {
        // Определяем ключевые слова и количество патронов в одной пачке
        string ammoKeyword = type == AmmoType.Pistol ? "пистолет" : "дробовик";
        int ammoPerBox = type == AmmoType.Pistol ? 12 : 10;

        // Вычисляем, сколько пачек патронов было использовано
        int boxesUsed = Mathf.CeilToInt((float)amountUsed / ammoPerBox);

        Debug.Log($"Удаление патронов из инвентаря: тип={type}, использовано патронов={amountUsed}, пачек к удалению={boxesUsed}");

        // Проходим по всем слотам и удаляем предметы с патронами
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            if (boxesUsed <= 0) break;

            if (!inventorySlots[i].isEmpty &&
                (inventorySlots[i].itemName.Contains("Патроны") && inventorySlots[i].itemName.ToLower().Contains(ammoKeyword)))
            {
                inventorySlots[i].Clear();
                boxesUsed--;
                Debug.Log($"Удален предмет с патронами из слота {i + 1}");
            }
        }

        PrintInventory();
    }
}
