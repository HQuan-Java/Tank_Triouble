using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vòng tròn đếm ngược hồi đạn (Image Filled → Radial 360).
/// Gán <see cref="PlayerShoot"/> + Image fill; khi tắt cooldown trên PlayerShoot thì ẩn UI (test spam).
/// </summary>
[DisallowMultipleComponent]
public class ShootCooldownUI : MonoBehaviour
{
    [SerializeField] private PlayerShoot playerShoot;
    [Tooltip("Image kiểu Filled, Fill Method = Radial 360. fillAmount = thời gian còn lại / cooldown.")]
    [SerializeField] private Image radialFill;
    [Tooltip("Nếu có: ẩn cả nhóm khi cooldown tắt (ví dụ Canvas con).")]
    [SerializeField] private GameObject visualsRoot;

    void Reset()
    {
        if (playerShoot == null)
            playerShoot = FindFirstObjectByType<PlayerShoot>();
    }

    void Update()
    {
        if (playerShoot == null) return;

        bool show = playerShoot.FireCooldownEnabled;
        if (visualsRoot != null)
            visualsRoot.SetActive(show);

        if (!show || radialFill == null)
            return;

        float cd = playerShoot.FireCooldown;
        float rem = playerShoot.CooldownRemaining;
        radialFill.fillAmount = cd > 0.0001f ? Mathf.Clamp01(rem / cd) : 0f;
    }
}
