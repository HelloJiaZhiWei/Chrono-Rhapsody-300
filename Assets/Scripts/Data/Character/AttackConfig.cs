using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "近战设置", menuName = "自定义/近战")]
public class AttackConfig : ScriptableObject
{
    public string animationTrigger;
    public Vector2 hitboxOffset;
    public Vector2 hitboxSize;
    public float damage = 20f;
    public float hitboxActiveStart = 0.2f;
    public float hitboxActiveDuration = 0.15f;
    public ParticleSystem swingEffect;
}