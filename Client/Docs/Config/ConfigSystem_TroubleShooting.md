# 配置表系统故障排除与性能优化指南

## 1. 常见问题排查

### 1.1 问题：配置为 null

#### 症状
```csharp
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
// config 为 null，导致后续调用异常
```

#### 原因分析
| 原因 | 检查方法 | 解决方案 |
|-----|---------|--------|
| 配置还未加载 | `IsLoaded` 标志 | 等待 `LoadAllTablesAsync()` 完成 |
| 配置表未注册 | 检查 `TablesComponent` | 确保配置表在 `AddConfig()` 中注册 |
| 配置表类型错误 | 检查泛型参数 | 确保类型与实际加载的类型一致 |
| ConfigComponent 未初始化 | 检查 `Awake()` 是否调用 | 确保在 MonoBehaviour 生命周期中初始化 |

#### 排查步骤
```csharp
// 第一步：验证 ConfigComponent 存在
var configComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
if (configComponent == null)
{
    Debug.LogError("ConfigComponent not found");
    return;
}

// 第二步：检查是否已加载完成
var tablesComponent = GetComponent<TablesComponent>();
if (!tablesComponent.IsLoaded)
{
    Debug.LogWarning("Configs not loaded yet");
    return;
}

// 第三步：检查配置是否存在
if (!configComponent.HasConfig<TbItemConfig>())
{
    Debug.LogError("ItemConfig not registered");
    return;
}

// 第四步：获取并验证
var config = configComponent.GetConfig<TbItemConfig>();
if (config == null)
{
    Debug.LogError("Failed to get ItemConfig");
    return;
}

Debug.Log("Config is valid");
```

### 1.2 问题：配置数据为空

#### 症状
```csharp
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
if (config.TryGet(1001, out var item))
{
    // 从不执行，说明没有数据
}
```

#### 原因分析
| 原因 | 检查方法 | 解决方案 |
|-----|---------|--------|
| 配置文件不存在 | 检查 Resources 路径 | 确保 JSON 文件在正确位置 |
| 文件格式错误 | 打开文件查看 | 确保 JSON 格式正确 |
| ID 值错误 | 查看配置文件 | 使用 `GetAll()` 遍历查看实际 ID |
| 反序列化失败 | 查看加载日志 | 检查字段类型是否匹配 |

#### 排查步骤
```csharp
// 第一步：检查配置数据数量
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
Debug.Log($"ItemConfig Count: {config.Count}");

if (config.Count == 0)
{
    Debug.LogError("ItemConfig is empty - loading failed");
    return;
}

// 第二步：查看实际包含的 ID
var allItems = config.GetAll();
foreach (var item in allItems)
{
    Debug.Log($"Item ID: {item.Id}");
}

// 第三步：验证要查询的 ID 是否存在
int searchId = 1001;
bool found = false;
foreach (var item in allItems)
{
    if (item.Id == searchId)
    {
        found = true;
        break;
    }
}

if (!found)
{
    Debug.LogWarning($"ID {searchId} not found in config");
}
```

### 1.3 问题：热更新后配置过时

#### 症状
```csharp
var config = m_ConfigComponent.GetConfig<TbItemConfig>();

// 触发配置热更新
await configUpdateManager.HotUpdateAllConfigs();

// 使用旧引用，数据已过期
config.TryGet(1001, out var item);  // 获取过期数据
```

#### 原因分析
这是因为缓存了配置对象的引用。热更新后，新配置注册到 ConfigManager，但缓存的旧引用仍指向旧数据。

#### 解决方案

? **方案 1：不缓存配置对象**（推荐）
```csharp
public class ItemSystem
{
    public void UseItem(int itemId)
    {
        // 每次使用前重新获取，确保使用最新配置
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        if (config != null && config.TryGet(itemId, out var item))
        {
            // 使用最新数据
        }
    }
}
```

? **方案 2：监听重载事件**
```csharp
public class ItemSystem : IDisposable
{
    private TbItemConfig m_ItemConfig;
    private ConfigUpdateManager m_ConfigUpdateManager;
    
    public void Initialize(ConfigComponent configComponent, ConfigUpdateManager updateManager)
    {
        m_ConfigUpdateManager = updateManager;
        m_ItemConfig = configComponent.GetConfig<TbItemConfig>();
        
        // 监听配置重载事件
        m_ConfigUpdateManager.OnConfigReloaded += OnConfigReloaded;
    }
    
    private void OnConfigReloaded()
    {
        // 配置更新时，重新获取
        m_ItemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        Debug.Log("Config updated, using new reference");
    }
    
    public void Dispose()
    {
        m_ConfigUpdateManager.OnConfigReloaded -= OnConfigReloaded;
    }
}
```

