using System;
using UnityEngine;

/// <summary>
/// Gắn lên Enemy GameObject – khi enemy chết sẽ có cơ hội thả item.
/// Dùng weighted random: item nào có weight cao hơn thì xác suất rơi cao hơn.
/// </summary>
public class ItemDropper : MonoBehaviour
{
    [Tooltip("Xác suất rơi item tổng thể (0 = không bao giờ, 1 = luôn luôn).")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.6f;

    [Tooltip("Bán kính tối thiểu và tối đa để spawn item, tránh bị vướng xác enemy.")]
    [SerializeField] private float dropRadiusMin = 0.8f;
    [SerializeField] private float dropRadiusMax = 1.4f;

    [Tooltip("Bảng drop: mỗi entry là một loại item và trọng số xác suất của nó.")]
    [SerializeField] public ItemDropEntry[] dropTable;
    public void TryDrop(Vector3 position)
    {
        if (dropTable == null || dropTable.Length == 0) return;
        if (UnityEngine.Random.value > dropChance) return;

        Drop(position);
    }

    public void Drop(Vector3 position)
    {
        if (dropTable == null || dropTable.Length == 0) return;
        PickupItem chosen = WeightedRandom(dropTable);
        if (chosen != null)
        {
            float radius = UnityEngine.Random.Range(dropRadiusMin, dropRadiusMax);
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * radius;
            Vector3 spawnPos = position + new Vector3(offset.x, offset.y, 0f);
            Instantiate(chosen, spawnPos, Quaternion.identity);
        }
    }
    private PickupItem WeightedRandom(ItemDropEntry[] table)
    {
        float totalWeight = 0f;
        foreach (var entry in table)
            if (entry.Prefab != null) totalWeight += entry.Weight;

        if (totalWeight <= 0f) return null;

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var entry in table)
        {
            if (entry.Prefab == null) continue;
            cumulative += entry.Weight;
            if (roll <= cumulative) return entry.Prefab;
        }

        return table[table.Length - 1].Prefab;
    }
}
[Serializable]
public class ItemDropEntry
{
    [field: SerializeField] public PickupItem Prefab  { get; set; }
    [field: SerializeField, Min(0f)] public float Weight { get; set; } = 1f;
}
