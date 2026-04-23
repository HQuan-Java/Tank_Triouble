using UnityEngine;
using UnityEngine.InputSystem;

public class TurretAim : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float rotateSpeed = 240f;
    [SerializeField] private float angleOffset = 0f;

    private SocketReceiver _receiver;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        _receiver = FindFirstObjectByType<SocketReceiver>();
    }

    void Update()
    {
        if (mainCamera == null) return;

        Vector3 targetWorldPos;

        // ── Python mode: dùng tay trỏ ────────────────────────────
        if (ControlMode.IsPython
            && _receiver != null
            && _receiver.pointerActive)
        {
            // pointerNorm: (0,0) = góc trên-trái frame camera
            // Camera KHÔNG flip → tay phải người = bên trái frame → px nhỏ
            // Để aim tự nhiên (tay phải → bắn phải), mirror trục x:
            float screenX = (1f - _receiver.pointerNorm.x) * Screen.width;
            // y=0 là trên frame, y=0 là dưới screen → đảo ngược:
            float screenY = (1f - _receiver.pointerNorm.y) * Screen.height;

            targetWorldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(screenX, screenY, Mathf.Abs(mainCamera.transform.position.z))
            );
        }
        // ── Keyboard/Mouse mode ───────────────────────────────────
        else
        {
            if (Mouse.current == null) return;
            Vector2 mp = Mouse.current.position.ReadValue();
            targetWorldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(mp.x, mp.y, 0f)
            );
        }

        Vector2 direction  = (Vector2)(targetWorldPos - transform.position);
        float   targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;

        float currentAngle = transform.eulerAngles.z;
        float newAngle     = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotateSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }
}
