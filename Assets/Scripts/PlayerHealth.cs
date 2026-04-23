using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private HealthBar healthBar;

    [Header("Hit Flash")]
    [SerializeField] private SpriteRenderer[] bodyRenderers;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Damage numbers (cần DamageNumberSpawner trong scene + prefab)")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageNumberColor = new Color(1f, 0.45f, 0.2f, 1f);
    [Tooltip("Empty child — kéo vào đây, đặt vị trí trong Scene; số damage spawn đúng tại transform này. Để trống thì dùng offset bên dưới.")]
    [SerializeField] private Transform damageNumberSpawnPoint;
    [Tooltip("Chỉ dùng khi Spawn Point để trống: vị trí = root tank + offset này (world).")]
    [SerializeField] private Vector3 damageNumberFallbackWorldOffset = new Vector3(0f, 0.55f, 0f);

    [Header("Death")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private ParticleSystem secondaryDeathEffect;
    [SerializeField] private Transform explosionPoint;

    private int currentHealth;
    private bool isDead;
    private AudioSource audioSource;

    /// <summary>Sự kiện gửi (currentHP, maxHP) mỗi khi HP thay đổi.</summary>
    public event Action<int, int> OnHealthChanged;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        healthBar?.SetHealth(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Khiên hấp thụ damage trước khi trừ HP thật
        PlayerShield shield = GetComponent<PlayerShield>();
        if (shield != null && shield.IsActive)
        {
            damage = shield.AbsorbDamage(damage);
            if (damage <= 0) return; // Khiên hấp thụ hết – HP không bị trừ
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        healthBar?.SetHealth(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.PlaySound(SoundType.TankHit, audioSource);
        }

        if (showDamageNumbers)
        {
            Vector3 pop = damageNumberSpawnPoint != null
                ? damageNumberSpawnPoint.position
                : transform.position + damageNumberFallbackWorldOffset;
            DamageNumberSpawner.Show(pop, damage, damageNumberColor);
        }

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(FlashHit());
    }

    /// <summary>Hồi HP cho player (dùng bởi HealthPickup). Không vượt quá maxHealth.</summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        healthBar?.SetHealth(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator FlashHit()
    {
        foreach (var sr in bodyRenderers)
            if (sr != null) sr.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        foreach (var sr in bodyRenderers)
            if (sr != null) sr.color = Color.white;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Vector3 deathPos = explosionPoint != null ? explosionPoint.position : transform.position;

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, deathPos, transform.rotation);
        }

        // Tắt hết effect trên xe (khói, lửa...) — trừ secondary death effect
        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
        {
            if (ps == secondaryDeathEffect) continue;
            var em = ps.emission;
            em.enabled = false;
            ps.Stop();
        }

        if (secondaryDeathEffect != null)
        {
            secondaryDeathEffect.Play();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundAtPosition(SoundType.TankExplode, transform.position);
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        PlayerShoot shoot = GetComponent<PlayerShoot>();
        if (shoot != null) shoot.enabled = false;

        TurretAim turret = GetComponentInChildren<TurretAim>();
        if (turret != null) turret.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType        = RigidbodyType2D.Kinematic;
        }

        float destroyDelay = 0f;
        if (secondaryDeathEffect != null)
            destroyDelay = secondaryDeathEffect.main.duration + secondaryDeathEffect.main.startLifetime.constantMax;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        Destroy(gameObject, destroyDelay);
    }
}
