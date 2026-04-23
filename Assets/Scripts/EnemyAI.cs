using Pathfinding;
using StateMachine;
using UnityEngine;

/// <summary>
/// Enemy tank AI - 5 states: Patrol → Alert → Chase → Attack → Search
/// 
/// Setup checklist:
///  1. Assign "Wall" / obstacle layers to wallMask  (Inspector → Detection → Wall Mask)
///  2. Assign bulletPrefab, firePoint, turretTransform
///  3. Player is auto-found by "Player" tag; or drag manually
///  4. Press Play → select Enemy → watch Gizmos in Scene view
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  STATE ENUM
    // ═══════════════════════════════════════════════════════════════
    public enum AIState { Patrol, Alert, Chase, Attack, Search }

    // ═══════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════
    [SerializeField] public Seeker seeker;
    [Header("Effects")]
    [SerializeField] private ParticleSystem leftTireSmoke;
    [SerializeField] private ParticleSystem rightTireSmoke;

    [Header("References")]
    [SerializeField]
    public Transform  playerTransform;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private Transform  turretTransform;

    [Header("Detection")]
    [SerializeField] private float     detectRadius = 10f;
    [Tooltip("Layer(s) chứa Wall / tường để kiểm tra line-of-sight")]
    [SerializeField] private LayerMask wallMask;

    [Header("Reaction Delay (giây ngẫu nhiên khi phát hiện)")]
    [SerializeField]
    public float minReactionDelay = 0.10f;
    [SerializeField] public float maxReactionDelay = 0.40f;

    [Header("Movement")]
    [SerializeField]
    public float moveSpeed     = 2.5f;
    [SerializeField] private float bodyTurnSpeed = 120f;
    [SerializeField] private float acceleration  = 2.5f;    // Giảm từ 8f xuống 2.5f
    [SerializeField] private float brakePower    = 5f;      // Giảm từ 12f xuống 5f

    [Header("Patrol")]
    [Tooltip("Tâm vùng patrol – kéo Transform vào đây. Để trống sẽ dùng vị trí spawn.")]
    [SerializeField] private Transform patrolCenter;
    [SerializeField] private float wanderRadius      = 6f;
    [SerializeField] public float minIdleTime       = 0.8f;
    [SerializeField] public float maxIdleTime       = 3.5f;
    [SerializeField] public float waypointTolerance = 0.4f;

    [Header("Combat")]
    [SerializeField]
    public float attackRange      = 4.5f;
    [SerializeField] public float minFireInterval  = 0.9f;
    [SerializeField] private float maxFireInterval  = 1.8f;
    [Tooltip("Góc ngẫu nhiên thêm vào mỗi phát đạn (độ)")]
    [SerializeField] private float aimJitter        = 5f;
    [SerializeField] public float turretTrackSpeed = 160f;
    [SerializeField] private float bulletSpeed      = 10f;
    [SerializeField] private float recoilImpulse    = 2.5f;

    [Header("Search – sau khi mất player")]
    [Tooltip("Khôn tăng dần khi giao tranh, giảm khi rời giao tranh")]
    [SerializeField] [Range(0f, 1f)] public float searchIntelligence = 0.2f;
    [SerializeField] [Range(0f, 1f)] public float maxIntelligence = 0.95f;
    [SerializeField] [Range(0f, 1f)] public float minIntelligence = 0.1f;
    [SerializeField] public float minSearchTime = 2.0f;
    [SerializeField] public float maxSearchTime = 4.5f;

    [Header("Boids Avoidance")]
    [Tooltip("Bán kính phát hiện bám đuôi tránh nhau boids")]
    [SerializeField] private float avoidRadius = 3.5f;
    [Tooltip("Lực đẩy lẫn nhau (trọng số)")]
    [SerializeField] private float avoidWeight = 2.8f;

    [Header("Knockback")]
    [SerializeField] private float knockbackRecovery = 0.55f;

    [Header("Debug Gizmos")]
    [SerializeField] private bool showGizmos = true;

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════════

    public Rigidbody2D rb;

    // Cache GameManager cuc bo de tranh tim nham instance cu khi scene dang chuyen tiep.
    private GameManager _gm;

    // State machine
    private IEnemyState state;
    private AIState currentState;
    public AIState CurrentState
    {
        get => currentState;
        set
        {
            currentState = value;
            state?.Exit();
            switch (value)
            {
                case AIState.Patrol:
                    state = new EnemyPatrol(this);
                    break;
                case AIState.Alert:
                    state = new EnemyAlert(this);
                    break;
                case AIState.Chase:
                    state = new EnemyChase(this);
                    break;
                case AIState.Attack:
                    state = new EnemyAttack(this);
                    break;
                case AIState.Search:
                    state = new EnemySearch(this);
                    break;
            }
            state?.Enter();
        }
    }
    // Patrol
    private Vector2 spawnPoint;
    private Vector2 PatrolOrigin => patrolCenter != null ? (Vector2)patrolCenter.position : spawnPoint;
    public Vector2 patrolTarget;
    private bool    hasPatrolTarget;

    // Physics targets – set in Update, consumed in FixedUpdate
    private Vector2 targetVelocity;
    private float   targetBodyAngle;
    public bool    wantBrake;

    // Combat
    private float nextFireInterval;

    // Knockback
    private float knockbackTimer;

    private AudioSource shootAudioSource;
    private AudioSource movementAudio;

    // ── Pathfinding (A* Seeker) ──────────────────────────────────
    [Header("Pathfinding")]
    [Tooltip("Khoảng cách tới waypoint để coi là đã đến (unit)")]
    [SerializeField] public float pathNodeTolerance = 0.35f;
    [Tooltip("Chase: tần suất làm mới đường đi theo player (giây)")]
    [SerializeField] public float pathRepathRate    = 0.3f;

    private Path    currentPath;            // path hiện tại trả về từ Seeker
    private int     pathNodeIndex;          // waypoint đang nhắm tới
    public  bool    PathPending { get; private set; }    // đang chờ Seeker tính
    public  bool    ReachedPathEnd => currentPath != null
                                      && !PathPending
                                      && pathNodeIndex >= currentPath.vectorPath.Count;

    // ═══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        if (seeker == null) seeker = GetComponent<Seeker>();
        CurrentState = AIState.Patrol;
        spawnPoint = transform.position;
        if (turretTransform == null) turretTransform = transform.Find("Turret");
        nextFireInterval = Random.Range(minFireInterval, maxFireInterval);

        shootAudioSource = gameObject.AddComponent<AudioSource>();
        movementAudio = gameObject.AddComponent<AudioSource>();
        movementAudio.loop = true;
        movementAudio.spatialBlend = 1f;

        Debug.Log($"[EnemyAI:{name}] Awake | rb={rb != null} | seeker={seeker != null} | rbType={rb?.bodyType}");
    }

    void Start()
    {
        if (SoundManager.Instance != null && movementAudio != null)
        {
            Sound sound = SoundManager.Instance.GetSound(SoundType.TankMoving);
            if (sound != null)
            {
                movementAudio.clip = sound.Clip;
                movementAudio.volume = sound.Volume;
            }
        }
        // Auto-find player by tag if not assigned
        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        _gm = GameManager.Instance;
        PickNewPatrolTarget();
        RequestPath(patrolTarget);
        StartCoroutine(LogStateAfterFirstFrame());

        Debug.Log($"[EnemyAI:{name}] Start | player={(playerTransform != null ? playerTransform.name : "NULL")} | " +
                  $"GameManager={(GameManager.Instance != null ? "OK" : "NULL")} | " +
                  $"IsGameEnded={GameManager.Instance?.IsGameEnded} | " +
                  $"timeScale={Time.timeScale} | rbType={rb?.bodyType}");
    }

    // Log mot lan sau 1 frame de check trang thai sau khi tat ca Awake/Start chay xong
    System.Collections.IEnumerator LogStateAfterFirstFrame()
    {
        yield return null;
        Debug.Log($"[EnemyAI:{name}] Frame1 | state={CurrentState} | wantBrake={wantBrake} | " +
                  $"IsGameEnded(cached)={_gm?.IsGameEnded}(_gm={((_gm != null) ? "OK" : "NULL")}) | " +
                  $"IsGameEnded(static)={GameManager.Instance?.IsGameEnded} | " +
                  $"enabled={enabled} | rbType={rb?.bodyType} | " +
                  $"pathPending={PathPending} | currentPath={(currentPath != null ? "OK" : "NULL")}");
    }

    void Update()
    {
        if (_gm != null && _gm.IsGameEnded)
        {
            wantBrake = true;
            HandleSmoke();
            return;
        }

        // Cập nhật độ tập trung (khôn) tùy theo tình trạng combat
        if (CurrentState == AIState.Chase || CurrentState == AIState.Attack || CurrentState == AIState.Alert)
        {
            // Tăng trí khôn lên từ từ khi phát hiện kẻ địch vả đang vào form combat (đạt max sau ~6s)
            searchIntelligence = Mathf.MoveTowards(searchIntelligence, maxIntelligence, Time.deltaTime * 0.15f);
        }
        else
        {
            // Ngu bớt từ từ lại khi mât dấu quá lâu hoặc đang thảnh thơi dạo bước tuần tra (giảm min sau ~15s)
            searchIntelligence = Mathf.MoveTowards(searchIntelligence, minIntelligence, Time.deltaTime * 0.06f);
        }

        // Đang bị knockback – không làm gì thêm, chờ hết timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            wantBrake = true;
            HandleSmoke();
            return;
        }
        wantBrake = false;
        state?.Update();
        HandleSmoke();
    }

    private void HandleSmoke()
    {
        // Điều kiện văng khói: AI đang muốn di chuyển (!wantBrake) và không kẹt knockback
        bool isMoving = !wantBrake && knockbackTimer <= 0f;
        
        if (leftTireSmoke != null)
        {
            var em = leftTireSmoke.emission;
            if (em.enabled != isMoving) em.enabled = isMoving;
        }
        if (rightTireSmoke != null)
        {
            var em = rightTireSmoke.emission;
            if (em.enabled != isMoving) em.enabled = isMoving;
        }

        if (movementAudio != null && movementAudio.clip != null)
        {
            if (isMoving && !movementAudio.isPlaying)
            {
                movementAudio.Play();
            }
            else if (!isMoving && movementAudio.isPlaying)
            {
                movementAudio.Stop();
            }
        }
    }

    private Vector2 CalculateSeparationForce()
    {
        if (EnemyManager.Instance == null) return Vector2.zero;
        
        Vector2 force = Vector2.zero;
        int count = 0;
        
        void ProcessEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.AI == null || enemy.AI == this) return;
            
            float dist = Vector2.Distance(rb.position, enemy.AI.rb.position);
            if (dist > 0.01f && dist < avoidRadius)
            {
                Vector2 dir = rb.position - enemy.AI.rb.position;
                // Càng gần thì lực đẩy ra xa càng mạnh (tỉ lệ nghịch với khoảng cách)
                force += dir.normalized / dist; 
                count++;
            }
        }

        // Né các xe tăng còn sống
        foreach (var enemy in EnemyManager.Instance.Enemies)
        {
            ProcessEnemy(enemy);
        }
        
        // Né luôn cả xác của các xe tăng đã bị phá huỷ
        foreach (var enemy in EnemyManager.Instance.DeadEnemies)
        {
            ProcessEnemy(enemy);
        }
        
        if (count > 0)
        {
            force /= count;
        }
        return force;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Knockback: để physics tự xử lý, không ghi đè velocity
        if (knockbackTimer > 0f) return;

        // -- Boids Separation --
        Vector2 finalTargetVelocity = targetVelocity;
        if (!wantBrake && targetVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 separation = CalculateSeparationForce() * avoidWeight;
            finalTargetVelocity += separation;

            // Xoay hướng xe theo hướng thực tế muốn đi sau khi cộng lực Boids
            if (finalTargetVelocity.sqrMagnitude > 0.1f)
            {
                targetBodyAngle = Mathf.Atan2(finalTargetVelocity.y, finalTargetVelocity.x) * Mathf.Rad2Deg;
                
                // Đảm bảo vận tốc không vượt quá giới hạn
                if (finalTargetVelocity.magnitude > moveSpeed)
                {
                    finalTargetVelocity = finalTargetVelocity.normalized * moveSpeed;
                }
            }
        }

        // Xoay thân xe
        float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetBodyAngle, bodyTurnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);

        // Di chuyển
        Vector2 desired = wantBrake ? Vector2.zero : finalTargetVelocity;
        float   rate    = wantBrake ? brakePower   : acceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, rate * Time.fixedDeltaTime);
    }
    public void PickNewPatrolTarget()
    {
        // Seeker sẽ tự vòng tường, không cần raycast thủ công
        Vector2 raw    = Random.insideUnitCircle * wanderRadius;
        Vector2 jitter = Random.insideUnitCircle * 0.5f;
        Vector2 rawTarget = PatrolOrigin + raw + jitter;
        patrolTarget    = ClampToBounds(rawTarget);
        hasPatrolTarget = true;
    }

    // ──────────────────────────────────────────────────────────────
    //  PATH HELPERS
    // ──────────────────────────────────────────────────────────────

    /// <summary>Yêu cầu Seeker tính đường đến <paramref name="destination"/>.</summary>
    public void RequestPath(Vector2 destination)
    {
        if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker == null || !seeker.IsDone()) return;
        PathPending = true;
        seeker.StartPath(rb.position, ClampToBounds(destination), OnPathComplete);
    }

    private void OnPathComplete(Path p)
    {
        PathPending = false;
        if (p.error) return;
        currentPath    = p;
        pathNodeIndex  = 0;
    }

    /// <summary>
    /// Theo path hiện tại ở tốc độ <paramref name="speed"/>.
    /// Gọi mỗi frame trong Update của state di chuyển.
    /// </summary>
    public void FollowPath(float speed)
    {
        if (currentPath == null || ReachedPathEnd)
        {
            wantBrake = true;
            return;
        }

        Vector2 waypoint = currentPath.vectorPath[pathNodeIndex];
        SetBodyMoveTarget(waypoint, speed);

        // Tiến sang waypoint tiếp theo khi đã đủ gần
        if (Vector2.Distance(rb.position, waypoint) <= pathNodeTolerance)
            pathNodeIndex++;
    }

    /// <summary>Huỷ path đang có (gọi khi chuyển state không cần di chuyển).</summary>
    public void ClearPath()
    {
        currentPath   = null;
        pathNodeIndex = 0;
        PathPending   = false;
    }

    // Turret quét sang hai bên khi idle / search
    public void ScanTurret()
    {
        if (turretTransform == null) return;
        // Sin wave tạo cảm giác quét chậm, tự nhiên
        float angle = transform.eulerAngles.z + Mathf.Sin(Time.time * 1.1f) * 35f;
        turretTransform.rotation = Quaternion.RotateTowards(
            turretTransform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            turretTrackSpeed * Time.deltaTime);
    }
    public void EnterState(AIState next)
    {
        CurrentState = next;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MOVEMENT HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Giới hạn toạ độ bên trong graph của PathFinder.</summary>
    public Vector2 ClampToBounds(Vector2 target)
    {
        if (AstarPath.active != null)
        {
            // Lấy điểm gần nhất trên hệ thống pathfinder
            var nearest = AstarPath.active.GetNearest(target, NNConstraint.None);
            if (nearest.node != null)
            {
                return (Vector3)nearest.position;
            }
        }
        return target;
    }

    /// <summary>Tính góc xoay thân và tốc độ di chuyển về target, lưu vào biến cache để FixedUpdate dùng.</summary>
    public void SetBodyMoveTarget(Vector2 target, float speed)
    {
        target = ClampToBounds(target);
        Vector2 dir = target - rb.position;
        if (dir.sqrMagnitude < 0.001f) { wantBrake = true; return; }

        targetBodyAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        targetVelocity  = dir.normalized * speed;
    }

    public void AimTurretAt(Transform target, float speed)
    {
        if (turretTransform == null || target == null) return;
        Vector2 dir   = (Vector2)target.position - (Vector2)turretTransform.position;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float   cur   = turretTransform.eulerAngles.z;
        float   next  = Mathf.MoveTowardsAngle(cur, angle, speed * Time.deltaTime);
        turretTransform.rotation = Quaternion.Euler(0f, 0f, next);
    }

    // ═══════════════════════════════════════════════════════════════
    //  DETECTION  – khoảng cách + line-of-sight
    // ═══════════════════════════════════════════════════════════════

    public bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        // Bỏ qua nếu player đã chết
        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null && ph.IsDead) return false;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > detectRadius) return false;

        // Raycast kiểm tra có Wall chắn không
        Vector2      origin = transform.position;
        Vector2      dir    = (Vector2)playerTransform.position - origin;
        RaycastHit2D hit    = Physics2D.Raycast(origin, dir.normalized, dist, wallMask);

        return hit.collider == null;   // không bị chặn = thấy player
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMBAT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Gọi mỗi frame từ EnemyAttack. Trả về true nếu vừa bắn.</summary>
    public bool TryFire(ref float fireTimer)
    {
        fireTimer += Time.deltaTime;
        if (fireTimer < nextFireInterval) return false;

        Shoot();
        fireTimer        = 0f;
        nextFireInterval = Random.Range(minFireInterval, maxFireInterval);
        return true;
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // Thêm jitter ngẫu nhiên vào góc bắn
        float   jitter   = Random.Range(-aimJitter, aimJitter);
        Vector2 shootDir = Quaternion.Euler(0f, 0f, jitter) * firePoint.right;

        // Tính rotation của bullet theo hướng bắn, với -90° offset cho sprite
        Quaternion rot = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg - 90f);

        GameObject b = Instantiate(bulletPrefab, firePoint.position, rot);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null) bullet.isPlayerBullet = false;

        Rigidbody2D bRb = b.GetComponent<Rigidbody2D>();
        if (bRb != null) bRb.linearVelocity = shootDir * bulletSpeed;

        // Độ giật súng
        ApplyKnockback(-shootDir * recoilImpulse, 0.06f);

        if (SoundManager.Instance != null && shootAudioSource != null)
        {
            SoundManager.Instance.PlaySound(SoundType.TankFire, shootAudioSource);
        }

        Destroy(b, 3f);
    }

    // ═══════════════════════════════════════════════════════════════
    //  KNOCKBACK  – gọi từ PlayerMovement khi đâm vào enemy
    // ═══════════════════════════════════════════════════════════════

    public void ApplyKnockback(Vector2 impulse, float customDuration = -1f)
    {
        if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic) return;
        knockbackTimer = customDuration > 0f ? customDuration : knockbackRecovery;
        rb.AddForce(impulse, ForceMode2D.Impulse);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GIZMOS  – luôn hiển thị trong Scene view khi chọn enemy
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 pos    = transform.position;
        Vector3 origin = patrolCenter != null
            ? patrolCenter.position
            : (Application.isPlaying ? (Vector3)spawnPoint : pos);

        // ── Detect zone (cyan) – tâm = origin ─────────────────────
        UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.06f);
        UnityEditor.Handles.DrawSolidDisc(origin, Vector3.forward, detectRadius);
        UnityEditor.Handles.color = new Color(0f, 0.9f, 1f, 0.80f);
        UnityEditor.Handles.DrawWireDisc(origin, Vector3.forward, detectRadius);

        // ── Attack zone (orange-red) – tâm = origin ───────────────
        UnityEditor.Handles.color = new Color(1f, 0.35f, 0f, 0.09f);
        UnityEditor.Handles.DrawSolidDisc(origin, Vector3.forward, attackRange);
        UnityEditor.Handles.color = new Color(1f, 0.35f, 0f, 0.90f);
        UnityEditor.Handles.DrawWireDisc(origin, Vector3.forward, attackRange);

        // ── Wander zone (green) – tâm = origin ────────────────────
        UnityEditor.Handles.color = new Color(0.3f, 1f, 0.3f, 0.06f);
        UnityEditor.Handles.DrawSolidDisc(origin, Vector3.forward, wanderRadius);
        UnityEditor.Handles.color = new Color(0.3f, 1f, 0.3f, 0.55f);
        UnityEditor.Handles.DrawWireDisc(origin, Vector3.forward, wanderRadius);

        if (!Application.isPlaying) return;

        // ── LOS line – vàng = thấy, đỏ = bị chặn ─────────────────
        if (playerTransform != null)
        {
            bool sees    = CanSeePlayer();
            Gizmos.color = sees
                ? new Color(1f, 1f, 0f, 1.0f)
                : new Color(1f, 0.15f, 0.15f, 0.65f);
            Gizmos.DrawLine(pos, playerTransform.position);

            if (sees)
            {
                // Chấm nhỏ trên player khi đang nhìn thấy
                Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
                Gizmos.DrawSphere(playerTransform.position, 0.12f);
            }
        }

        // ── Patrol waypoint ────────────────────────────────────────
        if (CurrentState == AIState.Patrol && hasPatrolTarget)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.9f);
            Gizmos.DrawSphere((Vector3)patrolTarget, 0.18f);
            Gizmos.DrawLine(pos, (Vector3)patrolTarget);
        }

        // ── State label ────────────────────────────────────────────
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(pos + Vector3.up * 1.35f, CurrentState.ToString());
    }
#endif
}
