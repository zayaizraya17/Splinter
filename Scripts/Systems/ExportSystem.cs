using UnityEngine;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Text;
using System.Diagnostics;

public static class ExportSystem
{
    /// <summary>
    /// Экспорт статистики в CSV (Excel)
    /// </summary>
    public static bool ExportStatisticsToCSV(System.Collections.Generic.List<StatisticsData> statistics,
        System.Collections.Generic.List<UserData> users, string filePath)
    {
        try
        {
            StringBuilder csv = new StringBuilder();

            // Заголовок (требование 3.k)
            csv.AppendLine("ID;Login;Role;Kills;Deaths;TotalDamage;PlayTime;LastPlayed");

            // Данные
            foreach (var stat in statistics)
            {
                var user = users.Find(u => u.Id == stat.UserId);
                string login = user != null ? user.Login : "Unknown";
                string role = user != null ? user.Role : "Unknown";

                csv.AppendLine($"{stat.Id};{login};{role};{stat.Kills};{stat.Deaths};" +
                              $"{stat.TotalDamage};{stat.PlayTime};{stat.LastPlayed}");
            }

            // Сохранение файла
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);

            UnityEngine.Debug.Log($"✅ Статистика экспортирована в {filePath}");
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"❌ Ошибка экспорта: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Экспорт статистики пользователя в CSV
    /// </summary>
    public static bool ExportUserStatisticsToCSV(StatisticsData stats, string login, string filePath)
    {
        try
        {
            StringBuilder csv = new StringBuilder();

            csv.AppendLine($"Статистика игрока: {login}");
            csv.AppendLine();
            csv.AppendLine("Параметр;Значение");
            csv.AppendLine($"Kills;{stats.Kills}");
            csv.AppendLine($"Deaths;{stats.Deaths}");
            csv.AppendLine($"Total Damage;{stats.TotalDamage}");
            csv.AppendLine($"Play Time;{stats.PlayTime:F0} sec");
            csv.AppendLine($"Last Played;{stats.LastPlayed}");

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);

            UnityEngine.Debug.Log($"✅ Статистика пользователя экспортирована в {filePath}");
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"❌ Ошибка экспорта: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Открыть файл в Excel (по умолчанию)
    /// </summary>
    public static void OpenInExcel(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Process.Start(filePath);
                UnityEngine.Debug.Log($"📊 Файл открыт в Excel: {filePath}");
            }
            else
            {
                UnityEngine.Debug.LogError($"❌ Файл не найден: {filePath}");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"❌ Ошибка открытия файла: {ex.Message}");
        }
    }

    /// <summary>
    /// Экспорт в TXT (простой формат)
    /// </summary>
    public static bool ExportToTXT(StatisticsData stats, string login, string filePath)
    {
        try
        {
            StringBuilder txt = new StringBuilder();

            txt.AppendLine($"========================================");
            txt.AppendLine($"СТАТИСТИКА ИГРОКА: {login}");
            txt.AppendLine($"========================================");
            txt.AppendLine();
            txt.AppendLine($"Убийств: {stats.Kills}");
            txt.AppendLine($"Смертей: {stats.Deaths}");
            txt.AppendLine($"Общий урон: {stats.TotalDamage}");
            txt.AppendLine($"Время в игре: {stats.PlayTime:F0} сек.");
            txt.AppendLine($"Последняя игра: {stats.LastPlayed}");
            txt.AppendLine();
            txt.AppendLine($"========================================");

            File.WriteAllText(filePath, txt.ToString(), Encoding.UTF8);

            UnityEngine.Debug.Log($"✅ Статистика экспортирована в TXT: {filePath}");
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"❌ Ошибка экспорта: {ex.Message}");
            return false;
        }
    }
}