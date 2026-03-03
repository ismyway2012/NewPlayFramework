# 配置表系统最佳实践

## 1. 设计原则

### 1.1 配置表设计 5 大原则

#### ? 原则 1：单一责任
每个配置表只负责一类数据，不要混合多个业务概念。

```csharp
// ? 好：清晰的业务边界
public class TbItemConfig      // 道具配置
public class TbEquipmentConfig // 装备配置
public class TbSkillConfig     // 技能配置

// ? 坏：混合了多个概念
public class TbItemAllConfig
{
    public ItemData[] Items;
    public EquipmentData[] Equipments;
    public SkillData[] Skills;
    // ... 太杂乱
}
```

#### ? 原则 2：用 ID 做主键
使用唯一的 ID（int/long/string）作为主键，支持快速查询。

```csharp
// ? 好：ID 作为主键
public class ItemConfig
{
    public int Id { get; }      // 主键，唯一标识
    public string Name { get; }
    public int Price { get; }
}

// ? 坏：使用字符串作为标识
public class ItemConfig
{
    public string Name { get; }     // 重复，低效
    public string UniqueName { get; }
}
```

#### ? 原则 3：用 Key 存文本
文本字段存储多语言 Key，而不是直接文本。

```csharp
// ? 好：存多语言 Key
public class ItemConfig
{
    public int Id { get; }
    public string NameKey { get; }      // "item_name_001"
    public string DescriptionKey { get; } // "item_desc_001"
}

// ? 坏：直接存文本
public class ItemConfig
{
    public int Id { get; }
    public string Name { get; }         // "剑"（无法多语言）
    public string Description { get; }  // "伤害 10" （写死）
}
```

使用方式：
```csharp
// 获取多语言文本
string displayName = m_LocalizationComponent.GetText(itemConfig.NameKey);
```

#### ? 原则 4：避免硬关联
配置之间通过 ID 引用，不要直接对象嵌套。

```csharp
// ? 好：使用 ID 引用
public class EquipmentConfig
{
    public int Id { get; }
    public int[] MaterialItemIds { get; }  // 引用道具的 ID
}

// 使用时：
var equipment = equipmentConfig.TryGet(eqpId, out var eqp);
foreach (var matId in eqp.MaterialItemIds)
{
    if (itemConfig.TryGet(matId, out var material))
    {
        // 使用材料信息
    }
}

// ? 坏：直接嵌套对象
public class EquipmentConfig
{
    public int Id { get; }
    public ItemConfig[] Materials { get; }  // 循环依赖，难以维护
}
```

#### ? 原则 5：保持配置独立性
配置表应该相互独立，不依赖其他配置表的加载顺序。

```csharp
// ? 好：独立加载
await Task.WhenAll(
    itemConfig.LoadAsync(),
    equipmentConfig.LoadAsync(),
    skillConfig.LoadAsync()
);
// 可以并发加载，顺序无关

// ? 坏：有依赖关系
await itemConfig.LoadAsync();
await equipmentConfig.LoadAsync();  // 必须等待 itemConfig 加载完
```

---

## 2. 代码使用规范

### 2.1 获取配置的正确方式

#### ? 推荐：每次使用前获取

```csharp
public class AchievementSystem
{
    public void OnAchievementUnlock(int achievementId)
    {
        // 每次使用前重新获取配置（支持热更新）
        var config = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        if (config != null && config.TryGet(achievementId, out var achievement))
        {
            // 使用最新配置数据
            HandleUnlock(achievement);
        }
    }
}
```

**优点**：
- 支持配置热更新
- 不会使用过期数据
- 代码逻辑清晰

#### ?? 警惕：缓存配置对象

```csharp
public class AchievementSystem
{
    private TbAchievementConfig m_AchievementConfig;
    
    public void Initialize(ConfigComponent configComponent)
    {
        // ?? 危险：保存配置引用
        m_AchievementConfig = configComponent.GetConfig<TbAchievementConfig>();
    }
    
    public void OnAchievementUnlock(int achievementId)
    {
        // 问题：热更新后 m_AchievementConfig 指向旧数据
        if (m_AchievementConfig.TryGet(achievementId, out var achievement))
        {
            // ? 可能使用过期数据
        }
    }
}
```

