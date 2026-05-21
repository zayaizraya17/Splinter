using System;

[Serializable]
public class SaveData
{
    public int Id;
    public int UserId;
    public string SaveName;
    public float Health;
    public float Stamina;
    public float PosX, PosY, PosZ;
    public int PistolAmmo;
    public int ShotgunAmmo;
    public float PlayTime;  // ← ДОБАВИТЬ ЭТУ СТРОКУ
    public string LastPlayed;

    public SaveData()
    {
        Id = UnityEngine.Random.Range(10000, 99999);
    }
}