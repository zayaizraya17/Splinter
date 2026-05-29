using System;
using UnityEngine;

/// <summary>
/// Класс для хранения данных о боеприпасах.
/// Используется в системе инвентаря и оружия.
/// </summary>
[Serializable]
public class AmmoData
{
    // Патроны для пистолета
    public int pistolAmmo = 0;

    // Патроны для дробовика
    public int shotgunAmmo = 0;

    // Максимальное количество патронов для пистолета
    public int maxPistolAmmoCapacity = 50;

    // Максимальное количество патронов для дробовика
    public int maxShotgunAmmoCapacity = 30;

    // Тип патронов (например, "9mm", "12gauge", "5.56")
    public string ammoType;

    // Текущее количество патронов
    public int currentAmmo;

    // Максимальное количество патронов, которое можно носить
    public int maxAmmo;

    public AmmoData(string type, int current, int max)
    {
        this.ammoType = type;
        this.currentAmmo = current;
        this.maxAmmo = max;
    }

    // Пустой конструктор необходим для корректной сериализации/десериализации JSON
    public AmmoData()
    {
        this.ammoType = "";
        this.currentAmmo = 0;
        this.maxAmmo = 0;
    }

    /// <summary>
    /// Добавить патроны. Возвращает true, если патроны были добавлены.
    /// </summary>
    public bool AddAmmo(int amount)
    {
        if (currentAmmo >= maxAmmo)
            return false;

        currentAmmo += amount;
        if (currentAmmo > maxAmmo)
            currentAmmo = maxAmmo;

        return true;
    }

    /// <summary>
    /// Потратить патроны. Возвращает true, если хватило патронов.
    /// </summary>
    public bool ConsumeAmmo(int amount)
    {
        if (currentAmmo >= amount)
        {
            currentAmmo -= amount;
            return true;
        }
        return false;
    }
}