**问题**：
- 配置热更新时，缓存的引用不会更新
- 难以调试和定位问题

#### ? 折中：在初始化时缓存，但要监听重载事件

```csharp
public class AchievementSystem : IDisposable
{
    private TbAchievementConfig m_AchievementConfig;
    
    public void Initialize(ConfigComponent configComponent, ConfigUpdateManager updateManager)
    {
        m_AchievementConfig = configComponent.GetConfig<TbAchievementConfig>();
        
        // 监听配置重载事件
        updateManager.OnConfigReloaded += RefreshConfig;
    }
    
    private void RefreshConfig()
    {
        // 配置更新时，重新获取配置对象
        var configComponent = GameFrameworkEntry.GetModule<IConfigManager>();
        // 强转获取
        m_AchievementConfig = ...
    }
    
    public void Dispose()
    {
        // 清理事件监听
        updateManager.OnConfigReloaded -= RefreshConfig;
    }
}
```

### 2.2 配置查询模式

#### 模式 1：单条查询

```csharp
public class ItemService
{
    public ItemConfig GetItem(int itemId)
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (config != null && config.TryGet(itemId, out var item))
        {
            return item;
        }
        
        return null;  // 配置不存在
    }
}
```

#### 模式 2：列表查询

```csharp
public class EquipmentService
{
    public List<EquipmentConfig> GetAllEquipments()
    {
        var config = m_ConfigComponent.GetConfig<TbEquipmentConfig>();
        
        if (config != null)
        {
            return config.GetAll().ToList();  // 获取全部
        }
        
        return new List<EquipmentConfig>();
    }
}
```

#### 模式 3：条件查询

```csharp
public class SkillService
{
    public List<SkillConfig> GetSkillsByLevel(int minLevel)
    {
        var config = m_ConfigComponent.GetConfig<TbSkillConfig>();
        
        if (config != null)
        {
            return config.GetAll()
                .Where(s => s.UnlockLevel >= minLevel)
                .ToList();
        }
        
        return new List<SkillConfig>();
    }
}
```

#### 模式 4：批量查询

```csharp
public class RewardService
{
    public List<ItemConfig> GetRewardItems(List<int> itemIds)
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        var result = new List<ItemConfig>();
        
        foreach (var itemId in itemIds)
        {
            if (config != null && config.TryGet(itemId, out var item))
            {
                result.Add(item);
            }
        }
        
        return result;
    }
}
```

### 2.3 配置验证模式

#### 初始化时验证

```csharp
public class GameInitializer
{
    public bool ValidateAllConfigs()
    {
        // 检查关键配置是否存在
        if (!m_ConfigComponent.HasConfig<TbItemConfig>())
        {
            Debug.LogError("Missing TbItemConfig - game cannot start");
            return false;
        }
        
        if (!m_ConfigComponent.HasConfig<TbEquipmentConfig>())
        {
            Debug.LogError("Missing TbEquipmentConfig - game cannot start");
            return false;
        }
        
        // 检查配置数据完整性
        if (!ValidateConfigData())
        {
            Debug.LogError("Config data validation failed");
            return false;
        }
        
        return true;
    }
    
    private bool ValidateConfigData()
    {
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        if (itemConfig?.Count == 0)
        {
            Debug.LogError("ItemConfig is empty");
            return false;
        }
        
        // 检查具体数据
        var allItems = itemConfig.GetAll();
        foreach (var item in allItems)
        {
            if (item.Id <= 0 || string.IsNullOrEmpty(item.NameKey))
            {
                Debug.LogError($"Invalid item config: {item.Id}");
                return false;
            }
        }
        
        return true;
    }
}
```

#### 运行时容错

```csharp
public class ItemPresenter
{
    public void ShowItemInfo(int itemId)
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        // 配置可能不存在或数据可能丢失
        if (config == null)
        {
            Debug.LogWarning("ItemConfig not loaded yet");
            return;
        }
        
        if (!config.TryGet(itemId, out var item))
        {
            Debug.LogWarning($"Item {itemId} not found in config");
            return;
        }
        
        // 安全地使用配置
        string displayName = m_LocalizationComponent.GetText(item.NameKey);
        uiDisplay.ShowItem(displayName, item.Price);
    }
}
```

---

## 3. 性能优化

### 3.1 加载优化

