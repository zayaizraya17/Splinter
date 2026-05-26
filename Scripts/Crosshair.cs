using UnityEngine;
using UnityEngine.UI; // Обязательно для работы с UI

public class CrosshairUI : MonoBehaviour
{
    [Header("Settings")]
    public Sprite crosshairSprite;
    public float crosshairScale = 1f;
    public Color crosshairColor = Color.white;

    private Image crosshairImage;
    private GameObject crosshairObj;
    private Canvas canvas;

    void Start()
    {
        SetupCrosshair();
        // Скрываем прицел при старте, чтобы он не мелькал до начала игры
        HideCrosshair();
    }

    void SetupCrosshair()
    {
        // Создаем объект прицела программно
        crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(transform); // Или найдите Canvas: GameObject.Find("Canvas").transform

        // Если у вас есть Canvas на сцене, лучше привязать к нему, чтобы прицел был поверх всего
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length > 0)
        {
            crosshairObj.transform.SetParent(canvases[0].transform, false);
        }

        crosshairImage = crosshairObj.AddComponent<Image>();
        crosshairImage.sprite = crosshairSprite;
        crosshairImage.color = crosshairColor;

        RectTransform rectTransform = crosshairObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(32 * crosshairScale, 32 * crosshairScale); // Базовый размер 32x32

        // Центрируем прицел
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // Убедимся, что прицел поверх всего
        crosshairObj.transform.SetAsLastSibling();
    }

    public void ShowCrosshair()
    {
        if (crosshairObj != null)
        {
            crosshairObj.SetActive(true);
            // Debug.Log("Прицел показан");
        }
    }

    public void HideCrosshair()
    {
        if (crosshairObj != null)
        {
            crosshairObj.SetActive(false);
            // Debug.Log("Прицел скрыт");
        }
    }
}
