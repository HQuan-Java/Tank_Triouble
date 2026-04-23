using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private float recoilImpulse = 4f;

    [Header("Cooldown (tắt để test spam)")]
    [SerializeField] private bool fireCooldownEnabled = true;

    public SocketReceiver receiver;

    private float nextFireTime = 0f;
    private AudioSource audioSource;

    public bool FireCooldownEnabled => fireCooldownEnabled;
    public float FireCooldown => fireCooldown;
    /// <summary>Thời gian còn lại trước khi bắn tiếp (0 nếu đã sẵn sàng hoặc cooldown tắt).</summary>
    public float CooldownRemaining =>
        !fireCooldownEnabled ? 0f : Mathf.Max(0f, nextFireTime - Time.time);

    bool CanFire() => !fireCooldownEnabled || Time.time >= nextFireTime;

    void ApplyFireTiming()
    {
        if (fireCooldownEnabled)
            nextFireTime = Time.time + fireCooldown;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // Tự tìm SocketReceiver nếu chưa gán trong Inspector
        if (receiver == null)
            receiver = FindFirstObjectByType<SocketReceiver>();
    }

    void Update()
    {
        // =========================
        // 👉 SHOOT BẰNG TAY (Python mode): 5 ngón tay điều khiển = bắn
        // =========================
        if (ControlMode.IsPython && receiver != null && receiver.handShoot)
        {
            if (CanFire())
            {
                Shoot();
                ApplyFireTiming();
            }
            return;
        }

        // =========================
        // 👉 FALLBACK: BÀN PHÍM (bật cooldown = từng phát; tắt = giữ Space spam)
        // =========================
        if (Keyboard.current == null) return;

        bool wantShoot = fireCooldownEnabled
            ? Keyboard.current.spaceKey.wasPressedThisFrame
            : Keyboard.current.spaceKey.isPressed;

        if (wantShoot && CanFire())
        {
            Shoot();
            ApplyFireTiming();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(
                muzzleFlashPrefab,
                firePoint.position,
                firePoint.rotation * Quaternion.Euler(0f, 0f, -90f),
                firePoint
            );
            Destroy(flash, 0.06f);
        }

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation * Quaternion.Euler(0f, 0f, -90f)
        );

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.right * bulletSpeed;
        }

        // Tạo lực giật lùi cho xe tăng
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.ApplyRecoil(-firePoint.right * recoilImpulse);
        }

        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.PlaySound(SoundType.TankFire, audioSource);
        }

        Destroy(bullet, 3f);
    }
}