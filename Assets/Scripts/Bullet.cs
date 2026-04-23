using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask obstacleLayerMask;

    /// <summary>
    /// true  → đây là đạn của Player (chỉ sát thương Enemy).
    /// false → đây là đạn của Enemy  (chỉ sát thương Player).
    /// Được gán ngay sau Instantiate bởi PlayerShoot / EnemyAI.
    /// </summary>
    public bool isPlayerBullet = true;

    [SerializeField] private float ricochetThreshold = 0.6f; // Ngưỡng dot product để tính là sạt mép
    [SerializeField] private MuzzleFlash muzzleFlashPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isHitTank = false;
        bool hitEnemy = false;
        if (isPlayerBullet && other.CompareTag("Enemy"))
        {
            isHitTank = true;
            hitEnemy = true;
        }
        else if (!isPlayerBullet && other.CompareTag("Player"))
        {
            isHitTank = true;
            hitEnemy = false;
        }
        Instantiate(muzzleFlashPrefab, transform.position, transform.rotation);
        if (isHitTank)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            Vector2 bulletDir = rb != null ? rb.linearVelocity.normalized : (Vector2)transform.right;
            var otherTransform = other.transform;
            Vector2 localHit = otherTransform.InverseTransformPoint(transform.position);
            BoxCollider2D box = other as BoxCollider2D;
            if (box != null)
            {
                localHit.x /= box.size.x;
                localHit.y /= box.size.y;
            }

            Vector2 localNormal;
            if (Mathf.Abs(localHit.x) > Mathf.Abs(localHit.y))
            {
                localNormal = new Vector2(Mathf.Sign(localHit.x), 0f);
            }
            else
            {
                localNormal = new Vector2(0f, Mathf.Sign(localHit.y));
            }

            Vector2 hitNormal = otherTransform.TransformDirection(localNormal).normalized;
            float hitAngleDot = Vector2.Dot(bulletDir, hitNormal);
            if (hitAngleDot > -ricochetThreshold)
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, hitNormal);
                    transform.rotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f);
                    rb.linearVelocity *= 0.8f;
                }

                return;
            }

            if (hitEnemy)
            {
                EnemyHealth enemyHp = other.GetComponentInParent<EnemyHealth>();
                if (enemyHp != null) enemyHp.TakeDamage(damage);
            }
            else
            {
                PlayerHealth playerHp = other.GetComponentInParent<PlayerHealth>();
                if (playerHp != null) playerHp.TakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        if ((obstacleLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            Destroy(gameObject);
        }
    }
}