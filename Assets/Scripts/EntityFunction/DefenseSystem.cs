using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

// DefenseSystem.cs 防御系统（更新版）
public class DefenseSystem : MonoBehaviour
{
    [Header("防御参数")]
    [Header("防御时间窗口（秒）")]
    [SerializeField] private float earlyThreshold = 0.2f;   // 过早判定阈值
    [SerializeField] private float perfectThreshold = 0.15f; // 完美防御窗口
    [SerializeField] private float lateThreshold = 0.05f;   // 过晚判定阈值
    [Header("碰撞检测")]
    [SerializeField] private BoxCollider2D parryCollider;
    [SerializeField] private LayerMask enemyAttackLayer;
    [Header("反馈效果")]
    [SerializeField] private ParticleSystem perfectParryEffect;
    [SerializeField] private AudioClip perfectParrySound;
    [SerializeField] private ParticleSystem normalParryEffect;
    [SerializeField] private AudioClip normalParrySound;
    [SerializeField] private ParticleSystem blockEffect;
    [SerializeField] private AudioClip blockSound;
    private PlayerCharacter player;
    private bool isDefending;
    private float defenseStartTime;
    private bool hasProcessedParry;
    private Coroutine defenseCoroutine;
    public delegate void ParryResultHandler(ParryResult result);
    public event ParryResultHandler OnParryResult;
    public enum ParryResult
    {
        Perfect,   // 完美弹反
        Early,     // 过早防御
        Late,      // 过晚防御
        Blocked    // 普通防御
    }
    void Start()
    {
        player = GameManager.Instance.player.GetComponent<PlayerCharacter>();
    }
    void Update()
    {
        if (GetComponentInParent<PlayerCharacter>().CharacterState == CharacterStates.Defending && !isDefending)
        {
            defenseCoroutine = StartCoroutine(DefenseRoutine());
        }
    }
    IEnumerator DefenseRoutine()
    {
        // 初始化防御状态
        InitializeDefense();

        // 开启碰撞检测
        parryCollider.enabled = true;
        defenseStartTime = Time.time;
        hasProcessedParry = false;

        // 防御持续时间
        //float timer = 0f;
        while (GetComponentInParent<PlayerCharacter>().CharacterState == CharacterStates.Defending)
        {
            //timer += Time.deltaTime;

            // 实时检测碰撞
            CheckContinuousCollision();

            yield return null;
        }

        // 结束防御
        TerminateDefense();
    }
    void InitializeDefense()
    {
        isDefending = true;
        // 可以在这里触发防御动画
    }
    void TerminateDefense()
    {
        parryCollider.enabled = false;
        isDefending = false;

        // 如果没有触发任何判定，视为普通防御
        if (!hasProcessedParry)
        {
            OnParryResult?.Invoke(ParryResult.Blocked);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject, enemyAttackLayer)) return;
        ProcessParry(other);
    }
    void CheckContinuousCollision()
    {
        // 持续检测避免快速攻击漏判
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            parryCollider.bounds.center,
            parryCollider.size,
            0,
            enemyAttackLayer
        );

        foreach (var hit in hits)
        {
            if (hit != null)
            {
                ProcessParry(hit);
            }
        }
    }
    void ProcessParry(Collider2D attackCollider)
    {
        if (hasProcessedParry) return;

        float elapsedTime = Time.time - defenseStartTime;
        ParryResult result = DetermineParryResult(elapsedTime);

        // 触发结果处理
        HandleParryResult(result, attackCollider);
        hasProcessedParry = true;

        // 立即结束防御流程
        if (defenseCoroutine != null)
            StopCoroutine(defenseCoroutine);
        TerminateDefense();
    }
    ParryResult DetermineParryResult(float elapsedTime)
    {
        if (elapsedTime < earlyThreshold)
            return ParryResult.Early;
        else if (elapsedTime < earlyThreshold + perfectThreshold)
            return ParryResult.Perfect;
        else if (elapsedTime < lateThreshold + perfectThreshold + earlyThreshold)
            return ParryResult.Late;
        else
            return ParryResult.Blocked;
    }
    void HandleParryResult(ParryResult result, Collider2D attackCollider)
    {
        switch (result)
        {
            case ParryResult.Perfect:
                PlayPerfectParryEffect();
                ApplyPerfectParryEffects(attackCollider);
                break;

            case ParryResult.Early:
                PlayNormalParryEffect();
                ApplyEarlyParryPenalty(attackCollider);
                break;

            case ParryResult.Late:
                PlayNormalParryEffect();
                ApplyLateParryPenalty(attackCollider);
                break;
            case ParryResult.Blocked:
                PlayBlockParryEffect();
                ApplyBlockPenalty(attackCollider);
                break;
        }

        OnParryResult?.Invoke(result);
    }

    void PlayPerfectParryEffect()
    {
        if (perfectParryEffect != null)
            perfectParryEffect.Play();

        if (perfectParrySound != null)
            AudioSource.PlayClipAtPoint(perfectParrySound, transform.position);
    }

    void ApplyPerfectParryEffects(Collider2D attackCollider)
    {
        // 反弹攻击逻辑
        if (attackCollider.TryGetComponent<BaseBullet>(out var attack))
        {
            attack.Deflect(transform.up);
        }
        player.AddPlayerPosture(10f);
    }

    void ApplyEarlyParryPenalty(Collider2D attackCollider)
    {
        if (attackCollider.TryGetComponent<BaseBullet>(out var attack))
        {
            attack.Blocked();
        }
        player.AddPlayerPosture(20f);
    }

    void ApplyLateParryPenalty(Collider2D attackCollider)
    {
        if (attackCollider.TryGetComponent<BaseBullet>(out var attack))
        {
            attack.Blocked();
        }
        player.AddPlayerPosture(20f);
    }
    void PlayNormalParryEffect()
    {
        if (normalParryEffect != null)
            normalParryEffect.Play();

        if (normalParrySound != null)
            AudioSource.PlayClipAtPoint(normalParrySound, transform.position);
    }
    void ApplyBlockPenalty(Collider2D attackCollider)
    {
        if (attackCollider.TryGetComponent<BaseBullet>(out var attack))
        {
            attack.Blocked();
        }
        player.AddPlayerPosture(25f);
    }
    void PlayBlockParryEffect()
    {
        if (blockEffect != null)
            blockEffect.Play();

        if (blockSound != null)
            AudioSource.PlayClipAtPoint(blockSound, transform.position);
    }
    bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}

