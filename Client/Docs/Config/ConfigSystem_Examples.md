# 配置表系统使用示例

## 1. 基础使用示例

### 1.1 简单的配置查询

```csharp
using GameFrameX.Config.Runtime;
using UnityEngine;

public class SimpleConfigDemo : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    private void Start()
    {
        // 获取配置组件
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
    }
    
    public void GetItemInfo(int itemId)
    {
        // 获取配置表
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (itemConfig == null)
        {
            Debug.LogWarning("ItemConfig not loaded");
            return;
        }
        
        // 查询具体数据
        if (itemConfig.TryGet(itemId, out var item))
        {
            Debug.Log($"Item ID: {item.Id}");
            Debug.Log($"Item Name: {item.Name}");
            Debug.Log($"Item Price: {item.Price}");
        }
        else
        {
            Debug.LogWarning($"Item {itemId} not found");
        }
    }
}
```

### 1.2 获取所有配置数据

```csharp
public class ListAllConfigsDemo : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    public void ShowAllAchievements()
    {
        var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        if (achievementConfig == null)
        {
            Debug.LogWarning("AchievementConfig not loaded");
            return;
        }
        
        // 获取所有成就配置
        var allAchievements = achievementConfig.GetAll();
        
        foreach (var achievement in allAchievements)
        {
            Debug.Log($"[{achievement.Id}] {achievement.Name}");
        }
        
        Debug.Log($"Total achievements: {achievementConfig.Count}");
    }
}
```

### 1.3 检查配置存在性

```csharp
public class ConfigExistenceCheckDemo : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    public void CheckAllConfigs()
    {
        // 方式 1：检查泛型配置
        if (m_ConfigComponent.HasConfig<TbItemConfig>())
        {
            Debug.Log("ItemConfig exists");
        }
        
        // 方式 2：获取配置后检查
        var skillConfig = m_ConfigComponent.GetConfig<TbSkillConfig>();
        if (skillConfig != null)
        {
            Debug.Log("SkillConfig loaded");
        }
        
        // 方式 3：检查配置数据是否为空
        if (skillConfig?.Count > 0)
        {
            Debug.Log($"SkillConfig has {skillConfig.Count} items");
        }
    }
}
```

---

## 2. 业务系统集成示例

### 2.1 成就系统 (Achievement System)

```csharp
using GameFrameX.Config.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class AchievementSystem : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    private void Start()
    {
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
    }
    
    /// <summary>
    /// 获取指定成就信息
    /// </summary>
    public AchievementConfig GetAchievementInfo(int achievementId)
    {
        var config = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        if (config != null && config.TryGet(achievementId, out var achievement))
        {
            return achievement;
        }
        
        Debug.LogWarning($"Achievement {achievementId} not found");
        return null;
    }
    
    /// <summary>
    /// 根据解锁条件查询成就
    /// </summary>
    public List<AchievementConfig> GetAchievementsByCondition(int conditionId)
    {
        var result = new List<AchievementConfig>();
        var config = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        if (config == null)
        {
            return result;
        }
        
        var allAchievements = config.GetAll();
        foreach (var achievement in allAchievements)
        {
            if (achievement.AchievementUnlockCondition.Contains(conditionId))
            {
                result.Add(achievement);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取成就的多语言显示名称
    /// </summary>
    public string GetAchievementDisplayName(int achievementId)
    {
        var achievement = GetAchievementInfo(achievementId);
        if (achievement == null)
        {
            return "Unknown";
        }
        
        // 假设有多语言系统，根据 Name Key 获取实际文本
        var localization = GameFrameworkEntry.GetComponent<LocalizationComponent>();
        return localization?.GetText(achievement.Name) ?? achievement.Name;
    }
}
```

### 2.2 道具系统 (Item System)

