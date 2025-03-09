// BulletPatternSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[System.Serializable]
public class BulletPool
{
    public GameObject prefab;
    public int initialSize = 20;
}
public class BulletPatternManager : Singleton<BulletPatternManager>
{
    #region 核心系统
    [Header("对象池设置")]
    [SerializeField] private BulletPool[] bulletPools;
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform target;
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        InitializePools();
    }

    void InitializePools()
    {
        foreach (var pool in bulletPools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject bullet = CreateNewBullet(pool.prefab);
                objectPool.Enqueue(bullet);
            }

            poolDictionary.Add(pool.prefab, objectPool);
        }
    }
    GameObject CreateNewBullet(GameObject prefab)
    {
        GameObject bullet = Instantiate(prefab);
        bullet.SetActive(false);
        bullet.transform.SetParent(transform);
        return bullet;
    }
    public GameObject GetBulletFromPool(GameObject prefab)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        if (poolDictionary[prefab].Count == 0)
        {
            ExpandPool(prefab, 5);
        }

        return poolDictionary[prefab].Dequeue(); ;
    }

    public void ReturnToPool(GameObject bullet)
    {
        if (bullet == null) return;

        if (poolDictionary.ContainsKey(bullet.GetComponent<BaseBullet>().bulletPrefab))
        {
            bullet.SetActive(false);
            poolDictionary[bullet.GetComponent<BaseBullet>().bulletPrefab].Enqueue(bullet);
        }
        else
        {
            Destroy(bullet);
        }
    }
    void ExpandPool(GameObject prefab, int expandSize)
    {
        for (int i = 0; i < expandSize; i++)
        {
            GameObject bullet = CreateNewBullet(prefab);
            poolDictionary[prefab].Enqueue(bullet);
        }
    }
    #endregion

    #region 弹幕生成
    public void FirePattern(FireConfig config, Vector2 spawnPosition, Vector2 direction)
    {
        StartCoroutine(SpawnBullets(config, spawnPosition, direction));
    }

    IEnumerator SpawnBullets(FireConfig config, Vector2 spawnPos, Vector2 direction)
    {
        float angleStep = config.angleSpread / config.bulletCount;
        float currentAngle = config.startAngle - config.angleSpread / 2f;

        for (int i = 0; i < config.bulletCount; i++)
        {
            GameObject bullet = GetBulletFromPool(config.bulletPrefab);
            if (!bullet) yield break;
            if (bullet.TryGetComponent<BaseBullet>(out var bulletComponent))
            {
                bullet.transform.position = spawnPos;
                bullet.SetActive(true);
                bulletComponent.Initialize(config, target, currentAngle, direction);
            }
            currentAngle += angleStep;
            yield return new WaitForSeconds(config.fireInterval); // 发射间隔
        }
    }
    #endregion
}