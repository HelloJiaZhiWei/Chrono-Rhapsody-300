using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MonsterPoolConfig
{
    public GameObject prefab;
    public int initialSize = 10;
    public int maxSize = 30;
}
public class MonsterManager : Singleton<MonsterManager>
{
    [Header("对象池设置")]
    [SerializeField] private MonsterPoolConfig[] poolConfigs;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxActiveMonsters = 20;
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private List<GameObject> activeMonsters = new List<GameObject>();
    private float spawnTimer;

    void Start()
    {
        Initialize();
        spawnTimer = spawnInterval;
    }
    void Initialize()
    {
        foreach (var config in poolConfigs)
        {
            Queue<GameObject> pool = new Queue<GameObject>();
            for (int i = 0; i < config.initialSize; i++)
            {
                GameObject monster = CreateNewMonster(config.prefab);
                pool.Enqueue(monster);
            }
            pools.Add(config.prefab.name, pool);
        }
    }
    GameObject CreateNewEffect(GameObject prefab)
    {
        GameObject VFX = Instantiate(prefab);
        VFX.SetActive(false);
        VFX.transform.SetParent(transform);
        VFX.name = prefab.name; // 移除Clone后缀
        return VFX;
    }
    GameObject CreateNewMonster(GameObject prefab)
    {
        GameObject monster = Instantiate(prefab);
        monster.SetActive(false);
        monster.transform.SetParent(transform);
        monster.name = prefab.name; // 移除Clone后缀
        return monster;
    }
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameStates.GamePlay) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0 && activeMonsters.Count < maxActiveMonsters)
        {
            SpawnRandomMonster();
            spawnTimer = spawnInterval;
        }
    }

    public void SpawnRandomMonster()
    {
        int index = Random.Range(0, poolConfigs.Length);
        MonsterPoolConfig config = poolConfigs[index];
        SpawnMonster(config.prefab);
    }
    GameObject SpawnMonster(GameObject prefab, Vector2? position = null)
    {
        // 获取或创建实例
        GameObject monster = GetMonsterFromPool(prefab.name);

        Vector2? spawnPos = position.HasValue ?
            position.Value :
            GridManager.Instance.GetValidSpawnPosition(
                SpawnStrategy.AvoidPlayerArea,
                playerRadius: 5
            );

        monster.transform.position = spawnPos.Value;
        monster.SetActive(true);

        // 初始化怪物
        monster.GetComponent<Monster>().Initialize();
        activeMonsters.Add(monster);

        return monster;
    }

    GameObject GetMonsterFromPool(string prefabName)
    {
        if (!pools.ContainsKey(prefabName)) return null;

        // 池中有可用对象
        if (pools[prefabName].Count > 0)
        {
            return pools[prefabName].Dequeue();
        }

        // 动态扩展池
        foreach (var config in poolConfigs)
        {
            if (config.prefab.name == prefabName &&
                pools[prefabName].Count < config.maxSize)
            {
                GameObject newMonster = CreateNewMonster(config.prefab);
                return newMonster;
            }
        }
        return null;
    }
    #region 回收逻辑
    public void ReturnMonster(GameObject monster)
    {
        if (monster == null) return;

        // 重置状态
        monster.SetActive(false);
        monster.transform.position = Vector3.zero;

        // 清除所有状态组件
        if (monster.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.velocity = Vector2.zero;
        }

        // 放回对应池
        string poolKey = monster.name;
        if (pools.ContainsKey(poolKey))
        {
            pools[poolKey].Enqueue(monster);
        }
        else
        {
            Destroy(monster);
        }

        activeMonsters.Remove(monster);
    }
    #endregion

    #region 生成策略
    public List<GameObject> SpawnMonsterWave(
        GameObject prefab,
        int count,
        SpawnStrategy strategy = SpawnStrategy.RandomWalkable)
    {
        List<GameObject> wave = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            Vector2? pos = GridManager.Instance.GetValidSpawnPosition(strategy);
            GameObject monster = SpawnMonster(prefab, pos);
            if (monster != null) wave.Add(monster);
        }
        return wave;
    }
    Vector2 GetPositionAroundPlayer()
    {
        Vector2 playerPos = GameManager.Instance.player.transform.position;
        Vector2 dir = Random.insideUnitCircle.normalized;
        return playerPos + dir * 5f;
    }
    #endregion

    #region 调试功能
    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUILayout.Label($"Active Monsters: {activeMonsters.Count}");
            foreach (var pool in pools)
            {
                GUILayout.Label($"{pool.Key}: {pool.Value.Count} in pool");
            }
        }
    }
    #endregion
}