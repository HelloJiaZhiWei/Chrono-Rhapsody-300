using UnityEngine;
using System.Collections.Generic;
#region 游戏事件
[System.Serializable]
[CreateAssetMenu(fileName = "事件设置", menuName = "自定义/基础事件")]
public class EventConfig : ScriptableObject
{
    [Header("基础设置")]
    public EventType eventType;
    public string eventName;
    [Tooltip("事件持续时间")] public float duration = 300f;
    [Header("触发条件")]
    public List<EventCondition> triggerConditions;
    public virtual void OnEventStart() { }
    public virtual void OnEventUpdate(float progress) { }
    public virtual void OnEventEnd(bool completed) { }
}
public abstract class EventCondition : ScriptableObject
{
    public abstract bool IsConditionMet();
}
#endregion