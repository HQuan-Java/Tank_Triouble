using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float acceleration = 3.5f;     // Giảm mạnh để tăng tốc từ từ
    [SerializeField] private float brakeDeceleration = 5f;
    [SerializeField] private float backwardSpeedMultiplier = 0.55f;
    [SerializeField] private float turnSpeed = 110f;

    [Header("Ram Collision")]
    [SerializeField] private float ramImpulseToEnemy = 14f;    // Đẩy Enemy thật mạnh
    [SerializeField] private float ramImpulseToPlayer = 10f;   // Đẩy ngược Player lại
    [SerializeField] private float inputLockDuration = 0.7f;
    [SerializeField] private int ramDamageToEnemy  = 1;
    [SerializeField] private int ramDamageToPlayer = 1;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    public SocketReceiver receiver;

    [Header("Effects")]
    [SerializeField] private ParticleSystem leftTireSmoke;
    [SerializeField] private ParticleSystem rightTireSmoke;

    private float moveInput;
    private float turnInput;
    private float inputLockTimer;

    private AudioSource movementAudio;
    private float _debugTimer;

    void Reset() => rb = GetComponent<Rigidbody2D>();

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        movementAudio = gameObject.AddComponent<AudioSource>();
        movementAudio.loop = true;
        movementAudio.spatialBlend = 1f; // Make it 3D if needed, or 0f for 2D. 3D is better for multiple tanks maybe
    }

    void Start()
    {
        // Tự tìm SocketReceiver nếu chưa gán trong Inspector
        if (receiver == null)
            receiver = FindFirstObjectByType<SocketReceiver>();

        if (SoundManager.Instance != null && movementAudio != null)
        {
            Sound sound = SoundManager.Instance.GetSound(SoundType.TankMoving);
            if (sound != null)
            {
                movementAudio.clip = sound.Clip;
                movementAudio.volume = sound.Volume;
            }
        }
    }

    void Update()
    {
        // Auto-tìm lại receiver nếu null (đề phòng Start() chưa kịp tìm)
        if (receiver == null)
            receiver = FindFirstObjectByType<SocketReceiver>();

        // Debug log mỗi 3s để kiểm tra trạng thái
        _debugTimer += Time.deltaTime;
        if (_debugTimer >= 3f)
        {
            _debugTimer = 0f;
            Debug.Log($"[Movement] Mode={ControlMode.Current} | " +
                      $"receiver={(receiver != null ? "OK" : "NULL")} | " +
                      $"gesture={receiver?.gesture ?? "N/A"}");
        }

        // Đang bị choáng sau va chạm → đếm ngược, không nhận input
        if (inputLockTimer > 0f)
        {
            inputLockTimer -= Time.deltaTime;
            moveInput = 0f;
            turnInput = 0f;
            return;
        }

        moveInput = 0f;
        turnInput = 0f;

        // --- Python hand: dùng moveDir/turnDir từ zone tay điều khiển ---
        if (ControlMode.IsPython && receiver != null)
        {
            moveInput = receiver.moveDir;
            turnInput = receiver.turnDir;
            HandleSmoke();
            return;
        }

        // --- Keyboard (WASD + Arrow) – bàn phím hoặc gesture STOP không ở Python mode ---
        if (Keyboard.current == null) return;

        bool fwd = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
        bool bwd = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        bool lft = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
        bool rgt = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;

        if      (fwd && !bwd) moveInput =  1f;
        else if (bwd && !fwd) moveInput = -1f;
        if      (lft && !rgt) turnInput =  1f;
        else if (rgt && !lft) turnInput = -1f;

        HandleSmoke();
    }

    private void HandleSmoke()
    {
        // Điều kiện văng khói: xe đang được lệnh di chuyển/rẽ và không bị choáng
        bool isMoving = (Mathf.Abs(moveInput) > 0.05f || Mathf.Abs(turnInput) > 0.05f) && inputLockTimer <= 0f;
        
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

        // Handle Movement Audio
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

    void FixedUpdate()
    {
        if (rb == null) return;

        // Xoay thân xe – chỉ khi không bị choáng
        if (inputLockTimer <= 0f)
        {
            // Xoay chậm hơn một chút khi đang tiến/lùi (cảm giác xe tăng)
            float turnReduction = Mathf.Abs(moveInput) > 0.01f ? 0.8f : 1f;
            float effectiveTurnInput = moveInput < 0f ? -turnInput : turnInput; // Hoán đổi lùi trái/lùi phải
            rb.MoveRotation(rb.rotation + effectiveTurnInput * turnSpeed * turnReduction * Time.fixedDeltaTime);
        }

        // Đang bị choáng → để Rigidbody2D physics tự "trượt" ra (không ghi đè velocity)
        if (inputLockTimer > 0f) return;

        Vector2 fwdDir = transform.right;

        // Chỉ lấy thành phần velocity theo trục tiến của xe
        // → hủy hoàn toàn drift ngang (xe tăng không trượt sang bên)
        float fwdSpeed = Vector2.Dot(rb.linearVelocity, fwdDir);

        // Tốc độ mục tiêu
        float targetSpeed = moveInput * maxSpeed;
        if (moveInput < 0f) targetSpeed *= backwardSpeedMultiplier;

        // Phanh nhanh hơn khi thả input; gia tốc khi bấm
        float rate = Mathf.Abs(moveInput) > 0.01f ? acceleration : brakeDeceleration;
        float newFwdSpeed = Mathf.MoveTowards(fwdSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        rb.linearVelocity = fwdDir * newFwdSpeed;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Enemy")) return;

        PlayerHealth playerHp = GetComponent<PlayerHealth>();
        if (playerHp != null && playerHp.IsDead) return;

        EnemyHealth enemyHp = col.gameObject.GetComponent<EnemyHealth>();
        if (enemyHp != null && enemyHp.IsDead) return;

        // Hướng từ player → enemy
        Vector2 hitDir = (col.transform.position - transform.position).normalized;

        // Đẩy enemy bay ra
        EnemyAI ai = col.gameObject.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.ApplyKnockback(hitDir * ramImpulseToEnemy);
        }
        else
        {
            Rigidbody2D enemyRb = col.rigidbody;
            if (enemyRb != null && enemyRb.bodyType != RigidbodyType2D.Kinematic)
                enemyRb.AddForce(hitDir * ramImpulseToEnemy, ForceMode2D.Impulse);
        }
        if (enemyHp != null) enemyHp.TakeDamage(ramDamageToEnemy);
        if (playerHp != null) playerHp.TakeDamage(ramDamageToPlayer);
        // Va chạm mạnh: Dừng xe và tác dụng lực bật ngược lại cho Player
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(-hitDir * ramImpulseToPlayer, ForceMode2D.Impulse);

        // Mắc kẹt: khoá input trong inputLockDuration giây
        inputLockTimer = inputLockDuration;
    }

    // Nhận độ giật lùi từ súng
    public void ApplyRecoil(Vector2 impulse)
    {
        if (rb == null) return;
        // Bơm thẳng xung lực giật lùi vào xe
        rb.AddForce(impulse, ForceMode2D.Impulse);
        
        // Khựng lại tẹo (0.05s) để có cảm giác xe tăng bị khựng vì súng giật
        inputLockTimer = Mathf.Max(inputLockTimer, 0.05f);
    }
}
