using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý hệ thống KHIÊN cho Player.
/// Khi có khiên: hấp thụ toàn bộ damage trước khi trừ HP thật.
/// Gắn lên cùng GameObject với <see cref="PlayerHealth"/>.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private int maxShield = 6;

    [Header("Shield Visual")]
    [Tooltip("Child GameObject chứa SpriteRenderer để hiển thị khiên (hình tròn bán trong suốt).")]
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color shieldHitColor = new Color(0f, 0.85f, 1f, 0.9f);

    private int currentShield = 0;
    private SpriteRenderer shieldRenderer;

    public bool IsActive => currentShield > 0;
    public int CurrentShield => currentShield;
    public int MaxShield => maxShield;

    void Awake()
    {
        if (shieldVisual != null)
            shieldRenderer = shieldVisual.GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    /// <summary>Thêm shield points, giới hạn bởi maxShield.</summary>
    public void AddShield(int amount)
    {
        currentShield = Mathf.Min(currentShield + amount, maxShield);
        UpdateVisual();
        Debug.Log($"[PlayerShield] Shield +{amount} → {currentShield}/{maxShield}");
    }

    /// <summary>
    /// Hấp thụ damage bằng khiên.
    /// Trả về lượng damage CÒN LẠI sau khi khiên hấp thụ (sẽ trừ vào HP thật).
    /// </summary>
    public int AbsorbDamage(int damage)
    {
        if (currentShield <= 0) return damage;

        int absorbed = Mathf.Min(damage, currentShield);
        currentShield -= absorbed;
        int remaining = damage - absorbed;

        UpdateVisual();
        StopAllCoroutines();
        StartCoroutine(FlashShield());

        Debug.Log($"[PlayerShield] Absorbed {absorbed} dmg → Shield remaining: {currentShield}");
        return remaining;
    }

    private void UpdateVisual()
    {
        if (shieldVisual != null)
            shieldVisual.SetActive(currentShield > 0);
    }

    private IEnumerator FlashShield()
    {
        if (shieldRenderer == null) yield break;

        Color original = shieldRenderer.color;
        shieldRenderer.color = shieldHitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (shieldRenderer != null)
            shieldRenderer.color = original;
    }
}
