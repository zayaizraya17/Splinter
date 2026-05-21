using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathScreen : MonoBehaviour
{
    [Header("UI")]
    public GameObject deathPanel;
    public TextMeshProUGUI deathText;

    [Header("Animator")]
    public Animator deathTextAnimator;

    [Header("Настройки")]
    public float restartDelay = 5f;

    private bool isDead = false;

    void Start()
    {
        // Скрываем панель при старте
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // Получаем Animator если не назначен
        if (deathTextAnimator == null && deathText != null)
        {
            deathTextAnimator = deathText.GetComponent<Animator>();
        }
    }

    public void ShowDeathScreen()
    {
        if (isDead) return;

        isDead = true;

        // Показываем панель
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        // Запускаем анимацию
        if (deathTextAnimator != null)
        {
            deathTextAnimator.SetBool("IsDead", true);
        }

        Debug.Log("ВЫ ПОГИБЛИ!");
        Debug.Log($"Рестарт через {restartDelay} секунд...");

        // Перезапуск сцены через 5 секунд
        Invoke(nameof(RestartGame), restartDelay);
    }

    void RestartGame()
    {
        Debug.Log("Перезапуск игры...");
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}