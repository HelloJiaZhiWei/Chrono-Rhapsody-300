using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "事件设置", menuName = "自定义/事件触发条件")]
public class TestEvent : EventCondition
{
    public override bool IsConditionMet()
    {
        if (Input.GetKeyDown(KeyCode.E)) return true;
        return false;
    }
}