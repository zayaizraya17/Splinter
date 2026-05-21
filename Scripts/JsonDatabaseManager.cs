using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.Overlays;
using UnityEngine;

public class JsonDatabaseManager : MonoBehaviour
{
    public static JsonDatabaseManager Instance { get; private set; }

    // Пути к файлам
    private string usersPath;
    private string savesPath;
    private string statisticsPath;

    // Данные в памяти
    private List<UserData> users = new List<UserData>();
    private List<SaveData> saves = new List<SaveData>();
    private List<StatisticsData> statistics = new List<StatisticsData>();

    private bool isInitialized = false;

    // События (требование 3.h)
    public event Action OnDatabaseLoaded;
    public event Action<UserData> OnUserLoggedIn;
    public event Action OnUserLoggedOut;

    public UserData CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();

            // === ПОДПИСКА НА СОБЫТИЯ ===
            OnUserLoggedIn += HandleUserLoggedIn;  // ← ДОБАВИТЬ ЭТУ СТРОКУ
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ ИСПРАВЛЕНО: метод переименован
    void HandleUserLoggedIn(UserData user)
    {
        Debug.Log($"✅ Пользователь вошёл: {user.Login} (ID: {user.Id})");

        // Проверяем, есть ли статистика для этого пользователя
        var stats = statistics.FirstOrDefault(s => s.UserId == user.Id);

        if (stats == null)
        {
            Debug.Log($"📊 Создаём статистику для нового пользователя {user.Login}...");

            var newStats = new StatisticsData
            {
                UserId = user.Id,
                Kills = 0,
                Deaths = 0,
                TotalDamage = 0,
                PlayTime = 0,
                LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            statistics.Add(newStats);
            SaveAllData();

            Debug.Log($"✅ Статистика создана для {user.Login}");
        }
        else
        {
            Debug.Log($"📊 Статистика найдена: {stats.Kills} убийств, {stats.Deaths} смертей");
        }
    }
    // === ИНИЦИАЛИЗАЦИЯ ===

    void InitializeDatabase()
    {
        // === ПРОВЕРКА: не инициализировано ли уже ===
        if (isInitialized)
        {
            Debug.Log("⚠️ База данных уже инициализирована! Пропускаем.");
            return;
        }

        try
        {
            string dbFolder = Path.Combine(Application.persistentDataPath, "Database");

            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
                Debug.Log($"📁 Создана папка: {dbFolder}");
            }

            usersPath = Path.Combine(dbFolder, "users.json");
            savesPath = Path.Combine(dbFolder, "saves.json");
            statisticsPath = Path.Combine(dbFolder, "statistics.json");

            Debug.Log($"📁 Путь к базе: {dbFolder}");

            // === СНАЧАЛА ЗАГРУЖАЕМ существующие данные ===
            LoadAllData();
            Debug.Log($"📥 Загружено: {users.Count} пользователей, {saves.Count} сохранений, {statistics.Count} статистик");

            // === Создаём админа ТОЛЬКО если пользователей нет ===
            if (users.Count == 0)
            {
                Debug.Log("Пользователей нет, создаём админа...");
                CreateDefaultAdmin();
            }
            else
            {
                Debug.Log($"✅ Пользователи уже существуют: {users.Count}");
                // Проверяем, есть ли админ
                var admin = users.FirstOrDefault(u => u.Login == "kira");
                if (admin == null)
                {
                    Debug.Log("Админа нет, создаём...");
                    CreateDefaultAdmin();
                }
            }

            isInitialized = true;  // === ВАЖНО: помечаем как инициализированное ===
            Debug.Log("✅ JSON База данных инициализирована!");
            OnDatabaseLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка инициализации БД: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    void LoadAllData()
    {
        try
        {
            // Загружаем users
            if (File.Exists(usersPath))
            {
                var loadedUsers = LoadJson<List<UserData>>(usersPath);
                users = loadedUsers ?? new List<UserData>();
                Debug.Log($"✅ users.json загружен: {users.Count} пользователей");
            }
            else
            {
                users = new List<UserData>();
                Debug.Log("⚠️ users.json не найден");
            }

            // Загружаем statistics
            if (File.Exists(statisticsPath))
            {
                string jsonContent = File.ReadAllText(statisticsPath);
                Debug.Log($"📄 statistics.json содержимое: {jsonContent}");

                var loadedStats = LoadJson<List<StatisticsData>>(statisticsPath);
                statistics = loadedStats ?? new List<StatisticsData>();
                Debug.Log($"✅ statistics.json загружен: {statistics.Count} записей");
            }
            else
            {
                statistics = new List<StatisticsData>();
                Debug.Log("⚠️ statistics.json не найден");
            }

            // Загружаем saves
            if (File.Exists(savesPath))
            {
                var loadedSaves = LoadJson<List<SaveData>>(savesPath);
                saves = loadedSaves ?? new List<SaveData>();
                Debug.Log($"✅ saves.json загружен: {saves.Count} сохранений");
            }
            else
            {
                saves = new List<SaveData>();
                Debug.Log("⚠️ saves.json не найден");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка загрузки: {ex.Message}");
            users = users ?? new List<UserData>();
            statistics = statistics ?? new List<StatisticsData>();
            saves = saves ?? new List<SaveData>();
        }
    }

    void SaveAllData()
    {
        try
        {
            Debug.Log($"💾 Сохранение данных: {users.Count} пользователей, {statistics.Count} статистик");

            SaveJson(usersPath, users);
            Debug.Log($"✅ users.json сохранён");

            SaveJson(savesPath, saves);
            Debug.Log($"✅ saves.json сохранён");

            SaveJson(statisticsPath, statistics);
            Debug.Log($"✅ statistics.json сохранён ({statistics.Count} записей)");

            // Проверяем, что файл не пустой
            if (File.Exists(statisticsPath))
            {
                string content = File.ReadAllText(statisticsPath);
                Debug.Log($"📄 Содержимое statistics.json: {content}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка сохранения: {ex.Message}");
        }
    }

    // === РАБОТА С JSON ===

    T LoadJson<T>(string path) where T : class, new()
    {
        if (!File.Exists(path))
        {
            Debug.Log($"⚠️ Файл не найден: {path}");
            return new T();
        }

        try
        {
            string json = File.ReadAllText(path);
            Debug.Log($"📄 Чтение файла: {path}");
            Debug.Log($"📝 Содержимое: {json}");  // ← Добавь это для отладки

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"⚠️ Файл пуст: {path}");
                return new T();
            }

            // Для списков используем обёртку
            if (typeof(T) == typeof(List<UserData>) ||
                typeof(T) == typeof(List<SaveData>) ||
                typeof(T) == typeof(List<StatisticsData>))
            {
                var wrapperType = typeof(Wrapper<>).MakeGenericType(typeof(T).GetGenericArguments()[0]);
                var wrapper = JsonUtility.FromJson(json, wrapperType);
                var itemsField = wrapperType.GetField("Items");
                var result = itemsField.GetValue(wrapper) as T;

                if (result != null)
                {
                    Debug.Log($"✅ Загружено: {typeof(T).Name}");
                    return result;
                }
            }

            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка загрузки {path}: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            return new T();
        }
    }

    void SaveJson<T>(string path, T data)
    {
        try
        {
            string json = "";

            // Для списков создаём обёртку Wrapper
            if (data is List<UserData>)
            {
                var wrapper = new Wrapper<UserData> { Items = data as List<UserData> };
                json = JsonUtility.ToJson(wrapper, true);
                Debug.Log($"💾 Сохраняем users.json: {wrapper.Items.Count} записей");
            }
            else if (data is List<SaveData>)
            {
                var wrapper = new Wrapper<SaveData> { Items = data as List<SaveData> };
                json = JsonUtility.ToJson(wrapper, true);
                Debug.Log($"💾 Сохраняем saves.json: {wrapper.Items.Count} записей");
            }
            else if (data is List<StatisticsData>)
            {
                var wrapper = new Wrapper<StatisticsData> { Items = data as List<StatisticsData> };
                json = JsonUtility.ToJson(wrapper, true);
                Debug.Log($"💾 Сохраняем statistics.json: {wrapper.Items.Count} записей");
                Debug.Log($"📝 JSON: {json}");  // Показываем что сохраняем
            }
            else
            {
                json = JsonUtility.ToJson(data, true);
            }

            // Проверка: если JSON пустой или {}, создаём правильную структуру
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                if (data is List<StatisticsData>)
                {
                    json = "{\"Items\":[]}";
                    Debug.Log("⚠️ statistics.json был пустым! Создана правильная структура");
                }
                else if (data is List<UserData>)
                {
                    json = "{\"Items\":[]}";
                    Debug.Log("⚠️ users.json был пустым! Создана правильная структура");
                }
                else if (data is List<SaveData>)
                {
                    json = "{\"Items\":[]}";
                    Debug.Log("⚠️ saves.json был пустым! Создана правильная структура");
                }
            }

            File.WriteAllText(path, json);
            Debug.Log($"📁 Файл сохранён: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка сохранения {path}: {ex.Message}");
        }
    }

    // Обёртка для сериализации списков (требование JsonUtility)
    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> Items = new List<T>();
    }

