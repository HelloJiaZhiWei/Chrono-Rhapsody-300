public enum CtrlType
{
    AI, Player
}
public enum GameStates
{
    GamePlay, GamePause, GameStop
}
public enum CharacterStates
{
    Idle, Moving, Attacking, Defending, Dashing, Dead
}
public enum PatternType
{
    直线, 扇形, 螺旋, 追踪, 波形, 激光, 抛物线, 自定义
}
public enum BulletType
{
    Normal, UnBlockable
}
public enum EventType
{
    SpawnEnemy, FireBulletStorm, FireBullet, Custom
}
public enum SpawnStrategy
{
    RandomWalkable, AvoidPlayerArea, ScreenEdges, RandomWithDirection
}
public enum MonsterAttackType
{
    Random, Order
}