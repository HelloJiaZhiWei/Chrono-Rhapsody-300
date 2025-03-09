using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class RangedMonster : Monster
{
    [Header("远程设置")]
    [SerializeField] protected MonsterAttackType attackType = MonsterAttackType.Random;
    public float keepDistance = 50f;
    public FireConfig[] bulletConfig;
    public float attackAngleVariance = 30f;
    private bool isEvading = false;
    private float lastAttackTime = -1000f;
    private int index = 0;
    private bool isAttacking = false;
    protected override void Start()
    {
        base.Start();
        UpdatePathfinding();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (isAttacking) return;
        UpdateAI();
    }
    protected virtual void UpdateAI()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < keepDistance)
        {
            isEvading = true;
            FollowPath();
        }
        else if (distance > keepDistance * 1.2f)
        {
            isEvading = false;
            FollowPath();
        }
        else
        {
            Attack();
        }
    }
    void EvadePlayer()
    {
        Vector2Int currentGrid = gridSystem.WorldToGridPosition(transform.position);
        Vector2 evadeDir = (transform.position - player.position).normalized;
        Vector2 targetPos = (Vector2)player.position + evadeDir * keepDistance;

        Vector2Int targetGrid = gridSystem.WorldToGridPosition(targetPos);
        currentPath = gridSystem.FindPath(
            currentGrid,
            targetGrid
        );
        currentPathIndex = 0;
        lastRepathTime = Time.time;
    }
    protected override void FollowPath()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count || Time.time - lastRepathTime > repathInterval)
        {
            if (!isEvading)
            {
                UpdatePathfinding();
            }
            else
            {
                EvadePlayer();
            }
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
        }
    }
    protected virtual void Attack()
    {
        float intervalTime = bulletConfig[index].fireInterval * (bulletConfig[index].bulletCount + 1) + bulletConfig[index].waveInterval;
        if (Time.time - lastAttackTime > intervalTime)
        {
            StartCoroutine(AttackRoutine(intervalTime));
        }
    }
    IEnumerator AttackRoutine(float intervalTime)
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;
        int i = 0;
        if (attackType == MonsterAttackType.Order) index = (index + 1) % bulletConfig.Length;
        else index = Random.Range(0, bulletConfig.Length - 1);
        while (i < bulletConfig[index].waveCount)
        {
            i++;
            Vector2 baseDir = (player.position - transform.position).normalized;
            float angle = Random.Range(-attackAngleVariance, attackAngleVariance);
            Vector2 shootDir = Quaternion.Euler(0, 0, angle) * baseDir;
            BulletPatternManager.Instance.FirePattern(
                bulletConfig[index],
                transform.position,
                shootDir
            );
            yield return new WaitForSeconds(intervalTime);
        }


        yield return null;
        lastAttackTime = Time.time;
        isAttacking = false;
    }
    protected override Vector2Int GetTargetGridPosition()
    {
        return gridSystem.WorldToGridPosition(
            player.position + (Vector3)Random.insideUnitCircle * keepDistance
        );
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, keepDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, keepDistance * 1.2f);
    }
    protected override void OnEnable()
    {
        isActive = true;
        isAttacking = false;
        lastAttackTime = Time.time;
        currentHealth = maxHealth;
        lastRepathTime = -1000f;
        currentPath = new List<Vector2Int>();
        currentPathIndex = 0;
    }
    void OnDisable()
    {
        StopAllCoroutines();
    }
}