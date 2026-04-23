using UnityEngine;

/// <summary>
/// Item TĂNG KHIÊN – kế thừa từ <see cref="PickupItem"/>.
/// Khi player nhặt: thêm shield points vào <see cref="PlayerShield"/>.
/// Khiên hấp thụ damage trước khi trừ HP thật.
/// </summary>
public class ShieldPickup : PickupItem
{
    [Header("Shield")]
    [SerializeField] private int shieldPoints = 2;

    protected override void OnPickup(PlayerHealth player)
    {
        PlayerShield shield = player.GetComponent<PlayerShield>();
        if (shield == null)
        {
            Debug.LogWarning("[ShieldPickup] PlayerShield component not found on player! " +
                             "Please add PlayerShield to the Player GameObject.");
            return;
        }
        shield.AddShield(shieldPoints);
    }
}
