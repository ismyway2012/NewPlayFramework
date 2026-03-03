# 配置表工作流程指南

## 1. 配置表的全生命周期

### 1.1 工作流程概览

```
┌─────────────────┐
│   定义配置数据   │  Excel / JSON / Proto
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│   使用 Luban 生成 C# 代码      │  自动代码生成
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  将配置文件放入资源目录      │  Assets/Hotfix/Config/...
└────────┬────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│  创建 TablesComponent 加载器      │  异步加载所有配置
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│  通过 ConfigComponent 访问配置    │  GetConfig<T>()
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│  应用代码使用配置数据            │  业务逻辑
└──────────────────────────────────┘
```

---

## 2. 定义配置数据

### 2.1 使用 Luban 定义配置

配置数据通常使用 Excel 或 JSON 定义，通过 Luban 代码生成工具转换为 C# 类。

#### 示例：成就配置 (AchievementConfig)

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| Id | int | 成就ID | 1001 |
| Image | int | 成就图标ID | 2001 |
| Name | string | 成就名称(多语言Key) | achievement_name_001 |
| AchievementContent | string | 成就描述(多语言Key) | achievement_desc_001 |
| LockText | string | 未解锁文字(多语言Key) | locked_text_001 |
| AchievementUnlockCondition | List<int> | 解锁条件ID列表 | [1, 2, 3] |

#### Luban 配置示例 (.proto 或 Excel 格式)

```proto
message AchievementConfig {
    int32 id = 1;
    int32 image = 2;
    string name = 3;           // 多语言Key
    string achievement_content = 4;
    string lock_text = 5;
    repeated int32 achievement_unlock_condition = 6;
}
```

### 2.2 配置设计原则

| 原则 | 说明 | 示例 |
|-----|------|------|
| **单一责任** | 每个配置表只管理一类数据 | AchievementConfig 只管成就，ItemConfig 只管道具 |
| **ID 作为主键** | 使用 int/long/string ID 唯一标识记录 | achievementId = 1001 |
| **避免硬关联** | 使用 ID 引用而非直接对象引用 | 配置中存 itemId，运行时 lookup |
| **多语言分离** | 文本字段存多语言 Key，而非直接文本 | Name = "achievement_name_001" |
| **版本控制** | 配置数据应与代码版本同步管理 | 配置提交到 Git |

---

## 3. 代码生成与配置类结构

### 3.1 自动生成的配置类

使用 Luban 生成后的配置类结构如下：

```csharp
// 自动生成的配置数据类（存储单个记录）
public sealed partial class AchievementConfig : LuBan.Runtime.BeanBase
{
    // 属性定义
    public int Id { get; }
    public int Image { get; }
    public string Name { get; }
    public string AchievementContent { get; }
    
    // 构造函数
    public AchievementConfig(int Id, int Image, string Name, ...) { }
    
    // JSON 反序列化
    public AchievementConfig(JSONNode _buf) { }
    public static AchievementConfig DeserializeAchievementConfig(JSONNode _buf) { }
}

// 自动生成的配置表类（存储全部记录）
public sealed partial class TbAchievementConfig : IDataTable<AchievementConfig>
{
    private readonly Dictionary<int, AchievementConfig> m_AchievementConfigDict;
    
    public int Count { get; }
    public bool TryGet(int id, out AchievementConfig value) { }
    
    public async Task LoadAsync()
    {
        // 从资源加载 JSON 并反序列化
    }
}
```

### 3.2 关键实现细节

#### 3.2.1 ID 索引机制
```csharp
// 内部使用字典进行快速查询
private readonly Dictionary<int, AchievementConfig> m_AchievementConfigDict;

public bool TryGet(int id, out AchievementConfig value)
{
    return m_AchievementConfigDict.TryGetValue(id, out value);
}
```

**性能特点**：
- ? O(1) 的查询复杂度
- ? 支持三种 ID 类型：int、long、string
- ?? 假设 ID 唯一（无检查）

#### 3.2.2 异步加载机制
```csharp
public async Task LoadAsync()
{
    // 1. 加载资源（JSON 或 Binary）
    var json = await LoadJsonAssetAsync("Config/achievement.json");
    
    // 2. 反序列化
    var jsonNode = JSON.Parse(json);
    
    // 3. 逐条反序列化为对象
    foreach (var item in jsonNode.Children)
    {
        var config = new AchievementConfig(item);
        m_AchievementConfigDict[config.Id] = config;
    }
}
```

---

## 4. 配置加载流程

### 4.1 TablesComponent 加载器

`TablesComponent` 是自动生成的加载器，负责加载所有配置表。

