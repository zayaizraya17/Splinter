using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int Id;
    public int UserId;
    public string SaveName;
    public float Health;
    public float Stamina;
    public float PosX, PosY, PosZ;
    public float RotX, RotY, RotZ, RotW;
    public int PistolAmmo;
    public int ShotgunAmmo;
    public float PlayTime;  // ← ДОБАВИТЬ ЭТУ СТРОКУ
    public string LastPlayed;


    // Вспомогательные свойства для удобной работы с позицией и ротацией
    public Vector3 playerPosition
    {
        get => new Vector3(PosX, PosY, PosZ);
        set { PosX = value.x; PosY = value.y; PosZ = value.z; }
    }

    public Quaternion playerRotation
    {
        get => new Quaternion(RotX, RotY, RotZ, RotW);
        set { RotX = value.x; RotY = value.y; RotZ = value.z; RotW = value.w; }
    }

    public float playerHealth
    {
        get => Health;
        set => Health = value;
    }

    public float playerStamina
    {
        get => Stamina;
        set => Stamina = value;
    }

    public int userId
    {
        get => UserId;
        set => UserId = value;
    }
    public SaveData()
    {
        Id = UnityEngine.Random.Range(10000, 99999);
    }
}