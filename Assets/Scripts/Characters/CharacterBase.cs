// CharacterBase.cs 基类
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour
{
    [Header("基础属性")]
    public float moveSpeed = 5f;
    public int maxHealth = 1;
    public Vector2 facingDirection = Vector2.right;

    [Header("战斗属性")]
    public int attackDamage = 20;
    public float attackCooldown = 1f;

    protected Rigidbody2D rb;
    [SerializeField] protected float currentHealth;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }
    protected virtual void Update()
    {
        if (GameManager.Instance.CurrentState != GameStates.GamePlay) return;
    }

    public virtual void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }
    public virtual void TakeDamage(float damage, Vector2 direction)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }
    protected virtual void Die()
    {
        gameObject.SetActive(false);
    }

    protected void UpdateFacing(Vector2 inputDirection)
    {
        if (inputDirection != Vector2.zero)
        {
            facingDirection = inputDirection.normalized;
            float angle = Vector3.SignedAngle(Vector3.up, facingDirection, Vector3.forward);
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, rotation, Time.fixedTime);
        }
    }
}