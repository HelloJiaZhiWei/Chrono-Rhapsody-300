using UnityEngine;

[CreateAssetMenu(menuName = "自定义/事件/轰炸机")]
public class BombingEvent : EventConfig
{
    public int bombWaves = 3;
    public int countPerWave = 5;
    public FireConfig bombPrefab;
    private int currentWave = 1;
    public override void OnEventStart()
    {
        base.OnEventStart();
        currentWave = 1;
    }

    public override void OnEventUpdate(float progress)
    {
        base.OnEventUpdate(progress);
        SpawnBombs(progress);
    }

    private void SpawnBombs(float progress)
    {
        if (progress > (1 / bombWaves) * currentWave)
        {
            currentWave++;
            EventManager.Instance.FireBullet(bombPrefab, countPerWave);
        }
    }

    public override void OnEventEnd(bool completed)
    {
        base.OnEventEnd(completed);
    }
}