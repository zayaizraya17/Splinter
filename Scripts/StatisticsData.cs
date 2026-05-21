using System;
using UnityEngine;

[System.Serializable]  // ← ОБЯЗАТЕЛЬНО!
public class StatisticsData
{
    // Поля должны быть public (не свойства!)
    public int Id;
    public int UserId;          // ← Ссылка на пользователя
    public int Kills;
    public int Deaths;
    public int TotalDamage;
    public float PlayTime;
    public string LastPlayed;

    public StatisticsData()
    {
        Id = UnityEngine.Random.Range(10000, 99999);
        UserId = 0;
        Kills = 0;
        Deaths = 0;
        TotalDamage = 0;
        PlayTime = 0f;
        LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}