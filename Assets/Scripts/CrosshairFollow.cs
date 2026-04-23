using UnityEngine;
using UnityEngine.InputSystem;

public class CrosshairFollow : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

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

        Vector3 worldPos;

        // Python mode + có tay trỏ → dùng pointer hand
        if (ControlMode.IsPython && _receiver != null && _receiver.pointerActive)
        {
            float screenX = (1f - _receiver.pointerNorm.x) * Screen.width;
            float screenY = (1f - _receiver.pointerNorm.y) * Screen.height;
            worldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(screenX, screenY, Mathf.Abs(mainCamera.transform.position.z))
            );
        }
        // Keyboard/Mouse mode
        else
        {
            if (Mouse.current == null) return;
            Vector2 mp = Mouse.current.position.ReadValue();
            worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 0f));
        }

        worldPos.z = 0f;
        transform.position = worldPos;
    }
}