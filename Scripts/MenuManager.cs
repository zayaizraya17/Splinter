using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class MenuManager : MonoBehaviour
{
    public static bool IsMenuOpen { get; private set; } = true;
    public static bool IsGameRunning { get; private set; } = false;

        [Header("Панели")]
    public GameObject preloaderPanel;
    public GameObject mainMenuPanel;
    public GameObject registerPanel;
    public GameObject loginPanel;
    public GameObject statisticsPanel;
    public GameObject hudPanel;
    public GameObject aboutPanel;
    public GameObject helpPanel;
    public GameObject chartPanel;
    public GameObject userMenuPanel;  // Пользовательское меню (во время игры)

    [Header("Главное меню")]
    public Button btnNewGame;
    public Button btnContinue;
    public Button btnStatistics;
    public Button btnExit;
    public Button btnAbout;           
    public Button btnHelp;              
    public Button btnAboutBack;        
    public Button btnHelpBack;

    [Header("Регистрация")]
    public TMP_InputField inputRegisterLogin;
    public TMP_InputField inputRegisterPassword;
    public TMP_InputField inputRegisterConfirm;
    public Button btnRegister;
    public Button btnRegisterBack;
    public TextMeshProUGUI registerErrorText;

    [Header("Вход")]
    public TMP_InputField inputLoginLogin;
    public TMP_InputField inputLoginPassword;
    public Button btnLogin;
    public Button btnLoginBack;
    public TextMeshProUGUI loginErrorText;

    [Header("Статистика - Панели")]
    public GameObject statisticsMainMenu;
    public GameObject myStatsPanel;
    public GameObject allStatsPanel;
    public GameObject exportPanel;
    public GameObject chartsPanel;

    [Header("Статистика - Текст")]
    public TextMeshProUGUI myStatsContent;
    public TextMeshProUGUI allStatsContent;
    public TextMeshProUGUI exportStatusText;

    [Header("Статистика - Кнопки")]
    public Button btnMyStats;           // ← ДОБАВИТЬ
    public Button btnAllStats;          // ← ДОБАВИТЬ
    public Button btnExportStats;
    public Button btnCharts;
    public Button btnStatsBack;
    public Button btnMyStatsBack;
    public Button btnAllStatsBack;
    public Button btnExportBack;
    public Button btnChartsBack;
    public Button btnExportMyStats;
    public Button btnExportAllStats;
    public TMP_InputField statsSearchInput;      // ← Поле поиска
    public TMP_Dropdown dropdownSort;               // ← Сортировка по времени


    [Header("Пользовательское меню (в игре)")]
    public Button btnUserStats;         // Статистика в пользовательском меню
    public Button btnUserCharts;        // Графики в пользовательском меню
    public Button btnSaveAndExit;       // Сохранить и выйти
    public Button btnUserMenuBack;      // Назад в игру из пользовательского меню

    [Header("Графики")]
    public ChartSystem chartSystem;               // ← Система графиков

    [Header("HUD")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI ammoText;

    [Header("Прелоадер")]
    public GameObject loadingBarFill;
    public TextMeshProUGUI preloaderText;

    [Header("Подсказки")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Ссылки")]
    public JsonDatabaseManager databaseManager;
    public PlayerController playerController;
    public WeaponManager weaponManager;
    public CrosshairUI crosshairUI;

    // Состояние
    private bool isGameRunning = false;
    private bool isPreloading = true;
    private float gameStartTime = 0f;

    // События (требование 3.h)
    public event Action OnGameStarted;
    public event Action OnGamePaused;
    public event Action OnGameSaved;

    // === ИНИЦИАЛИЗАЦИЯ ===

    void Start()
    {
        Time.timeScale = 1f;
        gameStartTime = Time.time;

        Debug.Log("🔍 MenuManager.Start() вызван");
        Debug.Log($"preloaderPanel: {preloaderPanel}");
        Debug.Log($"preloaderText: {preloaderText}");
        Debug.Log($"loadingBarFill: {loadingBarFill}");
        Debug.Log($"mainMenuPanel: {mainMenuPanel}");

        // Подписка на события
        if (databaseManager == null)
            databaseManager = FindFirstObjectByType<JsonDatabaseManager>();

        if (databaseManager != null)
        {
            databaseManager.OnDatabaseLoaded += OnDatabaseLoaded;
            databaseManager.OnUserLoggedIn += OnUserLoggedIn;
            databaseManager.OnUserLoggedOut += OnUserLoggedOut;
        }

        InitializeUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(PreloadGame());
    }

    void InitializeUI()
    {
        // Скрываем ВСЕ панели кроме прелоадера
        SetPanelActive(preloaderPanel, true);      // Показываем прелоадер
        SetPanelActive(mainMenuPanel, false);      // Скрываем меню
        SetPanelActive(registerPanel, false);
        SetupHotkeys();
        SubscribeToButtons();

        // Показ подсказок
        ShowTooltip("Загрузка игры...");
    }

    void SubscribeToButtons()
    {
        Debug.Log("🔧 SubscribeToButtons() ВЫЗВАН!");
     
        // Главное меню
        if (btnNewGame) btnNewGame.onClick.AddListener(ShowRegisterPanel);
        if (btnContinue) btnContinue.onClick.AddListener(ShowLoginPanel);
        if (btnStatistics) btnStatistics.onClick.AddListener(ShowStatisticsPanel);
        if (btnExit) btnExit.onClick.AddListener(ExitGame);

        // Пользовательское меню (в игре)
        if (btnSaveAndExit) btnSaveAndExit.onClick.AddListener(SaveAndExitToMainMenu);
        if (btnUserStats) btnUserStats.onClick.AddListener(ShowUserStatsPanel);
        if (btnUserCharts) btnUserCharts.onClick.AddListener(ShowUserChartsPanel);
        if (btnUserMenuBack) btnUserMenuBack.onClick.AddListener(CloseUserMenu);

        // Регистрация
        if (btnRegister) btnRegister.onClick.AddListener(OnRegisterClicked);
        if (btnRegisterBack) btnRegisterBack.onClick.AddListener(ShowMainMenu);

        // Вход
        if (btnLogin) btnLogin.onClick.AddListener(OnLoginClicked);
        if (btnLoginBack) btnLoginBack.onClick.AddListener(ShowMainMenu);

        // Статистика

        // Кнопка "Моя статистика"
        if (btnMyStats)
        {
            btnMyStats.onClick.RemoveAllListeners();
            btnMyStats.onClick.AddListener(ShowMyStatsPanel);
            Debug.Log("✅ Btn_MyStats настроена → ShowMyStatsPanel");
        }

        if (btnStatsBack)
        {
            btnStatsBack.onClick.RemoveAllListeners();
            btnStatsBack.onClick.AddListener(() =>
            {
                if (isGameRunning && databaseManager.IsLoggedIn)
                {
                    // Если игра запущена и пользователь вошел - возвращаемся в пользовательское меню
                    CloseUserMenu();
                    SetPanelActive(userMenuPanel, true);
                }
                else
                {
                    // Иначе - в главное меню
                    ShowMainMenu();
                }
            });
            Debug.Log("✅ Btn_Stats_Back настроена");
        }
        else
        {
            Debug.Log("⚠️ Btn_Stats_Back не назначена");
        }

            // Кнопка "Вся статистика"
            if (btnAllStats)
        {
            btnAllStats.onClick.RemoveAllListeners();
            btnAllStats.onClick.AddListener(ShowAllStatsPanel);
            Debug.Log("✅ Btn_AllStats настроена → ShowAllStatsPanel");
        }

        // Кнопка "Экспорт статистики" (в главном меню статистики)
        if (btnExportStats)
        {
            btnExportStats.onClick.RemoveAllListeners();
            btnExportStats.onClick.AddListener(ShowExportPanel);
            Debug.Log("✅ Btn_ExportStats настроена → ShowExportPanel");
        }

        // === СТАТИСТИКА - КНОПКИ "НАЗАД" ===
        // (этот код у тебя уже есть, оставь как есть)
        // Кнопки "Назад"
        if (btnMyStatsBack)
        {
            btnMyStatsBack.onClick.RemoveAllListeners();
            btnMyStatsBack.onClick.AddListener(ShowStatisticsMainMenu);
            Debug.Log("✅ Btn_MyStats_Back настроена");
        }

        if (btnAllStatsBack)
        {
            btnAllStatsBack.onClick.RemoveAllListeners();
            btnAllStatsBack.onClick.AddListener(ShowStatisticsMainMenu);
            Debug.Log("✅ Btn_AllStats_Back настроена");
        }

        if (btnExportBack)
        {
            btnExportBack.onClick.RemoveAllListeners();
            btnExportBack.onClick.AddListener(ShowStatisticsMainMenu);
            Debug.Log("✅ Btn_Export_Back настроена");
        }

        // Графики
        if (btnCharts)
        {
            btnCharts.onClick.RemoveAllListeners();
            btnCharts.onClick.AddListener(ShowChartsPanel);
            Debug.Log("✅ Btn_Charts настроена");
        }

        if (btnChartsBack)
        {
            btnChartsBack.onClick.RemoveAllListeners();
            btnChartsBack.onClick.AddListener(ShowStatisticsMainMenu);
            Debug.Log("✅ Btn_Charts_Back настроена");
        }



        // Настройка Dropdown для сортировки
        if (dropdownSort)
        {
            dropdownSort.onValueChanged.RemoveAllListeners();
            dropdownSort.onValueChanged.AddListener(SortStatisticsByDropdown);

            // Заполняем опции, если они ещё не заполнены в Unity
            if (dropdownSort.options.Count == 0)
            {
                dropdownSort.ClearOptions();
                dropdownSort.options.Add(new TMP_Dropdown.OptionData("Сортировка: Kills"));
                dropdownSort.options.Add(new TMP_Dropdown.OptionData("Сортировка: Deaths"));
                dropdownSort.options.Add(new TMP_Dropdown.OptionData("Сортировка: Damage"));
                dropdownSort.options.Add(new TMP_Dropdown.OptionData("Сортировка: Time"));
                dropdownSort.RefreshShownValue();
            }

            Debug.Log("✅ Dropdown сортировки настроен");
        }

        // About и Help
        if (btnAbout)
        {
            btnAbout.onClick.RemoveAllListeners();
            btnAbout.onClick.AddListener(ShowAboutPanel);
            Debug.Log("✅ Btn_About настроена");
        }

        if (btnHelp)
        {
            btnHelp.onClick.RemoveAllListeners();
            btnHelp.onClick.AddListener(ShowHelpPanel);
            Debug.Log("✅ Btn_Help настроена");
        }

        if (btnAboutBack)
        {
            btnAboutBack.onClick.RemoveAllListeners();
            btnAboutBack.onClick.AddListener(ShowMainMenu);
            Debug.Log("✅ Btn_About_Back настроена");
        }

        if (btnHelpBack)
        {
            btnHelpBack.onClick.RemoveAllListeners();
            btnHelpBack.onClick.AddListener(ShowMainMenu);
            Debug.Log("✅ Btn_Help_Back настроена");
        }

        if (btnExportMyStats)
        {
            btnExportMyStats.onClick.RemoveAllListeners();
            btnExportMyStats.onClick.AddListener(ExportMyStatistics);
            Debug.Log("✅ Btn_ExportMyStats настроена");
        }

        if (btnExportAllStats)
        {
            btnExportAllStats.onClick.RemoveAllListeners();
            btnExportAllStats.onClick.AddListener(ExportAllStatistics);
            Debug.Log("✅ Btn_ExportAllStats настроена");
        }

        if (statsSearchInput)
            statsSearchInput.onValueChanged.AddListener(FilterStatistics); 
        Debug.Log("🎉 Все кнопки настроены!");
    }

    void SetupHotkeys()
    {
        // Горячие клавиши (требование 2.h)
        Debug.Log("Горячие клавиши: Esc=Меню, Enter=Подтвердить, Tab=Инвентарь, Ctrl+S=Сохранить, F1=Помощь, F2=О игре");
    }

    // === ПРЕЛОАДЕР (требование 2.a) ===

    System.Collections.IEnumerator PreloadGame()
    {
        isPreloading = true;
        isGameRunning = false;
        MenuManager.IsMenuOpen = true;

        // ОСТАНАВЛИВАЕМ ВРЕМЯ
        Time.timeScale = 0f;

        // Проверка полей
        if (preloaderText == null)
        {
            Debug.LogError("❌ preloaderText не назначен!");
        }
        else
        {
            preloaderText.text = "ЗАГРУЗКА...";
        }

        // Имитация загрузки
        for (int i = 0; i <= 100; i += 10)
        {
            // Проверка loadingBarFill
            if (loadingBarFill != null)
            {
                Vector3 scale = loadingBarFill.transform.localScale;
                scale.x = i / 100f;
                loadingBarFill.transform.localScale = scale;
            }
            else
            {
                Debug.LogWarning($"⚠️ loadingBarFill не назначен (пропуск)");
            }

            if (preloaderText != null)
            {
                preloaderText.text = $"ЗАГРУЗКА... {i}%";
            }

            yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
        }

        yield return new UnityEngine.WaitForSecondsRealtime(0.5f);

        // ПОСЛЕ ПРЕЛОАДЕРА: показываем главное меню
        if (preloaderPanel != null)
            SetPanelActive(preloaderPanel, false);

        if (mainMenuPanel != null)
            SetPanelActive(mainMenuPanel, true);

        // Время ВСЁ ЕЩЁ остановлено
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPreloading = false;
        MenuManager.IsMenuOpen = true;
        isGameRunning = false;

        Debug.Log("✅ Прелоадер завершён! Показано главное меню.");
        ShowTooltip("Добро пожаловать! Выберите действие.");
    }

    // === НАВИГАЦИЯ ПО ПАНЕЛЯМ ===

    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    void ShowMainMenu()
    {
        // Показываем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MenuManager.IsMenuOpen = true;
        isGameRunning = false;

        // === ОСТАНАВЛИВАЕМ ВРЕМЯ ===
        Time.timeScale = 0f;

        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(registerPanel, false);
        SetPanelActive(loginPanel, false);
        SetPanelActive(statisticsPanel, false);
        SetPanelActive(aboutPanel, false);
        SetPanelActive(helpPanel, false);
        SetPanelActive(hudPanel, false);

        // === СКРЫТЬ ПРИЦЕЛ ===
        if (crosshairUI != null)
        {
            crosshairUI.HideCrosshair();
            Debug.Log("✅ Прицел скрыт");
        }
        // =======================
        ClearInputFields();
        ClearErrorMessages();

        ShowTooltip("Главное меню. Выберите действие.");
    }

    void ShowRegisterPanel()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(registerPanel, true);
        ShowTooltip("Введите логин (3-20 символов) и пароль (мин. 6 символов).");
    }

    void ShowLoginPanel()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(loginPanel, true);
        ShowTooltip("Введите логин и пароль для входа.");
    }

    // === НАВИГАЦИЯ ПО ПАНЕЛЯМ СТАТИСТИКИ ===
    public void CloseChartsPanel()
    {
        // 1. Скрываем панель графиков
        if (chartsPanel != null) // Или имя твоей переменной панели
        {
            chartsPanel.SetActive(false);
        }

        // 2. Очищаем график (опционально, для производительности)
        if (chartSystem != null)
        {
            chartSystem.ClearChart();
        }

        // 3. ВОЗВРАЩАЕМ УПРАВЛЕНИЕ ИГРОКУ
        // Возвращаем курсор в режим блокировки (для шутера)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Включаем время в игре (если оно останавливалось)
        Time.timeScale = 1f;

        Debug.Log("✅ Панель графиков закрыта, управление возвращено.");
    }
    void ShowStatisticsMainMenu()
    {
        Debug.Log("📊 ShowStatisticsMainMenu()");

        SetPanelActive(statisticsMainMenu, true);
        SetPanelActive(myStatsPanel, false);
        SetPanelActive(allStatsPanel, false);
        SetPanelActive(exportPanel, false);

        ShowTooltip("Статистика. Выберите действие.");
    }

    void ShowMyStatsPanel()
    {
        Debug.Log("📊 ShowMyStatsPanel()");

        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        SetPanelActive(statisticsMainMenu, false);
        SetPanelActive(myStatsPanel, true);
        SetPanelActive(allStatsPanel, false);
        SetPanelActive(exportPanel, false);

        // Показываем статистику
        DisplayMyStatistics();

        ShowTooltip("Ваша статистика. Нажмите НАЗАД.");
    }

    void ShowAllStatsPanel()
    {
        Debug.Log("📊 ShowAllStatsPanel()");

        if (!databaseManager.IsLoggedIn || databaseManager.CurrentUser.Role != "Admin")
        {
            ShowTooltip("❌ Только администратор!");
            ShowStatisticsMainMenu();
            return;
        }

        SetPanelActive(statisticsMainMenu, false);
        SetPanelActive(myStatsPanel, false);
        SetPanelActive(allStatsPanel, true);
        SetPanelActive(exportPanel, false);

        // Показываем статистику
        DisplayAllStatistics();

        ShowTooltip("Статистика всех игроков. Нажмите НАЗАД.");
    }

    void ShowExportPanel()
    {
        Debug.Log("📊 ShowExportPanel()");

        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        SetPanelActive(statisticsMainMenu, false);
        SetPanelActive(myStatsPanel, false);
        SetPanelActive(allStatsPanel, false);
        SetPanelActive(exportPanel, true);
        SetPanelActive(chartsPanel, false);

        if (exportStatusText != null)
            exportStatusText.text = "";

        ShowTooltip("Экспорт статистики. Выберите формат.");
    }

    void ShowChartsPanel()
    {
        Debug.Log("📊 ShowChartsPanel()");

        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        SetPanelActive(statisticsMainMenu, false);
        SetPanelActive(myStatsPanel, false);
        SetPanelActive(allStatsPanel, false);
        SetPanelActive(exportPanel, false);
        SetPanelActive(chartsPanel, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        // Получаем данные и строим графики
        if (chartsPanel != null)
        {
            chartsPanel.SetActive(true);

            // Освобождаем курсор для меню
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (chartSystem != null)
        {
            

        }

        ShowTooltip("Графики статистики. Нажмите НАЗАД.");
    }
    // === ОТОБРАЖЕНИЕ СТАТИСТИКИ ===

    void DisplayMyStatistics()
    {
        Debug.Log("📊 DisplayMyStatistics() вызван");
        Debug.Log($"   databaseManager: {databaseManager}");
        Debug.Log($"   IsLoggedIn: {databaseManager?.IsLoggedIn}");
        Debug.Log($"   CurrentUser: {databaseManager?.CurrentUser?.Login}");

        if (myStatsContent == null)
        {
            Debug.LogError("❌ myStatsContent не назначен!");
            return;
        }

        var stats = databaseManager.GetStats();

        Debug.Log($"   Получена статистика: {stats}");
        if (stats != null)
        {
            Debug.Log($"   Kills: {stats.Kills}, Deaths: {stats.Deaths}, Damage: {stats.TotalDamage}");
        }

        if (stats != null)
        {
            myStatsContent.text = FormatStatistics(stats, databaseManager.CurrentUser.Login);
        }
        else
        {
            myStatsContent.text = "Статистика не найдена.\nНачните играть!";
            Debug.LogWarning("⚠️ Статистика = null!");
        }
    }

    void DisplayAllStatistics()
    {
        if (allStatsContent == null)
        {
            Debug.LogError("❌ allStatsContent не назначен!");
            return;
        }

        var allStats = databaseManager.GetAllStats();

        if (allStats == null || allStats.Count == 0)
        {
            allStatsContent.text = "Пока нет данных.";
            return;
        }

        string text = "Игрок\t\tУбийства\tСмерти\tУрон\n";
        text += "===========================================\n";

        int rank = 1;
        foreach (var stat in allStats)
        {
            text += $"{rank}. Игрок #{stat.UserId}\t{stat.Kills}\t\t{stat.Deaths}\t{stat.TotalDamage}\n";
            rank++;
        }

        allStatsContent.text = text;
    }
    void ShowStatisticsPanel()
    {
        Debug.Log("📊 ShowStatisticsPanel() вызван");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        MenuManager.IsMenuOpen = true;

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(statisticsPanel, true);

        // Показываем главное меню статистики
        ShowStatisticsMainMenu();

        ShowTooltip("Статистика. Выберите раздел.");
    }

    void ShowAboutPanel()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        MenuManager.IsMenuOpen = true;

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(aboutPanel, true);
        ShowTooltip("Об игре. Нажмите Esc для возврата.");
    }

    void ShowHelpPanel()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        MenuManager.IsMenuOpen = true;

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(helpPanel, true);
        ShowTooltip("Помощь. Нажмите Esc для возврата.");
    }

    // === РЕГИСТРАЦИЯ ===

    void OnRegisterClicked()
    {
        // === ПРОВЕРКА: все ли поля назначены ===
        if (inputRegisterLogin == null)
        {
            Debug.LogError("❌ inputRegisterLogin не назначен в Inspector!");
            return;
        }

        if (inputRegisterPassword == null)
        {
            Debug.LogError("❌ inputRegisterPassword не назначен в Inspector!");
            return;
        }

        if (inputRegisterConfirm == null)
        {
            Debug.LogError("❌ inputRegisterConfirm не назначен в Inspector!");
            return;
        }

        if (registerErrorText == null)
        {
            Debug.LogError("❌ registerErrorText не назначен в Inspector!");
            return;
        }

        if (databaseManager == null)
        {
            Debug.LogError("❌ databaseManager не назначен в Inspector!");
            return;
        }

        // Получаем данные
        string login = inputRegisterLogin.text.Trim();
        string password = inputRegisterPassword.text;
        string confirm = inputRegisterConfirm.text;

        Debug.Log($" Регистрация: login={login}, password={password}, confirm={confirm}");

        // Проверка совпадения паролей
        if (password != confirm)
        {
            registerErrorText.text = "Пароли не совпадают!";
            registerErrorText.color = Color.red;
            ShowTooltip("Ошибка: пароли не совпадают!");
            return;
        }

        // Регистрация через базу данных
        if (databaseManager.RegisterUser(login, password, out string errorMessage))
        {
            registerErrorText.text = "✅ Регистрация успешна!";
            registerErrorText.color = Color.green;
            ShowTooltip($"Пользователь {login} зарегистрирован!");

            // Авто-вход после регистрации
            Invoke(nameof(AutoLoginAfterRegister), 1.5f);
        }
        else
        {
            registerErrorText.text = errorMessage;
            registerErrorText.color = Color.red;
            ShowTooltip($"Ошибка: {errorMessage}");
        }
    }

    void AutoLoginAfterRegister()
    {
        string login = inputRegisterLogin.text.Trim();
        string password = inputRegisterPassword.text;

        if (databaseManager.LoginUser(login, password, out _))
        {
            ShowMainMenu();  // Сначала показываем меню
                             // Игрок сам нажмёт "Продолжить" чтобы начать
        }
    }

    // === ВХОД ===

    void OnLoginClicked()
    {
        Debug.Log("🔘 OnLoginClicked() вызван");

        string login = inputLoginLogin.text.Trim();
        string password = inputLoginPassword.text;

        Debug.Log($"📝 Попытка входа: login={login}, password={password}");

        if (databaseManager.LoginUser(login, password, out string errorMessage))
        {
            Debug.Log("✅ Вход успешен! Запускаем таймер...");

            loginErrorText.text = "✅ Вход успешен!";
            loginErrorText.color = Color.green;
            ShowTooltip($"Добро пожаловать, {login}! Игра начнётся через 3 секунды...");

            if (isGameRunning)
            {
                Debug.LogWarning("⚠️ Игра уже запущена!");
                return;
            }

            // === ЗАПУСКАЕМ COROUTINE ВМЕСТО INVOKE ===
            Debug.Log("⏰ Запускаем корутину для запуска игры...");
            StartCoroutine(StartGameAfterDelay(3f));
        }
        else
        {
            Debug.LogError($"❌ Ошибка входа: {errorMessage}");
            loginErrorText.text = errorMessage;
            loginErrorText.color = Color.red;
            ShowTooltip($"Ошибка входа: {errorMessage}");
        }
    }

    // Новая корутина для задержки
    System.Collections.IEnumerator StartGameAfterDelay(float delay)
    {
        Debug.Log($"⏳ Ждём {delay} секунд (реальное время)...");
        yield return new UnityEngine.WaitForSecondsRealtime(delay);

        Debug.Log("⏰ Время вышло! Запускаем игру...");
        StartGame();
    }

    // === ИГРОВОЙ ПРОЦЕСС ===

    void StartGame()
    {
        Debug.Log("🎮 StartGame() вызван!");

        if (isGameRunning)
        {
            Debug.LogWarning("⚠️ Игра уже запущена!");
            return;
        }

        Debug.Log("🚀 Запуск игрового процесса...");

        isGameRunning = true;
        MenuManager.IsMenuOpen = false;

        Time.timeScale = 1f;

        // === ПОКАЗАТЬ ПРИЦЕЛ ===
        if (crosshairUI != null)
        {
            crosshairUI.ShowCrosshair();
            Debug.Log("✅ Прицел показан");
        }
        else
        {
            Debug.LogWarning("⚠️ crosshairUI не назначен в инспекторе!");
        }
        // =========================
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(loginPanel, false);
        SetPanelActive(registerPanel, false);
        SetPanelActive(statisticsPanel, false);

        SetPanelActive(hudPanel, true);

        // === ЗАГРУЗКА ПОСЛЕДНЕГО СОХРАНЕНИЯ ===
        LoadLastSave();
        // =======================================

        ShowGameplayHints();
        OnGameStarted?.Invoke();
        Debug.Log("✅ Игра запущена успешно!");
    }

    void LoadLastSave()
    {
        if (databaseManager == null || !databaseManager.IsLoggedIn)
        {
            Debug.LogWarning("⚠️ LoadLastSave(): Пользователь не вошёл!");
            return;
        }

        var saves = databaseManager.GetSaves();

        if (saves == null || saves.Count == 0)
        {
            Debug.Log("📄 Нет сохранений для этого пользователя.");
            return;
        }

        // Берём последнее сохранение
        var lastSave = saves.LastOrDefault();

        if (lastSave != null && playerController != null)
        {
            Debug.Log($"📥 Загрузка сохранения: {lastSave.SaveName}");

            playerController.transform.position = new Vector3(lastSave.PosX, lastSave.PosY, lastSave.PosZ);
            playerController.currentHealth = lastSave.Health;
            playerController.currentStamina = lastSave.Stamina;

            if (weaponManager != null)
            {
                weaponManager.pistolCurrentAmmo = lastSave.PistolAmmo;
                weaponManager.shotgunCurrentAmmo = lastSave.ShotgunAmmo;
            }

            Debug.Log($"✅ Сохранение загружено: HP={lastSave.Health}, Pos=({lastSave.PosX},{lastSave.PosY},{lastSave.PosZ})");
        }
    }
    void TogglePause()
    {
        Time.timeScale = Time.timeScale == 0 ? 1 : 0;
        isGameRunning = !isGameRunning;

        if (Time.timeScale == 0)
        {
            // Пауза - показываем курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            MenuManager.IsMenuOpen = true;

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(hudPanel, false);
            SetPanelActive(userMenuPanel, true);  // Показываем пользовательское меню
            ShowTooltip("Игра на паузе. Нажмите \"Назад\" для продолжения.");
            OnGamePaused?.Invoke();
        }
        else
        {
            // Продолжение - скрываем курсор
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            MenuManager.IsMenuOpen = false;

            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(userMenuPanel, false);  // Скрываем пользовательское меню
            SetPanelActive(hudPanel, true);
            ShowTooltip("Игра возобновлена!");
            Invoke(nameof(ClearTooltip), 2f);
        }
    }

    void ShowGameplayHints()
    {
        ShowTooltip("WASD - движение | ЛКМ - огонь | ПКМ - удар | R - перезарядка | Shift - бег");
        Invoke(nameof(ClearTooltip), 5f);
    }

    void Update()
    {
        // Горячие клавиши (требование 2.c, 2.h)
        HandleHotkeys();

        // Обновление HUD
        if (isGameRunning && playerController != null)
        {
            UpdateHUD();
            UpdatePlayTime();
        }
    }

    void UpdatePlayTime()
    {
        if (databaseManager != null && databaseManager.IsLoggedIn && gameStartTime > 0)
        {
            float playTime = Time.time - gameStartTime;

            // Обновляем статистику каждые 10 секунд
            if ((int)playTime % 10 == 0)
            {
                databaseManager.UpdatePlayTime(databaseManager.CurrentUser.Id, playTime);
            }
        }
    }

    void HandleHotkeys()
    {
        // Esc - пауза/меню (требование 2.c)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGameRunning)
            {
                TogglePause();
            }
            else if (!isPreloading)
            {
                // Закрыть открытые панели (About, Help, Statistics)
                if (aboutPanel.activeSelf || helpPanel.activeSelf || statisticsPanel.activeSelf)
                {
                    ShowMainMenu();
                }
                // F1 - Помощь
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    ShowHelpPanel();
                }

                // F2 - Об игре
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    ShowAboutPanel();
                }
            }
        }

        // Enter - подтверждение (требование 2.c)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (registerPanel.activeSelf)
                OnRegisterClicked();
            else if (loginPanel.activeSelf)
                OnLoginClicked();
        }

        // Tab - инвентарь (требование 2.c)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isGameRunning)
            {
                ToggleInventory();
            }
        }

        // Ctrl+S - сохранение (требование 2.h)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            if (isGameRunning)
            {
                SaveGame();
            }
        }

        // F1 - помощь (требование 2.l)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowHelpPanel();
        }

        // F2 - о игре (требование 2.l)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ShowAboutPanel();
        }
    }


    void ToggleInventory()
    {
        ShowTooltip("Инвентарь: 1-Пистолет | 2-Дробовик | 3-Нож | Q-выбросить");
        Invoke(nameof(ClearTooltip), 3f);
    }

    // === СОХРАНЕНИЕ (требование 3.j - потоки) ===

    void SaveGame()
    {
        Debug.Log("🔍 SaveGame() вызван!");
        Debug.Log($"   playerController: {playerController}");
        Debug.Log($"   databaseManager: {databaseManager}");
        Debug.Log($"   IsLoggedIn: {databaseManager?.IsLoggedIn}");
        Debug.Log($"   CurrentUser: {databaseManager?.CurrentUser?.Login}");

        if (playerController == null || databaseManager == null)
        {
            Debug.LogError("❌ SaveGame(): playerController или databaseManager = null!");
            return;
        }

        if (!databaseManager.IsLoggedIn)
        {
            Debug.LogError("❌ SaveGame(): Пользователь не вошёл в систему!");
            return;
        }

        Debug.Log("💾 Сохранение игры...");
        Debug.Log($"   HP: {playerController.currentHealth}");
        Debug.Log($"   Stamina: {playerController.currentStamina}");
        Debug.Log($"   Position: {playerController.transform.position}");

        // Асинхронное сохранение
        Task.Run(() =>
        {
            SaveData save = new SaveData
            {
                SaveName = $"Save_{DateTime.Now:yyyyMMdd_HHmmss}",
                UserId = databaseManager.CurrentUser.Id,
                Health = playerController.currentHealth,
                Stamina = playerController.currentStamina,
                PosX = playerController.transform.position.x,
                PosY = playerController.transform.position.y,
                PosZ = playerController.transform.position.z,
                PistolAmmo = weaponManager != null ? weaponManager.pistolCurrentAmmo : 10,
                ShotgunAmmo = weaponManager != null ? weaponManager.shotgunCurrentAmmo : 5,
                PlayTime = Time.time,
                LastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Debug.Log($"📝 Сохраняем: UserId={save.UserId}, HP={save.Health}, Pos=({save.PosX},{save.PosY},{save.PosZ})");

            databaseManager.SaveGame(save);

            Debug.Log($"✅ Игра сохранена!");
        });

        ShowTooltip("💾 Игра сохранена! (Ctrl+S)");
        OnGameSaved?.Invoke();
    }

    // === СТАТИСТИКА (требование 2.i, 2.j, 2.k) ===
    string FormatStatistics(StatisticsData stats, string playerName)
    {
        return $"=== СТАТИСТИКА: {playerName} ===\n\n" +
               $"Убийств: {stats.Kills}\n" +
               $"Смертей: {stats.Deaths}\n" +
               $"Общий урон: {stats.TotalDamage}\n" +
               $"Время в игре: {stats.PlayTime:F0} сек.\n" +
               $"Последняя игра: {stats.LastPlayed}";
    }

    void FilterStatistics(string searchText)
    {
        // Поиск по статистике (требование 2.j)
        ShowTooltip($"Поиск: {searchText}");

        if (string.IsNullOrEmpty(searchText))
        {
            // Если поиск пустой - показываем все данные
            if (allStatsPanel.activeSelf)
                DisplayAllStatistics();
            return;
        }

        // Получаем все данные
        var allStats = databaseManager.GetAllStats();
        var allUsers = databaseManager.GetAllUsers();

        // Фильтруем по имени игрока
        var filteredStats = allStats.Where(s =>
        {
            var user = allUsers.FirstOrDefault(u => u.Id == s.UserId);
            if (user != null && user.Login.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }).ToList();

        // Отображаем отфильтрованные данные
        if (allStatsContent == null)
        {
            Debug.LogError("❌ allStatsContent не назначен!");
            return;
        }

        if (filteredStats.Count == 0)
        {
            allStatsContent.text = $"Игроки по запросу \"{searchText}\" не найдены.";
            return;
        }

        string text = "Игрок\t\tУбийства\tСмерти\tУрон\tВремя\n";
        text += "===========================================\n";

        int rank = 1;
        foreach (var stat in filteredStats)
        {
            var user = allUsers.FirstOrDefault(u => u.Id == stat.UserId);
            string playerName = user != null ? user.Login : "Неизвестно";

            // Форматируем время
            TimeSpan timeSpan = TimeSpan.FromSeconds(stat.PlayTime);
            string timeFormatted = $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2} мин";

            text += $"{rank}. {playerName}\t{stat.Kills}\t\t{stat.Deaths}\t{stat.TotalDamage}\t{timeFormatted}\n";
            rank++;
        }

        allStatsContent.text = text;
        ShowTooltip($"Найдено игроков: {filteredStats.Count}"); 
    }


    void SortStatisticsByDropdown(int index)
    {
        if (!databaseManager.IsLoggedIn) return;

        var allStats = databaseManager.GetAllStats();
        if (allStats == null || allStats.Count == 0) return;

        List<StatisticsData> sortedStats = new List<StatisticsData>(allStats);

        // Сортировка по выбранному параметру
        switch (index)
        {
            case 0: // Kills
                sortedStats.Sort((a, b) => b.Kills.CompareTo(a.Kills));
                ShowTooltip("Сортировка: По убийствам");
                break;
            case 1: // Deaths
                sortedStats.Sort((a, b) => b.Deaths.CompareTo(a.Deaths));
                ShowTooltip("Сортировка: По смертям");
                break;
            case 2: // Damage
                sortedStats.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));
                ShowTooltip("Сортировка: По урону");
                break;
            case 3: // Time
                sortedStats.Sort((a, b) => b.PlayTime.CompareTo(a.PlayTime));
                ShowTooltip("Сортировка: По времени");
                break;
        }

        // Обновляем отображение
        DisplaySortedStatistics(sortedStats);



        string GetSortName(string sortBy)
        {
            switch (sortBy)
            {
                case "kills": return "убийствам";
                case "deaths": return "смертям";
                case "damage": return "урону";
                case "time": return "времени";
                default: return sortBy;
            }
        }
    }
    void DisplaySortedStatistics(List<StatisticsData> sortedStats)
    {
        if (allStatsContent == null) return;

        string text = "Игрок\t\tУбийства\tСмерти\tУрон\tВремя\n";
        text += "===========================================\n";

        int rank = 1;
        foreach (var stat in sortedStats)
        {
            text += $"{rank}. Игрок #{stat.UserId}\t{stat.Kills}\t\t{stat.Deaths}\t{stat.TotalDamage}\t{stat.PlayTime:F1}с\n";
            rank++;
        }

        allStatsContent.text = text;
    }
    /// 
    public void ExportAllStatistics()
    {
        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        // Проверка на админа
        if (databaseManager.CurrentUser.Role != "Admin")
        {
            ShowTooltip("❌ Только администратор может экспортировать всю статистику!");
            return;
        }

        // Путь для экспорта
        string exportFolder = Path.Combine(Application.persistentDataPath, "Exports");

        if (!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(exportFolder);
            Debug.Log($"📁 Создана папка для экспорта: {exportFolder}");
        }

        string fileName = $"Statistics_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(exportFolder, fileName);

        // Получаем все данные
        var allStats = databaseManager.GetAllStats();
        var allUsers = GetAllUsers();

        // Экспортируем
        if (ExportSystem.ExportStatisticsToCSV(allStats, allUsers, filePath))
        {
            exportStatusText.text = "✅ Экспорт успешен!";
            exportStatusText.color = Color.green;

            ShowTooltip($"📊 Статистика экспортирована!\nФайл: {fileName}");

            // Открываем файл сразу (без подтверждения)
            ExportSystem.OpenInExcel(filePath);
        }
        else
        {
            exportStatusText.text = "❌ Ошибка экспорта!";
            exportStatusText.color = Color.red;
            ShowTooltip("❌ Не удалось экспортировать статистику!");
        }
    }
   

    /// <summary>
    /// Экспорт моей статистики
    /// </summary>
    public void ExportMyStatistics()
    {
        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        var stats = databaseManager.GetStats();
        if (stats == null)
        {
            ShowTooltip("❌ Статистика не найдена!");
            return;
        }

        string exportFolder = Path.Combine(Application.persistentDataPath, "Exports");

        if (!Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);

        string fileName = $"MyStats_{databaseManager.CurrentUser.Login}_{DateTime.Now:yyyyMMdd}.csv";
        string filePath = Path.Combine(exportFolder, fileName);

        if (ExportSystem.ExportUserStatisticsToCSV(stats, databaseManager.CurrentUser.Login, filePath))
        {
            ShowTooltip($"📊 Ваша статистика экспортирована!\nФайл: {fileName}");

            // Открываем файл сразу
            ExportSystem.OpenInExcel(filePath);
        }
        else
        {
            ShowTooltip("❌ Ошибка экспорта!");
        }
    }

    // Вспомогательный метод для получения всех пользователей
    // Метод для получения всех пользователей (нужен для экспорта и админ-панели)
    private List<UserData> GetAllUsers()
    {
        if (databaseManager == null)
        {
            Debug.LogError("DatabaseManager не инициализирован!");
            return new List<UserData>();
        }

        // Получаем данные напрямую из базы через JsonDatabaseManager
        return databaseManager.GetAllUsers();
    }


    // === HUD ===

    void UpdateHUD()
    {
        if (healthText != null && playerController != null)
        {
            healthText.text = $"HP: {Mathf.CeilToInt(playerController.currentHealth)}/{Mathf.CeilToInt(playerController.maxHealth)}";
        }

        if (staminaText != null && playerController != null)
        {
            staminaText.text = $"Stamina: {Mathf.CeilToInt(playerController.currentStamina)}/{Mathf.CeilToInt(playerController.maxStamina)}";
        }

        if (ammoText != null && weaponManager != null)
        {
            ammoText.text = $"Ammo: {weaponManager.GetCurrentAmmo()}";
        }
    }

    // === ПОДСКАЗКИ (требование 2.g) ===

    void ShowTooltip(string message)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
        }

        if (tooltipText != null)
        {
            tooltipText.text = message;
        }

        Debug.Log($"💡 Подсказка: {message}");
    }

    void ClearTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    // === УТИЛИТЫ ===

    void ClearInputFields()
    {
        if (inputRegisterLogin) inputRegisterLogin.text = "";
        if (inputRegisterPassword) inputRegisterPassword.text = "";
        if (inputRegisterConfirm) inputRegisterConfirm.text = "";
        if (inputLoginLogin) inputLoginLogin.text = "";
        if (inputLoginPassword) inputLoginPassword.text = "";
        if (statsSearchInput) statsSearchInput.text = "";
    }

    void ClearErrorMessages()
    {
        if (registerErrorText)
        {
            registerErrorText.text = "";
            registerErrorText.color = Color.red;
        }
        if (loginErrorText)
        {
            loginErrorText.text = "";
            loginErrorText.color = Color.red;
        }
    }

    void ExitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // === ПОЛЬЗОВАТЕЛЬСКОЕ МЕНЮ (В ИГРЕ) ===

    void SaveAndExitToMainMenu()
    {
        Debug.Log("💾 Сохранение и выход в главное меню...");

        if (isGameRunning && playerController != null && databaseManager != null && databaseManager.IsLoggedIn)
        {
            SaveGame();
            Debug.Log("✅ Игра сохранена!");
        }

        // Останавливаем время
        Time.timeScale = 0f;
        isGameRunning = false;
        MenuManager.IsMenuOpen = true;

        // Скрываем пользовательское меню и HUD
        SetPanelActive(userMenuPanel, false);
        SetPanelActive(hudPanel, false);

        // Показываем главное меню
        SetPanelActive(mainMenuPanel, true);

        // Показываем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowTooltip("Игра сохранена. Вы в главном меню.");
    }

    void ShowUserStatsPanel()
    {
        Debug.Log("📊 ShowUserStatsPanel() - Статистика из пользовательского меню");

        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        // Скрываем пользовательское меню
        SetPanelActive(userMenuPanel, false);

        // Показываем панель статистики
        SetPanelActive(statisticsPanel, true);
        ShowStatisticsMainMenu();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowTooltip("Статистика. Выберите раздел.");
    }

    void ShowUserChartsPanel()
    {
        Debug.Log("📈 ShowUserChartsPanel() - Графики из пользовательского меню");

        if (!databaseManager.IsLoggedIn)
        {
            ShowTooltip("❌ Сначала войдите в систему!");
            return;
        }

        // Скрываем пользовательское меню
        SetPanelActive(userMenuPanel, false);

        // Показываем панель графиков
        SetPanelActive(chartsPanel, true);

        // Получаем данные и строим графики
        if (chartSystem != null)
        {
            chartSystem.ClearChart();
            // Здесь можно добавить загрузку данных для графиков
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowTooltip("Графики статистики. Нажмите НАЗАД для возврата.");
    }

    void CloseUserMenu()
    {
        Debug.Log("🔙 CloseUserMenu() - Возврат в игру");

        // Скрываем пользовательское меню
        SetPanelActive(userMenuPanel, false);

        // Возвращаем управление игроку
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        MenuManager.IsMenuOpen = false;

        // Показываем HUD
        SetPanelActive(hudPanel, true);

        // Запускаем время
        Time.timeScale = 1f;
        isGameRunning = true;

        ShowTooltip("Игра возобновлена!");
        Invoke(nameof(ClearTooltip), 2f);
    }

    // === СОБЫТИЯ БАЗЫ ДАННЫХ ===

    void OnDatabaseLoaded()
    {
        Debug.Log("✅ База данных загружена!");
        // НЕ запускаем игру здесь!

    }


    void OnUserLoggedIn(UserData user)
    {
        Debug.Log($"✅ Пользователь вошёл: {user.Login} ({user.Role})");
        // НЕ запускаем игру автоматически!
    }

    void OnUserLoggedOut()
    {
        Debug.Log("✅ Пользователь вышел");
        isGameRunning = false;
        MenuManager.IsMenuOpen = true;

        // Показываем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPanelActive(hudPanel, false);
        SetPanelActive(mainMenuPanel, true);

        // === СКРЫТЬ ПРИЦЕЛ ===
        if (crosshairUI != null)
        {
            crosshairUI.HideCrosshair();
        }
        // =======================
    }

    // === ОЧИСТКА ===

    void OnDestroy()
    {
        if (databaseManager != null)
        {
            databaseManager.OnDatabaseLoaded -= OnDatabaseLoaded;
            databaseManager.OnUserLoggedIn -= OnUserLoggedIn;
            databaseManager.OnUserLoggedOut -= OnUserLoggedOut;
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("💾 Выход из игры. Сохранение данных...");

        // Сохраняем игру перед выходом (если игра запущена)
        if (isGameRunning && playerController != null && databaseManager != null && databaseManager.IsLoggedIn)
        {
            Debug.Log("📥 Автосохранение при выходе...");
            SaveGame();

            // Ждём немного для завершения сохранения
            System.Threading.Thread.Sleep(500);
        }

        // Восстанавливаем время при выходе
        Time.timeScale = 1f;
    }
}
