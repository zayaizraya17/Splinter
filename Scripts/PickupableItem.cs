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
            itemName = gameObject.name;
        }
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
