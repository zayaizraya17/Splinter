using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;

    [Header("References")]
    public PlayerController playerController;

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerController != null)
        {
            // Обновляем здоровье
            if (healthText != null)
            {
                healthText.text = $" {Mathf.CeilToInt(playerController.currentHealth)}/{Mathf.CeilToInt(playerController.maxHealth)}";
            }

            // Обновляем стамину
            if (staminaText != null)
            {
                staminaText.text = $" {Mathf.CeilToInt(playerController.currentStamina)}/{Mathf.CeilToInt(playerController.maxStamina)}";
            }
        }
    }
}