using UnityEngine;

public class PickupableItem : MonoBehaviour
{
    [Header("Информация о предмете")]
    public string itemName = "Предмет";
    public string description = "Описание предмета";

    [Header("Визуализация")]
    public float rotationSpeed = 50f;
    public float bobbingHeight = 0.3f;
    public float bobbingSpeed = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;

        // Если имя не задано, используем имя объекта
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        // Добавляем BoxCollider если его нет
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }

        // Убеждаемся, что коллайдер включен и является триггером для лучшего обнаружения
        col.isTrigger = true;
        col.enabled = true;

        Debug.Log($"Предмет '{itemName}' инициализирован с коллайдером {col.GetType().Name}");
    }


    void Update()
    {
        // Вращение предмета
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Покачивание вверх-вниз
        float newY = startPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnDrawGizmos()
    {
        // Рисуем сферу вокруг предмета в редакторе
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
