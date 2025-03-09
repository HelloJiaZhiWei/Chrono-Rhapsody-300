// PlayerCharacter.cs 玩家控制
using UnityEngine;

public class PlayerCharacter : CharacterBase
{
    public delegate void PlayerHealthHandler(float newhealth);
    public event PlayerHealthHandler OnHealthChanged;
    public float CurrentHealth
    {
        get => currentHealth;
        private set
        {
            if (currentHealth != value)
            {
                currentHealth = value;
                OnHealthChanged?.Invoke(value);
            }
        }
    }
    [Header("防御系统")]
    public SpriteRenderer shieldObject;
    public float postureRecoverSpeed = 10f;
    private float maxPostureValue = 100f;
    private float postureValue = 0;
    public float MaxPostureValue => maxPostureValue;
    public float CurrentPosture => postureValue;
    [Header("闪现")]
    public float dashDistance = 3f;
    public float dashDuration = 0.2f;
    public float lastDashTime;
    public float dashCooldownTime = 0.6f;
    public LayerMask obstacleMask;

    [Header("攻击")]
    [SerializeField] private AttackConfig[] attackConfigs;
    [SerializeField] private LayerMask enemyLayer;
    public LayerMask EnemyLayer => enemyLayer;
    private Animator anim;
    private Collider2D _collider;
    private int currentComboIndex;

    private bool isDashing;
    private bool isAttacking;
    private bool isDefending;
    private CharacterStates characterState;
    public CharacterStates CharacterState => characterState;

    protected override void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        shieldObject.enabled = false;
        _collider = GetComponent<Collider2D>();
        if (_collider) _collider.enabled = true;
    }
    protected override void Update()
    {
        base.Update();
        UpdatePosture();
        HandleMovement();
        HandleAttack();
        HandleDefense();
        HandleDash();
    }
    private void UpdatePosture()
    {
        postureValue = characterState == CharacterStates.Defending
        ? Mathf.Clamp(postureValue - Time.deltaTime * postureRecoverSpeed * 0.5f, 0, maxPostureValue)
        : Mathf.Clamp(postureValue - Time.deltaTime * postureRecoverSpeed, 0, maxPostureValue);
    }
    void HandleMovement()
    {
        if (isDashing || isAttacking || isDefending) return;
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
        rb.velocity = input * moveSpeed;
        UpdateFacing(input);
        if (rb.velocity.magnitude > 0.1f) characterState = CharacterStates.Moving;
        else characterState = CharacterStates.Idle;
    }

    void HandleAttack()
    {
        if (isDashing || isAttacking || isDefending) return;
        if (Input.GetButtonDown("Fire1"))
        {
            rb.velocity = Vector2.zero;
            StartCoroutine(AttackRoutine(currentComboIndex));
            currentComboIndex = (currentComboIndex + 1) % attackConfigs.Length;
        }
    }

    System.Collections.IEnumerator AttackRoutine(int attackIndex)
    {
        characterState = CharacterStates.Attacking;
        isAttacking = true;
        AttackConfig config = attackConfigs[attackIndex];
        anim.SetTrigger(config.animationTrigger);
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    void HandleDefense()
    {
        if (isDashing || isAttacking) return;
        if (Input.GetButtonDown("Fire2"))
        {
            rb.velocity = Vector2.zero;
            characterState = CharacterStates.Defending;
            isDefending = true;
            shieldObject.enabled = true;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            characterState = CharacterStates.Defending;
            isDefending = false;
            shieldObject.enabled = false;
        }
    }
    void HandleDash()
    {
        if (isDashing || isAttacking || isDefending || !CanDash()) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = Vector2.zero;
            StartCoroutine(DashRoutine());
        }
    }
    bool CanDash()
    {
        return Time.time - lastDashTime > dashCooldownTime;
    }
    System.Collections.IEnumerator DashRoutine()
    {
        characterState = CharacterStates.Dashing;
        isDashing = true;
        _collider.enabled = false;
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + facingDirection * dashDistance;

        // 碰撞检测
        RaycastHit2D hit = Physics2D.Raycast(
            startPos,
            facingDirection,
            dashDistance,
            obstacleMask);

        if (hit.collider)
        {
            targetPos = hit.point - (Vector2)facingDirection * 0.1f;
        }

        float elapsed = 0;
        while (elapsed < dashDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsed / dashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _collider.enabled = true;
        isDashing = false;
    }

    protected override void Die()
    {
        // 玩家死亡处理
        characterState = CharacterStates.Dead;
    }
    public override void TakeDamage(float damage)
    {
        CurrentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }
    public override void TakeDamage(float damage, Vector2 direction)
    {
        CurrentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }
    public void AddPlayerPosture(float value)
    {
        float sum = postureValue + value;
        postureValue = Mathf.Clamp(sum, 0, maxPostureValue);
        if (sum > maxPostureValue)
        {
            characterState = CharacterStates.Defending;
            isDefending = false;
            shieldObject.enabled = false;
        }
    }
}