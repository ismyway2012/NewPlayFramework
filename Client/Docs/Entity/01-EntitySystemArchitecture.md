# GameFrameX 实体系统架构分析

## 目录
1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [核心组件](#核心组件)
4. [工作流程](#工作流程)
5. [优缺点分析](#优缺点分析)
6. [改进建议](#改进建议)
7. [最佳实践](#最佳实践)

---

## 系统概述

GameFrameX 实体系统是一个基于 Unity MonoBehaviour 的**组件式实体管理框架**，用于统一管理游戏中的动态对象（角色、敌人、NPC 等）。

### 系统目标
- ?? 提供统一的实体生命周期管理
- ?? 实现对象池复用机制
- ?? 支持实体分组管理
- ?? 支持父子实体附加关系
- ?? 提供异步资源加载接口
- ?? 解耦业务逻辑与底层管理

---

## 架构设计

### 整体架构图

```
┌─────────────────────────────────────────────────────┐
│          EntityComponent (访问层)                     │
│      [Unity GameFramework 组件入口]                   │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│        EntityManager (管理核心)                       │
│  ┌─────────────────────────────────────────────┐  │
│  │ ? 实体生命周期管理                            │  │
│  │ ? 实体组管理                                 │  │
│  │ ? 异步资源加载                              │  │
│  │ ? 事件分发                                  │  │
│  └─────────────────────────────────────────────┘  │
└────────────────┬────────────────────────────────────┘
                 │
        ┌────────┴───────┐
        │                 │
┌───────▼──────┐  ┌──────▼─────────┐
│ EntityGroup  │  │  EntityGroup   │
│ (Group 1)    │  │  (Group N)     │
└───────┬──────┘  └──────┬─────────┘
        │                 │
    ┌───┴───┬─────┐      ┌─┴─────┬───┐
    │       │     │      │       │   │
┌──▼─┐ ┌──▼─┐ ┌──▼──┐  │      │   │
│Ent1│ │Ent2│ │Ent3│  │...  │   │
└─┬──┘ └─┬──┘ └──┬──┘  │      │   │
  │      │      │      │      │   │
┌─▼──────▼──────▼──┐   │      │   │
│ EntityLogic      │   │      │   │
│ (业务逻辑)        │   │      │   │
└──────────────────┘   │      │   │
                       │      │   │
                   ┌───▼──────▼───▼───┐
                   │  GameObject      │
                   │  (Unity 表现层)   │
                   └──────────────────┘
```

### 核心接口关系

```
IEntity ?─────────── Entity (MonoBehaviour)
   ▲                    │
   │                    │ contains
   │              EntityLogic (业务逻辑)
   │
   │
IEntityGroup ?──────── EntityGroup
   ▲                    │
   │                    │ manages
   │                  EntityInfo
   │
   │
IEntityManager ?────── EntityManager
                       │
                       ├─ Dictionary<int, EntityInfo>
                       ├─ Dictionary<string, EntityGroup>
                       └─ Dictionary<int, int>
```

---

## 核心组件

### 1. IEntity 接口

**职责**: 定义实体的标准接口契约

```csharp
public interface IEntity
{
    int Id { get; }                              // 实体唯一编号
    string EntityAssetName { get; }              // 资源名称
    object Handle { get; }                       // GameObject 引用
    IEntityGroup EntityGroup { get; }            // 所属分组
    
    // 生命周期
    void OnInit(int entityId, string entityAssetName, 
                IEntityGroup entityGroup, bool isNewInstance, object userData);
    void OnRecycle();
    void OnShow(object userData);
    void OnHide(bool isShutdown, object userData);
    
    // 父子关系
    void OnAttached(IEntity childEntity, object userData);
    void OnDetached(IEntity childEntity, object userData);
    void OnAttachTo(IEntity parentEntity, object userData);
    void OnDetachFrom(IEntity parentEntity, object userData);
    
    // 更新
    void OnUpdate(float elapseSeconds, float realElapseSeconds);
}
```

### 2. Entity 类

**职责**: 实现 IEntity 接口，作为 MonoBehaviour 包装层

**关键特性**:
- ? 单一责任：只负责生命周期委派
- ? 异常处理：所有回调都有 try-catch 保护
- ? 对象池友好：支持实例复用
- ? 动态组件：支持运行时添加 EntityLogic

**关键代码分析**:
```csharp
public sealed class Entity : MonoBehaviour, IEntity
{
    private EntityLogic m_EntityLogic;  // 动态添加的业务逻辑
    
    public void OnInit(int entityId, string entityAssetName, 
                       IEntityGroup entityGroup, bool isNewInstance, object userData)
    {
        // 业务逻辑类型通过 userData 传入
        ShowEntityInfo showEntityInfo = (ShowEntityInfo)userData;
        Type entityLogicType = showEntityInfo.EntityLogicType;
        
        // 复用或创建 EntityLogic
        if (m_EntityLogic != null && m_EntityLogic.GetType() == entityLogicType)
        {
            m_EntityLogic.enabled = true;  // 复用已有实例
        }
        else
        {
            m_EntityLogic = gameObject.AddComponent(entityLogicType) as EntityLogic;
        }
    }
}
```

### 3. EntityLogic 类

**职责**: 实体业务逻辑基类，包含生命周期钩子和常用工具

**生命周期钩子**:
```csharp
public abstract class EntityLogic : MonoBehaviour
{
    // 生命周期
    protected virtual void OnInit(object userData) { }
    protected virtual void OnShow(object userData) { }
    protected virtual void OnHide(bool isShutdown, object userData) { }
    protected virtual void OnRecycle() { }
    protected virtual void OnUpdate(float elapseSeconds, float realElapseSeconds) { }
    
    // 父子关系
    protected virtual void OnAttached(EntityLogic childLogic, Transform parentTransform, object userData) { }
    protected virtual void OnDetached(EntityLogic childLogic, object userData) { }
    protected virtual void OnAttachTo(EntityLogic parentLogic, Transform parentTransform, object userData) { }
    protected virtual void OnDetachFrom(EntityLogic parentLogic, object userData) { }
    
    // 工具属性
    public Entity Entity { get; }           // 关联的 Entity
    public bool Available { get; }          // 是否初始化
    public bool Visible { get; set; }       // 是否可见
    public Transform CachedTransform { get; }
}
```

### 4. EntityManager 类

**职责**: 实体生命周期管理、资源加载、对象池协调

**核心功能**:
- ?? 实体创建与销毁
- ?? 实体组分组管理
- ?? 异步资源加载
- ?? 事件分发
- ?? 对象池集成

**内部数据结构**:
```csharp
private Dictionary<int, EntityInfo> m_EntityInfos;           // 所有实体
private Dictionary<string, EntityGroup> m_EntityGroups;      // 分组信息
private Dictionary<int, int> m_EntitiesBeingLoaded;          // 加载中的实体
private Queue<EntityInfo> m_RecycleQueue;                    // 待回收队列
```

### 5. EntityGroup 类

**职责**: 实体分组管理，实现类似标签的功能

**用途**:
- 分类管理（怪物、NPC、特效等）
- 批量操作
- 性能优化（快速查询）

---

## 工作流程

### 显示实体流程 (ShowEntity)

```
用户调用: ShowEntity(...)
    │
    ├─ 1. 分配唯一 ID (m_Serial++)
    ├─ 2. 创建或获取 EntityInfo
    ├─ 3. 加入加载队列
    │
    ├─ 4. 异步加载资源
    │   └─ 回调: LoadAssetSuccessCallback
    │
    ├─ 5. 从对象池获取或创建 GameObject
    │   └─ 挂载 Entity 和 EntityLogic 组件
    │
    ├─ 6. 调用 Entity.OnInit(...)
    │   └─ Entity 动态添加 EntityLogic 组件
    │   └─ 调用 EntityLogic.OnInit(userData)
    │
    ├─ 7. 激活实体 (gameObject.SetActive(true))
    │
    ├─ 8. 调用 Entity.OnShow(...)
    │   └─ 调用 EntityLogic.OnShow(userData)
    │
    └─ 9. 触发事件: ShowEntitySuccessEventArgs
```

### 隐藏实体流程 (HideEntity)

```
用户调用: HideEntity(entityId)
    │
    ├─ 1. 查找 EntityInfo
    ├─ 2. 调用 Entity.OnHide(false, userData)
    │   └─ 调用 EntityLogic.OnHide(userData)
    │
    ├─ 3. 禁用显示 (gameObject.SetActive(false))
    │
    ├─ 加入回收队列 (m_RecycleQueue)
    │
    └─ 9. 触发事件: HideEntityCompleteEventArgs
```

### 回收实体流程 (OnRecycle)

```
Update 中处理回收队列
    │
    ├─ 1. 遍历 m_RecycleQueue
    ├─ 2. 调用 Entity.OnRecycle()
    │   └─ 调用 EntityLogic.OnRecycle()
    │
    ├─ 3. 清空 Entity 内部状态
    ├─ 4. 解除父子关系
    │
    ├─ 5. 回收到对象池
    │   └─ ObjectPoolManager.ReleaseObject(...)
    │
    └─ 6. 移除 EntityInfo
```

---

## 优缺点分析

### ? 优势

#### 1. **清晰的生命周期管理**
- 生命周期钩子完整（Init → Show → Hide → Recycle）
- 支持实例复用（IsNewInstance 标志）
- 避免重复初始化

#### 2. **灵活的组件化架构**
- Entity 只负责管理，EntityLogic 专注业务
- 支持运行时动态添加/替换 EntityLogic
- 易于继承和扩展

#### 3. **完整的父子关系支持**
- 支持实体嵌套（Avatar → Weapon → Bullet）
- 自动处理位置、旋转同步
- 易于实现复杂的对象结构

#### 4. **集成对象池机制**
- 自动复用 GameObject
- 减少 GC 压力
- 提升性能

#### 5. **异步资源加载**
- 加载进度回调
- 依赖资源追踪
- 灵活的失败处理

#### 6. **异常安全**
- 所有生命周期回调都有 try-catch
- 错误日志记录详细
- 不会因个别实体崩溃影响整个系统

#### 7. **分组管理**
- 支持按分组查询和批量操作
- 便于管理不同类型的实体
- 性能优化点

---

### ? 缺点

#### 1. **数据结构复杂**
```csharp
Dictionary<int, EntityInfo>        // O(1) ID 查询，但不支持反向查询
Dictionary<string, EntityGroup>    // 分组查询需遍历
```
- ? 反向查询困难（ID → EntityAssetName）
- ? 按条件查询实体需遍历整个字典
- ? 无索引支持（如按 Tag、Layer）

#### 2. **EntityLogic 与 MonoBehaviour 耦合**
```csharp
public abstract class EntityLogic : MonoBehaviour
{
    // EntityLogic 必须是 MonoBehaviour
    // 无法在纯数据驱动的系统中使用
}
```
- ? 无法实现纯 ECS 架构
- ? 每个实体都占用一个 MonoBehaviour 槽位
- ? 难以在 JobSystem 中使用

#### 3. **动态类型加载开销**
```csharp
Type entityLogicType = showEntityInfo.EntityLogicType;
m_EntityLogic = gameObject.AddComponent(entityLogicType) as EntityLogic;
```
- ?? 使用反射添加组件
- ?? 类型校验每次都进行
- ?? 无类型缓存机制

#### 4. **userData 转换复杂**
```csharp
ShowEntityInfo showEntityInfo = (ShowEntityInfo)userData;
AttachEntityInfo attachEntityInfo = (AttachEntityInfo)userData;
```
- ? 需要多次类型转换
- ? 容易发生 InvalidCastException
- ? userData 结构不明确

#### 5. **内存泄漏隐患**
```csharp
private EntityLogic m_EntityLogic;  // 即使 Entity 回收，也可能不释放
```
- ?? EntityLogic 中的静态引用可能造成泄漏
- ?? 缺少引用计数检查
- ?? 没有自动清理未释放的委托

#### 6. **性能瓶颈**
- ?? EntityManager.Update() 中 Dictionary 遍历
- ?? 实体检索时需线性查询
- ?? 大量实体时性能下降明显
- ?? OnUpdate() 需要逐一调用，无批处理优化

#### 7. **缺乏调试工具**
- ? 没有可视化实体树
- ? 无实时性能监控
- ? 父子关系不直观
- ? 难以追踪实体生命周期问题

---

## 改进建议

### 1. 增强查询功能 (高优先级)

**问题**: 无法高效地查询实体

**解决方案**:
```csharp
// 增加索引
private Dictionary<string, HashSet<int>> m_EntityIndexByAssetName;
private Dictionary<int, int> m_EntityIndexByLayer;

// 新增查询方法
public IEnumerable<IEntity> FindEntitiesByAssetName(string assetName)
{
    if (m_EntityIndexByAssetName.TryGetValue(assetName, out var ids))
    {
        foreach (var id in ids)
        {
            if (m_EntityInfos.TryGetValue(id, out var info))
                yield return info.Entity;
        }
    }
}

public IEnumerable<IEntity> FindEntitiesByGroup(string groupName)
{
    if (m_EntityGroups.TryGetValue(groupName, out var group))
        return group.GetEntities();
}

public IEnumerable<IEntity> FindEntitiesByLayer(int layer)
{
    return m_EntityInfos.Values
        .Where(info => info.Entity.Handle as GameObject)?.layer == layer)
        .Select(info => info.Entity);
}

public IEnumerable<IEntity> FindEntitiesByTag(string tag)
{
    return m_EntityInfos.Values
        .Where(info => (info.Entity.Handle as GameObject)?.CompareTag(tag) ?? false)
        .Select(info => info.Entity);
}
```

### 2. 优化动态类型加载 (高优先级)

**问题**: 频繁使用反射，性能低下

**解决方案**:
```csharp
// 类型缓存
private static class EntityLogicTypeCache
{
    private static readonly Dictionary<string, Type> s_TypeCache = 
        new Dictionary<string, Type>();
    
    public static Type Get(string typeName)
    {
        if (!s_TypeCache.TryGetValue(typeName, out var type))
        {
            type = Type.GetType(typeName);
            if (type != null)
                s_TypeCache[typeName] = type;
        }
        return type;
    }
}

// 使用预编译委托工厂
private static class EntityLogicFactory
{
    private delegate EntityLogic ConstructorDelegate();
    private static readonly Dictionary<Type, ConstructorDelegate> s_Factories 
        = new Dictionary<Type, ConstructorDelegate>();
    
    public static EntityLogic CreateInstance(Type type)
    {
        if (!s_Factories.TryGetValue(type, out var factory))
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            var dm = new DynamicMethod("Create", type, Type.EmptyTypes);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            factory = (ConstructorDelegate)dm.CreateDelegate(
                typeof(ConstructorDelegate));
            s_Factories[type] = factory;
        }
        return factory();
    }
}
```

### 3. 改进 userData 结构 (中优先级)

**问题**: 类型转换繁琐且易出错

**解决方案**:
```csharp
// 使用对象初始化器
public abstract class EntityShowData
{
    public Type EntityLogicType { get; init; }
    public object UserData { get; init; }
}

// 具体实现
public class CombatEntityShowData : EntityShowData
{
    public Vector3 Position { get; init; }
    public Quaternion Rotation { get; init; }
    public int Level { get; init; }
}

// 使用方式
await entityManager.ShowEntity(
    entityGroupName: "Enemies",
    entityAssetName: "Assets/Prefabs/Enemy.prefab",
    userData: new CombatEntityShowData
    {
        EntityLogicType = typeof(EnemyLogic),
        Position = Vector3.zero,
        Level = 5
    }
);
```

### 4. 添加实体池统计 (中优先级)

**问题**: 无法监控对象池状态

**解决方案**:
```csharp
public class EntityPoolStatistics
{
    public int TotalLoaded { get; set; }         // 已加载的实体
    public int TotalActive { get; set; }         // 活跃实体数
    public int TotalInactive { get; set; }       // 非活跃实体数
    public Dictionary<string, int> ByGroup { get; set; }
    public Dictionary<string, int> ByAssetName { get; set; }
}

public EntityPoolStatistics GetStatistics()
{
    var stats = new EntityPoolStatistics
    {
        ByGroup = new Dictionary<string, int>(),
        ByAssetName = new Dictionary<string, int>()
    };
    
    foreach (var info in m_EntityInfos.Values)
    {
        if ((info.Entity.Handle as GameObject)?.activeSelf ?? false)
            stats.TotalActive++;
        else
            stats.TotalInactive++;
            
        var groupName = info.Entity.EntityGroup.GroupName;
        if (!stats.ByGroup.ContainsKey(groupName))
            stats.ByGroup[groupName] = 0;
        stats.ByGroup[groupName]++;
    }
    
    return stats;
}
```

### 5. 支持实体标签系统 (低优先级)

**问题**: 无法灵活标记和查询实体

**解决方案**:
```csharp
public interface IEntityTaggable
{
    void AddTag(string tag);
    void RemoveTag(string tag);
    bool HasTag(string tag);
    IEnumerable<string> GetAllTags();
}

// 在 EntityLogic 中实现
public abstract class EntityLogic : MonoBehaviour, IEntityTaggable
{
    private readonly HashSet<string> m_Tags = new HashSet<string>();
    
    public void AddTag(string tag) => m_Tags.Add(tag);
    public void RemoveTag(string tag) => m_Tags.Remove(tag);
    public bool HasTag(string tag) => m_Tags.Contains(tag);
    public IEnumerable<string> GetAllTags() => m_Tags;
}

// 在管理器中查询
public IEnumerable<IEntity> FindEntitiesByTag(string tag)
{
    return m_EntityInfos.Values
        .Where(info => (info.Entity as IEntityTaggable)?.HasTag(tag) ?? false)
        .Select(info => info.Entity);
}
```

### 6. 添加生命周期事件 (中优先级)

**问题**: 外部难以响应实体状态变化

**解决方案**:
```csharp
public class EntityLifecycleEventArgs : EventArgs
{
    public int EntityId { get; set; }
    public string EntityAssetName { get; set; }
}

// 在 Entity 中触发事件
public class Entity : MonoBehaviour, IEntity
{
    public event EventHandler<EntityLifecycleEventArgs> OnInitialized;
    public event EventHandler<EntityLifecycleEventArgs> OnShown;
    public event EventHandler<EntityLifecycleEventArgs> OnHidden;
    
    public void OnInit(...)
    {
        // ... existing code
        OnInitialized?.Invoke(this, new EntityLifecycleEventArgs 
        { 
            EntityId = m_Id,
            EntityAssetName = m_EntityAssetName 
        });
    }
}
```

---

## 最佳实践

### 1. EntityLogic 继承模式

#### ? 错误做法
```csharp
public class PlayerEntity : EntityLogic
{
    private Dictionary<string, object> m_Cache;  // 缓存未清理
    private static PlayerEntity s_Instance;      // 静态引用泄漏
    
    // 在 OnHide 中忘记释放资源
    protected override void OnHide(bool isShutdown, object userData)
    {
        // 没有清理
    }
}
```

#### ? 正确做法
```csharp
public class PlayerEntity : EntityLogic
{
    private List<IDisposable> m_Resources = new List<IDisposable>();
    
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // 初始化业务逻辑
    }
    
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        // 显示时的逻辑
    }
    
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        // 必须清理资源
        foreach (var resource in m_Resources)
        {
            resource?.Dispose();
        }
        m_Resources.Clear();
    }
    
    protected override void OnRecycle()
    {
        base.OnRecycle();
        // 最后的清理工作
    }
}
```

### 2. 父子实体关系最佳实践

#### ? 错误做法
```csharp
// 频繁重新附加
for (int i = 0; i < 100; i++)
{
    entityManager.AttachEntity(child, parent, parentTransform);
    // 这样会多次调用 OnAttachTo，低效
}
```

#### ? 正确做法
```csharp
// 一次性附加，之后通过 SetParent 管理位置
entityManager.AttachEntity(child, parent, parentTransform);

// 之后只修改相对位置
child.CachedTransform.localPosition = newPosition;
```

### 3. 资源加载最佳实践

#### ? 错误做法
```csharp
// 同时加载大量实体，造成卡顿
for (int i = 0; i < 100; i++)
{
    await entityManager.ShowEntityAsync(...);  // 100 个并发加载
}
```

#### ? 正确做法
```csharp
// 分批加载，控制并发
int batchSize = 10;
for (int i = 0; i < totalEntities; i += batchSize)
{
    var tasks = new List<Task>();
    for (int j = 0; j < batchSize && i + j < totalEntities; j++)
    {
        tasks.Add(entityManager.ShowEntityAsync(...));
    }
    await Task.WhenAll(tasks);
}
```

### 4. 事件订阅最佳实践

#### ? 错误做法
```csharp
// 在 OnShow 中订阅，OnHide 中忘记取消
protected override void OnShow(object userData)
{
    entityManager.ShowEntitySuccess += OnShowEntitySuccess;
}

// 这会导致内存泄漏
```

#### ? 正确做法
```csharp
protected override void OnInit(object userData)
{
    base.OnInit(userData);
    entityManager.ShowEntitySuccess += OnShowEntitySuccess;
}

protected override void OnRecycle()
{
    base.OnRecycle();
    entityManager.ShowEntitySuccess -= OnShowEntitySuccess;
}

private void OnShowEntitySuccess(object sender, ShowEntitySuccessEventArgs e)
{
    // 处理事件
}
```

### 5. 异常处理最佳实践

#### ? 错误做法
```csharp
protected override void OnInit(object userData)
{
    base.OnInit(userData);
    // 未处理的异常
    var data = userData as PlayerData;
    int level = data.Level;  // 如果 userData 为 null，直接崩溃
}
```

#### ? 正确做法
```csharp
protected override void OnInit(object userData)
{
    base.OnInit(userData);
    
    if (userData == null)
    {
        Log.Error("PlayerEntity requires PlayerData");
        return;
    }
    
    var data = userData as PlayerData;
    if (data == null)
    {
        Log.Error("Invalid userData type for PlayerEntity");
        return;
    }
    
    // 安全的初始化
    InitializeWithData(data);
}
```

### 6. 性能优化最佳实践

#### 使用对象池复用
```csharp
// ? 正确
// 配置实体组属性
entityGroup.InstanceAutoReleaseInterval = 60f;  // 60秒自动释放
entityGroup.InstanceCapacity = 100;              // 容量上限

// 再次获取时会复用
await entityManager.ShowEntityAsync(groupName, assetName, userData);
```

#### 批量查询优化
```csharp
// ? 低效
foreach (var entity in entityManager.GetAllEntities())
{
    if (entity.EntityAssetName == "Enemy" && entity.EntityGroup.GroupName == "Combat")
    {
        // 处理
    }
}

// ? 高效
var combatEnemies = entityManager.GetEntitiesInGroup("Combat")
    .Where(e => e.EntityAssetName == "Enemy");
```

### 7. 调试最佳实践

#### 添加调试信息
```csharp
#if UNITY_EDITOR
private void OnDrawGizmos()
{
    if (!Available) return;
    
    // 绘制实体范围
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(CachedTransform.position, 1f);
    
    // 显示实体信息
    var position = CachedTransform.position;
    UnityEditor.Handles.Label(position, $"[{Entity.Id}] {Name}");
}
#endif
```

#### 日志记录规范
```csharp
protected override void OnInit(object userData)
{
    base.OnInit(userData);
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Log.Info($"Entity initialized: {Entity.Id}, AssetName: {Entity.EntityAssetName}");
    #endif
}
```

---

## 总结

### 核心要点

| 方面 | 内容 |
|------|------|
| **适用场景** | 中小型游戏、快速开发、标准 Unity 工作流 |
| **优势** | 生命周期清晰、对象池友好、扩展性好 |
| **劣势** | 查询功能有限、反射开销、MonoBehaviour 耦合 |
| **学习成本** | 低（设计模式简单，易上手） |
| **扩展空间** | 高（基础架构扎实，改进空间充足） |

### 推荐使用场景

? **适合**:
- 角色、敌人、NPC 管理
- 特效、子弹等临时对象
- 不需要极致性能优化的项目
- 团队成员 C# 技能参差不齐

? **不适合**:
- 超大规模实体（>10000）
- 纯 ECS 架构游戏
- 需要极致性能的竞技游戏
- 服务端架构（需要去 MonoBehaviour）

---

## 相关文档

- [实体系统最佳实践指南](02-EntityBestPractices.md)
- [实体系统代码示例](03-EntityCodeExamples.md)
- [实体系统常见问题](04-EntityFAQ.md)
- [实体系统性能优化](05-EntityPerformance.md)
