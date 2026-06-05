
﻿using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [Header("Настройки прицела")]
    public float crosshairSize = 10f;       // Размер прицела
    public float crosshairThickness = 2f;   // Толщина линий
    public Color crosshairColor = Color.green;  // Цвет прицела

    [Header("Отображение")]
    public bool showCrosshair = true;       // Показывать ли прицел

    void OnGUI()
    {
        if (!showCrosshair) return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // Создаем стиль для рисования
        GUIStyle style = new GUIStyle();
        style.normal.background = MakeTexture((int)crosshairThickness, (int)crosshairSize, crosshairColor);

        // Вертикальная линия
        GUI.DrawTexture(new Rect(centerX - crosshairThickness / 2f, centerY - crosshairSize / 2f, crosshairThickness, crosshairSize), style.normal.background);

        // Горизонтальная линия
        GUI.DrawTexture(new Rect(centerX - crosshairSize / 2f, centerY - crosshairThickness / 2f, crosshairSize, crosshairThickness), style.normal.background);

        // Точка в центре (опционально)
        GUIStyle dotStyle = new GUIStyle();
        dotStyle.normal.background = MakeTexture(4, 4, crosshairColor);
        GUI.DrawTexture(new Rect(centerX - 2f, centerY - 2f, 4, 4), dotStyle.normal.background);
    }

    // Создаем текстуру нужного размера и цвета
    Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }
}

