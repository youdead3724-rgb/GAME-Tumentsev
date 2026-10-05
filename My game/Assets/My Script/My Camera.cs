using UnityEngine;

[ExecuteAlways] // выполняет скрипт и в редакторе
public class CameraFollow : MonoBehaviour
{
    [Header("🎯 Цель")]
    [Tooltip("Объект, за которым следует камера (обычно машина)")]
    [SerializeField] private Transform target;

    [Header("📐 Смещение камеры")]
    [Tooltip("Базовое смещение камеры относительно машины (Y — высота, Z — отступ назад)")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 15f, -10f);

    [Header("🔄 Вращение камеры")]
    [Tooltip("Максимальный угол поворота камеры вокруг машины (градусы)")]
    [SerializeField] private float maxRotationAngle = 45f;

    [Tooltip("Скорость вращения камеры при зажатой правой кнопке мыши")]
    [SerializeField] private float rotationSpeed = 150f;

    [Tooltip("Плавность возврата камеры в исходное положение")]
    [SerializeField] private float rotationReturnSmoothTime = 0.3f;

    [Tooltip("Время задержки перед возвратом камеры после отпускания мыши (секунды)")]
    [SerializeField] private float rotationReturnDelay = 2f;

    private float currentRotationAngle;
    private float rotationVelocity;

    private float returnTimer = 0f;
    private bool isReturning = false;

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        UpdateCameraPosition(); // Только в игре
        HandleRotation();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying || target == null) return;
        UpdateCameraPosition(); // В редакторе
    }

    private void UpdateCameraPosition()
    {
        Vector3 directionBack = -Vector3.forward;
        Quaternion rotationAroundY = Quaternion.Euler(0f, currentRotationAngle, 0f);
        Vector3 rotatedDirection = rotationAroundY * directionBack;

        Vector3 offset = new Vector3(0f, baseOffset.y, 0f) + rotatedDirection * Mathf.Abs(baseOffset.z);
        transform.position = target.position + target.TransformDirection(offset);

        Vector3 lookDir = target.position - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentRotationAngle += mouseX * rotationSpeed * Time.deltaTime;
            currentRotationAngle = Mathf.Clamp(currentRotationAngle, -maxRotationAngle, maxRotationAngle);

            returnTimer = 0f;
            isReturning = false;
        }
        else
        {
            if (!isReturning)
            {
                returnTimer += Time.deltaTime;
                if (returnTimer >= rotationReturnDelay)
                {
                    isReturning = true;
                }
            }

            if (isReturning)
            {
                currentRotationAngle = Mathf.SmoothDamp(currentRotationAngle, 0f, ref rotationVelocity, rotationReturnSmoothTime);
            }
        }
    }
}
