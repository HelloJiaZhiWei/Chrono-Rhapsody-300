using UnityEngine;
using System.Collections;
using System;

public class ProjectileBullet : BaseBullet
{
    public override void Initialize(FireConfig config, Transform target, float initialAngle, Vector2 direction)
    {
        base.Initialize(config, target, initialAngle, direction);
        MoveBullet(config, initialAngle, direction);
    }
    void MoveBullet(FireConfig config, float initialAngle, Vector2 direction)
    {
        StartCoroutine(Move(config, initialAngle, direction));
    }
    IEnumerator Move(FireConfig config, float initialAngle, Vector2 direction)
    {
        aliveTimer = 0f;
        Vector2 startPos = transform.position;
        Vector2 baseDirection = CalculateDirection(gameObject, config, initialAngle, direction);

        Vector2 lastValidDirection = baseDirection;
        Vector2 lastValidVelocity = GetDirectionFromAngle(initialAngle) * config.speed;
        bool isTracking = true;
        float trackingTimer = 0f;

        while (aliveTimer < lifeTime && gameObject.activeSelf)
        {
            if (!gameObject) yield break;

            Vector2 currentVelocity = baseDirection * config.speed;
            float dynamicAngle = initialAngle;

            switch (config.patternType)
            {
                case PatternType.螺旋:
                    dynamicAngle += aliveTimer * config.spiralSpeed * 100f;
                    currentVelocity = GetDirectionFromAngle(dynamicAngle) * config.speed;
                    currentVelocity += ((Vector2)(transform.position) - startPos).normalized * config.spiralRadius;
                    break;
                case PatternType.波形:
                    float waveOffset = Mathf.Sin(aliveTimer * config.waveFrequency) * config.waveAmplitude;
                    currentVelocity = Quaternion.Euler(0, 0, waveOffset * 90f) * currentVelocity;
                    break;
                case PatternType.追踪:
                    if (target && isTracking)
                    {
                        // 计算理想方向
                        Vector2 targetDir = (target.position - transform.position).normalized;
                        float currentAngle = Vector2.Angle(lastValidDirection, targetDir);

                        // 追踪有效性检查
                        trackingTimer += Time.deltaTime;
                        if (currentAngle > config.maxTrackingAngle || trackingTimer > config.trackingTimeout)
                        {
                            isTracking = false;
                            break;
                        }

                        // 限制转向速度
                        float maxRotation = config.maxTurnRate * Time.deltaTime;
                        lastValidDirection = Vector3.RotateTowards(
                            lastValidDirection,
                            targetDir,
                            maxRotation * Mathf.Deg2Rad,
                            0
                        ).normalized;

                        currentVelocity = lastValidDirection * config.speed;
                    }
                    break;
                case PatternType.抛物线:
                    lastValidVelocity.y += config.gravity * Time.deltaTime;
                    currentVelocity = lastValidVelocity;
                    break;
                case PatternType.自定义:
                    currentVelocity = GetDirectionFromAngle(dynamicAngle) * CalculateCustomPath(aliveTimer) * config.speed;
                    break;
            }

            // 应用速度曲线
            float speedMultiplier = config.speedCurve.Evaluate(aliveTimer / config.lifetime);
            transform.position += (Vector3)currentVelocity * speedMultiplier * Time.deltaTime;

            // 自动旋转方向
            if (config.autoAdjustDirection)
            {
                float rotAngle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(rotAngle, Vector3.forward);
            }

            aliveTimer += Time.deltaTime;
            yield return null;
        }

        BulletPatternManager.Instance.ReturnToPool(gameObject);
    }
    Vector2 CalculateDirection(GameObject bullet, FireConfig config, float currentAngle, Vector2 direction)
    {
        Vector2 autoDirection;
        if (direction == Vector2.zero)
        {
            autoDirection = (target.position - bullet.transform.position).normalized;
        }
        else
        {
            autoDirection = GetDirectionFromAngle(currentAngle);
        }
        switch (config.patternType)
        {
            case PatternType.扇形:
                return Quaternion.Euler(0, 0, currentAngle) * direction;
            case PatternType.螺旋:
                float spiralAngle = currentAngle + Time.time * config.spiralSpeed;
                return new Vector2(
                    Mathf.Cos(spiralAngle * Mathf.Deg2Rad),
                    Mathf.Sin(spiralAngle * Mathf.Deg2Rad)
                );
            case PatternType.波形:
                return Quaternion.Euler(0, 0,
                    Mathf.Sin(Time.time * config.waveFrequency) * config.waveAmplitude
                ) * autoDirection;
            default:
                return autoDirection;
        }
    }
    Vector2 GetDirectionFromAngle(float angle)
    {
        return new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );
    }
    Vector2 CalculateCustomPath(float time)
    {
        // 示例：玫瑰线方程
        float a = 2f;
        float k = 3f;
        float r = a * Mathf.Cos(k * time);
        return new Vector2(
            r * Mathf.Cos(time),
            r * Mathf.Sin(time)
        );
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (TryGetComponent<TrailRenderer>(out var trail))
        {
            trail.Clear();
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject, hitLayer)) return;
        Vector2 hitDirection = (other.transform.position - transform.position).normalized;
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.player.GetComponent<PlayerCharacter>().TakeDamage(damage, hitDirection);

        }
        else if (other.CompareTag("Monster"))
        {
            other.gameObject.GetComponent<Monster>().TakeDamage(damage, hitDirection);
        }
        BulletPatternManager.Instance.ReturnToPool(gameObject);
    }
    bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }

    public override void Deflect(Vector2 dir)
    {
        StopAllCoroutines();
        StartCoroutine(OnDeflect(dir));
    }
    IEnumerator OnDeflect(Vector2 dir)
    {
        aliveTimer = 0f;
        hitLayer = deflectLayer;
        //Vector2 startPos = transform.position;
        Vector2 baseDirection = dir;
        float rotAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(rotAngle, Vector3.forward);

        while (aliveTimer < lifeTime && gameObject.activeSelf)
        {
            if (GameManager.Instance.CurrentState != GameStates.GamePlay)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            if (!gameObject) yield break;

            Vector2 currentVelocity = baseDirection * speed;

            transform.position += (Vector3)currentVelocity * Time.deltaTime;

            aliveTimer += Time.deltaTime;
            yield return null;
        }

        BulletPatternManager.Instance.ReturnToPool(gameObject);
    }
    public override void Blocked()
    {
        BulletPatternManager.Instance.ReturnToPool(gameObject);
    }
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;

        Gizmos.color = Color.red;
        Vector2 start = transform.position;
        Vector2 end = target.position;

        for (float t = 0; t <= 1; t += 0.1f)
        {
            Vector2 pos = Vector2.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 3;
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}