    // === АДМИН ===

    void CreateDefaultAdmin()
    {
        // Проверяем, есть ли уже админ
        var admin = users.FirstOrDefault(u => u.Login == "kira");

        if (admin == null)
        {
            Debug.Log("Создаём админа kira/kiraadmin");
            var newAdmin = new UserData("kira", HashPassword("kiraadmin"), "Admin");
            users.Add(newAdmin);

            var adminStats = new StatisticsData { UserId = newAdmin.Id };
            statistics.Add(adminStats);

            // Сохраняем СРАЗУ
            SaveAllData();
            Debug.Log("✅ Админ создан и сохранён!");
        }
        else
        {
            Debug.Log("✅ Админ уже существует!");
        }
    }

    // === РЕГИСТРАЦИЯ ===

    public bool RegisterUser(string login, string password, out string errorMessage)
    {
        errorMessage = "";

        if (!Validator.ValidateLogin(login, out errorMessage))
            return false;

        if (!Validator.ValidatePassword(password, out errorMessage))
            return false;

        // Загружаем текущие данные (на всякий случай)
        LoadAllData();

        // Проверка на существование
        if (users.Any(u => u.Login == login))
        {
            errorMessage = "Пользователь с таким логином уже существует!";
            return false;
        }

        // Создаём пользователя
        var newUser = new UserData(login, HashPassword(password), "Player");
        users.Add(newUser);
        Debug.Log($" Пользователь добавлен: {login} (всего: {users.Count})");

        // Создаём статистику
        var newStats = new StatisticsData { UserId = newUser.Id };
        statistics.Add(newStats);

        // Сохраняем СРАЗУ
        SaveAllData();

        return true;
    }