#### ? 并发加载配置

```csharp
public class ConfigLoader
{
    public async Task LoadAllConfigs()
    {
        var tablesComponent = new TablesComponent();
        
        // 并发加载所有配置表（而非串行）
        await Task.WhenAll(
            tablesComponent.TbItemConfig.LoadAsync(),
            tablesComponent.TbEquipmentConfig.LoadAsync(),
            tablesComponent.TbSkillConfig.LoadAsync(),
            // ... 其他配置
        );
        
        // 注册到 ConfigManager
        RegisterAllConfigs(tablesComponent);
    }
}
```

**性能提升**：从 O(n) 降低到 O(1)（n 是配置表数量）

#### ? 延迟加载（可选配置）

```csharp
public class LazyConfigLoader
{
    private Dictionary<Type, Lazy<Task>> m_LazyConfigs = new();
    
    public async Task<TbConfig> GetConfigAsync<TbConfig>() 
        where TbConfig : class, IDataTable
    {
        var configType = typeof(TbConfig);
        
        if (!m_LazyConfigs.ContainsKey(configType))
        {
            // 首次访问时才加载
            m_LazyConfigs[configType] = new Lazy<Task>(
                async () => 
                {
                    var config = (TbConfig)Activator.CreateInstance(configType);
                    await config.LoadAsync();
                    m_ConfigComponent.Add(configType.Name, config);
                }
            );
        }
        
        await m_LazyConfigs[configType].Value;
        return m_ConfigComponent.GetConfig<TbConfig>();
    }
}
```

### 3.2 查询优化

#### ? 避免重复查询

```csharp
// ? 坏：多次查询相同配置
for (int i = 0; i < 100; i++)
{
    var config = m_ConfigComponent.GetConfig<TbItemConfig>();
    if (config.TryGet(i, out var item))
    {
        Process(item);
    }
}

// ? 好：查询一次，在本地使用
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
for (int i = 0; i < 100; i++)
{
    if (config.TryGet(i, out var item))
    {
        Process(item);
    }
}
```

#### ? 使用 TryGet 代替 try-catch

```csharp
// ? 坏：异常处理性能差
try
{
    var item = config.Get(itemId);  // 可能抛异常
    Use(item);
}
catch (Exception ex)
{
    Debug.LogWarning("Item not found");
}

// ? 好：使用 TryGet
if (config.TryGet(itemId, out var item))
{
    Use(item);
}
```

#### ? 批量操作优化

```csharp
// ? 坏：逐条查询
var result = new List<ItemConfig>();
foreach (var itemId in itemIds)
{
    if (config.TryGet(itemId, out var item))
    {
        result.Add(item);
    }
}

// ? 好：一次获取全部，再筛选
var allItems = config.GetAll();
var itemDict = new Dictionary<int, ItemConfig>(allItems.Count);
foreach (var item in allItems)
{
    itemDict[item.Id] = item;
}

var result = new List<ItemConfig>();
foreach (var itemId in itemIds)
{
    if (itemDict.TryGetValue(itemId, out var item))
    {
        result.Add(item);
    }
}
```

### 3.3 内存优化

#### ? 及时清理配置

```csharp
public class SceneLoader
{
    public async Task LoadScene(string sceneName)
    {
        // 卸载旧场景的配置
        if (sceneName == "Battle")
        {
            m_ConfigComponent.RemoveConfig<TbPvEConfig>();
            // 只保留必要配置
        }
        
        // 加载新场景的配置
        await LoadSceneSpecificConfigs(sceneName);
    }
}
```

#### ? 配置预热

```csharp
public class ConfigPreloader
{
    public async Task PreloadHotConfigs()
    {
        // 在后台线程预热频繁访问的配置
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        // 触发字典构建和缓存初始化
        var all = itemConfig.GetAll();
        var firstItem = itemConfig.Count > 0 ? all[0] : null;
    }
}
```

---

## 4. 常见错误与解决方案

### 错误 1：配置不存在仍然访问

```csharp
// ? 错误
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
var item = config.TryGet(itemId, out var result);  // 若 config 为 null，抛异常

// ? 正确
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
if (config != null && config.TryGet(itemId, out var result))
{
    // 安全使用
}
```

### 错误 2：配置热更新后使用缓存对象