```csharp
using GameFrameX.Config.Runtime;
using UnityEngine;

public class ItemSystem : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    private void Start()
    {
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
    }
    
    /// <summary>
    /// 获取道具价格
    /// </summary>
    public int GetItemPrice(int itemId)
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (config != null && config.TryGet(itemId, out var item))
        {
            return item.Price;
        }
        
        Debug.LogWarning($"Item {itemId} not found");
        return 0;
    }
    
    /// <summary>
    /// 批量获取道具信息
    /// </summary>
    public ItemConfig[] GetItems(int[] itemIds)
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        var result = new ItemConfig[itemIds.Length];
        
        if (config == null)
        {
            return result;
        }
        
        for (int i = 0; i < itemIds.Length; i++)
        {
            config.TryGet(itemIds[i], out result[i]);
        }
        
        return result;
    }
    
    /// <summary>
    /// 验证道具是否存在
    /// </summary>
    public bool ItemExists(int itemId)
    {
        var config = m_ConfigComponent.GetConfig<TbItemConfig>();
        return config != null && config.TryGet(itemId, out _);
    }
}
```

### 2.3 装备系统 (Equipment System)

```csharp
using GameFrameX.Config.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    private ItemSystem m_ItemSystem;
    
    private void Start()
    {
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        m_ItemSystem = GetComponent<ItemSystem>();
    }
    
    /// <summary>
    /// 获取装备的合成材料
    /// </summary>
    public List<ItemConfig> GetEquipmentMaterials(int equipmentId)
    {
        var result = new List<ItemConfig>();
        var equipConfig = m_ConfigComponent.GetConfig<TbEquipmentConfig>();
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (equipConfig == null || itemConfig == null)
        {
            return result;
        }
        
        if (!equipConfig.TryGet(equipmentId, out var equipment))
        {
            return result;
        }
        
        // 通过 ItemIds 获取实际道具配置
        foreach (var itemId in equipment.MaterialItemIds)
        {
            if (itemConfig.TryGet(itemId, out var item))
            {
                result.Add(item);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 计算装备总成本
    /// </summary>
    public int CalculateEquipmentCost(int equipmentId)
    {
        int totalCost = 0;
        var materials = GetEquipmentMaterials(equipmentId);
        
        foreach (var material in materials)
        {
            totalCost += material.Price;
        }
        
        return totalCost;
    }
    
    /// <summary>
    /// 获取装备属性加成
    /// </summary>
    public EquipmentConfig GetEquipmentStats(int equipmentId)
    {
        var config = m_ConfigComponent.GetConfig<TbEquipmentConfig>();
        
        if (config != null && config.TryGet(equipmentId, out var equipment))
        {
            return equipment;
        }
        
        return null;
    }
}
```

---

## 3. UI 系统集成示例

### 3.1 道具 UI 显示

```csharp
using GameFrameX.Config.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class ItemUIPanel : UIComponent
{
    [SerializeField] private Text m_ItemNameText;
    [SerializeField] private Text m_ItemPriceText;
    [SerializeField] private Image m_ItemIconImage;
    [SerializeField] private Text m_ItemDescriptionText;
    
    private ConfigComponent m_ConfigComponent;
    private LocalizationComponent m_LocalizationComponent;
    
    private void Start()
    {
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        m_LocalizationComponent = GameFrameworkEntry.GetComponent<LocalizationComponent>();
    }
    
    /// <summary>
    /// 显示道具信息
    /// </summary>
    public void ShowItem(int itemId)
    {
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (itemConfig == null || !itemConfig.TryGet(itemId, out var item))
        {
            Debug.LogWarning($"Cannot find item {itemId}");
            return;
        }
        
        // 显示道具名称（多语言）
        string displayName = m_LocalizationComponent.GetText(item.NameKey);
        m_ItemNameText.text = displayName;
        
        // 显示价格
        m_ItemPriceText.text = item.Price.ToString();
        
        // 显示图标（假设有图片管理系统）
        m_ItemIconImage.sprite = LoadItemIcon(item.IconId);
        
        // 显示描述（多语言）
        string description = m_LocalizationComponent.GetText(item.DescriptionKey);
        m_ItemDescriptionText.text = description;
    }
    
    private Sprite LoadItemIcon(int iconId)
    {
        // 实现图标加载逻辑
        return Resources.Load<Sprite>($"Icons/item_{iconId}");
    }
}
```

