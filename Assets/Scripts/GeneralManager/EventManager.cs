using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class EventManager : Singleton<EventManager>
{
    #region 核心系统
    private GridManager gridSystem;
    private Transform player;
    [SerializeField] private List<EventConfig> activeEvents = new List<EventConfig>();
    [SerializeField] private List<EventConfig> possibleEvents = new List<EventConfig>();
    public UnityAction<EventConfig> OnEventStarted;
    public UnityAction<EventConfig, float> OnEventProgress;
    public UnityAction<EventConfig, bool> OnEventEnded;
    void Start()
    {
        gridSystem = GridManager.Instance;
        player = GameManager.Instance.player.transform;
    }
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameStates.GamePlay) return;
        CheckEventTriggers();
    }
    void CheckEventTriggers()
    {
        foreach (var config in possibleEvents)
        {
            if (ShouldTriggerEvent(config))
            {
                StartEvent(config);
            }
        }
    }
    private bool ShouldTriggerEvent(EventConfig config)
    {
        foreach (var condition in config.triggerConditions)
        {
            if (!condition.IsConditionMet()) return false;
        }
        return true;
    }
    #endregion

    #region 事件处理器
    public void StartEvent(EventConfig config)
    {
        activeEvents.Add(config);
        config.OnEventStart();
        StartCoroutine(RunEventCoroutine(config));
        OnEventStarted?.Invoke(config);
    }
    private IEnumerator RunEventCoroutine(EventConfig config)
    {
        float timer = 0f;
        while (timer < config.duration)
        {
            timer += Time.deltaTime;
            float progress = timer / config.duration;
            config.OnEventUpdate(progress);
            OnEventProgress?.Invoke(config, progress);
            yield return null;
        }
        EndEvent(config, true);
    }
    public void EndEvent(EventConfig config, bool completed)
    {
        activeEvents.Remove(config);
        config.OnEventEnd(completed);
        OnEventEnded?.Invoke(config, completed);
    }
    #endregion
    #region 公用方法
    public void SpawnEnemyWave(GameObject enemyPrefab, int count)
    {
        MonsterManager.Instance.SpawnMonsterWave(enemyPrefab, count, SpawnStrategy.AvoidPlayerArea);
    }
    public void FireBulletStorm(FireConfig config)
    {
        List<Vector2Int> edgeCells = gridSystem.GetEdgeCells(SpawnStrategy.ScreenEdges);
        int count = 0; int random = Random.Range(0, edgeCells.Count - config.bulletCount); int round = 0;
        foreach (var cell in edgeCells)
        {
            if (count > config.bulletCount) break;
            if (round < random)
            {
                round++;
                continue;
            }
            count++;
            Vector2 origin = gridSystem.GridToWorldPosition(cell);
            Vector2 targetDir = ((Vector2)player.position - origin).normalized;

            BulletPatternManager.Instance.FirePattern(
                config,
                origin, targetDir
            );
        }
    }
    public void FireBullet(FireConfig config, int waveCount = 1, SpawnStrategy spawnStrategy = SpawnStrategy.RandomWithDirection)
    {
        StartCoroutine(BulletBarrageRoutine(config, waveCount, spawnStrategy));
    }
    IEnumerator BulletBarrageRoutine(FireConfig config, int waveCount, SpawnStrategy strategy)
    {
        for (int i = 0; i < waveCount; i++)
        {
            Vector2? spawnPoint = gridSystem.GetValidSpawnPosition(
                strategy,
                direction: Vector2.down
            );

            if (spawnPoint.HasValue)
            {
                Vector2 origin = spawnPoint.Value;
                Vector2 targetDir = ((Vector2)player.position - origin).normalized;
                BulletPatternManager.Instance.FirePattern(
                config,
                origin,
                targetDir
            );
                yield return new WaitForSeconds(1f);
            }
        }
    }
    #endregion
    // #region 智能坐标生成算法
    // public Vector2Int? GetValidSpawnPosition(
    //     SpawnStrategy strategy,
    //     int playerRadius = 0,
    //     Vector2? direction = null,
    //     int maxAttempts = 20
    // )
    // {
    //     for (int i = 0; i < maxAttempts; i++)
    //     {
    //         Vector2Int gridPos = Vector2Int.zero;

    //         switch (strategy)
    //         {
    //             case SpawnStrategy.RandomWalkable:
    //                 gridPos = new Vector2Int(
    //                     Random.Range(0, gridSystem.gridSize.x),
    //                     Random.Range(0, gridSystem.gridSize.y)
    //                 );
    //                 break;

    //             case SpawnStrategy.AvoidPlayerArea:
    //                 Vector2Int playerCell = gridSystem.WorldToGridPosition(player.position);
    //                 gridPos = new Vector2Int(
    //                     Random.Range(0, gridSystem.gridSize.x),
    //                     Random.Range(0, gridSystem.gridSize.y)
    //                 );
    //                 // 排除玩家周围区域
    //                 if (Vector2Int.Distance(playerCell, gridPos) < playerRadius) continue;
    //                 break;

    //             case SpawnStrategy.ScreenEdges:
    //                 int edge = Random.Range(0, 4);
    //                 switch (edge)
    //                 {
    //                     case 0: // 左边缘
    //                         gridPos = new Vector2Int(0, Random.Range(0, gridSystem.gridSize.y));
    //                         break;
    //                     case 1: // 右边缘
    //                         gridPos = new Vector2Int(gridSystem.gridSize.x - 1, Random.Range(0, gridSystem.gridSize.y));
    //                         break;
    //                     case 2: // 上边缘
    //                         gridPos = new Vector2Int(Random.Range(0, gridSystem.gridSize.x), gridSystem.gridSize.y - 1);
    //                         break;
    //                     case 3: // 下边缘
    //                         gridPos = new Vector2Int(Random.Range(0, gridSystem.gridSize.x), 0);
    //                         break;
    //                 }
    //                 break;

    //             case SpawnStrategy.RandomWithDirection:
    //                 Vector2 dir = direction.Value.normalized;
    //                 float angleVariance = 30f;
    //                 Vector2 variedDir = Quaternion.Euler(0, 0, Random.Range(-angleVariance, angleVariance)) * dir;
    //                 gridPos = gridSystem.WorldToGridPosition(
    //                     player.position + (Vector3)variedDir * gridSystem.gridSize.x / 2
    //                 );
    //                 break;
    //         }

    //         // 验证单元格有效性
    //         if (gridSystem.IsValidGridPosition(gridPos))
    //         {
    //             return gridPos;
    //         }
    //     }
    //     return null;
    // }

    // List<Vector2Int> GetEdgeCells(SpawnStrategy strategy)
    // {
    //     List<Vector2Int> cells = new List<Vector2Int>();
    //     foreach (var cell in GridManager.Instance.EdgeCell)
    //     {
    //         cells.Add(cell.position);
    //     }
    //     return cells;
    // }
    // #endregion
    // #region 工具方法
    // public Vector2 GetRandomDirection(float minAngle = 0, float maxAngle = 360)
    // {
    //     float angle = Random.Range(minAngle, maxAngle);
    //     return Quaternion.Euler(0, 0, angle) * Vector2.right;
    // }

    // public Vector2 GetPlayerAvoidancePosition(float minDistance)
    // {
    //     Vector2 basePos = player.position;
    //     int attempts = 0;
    //     do
    //     {
    //         Vector2 offset = Random.insideUnitCircle * minDistance;
    //         Vector2Int gridPos = gridSystem.WorldToGridPosition(basePos + offset);
    //         if (gridSystem.IsValidGridPosition(gridPos))
    //         {
    //             return gridSystem.GridToWorldPosition(gridPos);
    //         }
    //         attempts++;
    //     } while (attempts < 50);
    //     return basePos;
    // }
    // #endregion
}
