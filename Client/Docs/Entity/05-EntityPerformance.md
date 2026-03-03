# GameFrameX 实体系统性能优化指南

## 目录
1. [性能分析](#性能分析)
2. [常见瓶颈](#常见瓶颈)
3. [优化策略](#优化策略)
4. [性能基准测试](#性能基准测试)
5. [监控和调优](#监控和调优)

---

## 性能分析

### 性能指标体系

```
实体系统性能
│
├─ CPU 性能
│  ├─ Update 耗时
│  ├─ 查询耗时
│  ├─ 事件分发耗时
│  └─ GC Alloc
│
├─ 内存性能
│  ├─ 堆内存占用
│  ├─ 对象数量
│  ├─ 内存泄漏
│  └─ GC Pause
│
└─ 管理性能
   ├─ 加载时间
   ├─ 卸载时间
   ├─ 池回收效率
   └─ 并发能力
```

### 性能分析工具

```csharp
/// <summary>
/// 实体系统性能监控工具
/// </summary>
public class EntityPerformanceMonitor : MonoBehaviour
{
    [System.Diagnostics.Stopwatch]
    private Stopwatch m_Watch = new();
    
    public class PerformanceData
    {
        public int TotalEntityCount { get; set; }
        public int TotalGroupCount { get; set; }
        public float UpdateTimeMs { get; set; }
        public float AverageUpdateTimeMs { get; set; }
        public Dictionary<string, int> EntitiesByGroup { get; set; }
    }
    
    private IEntityManager m_EntityManager;
    private PerformanceData m_Data = new();
    
    private void Start()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
    }
    
    private void Update()
    {
        m_Watch.Restart();
        
        // ... EntityManager 执行 Update
        
        m_Watch.Stop();
        m_Data.UpdateTimeMs = (float)m_Watch.Elapsed.TotalMilliseconds;
        m_Data.TotalEntityCount = m_EntityManager.EntityCount;
        m_Data.TotalGroupCount = m_EntityManager.EntityGroupCount;
    }
    
    public void PrintPerformanceReport()
    {
        Debug.Log($"=== Entity Performance Report ===");
        Debug.Log($"Total Entities: {m_Data.TotalEntityCount}");
        Debug.Log($"Total Groups: {m_Data.TotalGroupCount}");
        Debug.Log($"Update Time: {m_Data.UpdateTimeMs:F2}ms");
        Debug.Log($"Average Update Time: {m_Data.AverageUpdateTimeMs:F2}ms");
    }
}
```

---

## 常见瓶颈

### 瓶颈 1: EntityManager.Update() 性能

**现象**: 
- 实体数量增加时，FPS 明显下降
- Update 方法执行时间线性增长
- 整个游戏帧率波动

**原因**:
```csharp
// EntityManager 的 Update 实现（伪代码）
public void Update()
{
    // 1. 遍历所有实体进行 OnUpdate 调用
    foreach (var entity in m_EntityInfos.Values)  // O(n)
    {
        entity.OnUpdate(...);
    }
    
    // 2. 处理回收队列
    while (m_RecycleQueue.Count > 0)  // O(m)
    {
        var info = m_RecycleQueue.Dequeue();
        // 回收处理
    }
    
    // 3. 其他管理逻辑 O(k)
}
```

**分析**:
- ?? 时间复杂度：O(n + m + k) = O(n)，线性增长
- ?? 当实体数量为 10000 时，可能需要 5-10ms
- ?? 无法被多线程优化（MonoBehaviour 限制）

**优化方案**:

#### 方案 1: 分帧更新
```csharp
public class FramedEntityUpdater : MonoBehaviour
{
    private IEntityManager m_EntityManager;
    private List<EntityLogic> m_AllEntities = new();
    private int m_UpdateIndex = 0;
    
    [SerializeField] private int m_EntitiesPerFrame = 100;
    
    private void Update()
    {
        int updated = 0;
        int startIndex = m_UpdateIndex;
        
        // 分帧处理实体更新
        while (updated < m_EntitiesPerFrame && m_UpdateIndex < m_AllEntities.Count)
        {
            var entity = m_AllEntities[m_UpdateIndex];
            if (entity.Available)
            {
                entity.ManualUpdate(Time.deltaTime);
            }
            
            m_UpdateIndex++;
            updated++;
        }
        
        // 循环
        if (m_UpdateIndex >= m_AllEntities.Count)
        {
            m_UpdateIndex = 0;
        }
    }
}
```

#### 方案 2: 实体激活过滤
```csharp
public class OptimizedEntityManager : IEntityManager
{
    private List<EntityLogic> m_ActiveEntities = new();
    
    public void Update()
    {
        // 只更新活跃的实体
        foreach (var entity in m_ActiveEntities)
        {
            entity.OnUpdate(...);
        }
    }
    
    public void ShowEntity(...)
    {
        // 显示时加入活跃列表
        m_ActiveEntities.Add(entityLogic);
    }
    
    public void HideEntity(...)
    {
        // 隐藏时移除活跃列表
        m_ActiveEntities.Remove(entityLogic);
    }
}
```

#### 方案 3: 优先级分组更新
```csharp
public class PriorityBasedEntityManager : IEntityManager
{
    private Dictionary<int, List<EntityLogic>> m_EntitiesByPriority = new();
    
    public void Update()
    {
        // 优先更新高优先级实体
        for (int priority = 100; priority >= 0; priority--)
        {
            if (m_EntitiesByPriority.TryGetValue(priority, out var entities))
            {
                foreach (var entity in entities)
                {
                    entity.OnUpdate(...);
                    
                    // 如果时间超限，跳过低优先级
                    if (IsFrameTimeLimited())
                        return;
                }
            }
        }
    }
    
    private bool IsFrameTimeLimited()
    {
        // 检查帧时间是否超过阈值（比如 8ms）
        return Time.realtimeSinceStartup > m_FrameStartTime + 0.008f;
    }
}
```

---

### 瓶颈 2: 实体查询性能

**现象**:
- 频繁按条件查询实体时 FPS 下降
- 无法快速定位特定实体

**原因**:
```csharp
// ? 低效的查询方式
var playerEntities = entityManager.GetAllEntities()
    .Where(e => e.EntityAssetName == "Player.prefab")
    .Where(e => e.EntityGroup.GroupName == "Players")
    .ToList();  // O(n) 遍历

// 每次都要遍历整个字典和 LINQ 操作
```

**优化方案**:

#### 方案 1: 索引优化
```csharp
public class IndexedEntityManager : IEntityManager
{
    // 添加多个索引
    private Dictionary<int, EntityInfo> m_EntitiesById;
    private Dictionary<string, HashSet<int>> m_EntitiesByAssetName;
    private Dictionary<string, HashSet<int>> m_EntitiesByGroup;
    private Dictionary<int, HashSet<int>> m_EntitiesByLayer;
    
    public IEnumerable<IEntity> FindByAssetName(string assetName)
    {
        if (m_EntitiesByAssetName.TryGetValue(assetName, out var ids))
        {
            foreach (var id in ids)
            {
                if (m_EntitiesById.TryGetValue(id, out var info))
                    yield return info.Entity;
            }
        }
    }
    
    public IEnumerable<IEntity> FindByGroup(string groupName)
    {
        if (m_EntitiesByGroup.TryGetValue(groupName, out var ids))
        {
            foreach (var id in ids)
            {
                if (m_EntitiesById.TryGetValue(id, out var info))
                    yield return info.Entity;
            }
        }
    }
    
    public IEnumerable<IEntity> FindByGroupAndAsset(string groupName, string assetName)
    {
        // 取交集，进一步缩小范围
        if (m_EntitiesByGroup.TryGetValue(groupName, out var groupIds) &&
            m_EntitiesByAssetName.TryGetValue(assetName, out var assetIds))
        {
            var intersection = groupIds.Intersect(assetIds);
            foreach (var id in intersection)
            {
                if (m_EntitiesById.TryGetValue(id, out var info))
                    yield return info.Entity;
            }
        }
    }
    
    // 维护索引
    private void AddEntity(EntityInfo info)
    {
        m_EntitiesById[info.Id] = info;
        
        // 更新索引
        if (!m_EntitiesByAssetName.ContainsKey(info.AssetName))
            m_EntitiesByAssetName[info.AssetName] = new HashSet<int>();
        m_EntitiesByAssetName[info.AssetName].Add(info.Id);
        
        // ... 其他索引更新
    }
    
    private void RemoveEntity(EntityInfo info)
    {
        m_EntitiesById.Remove(info.Id);
        
        // 更新索引
        if (m_EntitiesByAssetName.TryGetValue(info.AssetName, out var assetSet))
            assetSet.Remove(info.Id);
        
        // ... 其他索引更新
    }
}
```

#### 方案 2: 缓存最近查询
```csharp
public class CachedEntityManager : IEntityManager
{
    private Dictionary<string, List<IEntity>> m_QueryCache = new();
    private float m_CacheValidTime = 0.1f;  // 100ms 缓存有效期
    private Dictionary<string, float> m_CacheTimestamps = new();
    
    public IEnumerable<IEntity> FindByAssetName(string assetName)
    {
        var cacheKey = $"AssetName:{assetName}";
        
        // 检查缓存是否有效
        if (m_QueryCache.TryGetValue(cacheKey, out var cached) &&
            Time.realtimeSinceStartup - m_CacheTimestamps[cacheKey] < m_CacheValidTime)
        {
            return cached;  // 返回缓存结果
        }
        
        // 执行实际查询
        var result = QueryAssetNameSlow(assetName).ToList();
        
        // 更新缓存
        m_QueryCache[cacheKey] = result;
        m_CacheTimestamps[cacheKey] = Time.realtimeSinceStartup;
        
        return result;
    }
}
```

---

### 瓶颈 3: 对象池回收延迟

**现象**:
- 内存占用不断增长
- 对象池中的对象无法及时回收
- 物理内存压力大

**原因**:
```csharp
// 对象池配置不当
var group = entityManager.GetEntityGroup("Enemies");
group.InstanceAutoReleaseInterval = 3600f;  // 1小时才释放！
group.InstanceCapacity = 1000;              // 容量太大
```

**优化方案**:

```csharp
public class OptimizedPoolConfiguration
{
    public static void ConfigureEntityGroups(IEntityManager entityManager)
    {
        // ? 临时对象组（快速释放）
        var effectGroup = entityManager.CreateEntityGroup("Effects");
        effectGroup.InstanceAutoReleaseInterval = 30f;      // 30秒释放
        effectGroup.InstanceCapacity = 50;                  // 容量较小
        effectGroup.InstanceExpireTime = 60f;               // 60秒过期
        
        // ? 中期对象组（适度持有）
        var bulletGroup = entityManager.CreateEntityGroup("Bullets");
        bulletGroup.InstanceAutoReleaseInterval = 120f;     // 2分钟释放
        bulletGroup.InstanceCapacity = 200;                 // 中等容量
        bulletGroup.InstanceExpireTime = 300f;              // 5分钟过期
        
        // ? 长期对象组（保持池）
        var playerGroup = entityManager.CreateEntityGroup("Players");
        playerGroup.InstanceAutoReleaseInterval = 600f;     // 10分钟释放
        playerGroup.InstanceCapacity = 10;                  // 容量很小
        playerGroup.InstanceExpireTime = 1200f;             // 20分钟过期
        
        // ? 根据硬件能力动态调整
        if (SystemInfo.systemMemorySize < 4096)  // 内存小于 4GB
        {
            // 更激进的回收策略
            effectGroup.InstanceAutoReleaseInterval = 15f;
            effectGroup.InstanceCapacity = 20;
        }
    }
}
```

---

### 瓶颈 4: GC Alloc 压力

**现象**:
- 帧率不稳定，每隔几秒出现卡顿
- Profiler 显示频繁的 GC.Alloc
- 内存碎片化

**原因**:
```csharp
// ? 在 Update 中进行内存分配
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    // 1. List 分配
    var nearbyEnemies = new List<EnemyEntity>();  // GC.Alloc!
    foreach (var enemy in GetAllEnemies())
    {
        if (Vector3.Distance(transform.position, enemy.transform.position) < 10)
            nearbyEnemies.Add(enemy);
    }
    
    // 2. String 分配
    Debug.Log($"Found {nearbyEnemies.Count} enemies");  // string.Format GC.Alloc!
    
    // 3. 委托/闭包分配
    nearbyEnemies.ForEach(e => ProcessEnemy(e));  // 可能产生闭包
    
    // 4. LINQ 分配
    var filtered = m_AllEnemies.Where(e => e.Hp > 0).ToList();  // GC.Alloc!
}
```

**优化方案**:

#### 方案 1: 使用对象池
```csharp
public class ListPool<T>
{
    private static Stack<List<T>> s_ListPool = new();
    
    public static List<T> Rent()
    {
        return s_ListPool.Count > 0 ? s_ListPool.Pop() : new List<T>();
    }
    
    public static void Return(List<T> list)
    {
        list.Clear();
        s_ListPool.Push(list);
    }
}

// 使用方式
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    var nearbyEnemies = ListPool<EnemyEntity>.Rent();
    try
    {
        foreach (var enemy in GetAllEnemies())
        {
            if (Vector3.Distance(transform.position, enemy.transform.position) < 10)
                nearbyEnemies.Add(enemy);
        }
        
        ProcessEnemies(nearbyEnemies);
    }
    finally
    {
        ListPool<EnemyEntity>.Return(nearbyEnemies);  // 归还池
    }
}
```

#### 方案 2: 避免 LINQ 和字符串操作
```csharp
// ? GC 分配
var healthy = m_Enemies.Where(e => e.Hp > 0).ToList();
var names = string.Join(", ", m_Enemies.Select(e => e.Name));
Debug.Log($"Enemies: {names}");

// ? 无 GC 分配
// 使用循环替代 LINQ
var healthyCount = 0;
for (int i = 0; i < m_Enemies.Count; i++)
{
    if (m_Enemies[i].Hp > 0)
    {
        healthyCount++;
    }
}

// 避免字符串操作
#if DEVELOPMENT_BUILD
Debug.Log("Enemies count: " + m_Enemies.Count);  // 避免 string.Format
#endif
```

#### 方案 3: 预分配缓冲区
```csharp
public class EntityLogicOptimized : EntityLogic
{
    // 预分配
    private List<EnemyEntity> m_NearbyEnemies = new(capacity: 100);
    private List<Vector3> m_PathPoints = new(capacity: 50);
    private RaycastHit[] m_RaycastHits = new RaycastHit[10];
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 清空但不释放分配的内存
        m_NearbyEnemies.Clear();
        
        // 查询
        foreach (var enemy in GetAllEnemies())
        {
            if (IsNearby(enemy))
                m_NearbyEnemies.Add(enemy);
        }
        
        // 使用
        ProcessEnemies(m_NearbyEnemies);
    }
}
```

---

## 优化策略

### 优化等级 1: 快速胜利（低风险）

| 优化项 | 实现难度 | 性能提升 | 代码改动 |
|-------|--------|--------|---------|
| 索引优化 | ? 简单 | 10-30% | 小 |
| 缓存查询结果 | ? 简单 | 5-15% | 小 |
| 避免 LINQ | ? 简单 | 5-10% | 小 |
| 对象池配置 | ? 简单 | 10-20% | 小 |
| GC 预分配 | ?? 中等 | 5-15% | 中 |

### 优化等级 2: 中等优化（中等风险）

| 优化项 | 实现难度 | 性能提升 | 代码改动 |
|-------|--------|--------|---------|
| 分帧更新 | ?? 中等 | 20-40% | 中 |
| 实体激活过滤 | ?? 中等 | 15-30% | 中 |
| 缓存池列表 | ?? 中等 | 10-20% | 中 |
| 优先级更新 | ?? 中等 | 15-25% | 中 |

### 优化等级 3: 深度优化（高风险）

| 优化项 | 实现难度 | 性能提升 | 代码改动 |
|-------|--------|--------|---------|
| 多线程更新 | ??? 复杂 | 30-50% | 大 |
| JobSystem 集成 | ??? 复杂 | 40-60% | 大 |
| 空间分割（四叉树）| ??? 复杂 | 20-40% | 大 |

---

## 性能基准测试

### 测试场景 1: 大量实体更新

```csharp
[TestFixture]
public class EntityUpdatePerformanceTests
{
    private IEntityManager m_EntityManager;
    private Stopwatch m_Stopwatch;
    
    [SetUp]
    public void Setup()
    {
        m_EntityManager = new EntityManager();
        m_EntityManager.CreateEntityGroup("Enemies");
        m_Stopwatch = new Stopwatch();
    }
    
    [Test]
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(10000)]
    public void BenchmarkEntityUpdate(int entityCount)
    {
        // 创建大量实体
        for (int i = 0; i < entityCount; i++)
        {
            m_EntityManager.ShowEntity("Enemies", "Enemy.prefab", null);
        }
        
        // 测量 Update 性能
        m_Stopwatch.Restart();
        
        for (int frame = 0; frame < 100; frame++)
        {
            m_EntityManager.Update(Time.deltaTime, Time.realtimeSinceStartup);
        }
        
        m_Stopwatch.Stop();
        
        float averageTime = m_Stopwatch.ElapsedMilliseconds / 100f;
        Debug.Log($"EntityCount: {entityCount}, Avg Time: {averageTime:F2}ms");
        
        // 性能指标
        // 100 entities: < 0.5ms
        // 1000 entities: < 2ms
        // 10000 entities: < 15ms
        Assert.That(averageTime, Is.LessThan(20f), 
            $"Entity update too slow: {averageTime}ms per frame");
    }
}
```

### 测试场景 2: 查询性能

```csharp
[Test]
public void BenchmarkEntityQueries()
{
    // 创建测试实体
    CreateTestEntities(10000);
    
    var queries = new[]
    {
        ("FindById", () => m_EntityManager.GetEntity(5000)),
        ("FindByGroup", () => m_EntityManager.GetEntitiesInGroup("Enemies").Count()),
        ("FindByAsset", () => FindByAssetName("Enemy.prefab").Count()),
        ("FindByTag", () => FindByTag("Boss").Count()),
    };
    
    foreach (var (name, query) in queries)
    {
        m_Stopwatch.Restart();
        
        for (int i = 0; i < 1000; i++)
        {
            query();
        }
        
        m_Stopwatch.Stop();
        
        float avgTime = m_Stopwatch.ElapsedMilliseconds / 1000f;
        Debug.Log($"{name}: {avgTime:F3}ms");
    }
}
```

### 测试场景 3: 内存占用

```csharp
[Test]
public void BenchmarkMemoryUsage()
{
    var initialMemory = GC.GetTotalMemory(true);
    
    // 创建大量实体
    for (int i = 0; i < 10000; i++)
    {
        m_EntityManager.ShowEntity("Enemies", "Enemy.prefab", null);
    }
    
    var withEntitiesMemory = GC.GetTotalMemory(false);
    
    // 隐藏所有实体
    for (int i = 1; i <= 10000; i++)
    {
        m_EntityManager.HideEntity(m_EntityManager.GetEntity(i));
    }
    
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    var afterHideMemory = GC.GetTotalMemory(true);
    
    // 报告
    Debug.Log($"Initial: {initialMemory / 1024}KB");
    Debug.Log($"With Entities: {withEntitiesMemory / 1024}KB");
    Debug.Log($"After Hide: {afterHideMemory / 1024}KB");
    Debug.Log($"Leakage: {(afterHideMemory - initialMemory) / 1024}KB");
}
```

---

## 监控和调优

### 实时性能监控面板

```csharp
public class EntityPerformanceUI : MonoBehaviour
{
    private IEntityManager m_EntityManager;
    private GUIStyle m_Style;
    
    private float m_UpdateTime;
    private float[] m_UpdateTimes = new float[60];
    private int m_UpdateIndex;
    
    private void OnGUI()
    {
        if (GUILayout.Button("Toggle Performance Monitor"))
        {
            enabled = !enabled;
        }
        
        if (!enabled) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Entity Performance Monitor");
        
        // 统计信息
        GUILayout.Label($"Total Entities: {m_EntityManager.EntityCount}");
        GUILayout.Label($"Total Groups: {m_EntityManager.EntityGroupCount}");
        
        // 性能信息
        GUILayout.Label($"Update Time: {m_UpdateTime:F2}ms");
        GUILayout.Label($"Avg (60f): {GetAverageUpdateTime():F2}ms");
        
        // 内存信息
        GUILayout.Label($"Heap: {GC.GetTotalMemory(false) / 1024}KB");
        GUILayout.Label($"GC Alloc: {Profiler.GetMonoUsedSizeLong() / 1024}KB");
        
        GUILayout.EndArea();
    }
    
    private float GetAverageUpdateTime()
    {
        float sum = 0;
        for (int i = 0; i < m_UpdateTimes.Length; i++)
            sum += m_UpdateTimes[i];
        return sum / m_UpdateTimes.Length;
    }
}
```

### 性能优化检查清单

```
□ 索引优化
  └─ [ ] 添加 AssetName 索引
  └─ [ ] 添加 Layer 索引
  └─ [ ] 添加 Tag 索引
  └─ [ ] 测试查询性能

□ 查询优化
  └─ [ ] 避免频繁的 Where/Select
  └─ [ ] 缓存查询结果
  └─ [ ] 使用预先维护的列表

□ GC 优化
  └─ [ ] 移除 LINQ 操作
  └─ [ ] 预分配集合
  └─ [ ] 避免字符串操作
  └─ [ ] 使用对象池

□ 对象池优化
  └─ [ ] 调整 InstanceCapacity
  └─ [ ] 配置 InstanceAutoReleaseInterval
  └─ [ ] 设置 InstanceExpireTime
  └─ [ ] 验证内存回收

□ Update 优化
  └─ [ ] 分帧处理实体
  └─ [ ] 过滤非活跃实体
  └─ [ ] 优先级更新
  └─ [ ] 减少计算频率

□ 监控和测试
  └─ [ ] 建立性能基准测试
  └─ [ ] 添加实时监控
  └─ [ ] 定期性能分析
  └─ [ ] 内存泄漏检查
```

---

## 总结

### 优化原则

1. **测量优先** - 先用 Profiler 找到瓶颈
2. **因地制宜** - 根据实际场景选择优化策略
3. **逐步优化** - 一次优化一个方面，避免过度设计
4. **权衡取舍** - 性能 vs 代码复杂性 vs 维护成本

### 推荐优化流程

```
1. 建立基准测试
   ↓
2. 用 Profiler 分析
   ↓
3. 找到主要瓶颈（通常集中在 20% 的代码）
   ↓
4. 应用快速胜利优化
   ↓
5. 重新测量和分析
   ↓
6. 如果需要，应用中等优化
   ↓
7. 反复迭代
   ↓
8. 文档化优化内容
```

### 性能目标

| 指标 | 目标值 | 说明 |
|------|-------|------|
| Update 耗时 | < 5ms | 10000+ 实体 |
| 查询耗时 | < 1ms | 单次查询 |
| GC.Alloc | 0B/frame | 稳定后 |
| 内存占用 | < 100MB | 10000 实体 |
| 帧率 | >= 60 FPS | 目标帧率 |

**记住**: 过早优化是万恶之源，但完全忽视性能也是自杀。保持平衡！??