    // === ВХОД ===

    public bool LoginUser(string login, string password, out string errorMessage)
    {
        errorMessage = "";

        var user = users.FirstOrDefault(u => u.Login == login);
        if (user == null)
        {
            errorMessage = "Пользователь не найден!";
            return false;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            errorMessage = "Неверный пароль!";
            return false;
        }

        CurrentUser = user;
        Debug.Log($"✅ Вход выполнен: {login} (Роль: {user.Role})");
        OnUserLoggedIn?.Invoke(CurrentUser);
        return true;
    }

    // === ВЫХОД ===

    public void LogoutUser()
    {
        CurrentUser = null;
        Debug.Log("✅ Пользователь вышел");
        OnUserLoggedOut?.Invoke();
    }

    // === СОХРАНЕНИЯ ИГРЫ ===

    public void SaveGame(SaveData save)
    {
        Debug.Log($"📥 JsonDatabaseManager.SaveGame() вызван");
        Debug.Log($"   UserId: {save.UserId}");
        Debug.Log($"   SaveName: {save.SaveName}");

        if (!IsLoggedIn)
        {
            Debug.LogWarning("⚠️ SaveGame(): Пользователь не вошёл!");
            return;
        }

        save.UserId = CurrentUser.Id;
        save.LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        saves.Add(save);

        Debug.Log($"💾 Добавлено сохранение. Всего сохранений: {saves.Count}");

        SaveAllData();
        Debug.Log("✅ Игра сохранена в JSON!");
    }

