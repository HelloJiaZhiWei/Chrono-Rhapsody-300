// Monster.cs 怪物基类
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public abstract class Monster : CharacterBase
{

    protected bool isActive = false;
    [Header("AI设置")]
    protected List<Vector2Int> currentPath = new List<Vector2Int>();
    [SerializeField] protected int currentPathIndex = 0;
    [SerializeField] protected float repathInterval = 2f;
    protected float lastRepathTime = -1000f;
    protected Transform player;
    [SerializeField] protected LayerMask hitLayer;
    protected GridManager gridSystem;
    protected ParticleSystem hitEffect;

    protected override void Start()
    {
        base.Start();
        Initialize();
        if (!player) player = GameManager.Instance.player.transform;
        if (!gridSystem) gridSystem = GridManager.Instance;
        if (!hitEffect) hitEffect = GetComponentInChildren<ParticleSystem>();
    }
    protected virtual void FixedUpdate()
    {
        if (!isActive || GameManager.Instance.CurrentState != GameStates.GamePlay)
        {
            rb.velocity = Vector2.zero;
            return;
        }
    }
    public virtual void Initialize()
    {
        currentHealth = maxHealth;
        isActive = true;
        lastRepathTime = -1000f;
        currentPath = new List<Vector2Int>();
        currentPathIndex = 0;
    }
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        hitEffect.Play();
        Vector3 dir = -facingDirection;
        transform.position += (dir * moveSpeed * 0.1f);
    }
    public override void TakeDamage(float damage, Vector2 direction)
    {
        base.TakeDamage(damage, direction);
        hitEffect.Play();
        transform.position += ((Vector3)direction * moveSpeed * 0.1f);
    }
    protected override void Die()
    {
        isActive = false;
        StartCoroutine(ReturnToPool());
    }
    IEnumerator ReturnToPool()
    {
        yield return new WaitForSeconds(1f); // 播放死亡动画时间
        MonsterManager.Instance.ReturnMonster(gameObject);
    }
    protected virtual void UpdatePathfinding()
    {
        Vector2Int targetGrid = GetTargetGridPosition();
        Vector2Int currentGrid = gridSystem.WorldToGridPosition(transform.position);
        currentPath = gridSystem.FindPath(currentGrid, targetGrid);
        currentPathIndex = 0;
        lastRepathTime = Time.time;
    }
    protected virtual void FollowPath()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count || Time.time - lastRepathTime > repathInterval)
        {
            UpdatePathfinding();
            return;
        }

        Vector3 targetPos = gridSystem.GridToWorldPosition(currentPath[currentPathIndex]);
        if (Vector2.Distance(transform.position, targetPos) < 1f)
        {
            currentPathIndex++;
        }
        else
        {
            Vector3 moveDir = (targetPos - transform.position).normalized;
            rb.velocity = moveDir * moveSpeed;
            UpdateFacing(moveDir);
        }
    }
    protected abstract Vector2Int GetTargetGridPosition();
    protected bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
    protected virtual void OnEnable()
    {
        Initialize();
        if (!player) player = GameManager.Instance.player.transform;
        if (!gridSystem) gridSystem = GridManager.Instance;
        if (!hitEffect) hitEffect = GetComponentInChildren<ParticleSystem>();
    }
}