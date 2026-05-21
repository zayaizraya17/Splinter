using System;
using UnityEngine;

[Serializable]
public class UserData
{
    public int Id;
    public string Login;
    public string PasswordHash;
    public string Role; // "Player" или "Admin"
    public string CreatedAt;

    public UserData() { }

    public UserData(string login, string hash, string role = "Player")
    {
        Id = UnityEngine.Random.Range(1000, 9999); // Простой ID
        Login = login;
        PasswordHash = hash;
        Role = role;
        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}