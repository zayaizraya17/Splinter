using UnityEngine;

public enum AmmoType
{
    Pistol,
    Shotgun
}

[System.Serializable]
public class AmmoData
{
    public int pistolAmmo = 0;
    public int shotgunAmmo = 0;
    public int maxPistolAmmoCapacity = 50;
    public int maxShotgunAmmoCapacity = 30;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Патроны")]
    public AmmoData ammoData = new AmmoData();

    void Start()
    {
        // Инициализация стартовых патронов (опционально)
        // ammoData.pistolAmmo = 12;
        // ammoData.shotgunAmmo = 10;
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

        Debug.Log($"Перезарядка! Использовано патронов: {ammoToReload}, Осталось в запасе: {GetAmmoCount(type)}");
    }
}