### 3.2 成就列表 UI

```csharp
using GameFrameX.Config.Runtime;
using GameFrameX.UI.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class AchievementListPanel : UIComponent
{
    [SerializeField] private Transform m_AchievementListContainer;
    [SerializeField] private GameObject m_AchievementItemPrefab;
    
    private ConfigComponent m_ConfigComponent;
    
    private void Start()
    {
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
    }
    
    /// <summary>
    /// 刷新成就列表
    /// </summary>
    public void RefreshAchievementList()
    {
        // 清空旧列表
        foreach (Transform child in m_AchievementListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 获取所有成就配置
        var achievementConfig = m_ConfigComponent.GetConfig<TbAchievementConfig>();
        
        if (achievementConfig == null)
        {
            Debug.LogWarning("AchievementConfig not loaded");
            return;
        }
        
        var allAchievements = achievementConfig.GetAll();
        
        // 创建成就列表项
        foreach (var achievement in allAchievements)
        {
            var itemGO = Instantiate(m_AchievementItemPrefab, m_AchievementListContainer);
            var itemUI = itemGO.GetComponent<AchievementItemUI>();
            itemUI.SetData(achievement);
        }
    }
}

public class AchievementItemUI : MonoBehaviour
{
    [SerializeField] private Text m_NameText;
    [SerializeField] private Text m_DescriptionText;
    [SerializeField] private Image m_IconImage;
    
    private LocalizationComponent m_LocalizationComponent;
    
    public void SetData(AchievementConfig achievement)
    {
        m_LocalizationComponent = GameFrameworkEntry.GetComponent<LocalizationComponent>();
        
        // 设置成就名称
        string displayName = m_LocalizationComponent.GetText(achievement.Name);
        m_NameText.text = displayName;
        
        // 设置成就描述
        string description = m_LocalizationComponent.GetText(achievement.AchievementContent);
        m_DescriptionText.text = description;
        
        // 设置成就图标
        m_IconImage.sprite = LoadAchievementIcon(achievement.Image);
    }
    
    private Sprite LoadAchievementIcon(int iconId)
    {
        return Resources.Load<Sprite>($"Icons/achievement_{iconId}");
    }
}
```

---

## 4. 异步加载示例

### 4.1 游戏初始化时加载配置

```csharp
using GameFrameX.Config.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    private TablesComponent m_TablesComponent;
    
    public async void InitializeGame()
    {
        Debug.Log("Starting game initialization...");
        
        // 步骤 1：获取组件
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        m_TablesComponent = new TablesComponent();
        m_TablesComponent.Init(m_ConfigComponent);
        
        // 步骤 2：异步加载所有配置表
        Debug.Log("Loading configuration tables...");
        await m_TablesComponent.LoadAllTablesAsync();
        
        // 步骤 3：验证配置
        if (!ValidateConfigs())
        {
            Debug.LogError("Configuration validation failed!");
            return;
        }
        
        Debug.Log("Game initialized successfully!");
        
        // 步骤 4：启动游戏主逻辑
        OnGameInitializationComplete();
    }
    
    private bool ValidateConfigs()
    {
        // 检查关键配置是否存在
        if (!m_ConfigComponent.HasConfig<TbItemConfig>())
        {
            Debug.LogError("Missing TbItemConfig");
            return false;
        }
        
        if (!m_ConfigComponent.HasConfig<TbAchievementConfig>())
        {
            Debug.LogError("Missing TbAchievementConfig");
            return false;
        }
        
        // 检查配置数据完整性
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        if (itemConfig?.Count == 0)
        {
            Debug.LogError("ItemConfig is empty");
            return false;
        }
        
        return true;
    }
    
    private void OnGameInitializationComplete()
    {
        // 启动场景或进入主菜单
        Debug.Log("All systems initialized. Starting game...");
    }
}
```

### 4.2 配置热更新

