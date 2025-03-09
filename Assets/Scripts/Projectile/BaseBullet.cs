using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract class BaseBullet : MonoBehaviour
{
    [Header("移动参数")]
    protected LayerMask hitLayer;
    [SerializeField] protected LayerMask nonDeflectLayer;
    [SerializeField] protected LayerMask deflectLayer;
    protected float speed = 10f;
    [SerializeField] protected Vector2 direction = Vector2.right;
    [HideInInspector] public GameObject bulletPrefab;//标识符
    protected float aliveTimer;
    protected float lifeTime = 5f;
    protected float damage = 0.5f;
    protected Transform target;

    public virtual void Initialize(FireConfig config, Transform target, float initialAngle, Vector2 direction)
    {
        speed = config.speed;
        this.direction = direction;
        lifeTime = config.lifetime;
        aliveTimer = config.lifetime;
        this.target = target;
        hitLayer = nonDeflectLayer;

        // 自动获取预制体引用（需要保证子弹预制体结构一致）
        if (bulletPrefab == null)
        {
            bulletPrefab = config.bulletPrefab;
        }
    }
    protected virtual void OnDisable()
    {
        // 重置所有可能修改的参数
        aliveTimer = lifeTime;
        direction = Vector2.zero;
    }
    public abstract void Deflect(Vector2 dir);
    public abstract void Blocked();
}