---

## 2. 加载问题诊断

### 2.1 配置加载失败

#### 常见错误信息

```
错误 1: "Config manager is invalid"
→ ConfigManager 初始化失败
→ 检查 GameFrameworkEntry 是否正确初始化

错误 2: "Failed to load resource at path: Config/item.json"
→ 配置文件不存在或路径错误
→ 检查文件是否在 Resources/Config/ 目录下

错误 3: "SerializationException in ConfigDeserialize"
→ JSON 格式或字段类型不匹配
→ 验证 JSON 文件格式和 C# 类定义一致

错误 4: "NullReferenceException when accessing config"
→ 配置对象为 null
→ 确保配置已加载并注册
```

#### 诊断脚本
```csharp
public class ConfigDiagnostics : MonoBehaviour
{
    public void DiagnoseConfigLoading()
    {
        Debug.Log("=== Config Diagnostics ===");
        
        // 检查 ConfigComponent
        var configComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        Debug.Log($"ConfigComponent found: {configComponent != null}");
        
        // 检查 ConfigManager
        var configManager = GameFrameworkEntry.GetModule<IConfigManager>();
        Debug.Log($"ConfigManager found: {configManager != null}");
        
        if (configManager != null)
        {
            Debug.Log($"Total configs loaded: {configManager.Count}");
        }
        
        // 检查加载状态
        var tablesComponent = GetComponent<TablesComponent>();
        Debug.Log($"Tables loaded: {tablesComponent?.IsLoaded ?? false}");
        
        // 尝试获取配置
        try
        {
            var itemConfig = configComponent.GetConfig<TbItemConfig>();
            Debug.Log($"ItemConfig: {(itemConfig != null ? "Found" : "Not found")}");
            
            if (itemConfig != null)
            {
                Debug.Log($"ItemConfig count: {itemConfig.Count}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting ItemConfig: {ex.Message}");
        }
        
        Debug.Log("=== End Diagnostics ===");
    }
}
```

### 2.2 异步加载卡顿

#### 症状
- 游戏启动时长加载
- 首次使用配置时卡顿
- 内存突然增加

#### 优化方案

**优化 1：并发加载**
```csharp
// ? 坏：串行加载，耗时累加
await TbItemConfig.LoadAsync();        // 100ms
await TbEquipmentConfig.LoadAsync();   // 100ms
await TbSkillConfig.LoadAsync();       // 100ms
// 总耗时：300ms

// ? 好：并发加载，耗时取最长
await Task.WhenAll(
    TbItemConfig.LoadAsync(),
    TbEquipmentConfig.LoadAsync(),
    TbSkillConfig.LoadAsync()
);
// 总耗时：100ms（理论值）
```

**优化 2：延迟加载**
```csharp
public class LazyConfigLoader
{
    private Dictionary<Type, IDataTable> m_LoadedConfigs = new();
    private Dictionary<Type, Task> m_LoadingTasks = new();
    
    public async Task<T> GetOrLoadAsync<T>() where T : class, IDataTable, new()
    {
        var type = typeof(T);
        
        // 已加载
        if (m_LoadedConfigs.TryGetValue(type, out var config))
        {
            return config as T;
        }
        
        // 正在加载，等待
        if (m_LoadingTasks.TryGetValue(type, out var task))
        {
            await task;
            return m_LoadedConfigs[type] as T;
        }
        
        // 首次加载
        var loadingTask = LoadConfigAsync<T>();
        m_LoadingTasks[type] = loadingTask;
        
        await loadingTask;
        return m_LoadedConfigs[type] as T;
    }
    
    private async Task LoadConfigAsync<T>() where T : class, IDataTable, new()
    {
        var config = new T();
        await config.LoadAsync();
        m_LoadedConfigs[typeof(T)] = config;
    }
}
```

---

## 3. 性能优化指南

### 3.1 查询性能优化

#### 问题：频繁查询相同配置

```csharp
// ? 坏：在循环中重复查询同一配置
for (int i = 0; i < 1000; i++)
{
    var config = m_ConfigComponent.GetConfig<TbItemConfig>();  // 重复获取
    config.TryGet(i, out var item);
}

// ? 好：获取一次，多次使用
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
for (int i = 0; i < 1000; i++)
{
    config.TryGet(i, out var item);
}
```

**性能提升**：~10-20%（取决于获取过程的复杂度）

#### 问题：重复的 TryGet 调用

```csharp
// ? 坏：多次查询同一 ID
if (config.TryGet(itemId, out var item1))
{
    if (config.TryGet(itemId, out var item2))
    {
        if (config.TryGet(itemId, out var item3))
        {
            // 使用 item
        }
    }
}

// ? 好：查询一次，多次使用
if (config.TryGet(itemId, out var item))
{
    // 使用 item 三次
}
```

