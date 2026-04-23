using UnityEngine;

/// <summary>
/// Item HỒI MÁU – kế thừa từ <see cref="PickupItem"/>.
/// Khi player nhặt: hồi một lượng HP được cấu hình.
/// </summary>
public class HealthPickup : PickupItem
{
    [Header("Health Restore")]
    [SerializeField] private int healAmount = 1;

    protected override void OnPickup(PlayerHealth player)
    {
        player.Heal(healAmount);
    }
}