    public List<SaveData> GetSaves()
    {
        if (!IsLoggedIn) return new List<SaveData>();
        return saves.Where(s => s.UserId == CurrentUser.Id).ToList();
    }

    public void UpdatePlayTime(int userId, float playTime)
    {
        var stats = statistics.FirstOrDefault(s => s.UserId == userId);

        if (stats != null)
        {
            stats.PlayTime = playTime;
            // Не сохраняем каждый кадр! Сохраняем при выходе
        }
    }

    // === СТАТИСТИКА ===

    public void UpdateStats(int kills, int damage)
    {
        if (!IsLoggedIn) return;

        var stats = statistics.FirstOrDefault(s => s.UserId == CurrentUser.Id);
        if (stats != null)
        {
            stats.Kills += kills;
            stats.TotalDamage += damage;
            stats.LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveAllData();
        }
    }

    public void UpdateKillStatistics(int userId)
    {
        var stats = statistics.FirstOrDefault(s => s.UserId == userId);

        if (stats != null)
        {
            stats.Kills++;
            stats.LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveAllData();
            Debug.Log($"✅ Статистика обновлена: {userId} теперь имеет {stats.Kills} убийств");
        }
        else
        {
            // Создаём новую статистику если нет
            var newStats = new StatisticsData
            {
                UserId = userId,
                Kills = 1,
                Deaths = 0,
                TotalDamage = 0,
                PlayTime = 0,
                LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            statistics.Add(newStats);
            SaveAllData();
            Debug.Log($"✅ Создана новая статистика для пользователя {userId}");
        }
    }

    /// <summary>
    /// Обновить статистику смерти для пользователя
    /// </summary>
    public void UpdateDeathStatistics(int userId)
    {
        var stats = statistics.FirstOrDefault(s => s.UserId == userId);

        if (stats != null)
        {
            stats.Deaths++;
            SaveAllData();
        }
    }

    /// <summary>
    /// Обновить статистику урона для пользователя
    /// </summary>
    public void UpdateDamageStatistics(int userId, float damage)
    {
        var stats = statistics.FirstOrDefault(s => s.UserId == userId);

        if (stats != null)
        {
            stats.TotalDamage += Mathf.CeilToInt(damage);
            SaveAllData();
        }
    }

    public StatisticsData GetStats()
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("⚠️ GetStats(): пользователь не вошёл!");
            return null;
        }

        if (CurrentUser == null)
        {
            Debug.LogError("⚠️ GetStats(): CurrentUser = null!");
            return null;
        }

        Debug.Log($"🔍 Поиск статистики для UserId: {CurrentUser.Id}");

        var stats = statistics.FirstOrDefault(s => s.UserId == CurrentUser.Id);

        if (stats == null)
        {
            Debug.Log($"📊 Статистика не найдена для UserId={CurrentUser.Id}. Создаём...");

            var newStats = new StatisticsData
            {
                UserId = CurrentUser.Id,
                Kills = 0,
                Deaths = 0,
                TotalDamage = 0,
                PlayTime = 0,
                LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            statistics.Add(newStats);
            SaveAllData();

            return newStats;
        }

        Debug.Log($"✅ Статистика найдена: Kills={stats.Kills}, Deaths={stats.Deaths}");
        return stats;
    }

    public List<StatisticsData> GetAllStats()
    {
        if (statistics == null)
        {
            Debug.LogWarning("⚠️ GetAllStats(): statistics = null!");
            return new List<StatisticsData>();
        }
        return statistics.OrderByDescending(s => s.Kills).ToList();
    }
    // === УТИЛИТЫ ===

    string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }
    }

    bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }

    void OnApplicationQuit()
    {
        Debug.Log("💾 Выход из игры. Сохранение данных...");

        // Сохраняем статистику времени
        if (IsLoggedIn && CurrentUser != null)
        {
            var stats = statistics.FirstOrDefault(s => s.UserId == CurrentUser.Id);
            if (stats != null)
            {
                SaveAllData();
                Debug.Log($"✅ Статистика сохранена: PlayTime={stats.PlayTime}");
            }
        }

        Time.timeScale = 1f;
    }
    void OnDestroy()
    {
        Debug.Log("💾 Уничтожение объекта. Сохранение данных...");
        SaveAllData();
    }
}