#### 问题：不必要的 GetAll() 调用

```csharp
// ? 坏：多次遍历全部配置
for (int j = 0; j < 10; j++)
{
    var allItems = config.GetAll();  // 重复遍历
    // 处理 allItems
}

// ? 好：获取一次缓存
var allItems = config.GetAll();
for (int j = 0; j < 10; j++)
{
    // 使用缓存的 allItems
}
```

### 3.2 内存优化

#### 减少配置占用

```csharp
public class MemoryEfficientConfigUsage
{
    /// <summary>
    /// 问题：将配置全部加载到内存
    /// </summary>
    public void InefficientWay()
    {
        var allItems = m_ItemConfig.GetAll();  // 所有道具在内存中
        
        // 但可能只使用其中 1% 的数据
        ProcessItems(allItems);
    }
    
    /// <summary>
    /// 优化：按需加载
    /// </summary>
    public void EfficientWay(List<int> itemIds)
    {
        var items = new List<ItemConfig>();
        
        foreach (var id in itemIds)
        {
            if (m_ItemConfig.TryGet(id, out var item))
            {
                items.Add(item);  // 只保存需要的数据
            }
        }
        
        ProcessItems(items);
    }
}
```

#### 卸载不用的配置

```csharp
public class ConfigMemoryManager
{
    private ConfigComponent m_ConfigComponent;
    
    /// <summary>
    /// 场景切换时卸载不用的配置
    /// </summary>
    public void OnSceneChange(string newScene)
    {
        // 卸载战斗配置（如果进入大厅场景）
        if (newScene == "Hall")
        {
            m_ConfigComponent.RemoveConfig<TbPvEConfig>();
            m_ConfigComponent.RemoveConfig<TbBossConfig>();
        }
        
        // 卸载大厅配置（如果进入战斗场景）
        if (newScene == "Battle")
        {
            m_ConfigComponent.RemoveConfig<TbShopConfig>();
            m_ConfigComponent.RemoveConfig<TbActivityConfig>();
        }
    }
}
```

### 3.3 缓存优化

#### 本地缓存策略

```csharp
public class ConfigCache
{
    private Dictionary<(Type, int), object> m_QueryCache = new();
    private Dictionary<Type, List<object>> m_ListCache = new();
    
    /// <summary>
    /// 缓存单条查询结果
    /// </summary>
    public bool TryGetCached<T>(int id, out T result) where T : class
    {
        var key = (typeof(T), id);
        if (m_QueryCache.TryGetValue(key, out var cached))
        {
            result = cached as T;
            return result != null;
        }
        
        result = null;
        return false;
    }
    
    /// <summary>
    /// 缓存 GetAll() 结果
    /// </summary>
    public List<T> GetAllCached<T>() where T : class
    {
        var type = typeof(T);
        if (m_ListCache.TryGetValue(type, out var cached))
        {
            return cached as List<T>;
        }
        
        // 未缓存，返回 null，调用者应自行调用 config.GetAll()
        return null;
    }
    
    /// <summary>
    /// 缓存查询结果
    /// </summary>
    public void Cache<T>(int id, T value) where T : class
    {
        var key = (typeof(T), id);
        m_QueryCache[key] = value;
    }
    
    /// <summary>
    /// 清空缓存（配置热更新时调用）
    /// </summary>
    public void Clear()
    {
        m_QueryCache.Clear();
        m_ListCache.Clear();
    }
}
```

---

## 4. 性能基准测试

### 4.1 关键指标

| 指标 | 目标值 | 优化方法 |
|-----|-------|--------|
| 配置加载时间 | < 1s | 并发加载、资源优化 |
| 单条查询时间 | < 1ms | 优化数据结构、索引 |
| 内存占用 | < 50MB | 卸载不用的配置、压缩 |
| 首次使用延迟 | < 100ms | 预热缓存、延迟加载 |

### 4.2 基准测试代码

