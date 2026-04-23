using UnityEngine;

/// <summary>
/// Đặt một object trong scene (ví dụ GameSystems), gán prefab <see cref="DamagePopup"/>.
/// <see cref="PlayerHealth"/> / <see cref="EnemyHealth"/> gọi <see cref="Show"/> khi nhận damage.
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamagePopup prefab;

    private static DamageNumberSpawner _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public static void Show(Vector3 worldPosition, int damage, Color color)
    {
        if (_instance == null || _instance.prefab == null) return;

        DamagePopup pop = Instantiate(_instance.prefab, worldPosition, Quaternion.identity);
        pop.Play(damage, color);
    }
}