```csharp
// ? 错误
class ItemSystem
{
    private TbItemConfig m_ItemConfig;  // 保存引用
    
    public void Initialize()
    {
        m_ItemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
    }
    
    public void Use()
    {
        // 热更新后，m_ItemConfig 可能已过期
        m_ItemConfig.TryGet(id, out var item);
    }
}

// ? 正确
class ItemSystem
{
    public void Use()
    {
        // 每次使用前获取最新配置
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        if (itemConfig != null && itemConfig.TryGet(id, out var item))
        {
            // 使用最新数据
        }
    }
}
```

### 错误 3：忘记异步加载

```csharp
// ? 错误：同步访问未加载的配置
public void OnGameStart()
{
    var config = m_ConfigComponent.GetConfig<TbItemConfig>();
    // 此时配置还在异步加载中，为 null
    config.TryGet(1, out var item);  // 异常
}

// ? 正确：等待加载完成
public async Task OnGameStart()
{
    await m_TablesComponent.LoadAllTablesAsync();
    
    var config = m_ConfigComponent.GetConfig<TbItemConfig>();
    if (config != null && config.TryGet(1, out var item))
    {
        // 安全使用
    }
}
```

### 错误 4：ID 冲突未检测

```csharp
// ? 错误：假设所有 ID 都有效
List<ItemConfig> items = new();
foreach (int id in someList)
{
    // 某些 ID 可能不存在
    items.Add(config.Get(id));  // 异常或返回空
}

// ? 正确：验证 ID 存在性
List<ItemConfig> items = new();
foreach (int id in someList)
{
    if (config.TryGet(id, out var item))
    {
        items.Add(item);
    }
    else
    {
        Debug.LogWarning($"Invalid item id: {id}");
    }
}
```

### 错误 5：配置表间循环引用

```csharp
// ? 错误：配置表间相互依赖
public class TbEquipmentConfig : IDataTable<EquipmentConfig>
{
    public async Task LoadAsync()
    {
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        // 假设 TbItemConfig 还没加载...死锁
    }
}

// ? 正确：配置表独立加载
public class TbEquipmentConfig : IDataTable<EquipmentConfig>
{
    public async Task LoadAsync()
    {
        // 只加载自己的数据，不依赖其他配置表
        var json = await LoadJsonAsync("Config/equipment.json");
        // ... 解析数据
    }
}

// 使用时才建立关系
var equipmentConfig = m_ConfigComponent.GetConfig<TbEquipmentConfig>();
var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
```

---

## 5. 文档维护规范

### 5.1 配置表变更记录

创建 `Docs/Config/CHANGELOG.md`：

```markdown
# 配置表变更日志

## 2024-01-15
- 新增 TbQuestConfig（任务配置表）
- AchievementConfig 新增字段：RewardItemIds (List<int>)
- ItemConfig 字段重命名：Price → SellPrice

## 2024-01-10
- 移除 TbLegacyConfig（旧配置表）
- 优化 TbItemConfig 加载性能
```

### 5.2 配置表数据字典

为每个配置表创建字段文档：

```markdown
# TbAchievementConfig 数据字典

| 字段名 | 类型 | 说明 | 示例 | 备注 |
|--------|------|------|------|------|
| Id | int | 成就 ID（主键） | 1001 | 全局唯一 |
| Image | int | 成就图标 ID | 2001 | 引用图片资源 |
| Name | string | 成就名称（多语言 Key） | achievement_001 | 支持多语言 |
| RewardItemIds | List<int> | 奖励道具 ID | [1,2,3] | 逗号分隔的 ID 列表 |
```

---

## 6. 检查清单

### 配置表新增时
- [ ] 在 Luban 中定义配置结构
- [ ] 生成 C# 代码
- [ ] 在 TablesComponent 注册
- [ ] 创建配置数据文件
- [ ] 添加到加载列表
- [ ] 编写文档

### 配置表修改时
- [ ] 更新 Luban 定义
- [ ] 重新生成代码
- [ ] 更新配置数据
- [ ] 更新变更日志
- [ ] 通知相关开发者

### 上线前检查
- [ ] 所有配置表已加载
- [ ] 关键数据已验证
- [ ] 无未使用的配置表
- [ ] 性能测试通过
- [ ] 文档已更新

