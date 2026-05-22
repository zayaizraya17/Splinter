
﻿using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Система для отрисовки простых графиков на Canvas (требование 2.i)
/// </summary>
public class ChartSystem : MonoBehaviour
{
    [Header("Настройки графика")]
    public RectTransform chartContainer;      // Контейнер для графика
    public GameObject barPrefab;              // Префаб столбца
    public Color barColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color highlightColor = new Color(1f, 0.8f, 0.2f, 0.9f);

    [Header("Подписи")]
    public TextMeshProUGUI xAxisLabelPrefab;  // Подпись оси X
    public TextMeshProUGUI yAxisLabelPrefab;  // Подпись оси Y
    public TextMeshProUGUI titleText;         // Заголовок графика
    public TextMeshProUGUI legendText;        // Легенда

    private List<GameObject> bars = new List<GameObject>();
    private List<TextMeshProUGUI> xLabels = new List<TextMeshProUGUI>();

    /// <summary>
    /// Построить столбчатую диаграмму по данным статистики
    /// </summary>
    public void BuildBarChart(List<StatisticsData> stats, List<UserData> users, string title = "Статистика игроков")
    {
        ClearChart();

        if (stats == null || stats.Count == 0)
        {
            SetTitle(title);
            return;
        }

        SetTitle(title);

        // Берём топ-10 игроков по убийствам
        var topPlayers = stats.OrderByDescending(s => s.Kills).Take(10).ToList();

        if (topPlayers.Count == 0)
            return;

        // Находим максимальное значение для масштабирования
        float maxValue = topPlayers.Max(s => Mathf.Max(s.Kills, s.Deaths, s.TotalDamage));
        if (maxValue <= 0) maxValue = 1;

        // Создаём столбцы
        float barWidth = 40f;
        float spacing = 10f;
        float containerWidth = chartContainer.rect.width;
        float startX = -(containerWidth / 2) + barWidth / 2 + spacing;

        int index = 0;
        foreach (var stat in topPlayers)
        {
            var user = users.FirstOrDefault(u => u.Id == stat.UserId);
            string playerName = user != null ? user.Login : $"Игрок {stat.UserId}";

            // Обрезаем имя если слишком длинное
            if (playerName.Length > 8)
                playerName = playerName.Substring(0, 8) + "...";

            CreateBar(startX + index * (barWidth + spacing), stat.Kills, maxValue, playerName, barColor);
            index++;
        }

        UpdateLegend("🔵 Убийства");
    }

    /// <summary>
    /// Построить сравнительную диаграмму (убийства vs смерти)
    /// </summary>
    public void BuildComparisonChart(List<StatisticsData> stats, List<UserData> users, string title = "Сравнение статистики")
    {
        ClearChart();

        if (stats == null || stats.Count == 0)
        {
            SetTitle(title);
            return;
        }

        SetTitle(title);

        // Берём топ-5 игроков
        var topPlayers = stats.OrderByDescending(s => s.Kills).Take(5).ToList();

        if (topPlayers.Count == 0)
            return;

        float maxValue = topPlayers.Max(s => Mathf.Max(s.Kills, s.Deaths));
        if (maxValue <= 0) maxValue = 1;

        float barWidth = 30f;
        float spacing = 15f;
        float groupSpacing = 30f;
        float containerWidth = chartContainer.rect.width;
        float startX = -(containerWidth / 2) + spacing + barWidth / 2;

        int index = 0;
        foreach (var stat in topPlayers)
        {
            var user = users.FirstOrDefault(u => u.Id == stat.UserId);
            string playerName = user != null ? user.Login : $"Игрок {stat.UserId}";

            if (playerName.Length > 6)
                playerName = playerName.Substring(0, 6) + "...";

            float groupX = startX + index * (barWidth * 2 + spacing + groupSpacing);

            // Столбец убийств (синий)
            CreateBar(groupX - barWidth / 2 - spacing / 2, stat.Kills, maxValue, playerName, barColor, isGrouped: true);

            // Столбец смертей (красный)
            CreateBar(groupX + barWidth / 2 + spacing / 2, stat.Deaths, maxValue, playerName,
                     new Color(1f, 0.3f, 0.3f, 0.8f), isGrouped: true);

            index++;
        }

        UpdateLegend("🔵 Убийства  🔴 Смерти");
    }

    /// <summary>
    /// Построить круговую диаграмму (распределение урона)
    /// </summary>
    public void BuildPieChart(float kills, float deaths, float damage, string title = "Распределение")
    {
        ClearChart();
        SetTitle(title);

        float total = kills + deaths + damage;
        if (total <= 0)
        {
            UpdateLegend("Нет данных");
            return;
        }

        // Для круговой диаграммы используем Image с Fill Method
        // Это упрощённая реализация через сегменты
        float killAngle = (kills / total) * 360f;
        float deathAngle = (deaths / total) * 360f;
        float damageAngle = (damage / total) * 360f;

        UpdateLegend($"🔵 Убийства: {kills} ({killAngle:F0}°)\n🔴 Смерти: {deaths} ({deathAngle:F0}°)\n🟠 Урон: {damage} ({damageAngle:F0}°)");
    }

    private void CreateBar(float x, float value, float maxValue, string label, Color color, bool isGrouped = false)
    {
        if (barPrefab == null || chartContainer == null)
            return;

        // Создаём столбец
        GameObject bar = Instantiate(barPrefab, chartContainer);
        bar.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);

        // Масштабируем высоту
        float normalizedHeight = value / maxValue;
        float maxHeight = chartContainer.rect.height - 40f; // Оставляем место для подписей
        bar.GetComponent<RectTransform>().sizeDelta = new Vector2(
            bar.GetComponent<RectTransform>().sizeDelta.x,
            Mathf.Max(normalizedHeight * maxHeight, 5f) // Минимальная высота
        );

        // Устанавливаем цвет
        Image barImage = bar.GetComponent<Image>();
        if (barImage != null)
            barImage.color = color;

        bars.Add(bar);

        // Добавляем подпись
        if (!isGrouped && xAxisLabelPrefab != null)
        {
            TextMeshProUGUI labelText = Instantiate(xAxisLabelPrefab, chartContainer);
            labelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, -25f);
            labelText.text = label;
            labelText.fontSize = 10;
            xLabels.Add(labelText);
        }
    }

    private void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    private void UpdateLegend(string legend)
    {
        if (legendText != null)
            legendText.text = legend;
    }

    public void ClearChart()
    {
        foreach (var bar in bars)
        {
            if (bar != null)
                Destroy(bar);
        }
        bars.Clear();

        foreach (var label in xLabels)
        {
            if (label != null)
                Destroy(label);
        }
        xLabels.Clear();
    }

    void OnDestroy()
    {
        ClearChart();
    }

}