```csharp
public partial class TablesComponent
{
    // 所有配置表的引用
    internal Tables.TbAchievementConfig TbAchievementConfig { get; set; }
    internal Tables.TbItemConfig TbItemConfig { get; set; }
    // ... 其他配置表
    
    private ConfigComponent m_ConfigComponent;
    
    public void Init(ConfigComponent configComponent)
    {
        m_ConfigComponent = configComponent;
        configComponent.RemoveAllConfigs();  // 清空旧数据
    }
    
    /// <summary>
    /// 异步加载所有配置表
    /// </summary>
    public async Task LoadAllTablesAsync()
    {
        // 1. 创建配置表实例
        TbAchievementConfig = new Tables.TbAchievementConfig();
        TbItemConfig = new Tables.TbItemConfig();
        
        // 2. 并发加载（Task.WhenAll）
        await Task.WhenAll(
            TbAchievementConfig.LoadAsync(),
            TbItemConfig.LoadAsync(),
            // ... 其他配置表
        );
        
        // 3. 注册到 ConfigManager
        m_ConfigComponent.Add("TbAchievementConfig", TbAchievementConfig);
        m_ConfigComponent.Add("TbItemConfig", TbItemConfig);
        
        IsLoaded = true;
    }
}
```

### 4.2 加载完整流程图

```
应用启动
   │
   ▼
ProcedureComponent 初始化
   │
   ├─ ConfigComponent.Awake()
   │  └─ m_ConfigManager = GameFrameworkEntry.GetModule<IConfigManager>()
   │
   ▼
HotfixLauncher 或业务 Launcher
   │
   ├─ TablesComponent.Init(configComponent)
   │  └─ configComponent.RemoveAllConfigs()
   │
   ▼
TablesComponent.LoadAllTablesAsync()
   │
   ├─ 创建所有 TbXxxConfig 实例
   │
   ├─ 并发执行 LoadAsync()
   │  ├─ TbAchievementConfig.LoadAsync()
   │  ├─ TbItemConfig.LoadAsync()
   │  └─ ... 其他
   │
   ▼
ConfigComponent.Add() 逐个注册
   │
   ▼
IsLoaded = true
   │
   ▼
业务代码开始使用配置
```

---

## 5. 配置访问方式

### 5.1 推荐的访问方式（类型安全）

```csharp
public class AchievementSystem
{
    private ConfigComponent m_ConfigComponent;
    
    public AchievementSystem(ConfigComponent configComponent)
    {
        m_ConfigComponent = configComponent;
    }
    
    public void Initialize()
    {
        // 方式 1：使用泛型 API（推荐）
        var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        // 方式 2：检查配置是否存在
        if (m_ConfigComponent.HasConfig<TbAchievementConfig>())
        {
            var config = m_ConfigComponent.GetConfig<TbAchievementConfig>();
            
            // 方式 3：使用 TryGet 查询具体数据
            if (config.TryGet(achievementId, out var achievement))
            {
                // 使用配置数据
                Debug.Log($"Achievement: {achievement.Name}");
            }
        }
    }
}
```

### 5.2 多语言集成

配置表支持多语言，通过存储多语言 Key：

```csharp
public class AchievementUIPresenter
{
    private ConfigComponent m_ConfigComponent;
    private LocalizationComponent m_LocalizationComponent;
    
    public void ShowAchievementName(int achievementId)
    {
        var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        if (achievementConfig.TryGet(achievementId, out var achievement))
        {
            // achievement.Name 存的是多语言 Key
            string displayName = m_LocalizationComponent.GetText(achievement.Name);
            Debug.Log(displayName);
        }
    }
}
```

---

## 6. 配置热更新流程

### 6.1 热更新场景

在 Hot Fix（热修复）或资源更新中，可能需要重新加载配置：

```csharp
public class ConfigUpdateManager
{
    private ConfigComponent m_ConfigComponent;
    private TablesComponent m_TablesComponent;
    
    /// <summary>
    /// 热更新配置（全量重载）
    /// </summary>
    public async Task HotUpdateConfigAsync()
    {
        // 1. 清空旧配置
        m_ConfigComponent.RemoveAllConfigs();
        
        // 2. 重新创建和加载所有配置表
        await m_TablesComponent.LoadAllTablesAsync();
        
        // 3. 通知依赖系统
        OnConfigReloaded?.Invoke();
    }
    
    public event Action OnConfigReloaded;
}
```

### 6.2 热更新注意事项

?? **问题**：配置重新加载后，已有的旧对象引用会过时

```csharp
// 不正确的做法：保存配置引用
var oldConfig = m_ConfigComponent.GetConfig<TbItemConfig>();

await configUpdateManager.HotUpdateConfigAsync();

// 现在 oldConfig 指向旧数据，oldConfig.TryGet() 可能返回过期数据
oldConfig.TryGet(itemId, out var item);  // ? 使用过期数据
```

? **正确做法**：每次都重新获取配置