#region 暂时弃用
// public class DefenseSystem : MonoBehaviour
// {
//     #region 核心系统
//     [Header("Shield Parameter")]
//     [SerializeField] private float radiusDetact = 100f;
//     [SerializeField] private LayerMask detactLayer;
//     [SerializeField] private float detactInterval = 0.1f;
//     [Header("Defense Parameter")]
//     [SerializeField] private float perfectBlockWindow = 0.15f;
//     [SerializeField] private float blockCooldown = 0f;
//     [SerializeField] private float blockAngle = 120f;
//     [SerializeField] private float bulletDeflectSpeed = 15f;
//     [Header("Visual Setting")]
//     [SerializeField] private ParticleSystem perfectBlockEffect;
//     [SerializeField] private ParticleSystem normalBlockEffect;
//     [SerializeField] private AudioClip perfectBlockSound;
//     [SerializeField] private AudioClip normalBlockSound;

//     private Transform playerTransform;
//     private SpriteRenderer sprite;
//     private float lastBlockTime;
//     private float lastDetactTime;
//     private bool isBlocking;
//     [SerializeField] private List<BaseBullet> trackedBullets = new List<BaseBullet>();

//     void Start()
//     {
//         playerTransform = transform.parent;
//         sprite = GetComponent<SpriteRenderer>();
//         sprite.enabled = false;
//     }
//     void Update()
//     {
//         //HandleBlockInput();
//         CheckBulletCollisions();
//     }
//     void LateUpdate()
//     {
//         AutoAddBulletToList();
//         AutoRemoveBulletFromList();
//     }
//     #endregion
//     #region 输入处理
//     public void HandleBlockInput()
//     {
//         //bool blockInput = Input.GetButtonDown("Block");
//         if (Time.time - lastBlockTime > blockCooldown)
//         {
//             StartCoroutine(BlockRoutine());
//             lastBlockTime = Time.time;
//         }
//     }
//     IEnumerator BlockRoutine()
//     {
//         isBlocking = true;
//         sprite.enabled = true;
//         float timer = 0;
//         while (timer < perfectBlockWindow)
//         {
//             timer += Time.deltaTime;
//             yield return null;
//         }
//         isBlocking = false;
//         sprite.enabled = false;
//     }
//     #endregion