```csharp
using GameFrameX.Config.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ConfigUpdateManager : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    private TablesComponent m_TablesComponent;
    
    // 配置重载事件
    public event Action OnConfigReloaded;
    
    public ConfigUpdateManager(ConfigComponent configComponent)
    {
        m_ConfigComponent = configComponent;
        m_TablesComponent = new TablesComponent();
        m_TablesComponent.Init(m_ConfigComponent);
    }
    
    /// <summary>
    /// 热更新所有配置
    /// </summary>
    public async void HotUpdateAllConfigs()
    {
        Debug.Log("Starting configuration hot update...");
        
        // 步骤 1：清空旧配置
        m_ConfigComponent.RemoveAllConfigs();
        
        // 步骤 2：重新加载所有配置
        try
        {
            await m_TablesComponent.LoadAllTablesAsync();
            Debug.Log("Configuration updated successfully!");
            
            // 步骤 3：通知所有监听者
            OnConfigReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update configuration: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 热更新特定配置表
    /// </summary>
    public async void HotUpdateSpecificConfig<T>() where T : class, IDataTable, new()
    {
        Debug.Log($"Updating {typeof(T).Name}...");
        
        try
        {
            // 清空旧配置
            m_ConfigComponent.RemoveConfig<T>();
            
            // 创建新配置实例
            var config = new T();
            await config.LoadAsync();
            
            // 注册到配置管理器
            m_ConfigComponent.Add(typeof(T).Name, config);
            
            Debug.Log($"{typeof(T).Name} updated successfully!");
            OnConfigReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update {typeof(T).Name}: {ex.Message}");
        }
    }
}
```

---

## 5. 配置验证示例

### 5.1 初始化时验证

```csharp
using GameFrameX.Config.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class ConfigValidator : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    public bool ValidateAllConfigs(out List<string> errors)
    {
        errors = new List<string>();
        m_ConfigComponent = GameFrameworkEntry.GetComponent<ConfigComponent>();
        
        // 检查配置是否存在
        if (!CheckConfigsExist(errors))
        {
            return false;
        }
        
        // 检查配置数据完整性
        if (!CheckConfigsIntegrity(errors))
        {
            return false;
        }
        
        // 检查配置之间的引用关系
        if (!CheckConfigReferences(errors))
        {
            return false;
        }
        
        return errors.Count == 0;
    }
    
    private bool CheckConfigsExist(List<string> errors)
    {
        string[] requiredConfigs = { "TbItemConfig", "TbAchievementConfig", "TbEquipmentConfig" };
        
        foreach (var configName in requiredConfigs)
        {
            if (!m_ConfigComponent.HasConfig<TbItemConfig>())
            {
                errors.Add($"Missing required config: {configName}");
            }
        }
        
        return errors.Count == 0;
    }
    
    private bool CheckConfigsIntegrity(List<string> errors)
    {
        // 检查 ItemConfig
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        if (itemConfig?.Count == 0)
        {
            errors.Add("ItemConfig is empty");
            return false;
        }
        
        var allItems = itemConfig.GetAll();
        foreach (var item in allItems)
        {
            if (item.Id <= 0)
            {
                errors.Add($"Invalid item id: {item.Id}");
            }
            
            if (string.IsNullOrEmpty(item.NameKey))
            {
                errors.Add($"Item {item.Id} has no name key");
            }
        }
        
        return errors.Count == 0;
    }
    
    private bool CheckConfigReferences(List<string> errors)
    {
        // 检查装备配置中的物品引用是否有效
        var equipConfig = m_ConfigComponent.GetConfig<TbEquipmentConfig>();
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (equipConfig == null || itemConfig == null)
        {
            return true;
        }
        
        var allEquipments = equipConfig.GetAll();
        foreach (var equipment in allEquipments)
        {
            foreach (var itemId in equipment.MaterialItemIds)
            {
                if (!itemConfig.TryGet(itemId, out _))
                {
                    errors.Add($"Equipment {equipment.Id} references non-existent item {itemId}");
                }
            }
        }
        
        return errors.Count == 0;
    }
}
```

---

## 6. 错误处理示例

### 6.1 健壮的配置访问

