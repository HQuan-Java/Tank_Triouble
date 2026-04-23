using UnityEngine;

/// <summary>
/// Item TÊN LỬA TỰ DẪN – kế thừa từ <see cref="PickupItem"/>.
/// Khi player nhặt: tự động phóng tên lửa bay về phía kẻ địch còn lại.
/// </summary>
public class HomingMissilePickup : PickupItem
{
    [Header("Homing Missile")]
    [SerializeField] private GameObject homingMissilePrefab;
    [SerializeField] private int missileCount = 1;
    [SerializeField] private float spawnOffset = 0.5f;

    protected override void OnPickup(PlayerHealth player)
    {
        if (homingMissilePrefab == null)
        {
            Debug.LogWarning("[HomingMissilePickup] Missing homingMissilePrefab! " +
                             "Please assign HomingMissile prefab in the Inspector.");
            return;
        }

        for (int i = 0; i < missileCount; i++)
        {
            // Offset nhỏ để nhiều tên lửa không chồng lên nhau
            Vector2 randomOffset = Random.insideUnitCircle.normalized * spawnOffset;
            Vector3 spawnPos = player.transform.position + (Vector3)randomOffset;
            Instantiate(homingMissilePrefab, spawnPos, Quaternion.identity);
        }
    }
}
