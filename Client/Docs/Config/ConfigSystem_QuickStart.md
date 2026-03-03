# 配置表系统新员工快速入门指南

> 本文档为新员工快速上手配置表系统而编写，通过 5 分钟了解基本概念，15 分钟完成第一个配置表，30 分钟掌握常见操作。

---

## 快速导航

- **5 分钟了解概念** → 跳转 [1. 5分钟速成](#1-5分钟速成)
- **15 分钟上手操作** → 跳转 [2. 15分钟快速开始](#2-15分钟快速开始)
- **30 分钟掌握技能** → 跳转 [3. 常见操作指南](#3-常见操作指南)
- **遇到问题** → 查看 [4. 常见问题 Q&A](#4-常见问题qa)

---

## 1. 5分钟速成

### 1.1 什么是配置表系统？

配置表是存储游戏静态数据的地方。比如：
- ?? 道具属性（伤害、价格等）
- ?? 成就配置（解锁条件、奖励等）
- ?? 装备信息（防御、耐久等）

在游戏启动时一次性加载，运行期直接查询使用。

### 1.2 三个关键角色

```
┌──────────────────────────────────────────────────────┐
│                    配置表系统三层                       │
├──────────────────────────────────────────────────────┤
│                                                        │
│  Layer 1: ConfigComponent (组件)                      │
│  └─ 你的业务代码直接使用这个                           │
│     GetConfig<TbItemConfig>() 获取配置                │
│                                                        │
│  Layer 2: ConfigManager (管理器)                      │
│  └─ 后台管理所有配置的存储和加载                       │
│     不需要直接使用                                    │
│                                                        │
│  Layer 3: IDataTable (数据表)                         │
│  └─ 具体的配置表实现（TbItemConfig、TbAchievementConfig）
│     自动生成，不需要手写                               │
│                                                        │
└──────────────────────────────────────────────────────┘
```

### 1.3 一句话工作流

```
定义数据 → 生成代码 → 加载配置 → 查询使用 → 完成！
```

---

## 2. 15分钟快速开始

### 2.1 第一步：了解已有配置表

现有项目已有这些配置表：

```
? TbItemConfig         - 道具配置
? TbAchievementConfig  - 成就配置  
? TbEquipmentConfig    - 装备配置
? TbSkillConfig        - 技能配置
? ... 还有很多
```

你可以**直接使用**，无需自己创建。

### 2.2 第二步：在代码中使用配置

```csharp
using GameFrameX.Config.Runtime;

public class MyFirstConfigTest : MonoBehaviour
{
    public void GetItemInfo()
    {
        // 第一步：获取 ConfigComponent
        var configComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        
        // 第二步：获取具体的配置表
        var itemConfig = configComponent.GetConfig<TbItemConfig>();
        
        // 第三步：查询数据
        if (itemConfig != null && itemConfig.TryGet(1001, out var item))
        {
            Debug.Log($"道具名: {item.NameKey}");
            Debug.Log($"价格: {item.Price}");
        }
    }
}
```

**就这么简单！** 三步走完。

### 2.3 第三步：理解基本概念

#### ?? ID 是什么？
- 每条配置数据的唯一标识（数字）
- 例如：道具 ID = 1001，成就 ID = 5001
- 用来快速查询具体数据

#### ?? TryGet 是什么？
```csharp
// 安全的查询方法，不会抛异常
if (itemConfig.TryGet(1001, out var item))
{
    // 找到了，使用 item
}
else
{
    // 没找到，处理错误
}
```

#### ?? Name / NameKey 的区别？
```csharp
// ? Name 和 NameKey 是两个不同的东西！
item.Name      // "item_name_001" <- 多语言 Key
item.Price     // 100              <- 实际价格

// 要显示给玩家，需要转换：
string displayName = localization.GetText(item.NameKey);  // "长剑"
```

---

## 3. 常见操作指南

### 3.1 操作 1??：查询单个道具

```csharp
public void GetSingleItem()
{
    var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
    
    // ? 正确做法
    if (itemConfig != null && itemConfig.TryGet(1001, out var item))
    {
        Debug.Log($"Item: {item.NameKey}, Price: {item.Price}");
    }
}
```

### 3.2 操作 2??：查询所有数据

```csharp
public void GetAllItems()
{
    var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
    
    if (itemConfig != null)
    {
        // 获取全部数据
        var allItems = itemConfig.GetAll();
        
        // 遍历
        foreach (var item in allItems)
        {
            Debug.Log($"ID: {item.Id}, Name: {item.NameKey}");
        }
    }
}
```

### 3.3 操作 3??：条件查询

```csharp
public void FindExpensiveItems()
{
    var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
    
    if (itemConfig != null)
    {
        var allItems = itemConfig.GetAll();
        
        // 筛选价格 > 100 的道具
        var expensiveItems = allItems.Where(item => item.Price > 100).ToList();
        
        foreach (var item in expensiveItems)
        {
            Debug.Log($"{item.NameKey}: {item.Price}");
        }
    }
}
```

### 3.4 操作 4??：处理多语言

```csharp
public void DisplayItemWithLanguage()
{
    var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
    var localization = GameFrameworkEntry.GetComponent<LocalizationComponent>();
    
    if (itemConfig != null && itemConfig.TryGet(1001, out var item))
    {
        // ? 获取多语言文本
        string displayName = localization.GetText(item.NameKey);
        
        // 显示给玩家
        uiText.text = displayName;  // "长剑" or "Iron Sword"
    }
}
```

---

## 4. 常见问题 Q&A

### Q1: 为什么配置为 null？

**A:** 通常有三个原因：

```csharp
// ? 原因 1：还没加载完
// 解决：等待 TablesComponent.LoadAllTablesAsync() 完成

// ? 原因 2：获取类型错了
var config1 = m_ConfigComponent.GetConfig<TbItemConfig>();      // ? 正确
var config2 = m_ConfigComponent.GetConfig<TbEquipmentConfig>();  // ? 类型不对

// ? 原因 3：忘记判空
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
// 直接使用，没有判空
config.TryGet(1001, out var item);  // ? 可能异常

// ? 正确做法
if (config != null && config.TryGet(1001, out var item))
{
    // 使用 item
}
```

### Q2: TryGet 找不到数据怎么办？

**A:** 这是正常的，ID 可能不存在。安全处理即可：

```csharp
if (config.TryGet(99999, out var item))
{
    // 存在，使用
}
else
{
    // 不存在，处理缺失情况
    Debug.LogWarning("Item 99999 not found");
}
```

### Q3: 配置热更新后旧数据怎么办？

**A:** **不要缓存配置对象**，每次使用前重新获取：

```csharp
// ? 错误：保存引用
private TbItemConfig m_ItemConfig;
public void Start()
{
    m_ItemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();  // 保存了
}

// 热更新后，m_ItemConfig 已过期 ?

// ? 正确：每次使用前获取
public void UseItem(int itemId)
{
    var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();  // 重新获取
    if (itemConfig != null && itemConfig.TryGet(itemId, out var item))
    {
        // 使用最新配置
    }
}
```

### Q4: 如何添加新配置表？

**A:** 三步走：

1. **定义配置数据**（Excel/JSON）
2. **运行 Luban 生成 C# 代码**
3. **在 TablesComponent 注册加载**

详细步骤见 [ConfigSystem_Workflow.md](ConfigSystem_Workflow.md)

### Q5: 性能会不会有问题？

**A:** 放心，配置查询非常快（通常 < 1ms）。

但注意避免这些坏习惯：

```csharp
// ? 坏：在循环中重复获取
for (int i = 0; i < 1000; i++)
{
    var config = m_ConfigComponent.GetConfig<TbItemConfig>();  // 重复 1000 次
    config.TryGet(i, out _);
}

// ? 好：获取一次，使用多次
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
for (int i = 0; i < 1000; i++)
{
    config.TryGet(i, out _);
}
```

---

## 5. 常见错误警告

### ?? 警告 1：配置空指针

```csharp
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
config.TryGet(1001, out var item);  // ?? 如果 config 为 null，会异常！

// ? 正确做法
if (config != null && config.TryGet(1001, out var item))
{
    // 安全
}
```

### ?? 警告 2：类型不匹配

```csharp
// ?? 错误的泛型参数
var config = m_ConfigComponent.GetConfig<TbEquipmentConfig>();  // 获取的是道具配置！

// 应该是
var config = m_ConfigComponent.GetConfig<TbItemConfig>();
```

### ?? 警告 3：多语言 Key 硬编码

```csharp
// ?? 错误：直接显示 Key
uiText.text = item.NameKey;  // 显示 "item_name_001"，用户看不懂！

// ? 正确：转换为实际文本
string displayName = localization.GetText(item.NameKey);
uiText.text = displayName;  // 显示 "长剑"
```

---

## 6. 实战小练习

### 练习 1：显示玩家背包

需求：显示玩家拥有的所有道具

```csharp
public class InventoryUI : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    private List<int> m_PlayerItemIds;  // [1001, 1002, 1001]
    
    public void ShowInventory()
    {
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        // TODO: 完成以下功能
        // 1. 遍历 m_PlayerItemIds
        // 2. 获取每个道具的配置数据
        // 3. 显示到 UI 上
    }
}
```

**提示**：用 TryGet 获取每个 ID 对应的配置数据

### 练习 2：计算装备升级成本

需求：计算升级装备需要的材料花费

```csharp
public class EquipmentUpgrade : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    public int CalculateUpgradeCost(int equipmentId)
    {
        // TODO: 完成以下功能
        // 1. 获取装备配置
        // 2. 获取装备需要的材料 ID 列表
        // 3. 获取每种材料的价格
        // 4. 累加总成本
        
        return 0;  // 返回总成本
    }
}
```

**提示**：需要同时查询两个配置表（装备表 + 道具表）

### 练习 3：检查成就解锁条件

需求：根据条件判断哪些成就可以解锁

```csharp
public class AchievementCheck : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    public List<int> GetUnlockableAchievements(int conditionId)
    {
        // TODO: 完成以下功能
        // 1. 获取所有成就配置
        // 2. 遍历每个成就
        // 3. 检查该成就的解锁条件中是否包含 conditionId
        // 4. 返回可解锁的成就 ID 列表
        
        return new List<int>();
    }
}
```

**提示**：GetAll() 获取全部成就，然后筛选

---

## 7. 完整代码示例

### 示例：完整的成就显示系统

```csharp
using GameFrameX.Config.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class AchievementDisplaySystem : MonoBehaviour
{
    [SerializeField] private Text m_AchievementNameText;
    [SerializeField] private Text m_AchievementDescText;
    [SerializeField] private Image m_AchievementIcon;
    
    private ConfigComponent m_ConfigComponent;
    private LocalizationComponent m_LocalizationComponent;
    
    private void Start()
    {
        // 获取组件
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        m_LocalizationComponent = GameFrameworkEntry.GetComponent<LocalizationComponent>();
    }
    
    /// <summary>
    /// 显示指定成就
    /// </summary>
    public void ShowAchievement(int achievementId)
    {
        // 第一步：获取配置
        var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        // 第二步：判空
        if (achievementConfig == null)
        {
            Debug.LogWarning("AchievementConfig not loaded");
            return;
        }
        
        // 第三步：查询数据
        if (!achievementConfig.TryGet(achievementId, out var achievement))
        {
            Debug.LogWarning($"Achievement {achievementId} not found");
            return;
        }
        
        // 第四步：转换多语言文本
        string displayName = m_LocalizationComponent.GetText(achievement.Name);
        string displayDesc = m_LocalizationComponent.GetText(achievement.AchievementContent);
        
        // 第五步：更新 UI
        m_AchievementNameText.text = displayName;
        m_AchievementDescText.text = displayDesc;
        m_AchievementIcon.sprite = LoadAchievementIcon(achievement.Image);
    }
    
    private Sprite LoadAchievementIcon(int iconId)
    {
        return Resources.Load<Sprite>($"Icons/achievement_{iconId}");
    }
}
```

---

## 8. 学习路径

### ?? 初级（今天）
- [x] 了解基本概念
- [x] 学会使用 GetConfig<T>()
- [x] 掌握 TryGet() 查询

### ?? 中级（本周）
- [ ] 理解配置表生命周期
- [ ] 完成 3 个实战练习
- [ ] 学会多语言集成

### ?? 高级（本月）
- [ ] 添加新的配置表
- [ ] 优化配置查询性能
- [ ] 处理配置热更新

---

## 9. 常用链接

| 文档 | 说明 |
|-----|------|
| [ConfigSystem_ArchitectureAnalysis.md](ConfigSystem_ArchitectureAnalysis.md) | 系统架构深入分析 |
| [ConfigSystem_Workflow.md](ConfigSystem_Workflow.md) | 详细工作流程 |
| [ConfigSystem_BestPractices.md](ConfigSystem_BestPractices.md) | 最佳实践规范 |
| [ConfigSystem_Examples.md](ConfigSystem_Examples.md) | 大量代码示例 |
| [ConfigSystem_TroubleShooting.md](ConfigSystem_TroubleShooting.md) | 故障排除指南 |

---

## 10. 寻求帮助

### 遇到问题，按顺序查看：

1. **快速查询** → 本文档的 Q&A 部分
2. **详细说明** → [ConfigSystem_TroubleShooting.md](ConfigSystem_TroubleShooting.md)
3. **代码示例** → [ConfigSystem_Examples.md](ConfigSystem_Examples.md)
4. **深入理解** → [ConfigSystem_ArchitectureAnalysis.md](ConfigSystem_ArchitectureAnalysis.md)

### 联系方式
- 提问讨论：项目 Wiki / 开发讨论区
- 问题报告：项目 Issue Tracker
- 代码审查：提交 PR 时可要求配置相关的 review

---

**祝你快速上手配置表系统！如有问题，欢迎提问！** ??

