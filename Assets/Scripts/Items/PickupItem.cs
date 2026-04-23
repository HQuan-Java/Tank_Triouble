using System.Collections;
using UnityEngine;

/// <summary>
/// Lớp CƠ SỞ TRỪU TƯỢNG cho tất cả item nhặt được.
/// Xử lý: hiệu ứng xuất hiện (pop scale-in + arc), bob nổi, xoay, tự hủy, trigger Player.
/// Các lớp con chỉ cần override <see cref="OnPickup"/> để áp dụng hiệu ứng riêng.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class PickupItem : MonoBehaviour
{
    [Header("Base – Pickup Settings")]
    [SerializeField] private float autoDestroyTime = 10f;
    [SerializeField] private SoundType pickupSound = SoundType.ItemPickup;

    [Header("Spawn Animation")]
    [SerializeField] private float spawnArcHeight   = 0.6f;  // chiều cao cung bay
    [SerializeField] private float spawnArcDuration = 0.35f; // thời gian bay ra
    [SerializeField] private AnimationCurve spawnScaleCurve = DefaultScaleCurve();

    [Header("Bob Animation")]
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobFrequency = 2f;


    // trạng thái nội bộ
    private Vector3  startPos;
    private bool     spawnDone;
    private Collider2D col;

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    protected virtual void Start()
    {
        // Ẩn vật lý trong lúc đang bay ra
        if (col != null) col.enabled = false;
        transform.localScale = Vector3.zero;
        StartCoroutine(SpawnAnimation());
        Destroy(gameObject, autoDestroyTime);
    }

    // ─── Hiệu ứng xuất hiện ──────────────────────────────────────────────
    private IEnumerator SpawnAnimation()
    {
        Vector3 origin = transform.position;
        // Hướng arc ngẫu nhiên (đã được ItemDropper đẩy ra xa, arc thêm chiều đứng)
        Vector3 arcPeak = origin + new Vector3(0f, spawnArcHeight, 0f);

        float elapsed = 0f;
        while (elapsed < spawnArcDuration)
        {
            elapsed  += Time.deltaTime;
            float t   = Mathf.Clamp01(elapsed / spawnArcDuration);

            // Cung parabol: lerp từ origin → peak → origin (item "bật" ra rồi rơi tại chỗ)
            float arcY = Mathf.Sin(t * Mathf.PI) * spawnArcHeight;
            transform.position = new Vector3(origin.x, origin.y + arcY, origin.z);

            // Scale pop-in theo curve (0 → 1.3 → 1.0)
            float s = spawnScaleCurve.Evaluate(t);
            transform.localScale = Vector3.one * s;

            yield return null;
        }

        // Đặt lại vị trí chính xác và bật collider
        transform.position   = origin;
        transform.localScale = Vector3.one;
        startPos = origin;
        spawnDone = true;
        if (col != null) col.enabled = true;
    }

    // ─── Bob + Spin sau khi spawn xong ───────────────────────────────────
    protected virtual void Update()
    {
        if (!spawnDone) return;

        float y = startPos.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(startPos.x, y, startPos.z);
    }

    // ─── Nhặt item ───────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySoundAtPosition(pickupSound, transform.position);

        OnPickup(player);
        Destroy(gameObject);
    }

    /// <summary>Override trong lớp con: áp dụng hiệu ứng item lên player.</summary>
    protected abstract void OnPickup(PlayerHealth player);

    // ─── Default curve: 0→1.25→1.0 (bounce nhẹ) ────────────────────────
    private static AnimationCurve DefaultScaleCurve()
    {
        var curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f,    0f,   0f,  4f));   // bắt đầu từ 0, tốc độ vọt nhanh
        curve.AddKey(new Keyframe(0.55f, 1.25f, 0f, 0f));   // overshoot 125%
        curve.AddKey(new Keyframe(1f,    1f,   0f, 0f));    // về 100%
        return curve;
    }
}
