using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class MeleeMonster : Monster
{
    [Header("近战设置")]
    public float attackRange = 1.5f;
    public float circleRadius = 3f;
    public float chargeInterval = 5f;
    private float lastChargeTime;

    protected override void Start()
    {
        base.Start();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateAI();
    }
    protected virtual void UpdateAI()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            Attack();
        }
        else if (distance <= circleRadius * 1.2f)
        {
            CircleAroundPlayer();
        }
        else
        {
            ApproachPlayer();
        }
    }

    void ApproachPlayer()
    {
        FollowPath();
    }

    void CircleAroundPlayer()
    {
        // 计算环绕方向
        Vector2 circleDir = Vector2.Perpendicular(player.position - transform.position).normalized;
        Vector2 targetPos = (Vector2)player.position + circleDir * circleRadius;

        Vector2Int targetGrid = gridSystem.WorldToGridPosition(targetPos);
        currentPath = gridSystem.FindPath(
            gridSystem.WorldToGridPosition(transform.position),
            targetGrid
        );

        FollowPath();

        // 随机冲锋
        if (Time.time - lastChargeTime > chargeInterval)
        {
            StartCoroutine(ChargeAttack());
            lastChargeTime = Time.time;
        }
    }

    IEnumerator ChargeAttack()
    {
        Vector2 chargeDir = (player.position - transform.position).normalized;
        float chargeSpeed = moveSpeed * 3f;
        float chargeTime = 0.5f;

        rb.velocity = chargeDir * chargeSpeed;
        yield return new WaitForSeconds(chargeTime);
        rb.velocity = Vector2.zero;
    }

    protected override Vector2Int GetTargetGridPosition()
    {
        return gridSystem.WorldToGridPosition(player.position);
    }

    void OnDrawGizmos()
    {
        if (currentPath == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(gridSystem.GridToWorldPosition(currentPath[i]), gridSystem.GridToWorldPosition(currentPath[i + 1]));
        }
    }
    protected virtual void Attack()
    {
        // 近战攻击实现
        if (Time.time - lastChargeTime > chargeInterval)
        {
            StartCoroutine(ChargeAttack());
            lastChargeTime = Time.time;
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject, hitLayer)) return;
        if (other.CompareTag("Player"))
        {
            // 处理玩家伤害...
            GameManager.Instance.player.GetComponent<PlayerCharacter>().TakeDamage(1);
            MonsterManager.Instance.ReturnMonster(gameObject);
        }
    }
    private void OnDisable()
    {
        isActive = true;
        lastChargeTime = Time.time;
        currentHealth = maxHealth;
        lastRepathTime = -1000f;
        currentPath = new List<Vector2Int>();
        currentPathIndex = 0;
    }
}