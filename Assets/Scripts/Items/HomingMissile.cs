using UnityEngine;

/// <summary>
/// TÊN LỬA TỰ DẪN – tự tìm và bay về phía kẻ địch gần nhất còn sống.
/// Chỉ gây sát thương Enemy, không ảnh hưởng Player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 9f;
    [SerializeField] private float rotationSpeed = 220f; // degrees/second

    [Header("Combat")]
    [SerializeField] private int damage = 2;
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("FX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float maxLifetime = 8f;

    private Rigidbody2D rb;
    private Enemy target;
    private bool exploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    void Start()
    {
        target = FindNearestEnemy();
        Destroy(gameObject, maxLifetime);
    }

    void FixedUpdate()
    {
        if (exploded) return;

        // Nếu mục tiêu đã chết hoặc null → tìm mục tiêu mới
        if (target == null || target.Health == null || target.Health.IsDead)
            target = FindNearestEnemy();

        if (target == null)
        {
            // Không còn kẻ địch → bay thẳng rồi tự hủy theo lifetime
            rb.linearVelocity = transform.up * speed;
            return;
        }

        // Tính góc xoay tới mục tiêu
        Vector2 dir = ((Vector2)target.transform.position - rb.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle,
                                                 rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);
        rb.linearVelocity = transform.up * speed;
    }

    private Enemy FindNearestEnemy()
    {
        var enemies = EnemyManager.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0) return null;

        Enemy nearest = null;
        float minDist = float.MaxValue;

        foreach (Enemy e in enemies)
        {
            if (e == null || e.Health == null || e.Health.IsDead) continue;
            float dist = Vector2.Distance(rb.position, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e; }
        }
        return nearest;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth hp = other.GetComponentInParent<EnemyHealth>();
            if (hp != null) hp.TakeDamage(damage);
            Explode();
            return;
        }

        if ((obstacleLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySoundAtPosition(SoundType.TankExplode, transform.position);

        Destroy(gameObject);
    }
}