```csharp
using GameFrameX.Config.Runtime;
using UnityEngine;

public class RobustConfigAccess : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    /// <summary>
    /// 安全获取配置，带有多层防护
    /// </summary>
    public T GetConfigSafely<T>() where T : class, IDataTable
    {
        // 层 1：检查 ConfigComponent 是否存在
        if (m_ConfigComponent == null)
        {
            Debug.LogError("ConfigComponent not found");
            return null;
        }
        
        // 层 2：检查配置是否存在
        if (!m_ConfigComponent.HasConfig<T>())
        {
            Debug.LogWarning($"Config {typeof(T).Name} not loaded yet");
            return null;
        }
        
        // 层 3：获取配置
        var config = m_ConfigComponent.GetConfig<T>();
        if (config == null)
        {
            Debug.LogError($"Failed to get config {typeof(T).Name}");
            return null;
        }
        
        return config;
    }
    
    /// <summary>
    /// 安全查询单条数据
    /// </summary>
    public TData GetDataItemSafely<TTable, TData>(int id, out TData result)
        where TTable : class, IDataTable
        where TData : class
    {
        result = null;
        
        var config = GetConfigSafely<TTable>();
        if (config == null)
        {
            return null;
        }
        
        // 假设 TTable 支持 TryGet(int, out TData)
        // 这里需要使用反射或其他方式调用
        
        return result;
    }
    
    /// <summary>
    /// 带重试的配置加载
    /// </summary>
    public async void LoadConfigWithRetry<T>(int maxRetries = 3)
        where T : class, IDataTable, new()
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var config = new T();
                await config.LoadAsync();
                m_ConfigComponent.Add(typeof(T).Name, config);
                Debug.Log($"Successfully loaded {typeof(T).Name}");
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load {typeof(T).Name} (attempt {i + 1}/{maxRetries}): {ex.Message}");
                
                if (i < maxRetries - 1)
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                }
            }
        }
        
        Debug.LogError($"Failed to load {typeof(T).Name} after {maxRetries} attempts");
    }
}
```

### 6.2 配置访问的 Try-Catch 模式

```csharp
public void SafelyAccessConfig()
{
    try
    {
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        if (itemConfig == null)
        {
            Debug.LogWarning("ItemConfig not available");
            return;
        }
        
        if (itemConfig.TryGet(1001, out var item))
        {
            Debug.Log($"Found item: {item.NameKey}");
        }
        else
        {
            Debug.LogWarning("Item 1001 not found");
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"Unexpected error accessing config: {ex}");
    }
}
```

---

## 7. 性能测试示例

```csharp
using GameFrameX.Config.Runtime;
using System.Diagnostics;
using UnityEngine;

public class ConfigPerformanceTest : MonoBehaviour
{
    private ConfigComponent m_ConfigComponent;
    
    public void TestLoadingPerformance()
    {
        var sw = Stopwatch.StartNew();
        
        var tablesComponent = new TablesComponent();
        tablesComponent.Init(m_ConfigComponent);
        
        // 测试并发加载性能
        // await tablesComponent.LoadAllTablesAsync();
        
        sw.Stop();
        Debug.Log($"Config loading time: {sw.ElapsedMilliseconds}ms");
    }
    
    public void TestQueryPerformance()
    {
        var sw = Stopwatch.StartNew();
        
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        
        // 测试 1000 次查询
        for (int i = 0; i < 1000; i++)
        {
            itemConfig.TryGet(i, out _);
        }
        
        sw.Stop();
        Debug.Log($"1000 queries time: {sw.ElapsedMilliseconds}ms");
        Debug.Log($"Average query time: {sw.ElapsedMilliseconds / 1000.0f}ms");
    }
    
    public void TestMemoryUsage()
    {
        long beforeMemory = System.GC.GetTotalMemory(true);
        
        var itemConfig = m_ConfigComponent.GetConfig<TbItemConfig>();
        var allItems = itemConfig.GetAll();
        
        long afterMemory = System.GC.GetTotalMemory(true);
        long memoryUsed = (afterMemory - beforeMemory) / 1024 / 1024;  // 转换为 MB
        
        Debug.Log($"ItemConfig memory usage: {memoryUsed}MB");
    }
}
```

