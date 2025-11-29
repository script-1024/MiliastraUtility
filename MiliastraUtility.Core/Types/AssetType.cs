namespace MiliastraUtility.Core.Types;

/// <summary>
/// 表示资产的具体类型。
/// </summary>
public enum AssetType : byte
{
    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 预制体
    /// </summary>
    Prefab = 1,

    /// <summary>
    /// 造物
    /// </summary>
    Creation = 2,

    /// <summary>
    /// 实体
    /// </summary>
    Entity = 3,

    /// <summary>
    /// 地形
    /// </summary>
    Terrain = 5,

    /// <summary>
    /// 预设点
    /// </summary>
    PresetPoint = 6,

    /// <summary>
    /// 单位状态
    /// </summary>
    UnitStatus = 7,

    /// <summary>
    /// 技能
    /// </summary>
    Skill = 8,

    /// <summary>
    /// 实体节点图
    /// </summary>
    EntityNodeGraph = 9,

    /// <summary>
    /// 布尔过滤器
    /// </summary>
    BooleanFilter = 10,

    /// <summary>
    /// 技能节点图
    /// </summary>
    SkillNodeGraph = 11,

    /// <summary>
    /// 复合节点
    /// </summary>
    CompositeNode = 12,

    /// <summary>
    /// 镜头
    /// </summary>
    Camera = 13,

    /// <summary>
    /// 信号
    /// </summary>
    Signal = 14,

    /// <summary>
    /// 交互控件
    /// </summary>
    UIControl = 15,

    /// <summary>
    /// 技能资源
    /// </summary>
    SkillResource = 16,

    /// <summary>
    /// 玩家模板
    /// </summary>
    Player = 18,

    /// <summary>
    /// 角色模板
    /// </summary>
    Character = 19,

    /// <summary>
    /// 界面布局
    /// </summary>
    InterfaceLayout = 20,

    /// <summary>
    /// 界面控件组
    /// </summary>
    UIControlGroup = 21,

    /// <summary>
    /// 状态节点图
    /// </summary>
    StatusNodeGraph = 22,

    /// <summary>
    /// 职业节点图
    /// </summary>
    ClassNodeGraph = 23,

    /// <summary>
    /// 道具
    /// </summary>
    Item = 26,

    /// <summary>
    /// 装饰物
    /// </summary>
    Decoration = 28,

    /// <summary>
    /// 结构体定义
    /// </summary>
    StructureDefinition = 29,

    /// <summary>
    /// 道具节点图
    /// </summary>
    ItemNodeGraph = 46,

    /// <summary>
    /// 整数过滤器
    /// </summary>
    IntegerFilter = 47,

    /// <summary>
    /// 环境配置
    /// </summary>
    Environment = 49,
}