```csharp
public void GetItemAsync(int itemId)
{
    // 每次使用前重新获取，确保使用最新配置
    var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
    if (itemConfig?.TryGet(itemId, out var item) ?? false)
    {
        // 使用配置
    }
}
```

---

## 7. 配置卸载流程

### 7.1 完全卸载

```csharp
public void Cleanup()
{
    // 清空所有配置（通常在场景切换或游戏退出时调用）
    m_ConfigComponent.RemoveAllConfigs();
}
```

### 7.2 单个配置卸载

```csharp
public void UnloadAchievementConfig()
{
    // 移除特定配置表
    if (m_ConfigComponent.HasConfig<TbAchievementConfig>())
    {
        m_ConfigComponent.RemoveConfig<TbAchievementConfig>();
    }
}
```

---

## 8. 配置文件组织

### 8.1 文件结构

```
Assets/Hotfix/Config/
├── Generate/
│   ├── Tables/
│   │   ├── AchievementConfig.cs       # 自动生成的数据类
│   │   ├── TbAchievementConfig.cs     # 自动生成的表类
│   │   ├── ItemConfig.cs
│   │   ├── TbItemConfig.cs
│   │   └── ...
│   ├── Local/
│   │   ├── Localization.cs            # 多语言配置
│   │   └── TbLocalization.cs
│   ├── TablesComponent.cs             # 自动生成的加载器
│   └── Core/
│       └── TbXxxTable.cs              # 核心配置表
└── Resources/
    ├── achievement.json               # 配置数据文件
    ├── item.json
    └── ...
```

### 8.2 资源加载路径

```csharp
// Luban 生成的加载代码会自动处理路径
// 配置文件应放在 Resources/Config/ 或通过 Addressables 加载
// 默认路径：Assets/Hotfix/Config/Resources/

var json = await Resources.LoadAsync<TextAsset>("Config/achievement.json");
```

---

## 9. 常见操作示例

### 9.1 遍历配置表

```csharp
public void IterateAllAchievements()
{
    var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
    
    // 方式 1：使用 GetAll() 获取完整列表
    var allAchievements = achievementConfig.GetAll();
    foreach (var achievement in allAchievements)
    {
        Debug.Log($"Achievement {achievement.Id}: {achievement.Name}");
    }
    
    // 方式 2：按 ID 遍历（需要知道所有 ID）
    for (int id = 1001; id <= 1010; id++)
    {
        if (achievementConfig.TryGet(id, out var achievement))
        {
            Debug.Log($"Achievement {id}: {achievement.Name}");
        }
    }
}
```

### 9.2 条件查询

```csharp
public List<AchievementConfig> FindAchievementsByCondition(int conditionId)
{
    var result = new List<AchievementConfig>();
    var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
    
    var allAchievements = achievementConfig.GetAll();
    foreach (var achievement in allAchievements)
    {
        if (achievement.AchievementUnlockCondition.Contains(conditionId))
        {
            result.Add(achievement);
        }
    }
    
    return result;
}
```

### 9.3 配置验证

```csharp
public bool ValidateConfigs()
{
    // 检查必需配置是否存在
    if (!m_ConfigComponent.HasConfig<TbAchievementConfig>())
    {
        Debug.LogError("Missing TbAchievementConfig");
        return false;
    }
    
    if (!m_ConfigComponent.HasConfig<TbItemConfig>())
    {
        Debug.LogError("Missing TbItemConfig");
        return false;
    }
    
    // 检查配置数据完整性
    var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
    if (achievementConfig.Count == 0)
    {
        Debug.LogError("AchievementConfig is empty");
        return false;
    }
    
    return true;
}
```

---

## 10. 工作流检查清单

### ?? 定义配置阶段
- [ ] 在 Excel/JSON 中定义配置数据
- [ ] 明确所有字段的类型和含义
- [ ] 使用 ID 作为主键
- [ ] 文本字段存储多语言 Key

### ?? 生成代码阶段
- [ ] 运行 Luban 代码生成工具
- [ ] 生成的类已存放在 Assets/Hotfix/Config/Generate/
- [ ] 配置数据文件已放在 Assets/Hotfix/Config/Resources/

### ?? 加载配置阶段
- [ ] ConfigComponent 已初始化
- [ ] TablesComponent.Init() 已调用
- [ ] TablesComponent.LoadAllTablesAsync() 已执行
- [ ] IsLoaded 标志已置为 true

### ?? 使用配置阶段
- [ ] 使用 ConfigComponent.GetConfig<T>() 获取配置
- [ ] 使用 TryGet() 查询具体数据
- [ ] 处理配置不存在的情况
- [ ] 在需要时重新获取配置（支持热更新）

### ?? 维护配置阶段
- [ ] 配置变更已提交到版本控制系统
- [ ] 新增配置已在 TablesComponent 注册
- [ ] 配置数据已通过验证
- [ ] 文档已更新