//     #region 碰撞检测
//     void AutoAddBulletToList()
//     {
//         if (Time.time - lastDetactTime < detactInterval) return;
//         Collider2D[] colliders = Physics2D.OverlapBoxAll(playerTransform.position, new Vector2(radiusDetact, radiusDetact), 0, detactLayer);
//         foreach (Collider2D collider in colliders)
//         {
//             BaseBullet bullet = collider.GetComponent<BaseBullet>();
//             if (trackedBullets.Contains(bullet)) continue;
//             trackedBullets.Add(bullet);
//         }
//         lastDetactTime = Time.time;
//     }
//     void AutoRemoveBulletFromList()
//     {
//         if (Time.time - lastDetactTime < detactInterval) return;

//         foreach (BaseBullet bullet in trackedBullets)
//         {
//             if (Vector2.Distance((Vector2)bullet.transform.position, playerTransform.position) > radiusDetact)
//             {
//                 trackedBullets.Remove(bullet);
//             }
//         }
//     }
//     void CheckBulletCollisions()
//     {
//         for (int i = trackedBullets.Count - 1; i >= 0; i--)
//         {
//             BaseBullet bullet = trackedBullets[i];
//             if (!bullet) continue;

//             if (IsInBlockRange(bullet.transform.position))
//             {
//                 float distance = Vector2.Distance(transform.position, bullet.transform.position);
//                 float timeToImpact = distance / bullet.Speed;
//                 if (timeToImpact <= perfectBlockWindow * 2f)
//                 {
//                     EvaluateBlockTiming(bullet, timeToImpact);
//                 }
//             }
//         }
//     }

//     private void EvaluateBlockTiming(BaseBullet bullet, float impactTime)
//     {
//         bool isPerfect = impactTime <= perfectBlockWindow && isBlocking;
//         bool isNormal = impactTime <= perfectBlockWindow * 2f;

//         if (isPerfect)
//         {
//             HandlePerfectBlock(bullet);
//         }
//         else if (isNormal)
//         {
//             HandleNormalBlock(bullet);
//         }
//     }
//     #endregion
//     #region 格挡处理
//     void HandlePerfectBlock(BaseBullet bullet)
//     {
//         Vector2 deflectDirection = GetDeflectDirection(bullet.Direction);
//         bullet.Deflect(deflectDirection * bulletDeflectSpeed);

//         perfectBlockEffect.Play();
//         AudioSource.PlayClipAtPoint(perfectBlockSound, transform.position);

//         bullet.OnPerfectDeflected();
//     }
//     void HandleNormalBlock(BaseBullet bullet)
//     {
//         //bullet.ReduceDamage(0.5f);
//         normalBlockEffect.Play();
//         AudioSource.PlayClipAtPoint(normalBlockSound, transform.position);
//         bullet.OnNormalDeflected();
//     }
//     Vector2 GetDeflectDirection(Vector2 originalDirection)
//     {
//         return transform.right * Mathf.Sign(transform.localScale.x);
//     }
//     #endregion
//     #region 工具方法
//     private bool IsInBlockRange(Vector2 bulletPos)
//     {
//         Vector2 toBullet = bulletPos - (Vector2)transform.position;
//         float angle = Vector2.Angle(transform.right, toBullet);

//         return angle <= blockAngle / 2f;
//     }
//     public void RegisterBullet(BaseBullet bullet)
//     {
//         if (!trackedBullets.Contains(bullet))
//         {
//             trackedBullets.Add(bullet);
//             //bullet.OnDestroyed += () => trackedBullets.Remove(bullet);
//         }
//     }
//     public void UnregisterBullet(BaseBullet bullet)
//     {
//         if (trackedBullets.Contains(bullet))
//         {
//             trackedBullets.Remove(bullet);
//         }
//     }
//     #endregion
// }
#endregion