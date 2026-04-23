using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào một Canvas (World Space) con của xe tăng.
/// Gọi SetHealth() mỗi khi HP thay đổi để cập nhật thanh.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Tooltip("Giữ rotation thế giới = 0 → thanh HP không xoay theo thân xe. Không đổi vị trí/size bạn đã căn trong Editor.")]
    [SerializeField] private bool keepUprightWorldRotation = true;

    [SerializeField] private Image fillImage;

    [Header("Màu theo % HP (đầy → vừa → gần hết)")]
    [SerializeField] private Color colorFull  = new Color(0.2f, 0.85f, 0.25f, 1f);
    [SerializeField] private Color colorMid   = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color colorEmpty = new Color(0.95f, 0.2f, 0.2f, 1f);

    void LateUpdate()
    {
        if (!keepUprightWorldRotation) return;
        transform.rotation = Quaternion.identity;
    }

    static Color LerpHealthColor(float t, Color empty, Color mid, Color full)
    {
        t = Mathf.Clamp01(t);
        if (t > 0.5f)
            return Color.Lerp(mid, full, (t - 0.5f) * 2f);
        return Color.Lerp(empty, mid, t * 2f);
    }

    /// <summary>Cập nhật độ dài và màu thanh HP.</summary>
    public void SetHealth(int current, int max)
    {
        if (fillImage == null) return;
        float t = max > 0 ? (float)current / max : 0f;
        fillImage.fillAmount = t;
        fillImage.color = LerpHealthColor(t, colorEmpty, colorMid, colorFull);
    }
}
