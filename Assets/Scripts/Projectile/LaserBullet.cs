using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(LineRenderer))]
public class LaserBullet : BaseBullet
{
    #region 核心参数
    [Header("Basic Setting")]
    public float maxLength = 20f;
    public float activeDuration = 3f;
    public LayerMask collisionMask;
    [Header("Alarm System")]
    public Material warningMaterial;
    public Material activeMaterial;
    public float warningDuration = 1f;
    [Header("Rotation")]
    public bool m_rotate = true;
    public bool m_rotateClockwise = true;
    public float m_rotationSpeed = 20;
    [Header("Component Referance")]
    public LineRenderer lineRenderer;
    public GameObject hitEffect;

    private float currentLength;
    private bool isActive;
    private bool shouldCollide = true;
    #endregion

    #region 生命周期
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameStates.GamePlay) return;
        UpdateLifeTime();
    }
    public override void Initialize(FireConfig config, Transform target, float initialAngle, Vector2 direction)
    {
        base.Initialize(config, target, initialAngle, direction);
        shouldCollide = true;
        Vector2 startPos = transform.position;
        this.direction = Quaternion.Euler(0, 0, initialAngle) * direction;
        currentLength = 0;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, (Vector2)transform.position + this.direction * maxLength);
        lineRenderer.material = warningMaterial;
        lineRenderer.material.enableInstancing = true;

        StartCoroutine(LaserRoutine());
    }
    public void Initialize(FireConfig config, Transform target, float initialAngle, Vector2 direction, LayerMask hitlayer)
    {
        collisionMask = hitlayer;
        Initialize(config, target, initialAngle, direction);
    }
    public void Initialize(float speed, Vector2 direction, Vector2 startPos)
    {
        this.direction = direction;
        currentLength = 0;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, (Vector2)transform.position + direction * maxLength);
        lineRenderer.material = warningMaterial;
        lineRenderer.material.enableInstancing = true;
    }
    IEnumerator LaserRoutine()
    {
        float timer = 0;
        while (timer < warningDuration)
        {
            lineRenderer.material.SetFloat("_WarningProgress", timer / warningDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        isActive = true;
        lineRenderer.material = activeMaterial;
        StartCoroutine(ExtendLaser());

        yield return new WaitForSeconds(activeDuration);
        StartCoroutine(RetractLaser());
    }
    #endregion
    #region 激光延伸
    IEnumerator ExtendLaser()
    {
        while (currentLength < maxLength)
        {
            currentLength = Mathf.Min(currentLength + speed * Time.deltaTime, maxLength);
            UpdateLaserVisual(true);
            CheckCollisionDuringGrowth();
            yield return null;
        }
    }
    IEnumerator RetractLaser()
    {
        //currentLength = 0;
        while (currentLength > 0)
        {
            //currentLength = Mathf.Min(currentLength + speed * Time.deltaTime, maxLength);
            currentLength = Mathf.Max(currentLength - speed * Time.deltaTime, 0);
            UpdateLaserVisual(true);
            yield return null;
        }
    }

    void UpdateLaserVisual(bool order)
    {
        if (m_rotate)
        {
            if (m_rotateClockwise)
            {
                direction = RotateClockwiseWithQuaternion(direction, m_rotationSpeed * Time.deltaTime);
            }
            else
            {
                direction = RotateClockwiseWithQuaternion(direction, -m_rotationSpeed * Time.deltaTime);
            }
        }
        Vector2 endPoint = (Vector2)transform.position + direction * currentLength;
        if (order) lineRenderer.SetPosition(1, endPoint);
        else lineRenderer.SetPosition(0, endPoint);
    }
    Vector2 RotateClockwiseWithQuaternion(Vector2 originalDirection, float degrees)
    {
        Quaternion rotation = Quaternion.Euler(0, 0, -degrees);
        Vector3 rotated = rotation * originalDirection;
        return new Vector2(rotated.x, rotated.y).normalized;
    }
    #endregion
    #region 碰撞检测
    void CheckCollisionDuringGrowth()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            currentLength,
            collisionMask
        );
        if (hit.collider)
        {
            if (IsInLayerMask(hit.collider.gameObject, collisionMask))
            {
                HandleCollision(hit.point);

                if (isActive && shouldCollide && hit.collider.CompareTag("Player"))
                {

                    print("hit ");
                    hit.collider.GetComponent<CharacterBase>().TakeDamage(damage);

                }
                shouldCollide = false;
                maxLength = Vector2.Distance(transform.position, hit.point);
            }
        }
    }
    void UpdateLifeTime()
    {
        aliveTimer -= Time.deltaTime;
        if (aliveTimer <= 0)
        {
            BulletPatternManager.Instance.ReturnToPool(gameObject);
        }
    }
    void HandleCollision(Vector2 hitPoint)
    {
        if (hitEffect && shouldCollide)
        {
            Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(direction));
        }
    }

    public override void Deflect(Vector2 dir)
    {
        GameObject laser = BulletPatternManager.Instance.GetBulletFromPool(bulletPrefab);
        laser.GetComponent<LaserBullet>().Initialize(speed * 1.2f, dir, (Vector2)transform.position + direction * currentLength);
        BulletPatternManager.Instance.ReturnToPool(gameObject);
    }
    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public override void Blocked()
    {
        maxLength = currentLength;
        //BulletPatternManager.Instance.ReturnToPool(gameObject);
    }
    bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
    #endregion
}