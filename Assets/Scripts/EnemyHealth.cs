using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private HealthBar healthBar;

    [Header("Hit Flash")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer turretRenderer;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Damage numbers (cần DamageNumberSpawner trong scene + prefab)")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageNumberColor = new Color(1f, 0.92f, 0.25f, 1f);
    [Tooltip("Empty child — kéo vào đây, đặt vị trí trong Scene; số damage spawn đúng tại transform này. Để trống thì dùng offset bên dưới.")]
    [SerializeField] private Transform damageNumberSpawnPoint;
    [Tooltip("Chỉ dùng khi Spawn Point để trống: vị trí = root enemy + offset này (world).")]
    [SerializeField] private Vector3 damageNumberFallbackWorldOffset = new Vector3(0f, 0.55f, 0f);

    [Header("Death")]
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private ParticleSystem secondaryDeathEffect;
    [SerializeField] private Transform explosionPoint;
    [SerializeField] private Color burnedColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private int currentHealth;
    private bool isDead;
    private AudioSource audioSource;

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
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        healthBar?.SetHealth(currentHealth, maxHealth);

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

    private IEnumerator FlashHit()
    {
        if (bodyRenderer != null)   bodyRenderer.color   = hitFlashColor;
        if (turretRenderer != null) turretRenderer.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        if (bodyRenderer != null)   bodyRenderer.color   = Color.white;
        if (turretRenderer != null) turretRenderer.color = Color.white;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Vector3 deathPos = explosionPoint != null ? explosionPoint.position : transform.position;

        if (destroyEffectPrefab != null)
        {
            Instantiate(destroyEffectPrefab, deathPos, transform.rotation);
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

        if (bodyRenderer != null)   bodyRenderer.color   = burnedColor;
        if (turretRenderer != null) turretRenderer.color = burnedColor;

        if (healthBar != null) healthBar.gameObject.SetActive(false);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType        = RigidbodyType2D.Kinematic;
        }
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script != this) script.enabled = false;
        }

        GetComponent<Enemy>().OnDeath();

        // ItemDropper dropper = GetComponent<ItemDropper>();
        // dropper?.TryDrop(transform.position);
    }
}
