using UnityEngine;

/// <summary>
/// 建筑数据定义类
/// </summary>
[System.Serializable]
public class BuildingData
{
    [Header("基础信息")]
    [Tooltip("建筑名称")]
    public string buildingName;
    [Tooltip("建筑预制体")]
    public GameObject prefab;

    [Header("尺寸信息")]
    [Tooltip("占用的网格宽度")]
    public int width = 1;
    [Tooltip("占用的网格高度")]
    public int height = 1;

    [Header("建造信息")]
    [Tooltip("建造所需资源")]
    public int cost = 100;
    [Tooltip("建造时间（秒）")]
    public float buildTime = 2.0f;

    // 可扩展：添加更多属性如生命值、攻击、特殊能力等
}
