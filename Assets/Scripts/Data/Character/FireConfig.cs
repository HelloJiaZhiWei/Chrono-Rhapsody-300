using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region 弹幕设置
[System.Serializable]
[CreateAssetMenu(fileName = "子弹设置", menuName = "自定义/子弹")]
public class FireConfig : ScriptableObject
{
    [Header("基础设置")]
    public PatternType patternType;
    public GameObject bulletPrefab;
    public int waveCount = 1;
    public int bulletCount = 10;
    public float waveInterval = 1f;
    public float fireInterval = 0.5f;
    public float speed = 50f;
    public float lifetime = 5f;

    [Header("方向参数")]
    [Range(0, 360)] public float startAngle = 0f;
    [Range(0, 360)] public float angleSpread = 0f;

    [Header("螺旋子弹参数")]
    public float spiralRadius = 1f;    // 螺旋半径
    public float spiralSpeed = 1f;    // 螺旋转速
    [Header("波形子弹参数")]
    public float waveFrequency = 2f;  // 波形频率
    public float waveAmplitude = 0.5f;// 波形幅度
    [Header("跟踪子弹参数")]
    public float maxTurnRate = 180f;
    public float maxTrackingAngle = 45f;
    public float trackingTimeout = 5f;// 超出此距离停止追踪
    [Header("抛物线子弹参数")]
    public float gravity = -Physics.gravity.magnitude;

    [Header("动态参数")]
    public AnimationCurve speedCurve;  // 速度变化曲线
    public bool autoAdjustDirection;   // 自动追踪目标
}
#endregion