```csharp
using System.Diagnostics;
using UnityEngine;

public class ConfigBenchmark : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    /// <summary>
    /// 测试加载性能
    /// </summary>
    public async void BenchmarkLoading()
    {
        var sw = Stopwatch.StartNew();
        
        var tablesComponent = new TablesComponent();
        tablesComponent.Init(m_ConfigComponent);
        await tablesComponent.LoadAllTablesAsync();
        
        sw.Stop();
        
        Debug.Log($"Loading time: {sw.ElapsedMilliseconds}ms");
        Debug.Log($"Average per table: {sw.ElapsedMilliseconds / 13}ms");  // 假设 13 个表
    }
    
    /// <summary>
    /// 测试查询性能
    /// </summary>
    public void BenchmarkQuery()
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        var sw = Stopwatch.StartNew();
        
        // 测试 10000 次查询
        for (int i = 0; i < 10000; i++)
        {
            config.TryGet(i, out _);
        }
        
        sw.Stop();
        
        double avgTime = (double)sw.ElapsedMilliseconds / 10000;
        Debug.Log($"10000 queries: {sw.ElapsedMilliseconds}ms");
        Debug.Log($"Average per query: {avgTime}ms");
    }
    
    /// <summary>
    /// 测试内存占用
    /// </summary>
    public void BenchmarkMemory()
    {
        System.GC.Collect();
        long beforeMemory = System.GC.GetTotalMemory(true);
        
        var allItems = m_ConfigComponent.GetConfig<TbItemConfig>().GetAll();
        
        long afterMemory = System.GC.GetTotalMemory(true);
        long memoryUsed = (afterMemory - beforeMemory) / 1024;  // KB
        
        Debug.Log($"ItemConfig memory: {memoryUsed}KB");
        Debug.Log($"Per item memory: {memoryUsed / allItems.Count}KB");
    }
    
    /// <summary>
    /// 测试并发加载 vs 串行加载
    /// </summary>
    public async void BenchmarkConcurrency()
    {
        // 测试 1: 串行加载
        var sw1 = Stopwatch.StartNew();
        var config1 = new TbItemConfig();
        var config2 = new TbEquipmentConfig();
        
        await config1.LoadAsync();
        await config2.LoadAsync();
        
        sw1.Stop();
        Debug.Log($"Sequential loading: {sw1.ElapsedMilliseconds}ms");
        
        // 测试 2: 并发加载
        var sw2 = Stopwatch.StartNew();
        var config3 = new TbItemConfig();
        var config4 = new TbEquipmentConfig();
        
        await System.Threading.Tasks.Task.WhenAll(
            config3.LoadAsync(),
            config4.LoadAsync()
        );
        
        sw2.Stop();
        Debug.Log($"Concurrent loading: {sw2.ElapsedMilliseconds}ms");
        Debug.Log($"Speedup: {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds}x");
    }
}
```

---

## 5. 调试技巧

### 5.1 配置状态监视

```csharp
public class ConfigMonitor : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("=== Config Monitor ===");
        
        var configManager = GameFrameworkEntry.GetModule<IConfigManager>();
        GUILayout.Label($"Total Configs: {configManager?.Count ?? 0}");
        
        // 列出所有已加载的配置
        GUILayout.Label("Loaded Configs:");
        var configs = new[]
        {
            ("ItemConfig", m_ConfigComponent.HasConfig<TbItemConfig>()),
            ("AchievementConfig", m_ConfigComponent.HasConfig<TbAchievementConfig>()),
            ("EquipmentConfig", m_ConfigComponent.HasConfig<TbEquipmentConfig>())
        };
        
        foreach (var (name, loaded) in configs)
        {
            GUILayout.Label($"  {name}: {(loaded ? "?" : "?")}");
        }
        
        GUILayout.EndArea();
    }
}
```

### 5.2 日志记录

```csharp
public class ConfigLogger
{
    public static void LogConfigLoadingProgress(string configName, int itemsLoaded, int totalItems)
    {
        float progress = (float)itemsLoaded / totalItems * 100;
        Debug.Log($"[Config] {configName}: {progress:F1}% ({itemsLoaded}/{totalItems})");
    }
    
    public static void LogConfigAccessPattern(string configName, int queryCount)
    {
        Debug.Log($"[Config] {configName} queried {queryCount} times");
    }
    
    public static void LogConfigMemoryUsage(string configName, long bytes)
    {
        Debug.Log($"[Config] {configName} memory: {bytes / 1024}KB");
    }
}
```

---

## 6. 常用优化检查清单

### 性能优化
- [ ] 使用并发加载配置表
- [ ] 避免在循环中重复获取配置
- [ ] 缓存 GetAll() 结果
- [ ] 使用 TryGet 代替异常处理
- [ ] 定期卸载不需要的配置

### 内存优化
- [ ] 及时清理配置缓存
- [ ] 场景切换时卸载相关配置
- [ ] 避免重复加载相同配置
- [ ] 使用流式加载大型配置

### 稳定性优化
- [ ] 添加配置验证机制
- [ ] 处理配置不存在的情况
- [ ] 实现加载失败重试逻辑
- [ ] 记录配置加载日志

### 可维护性优化
- [ ] 统一的配置访问接口
- [ ] 清晰的错误提示
- [ ] 完善的文档注释
- [ ] 单元测试覆盖

