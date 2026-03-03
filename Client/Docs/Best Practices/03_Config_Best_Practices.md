# 配置系统（Config）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [核心概念](#核心概念)
3. [配置文件格式](#配置文件格式)
4. [最佳实践](#最佳实践)
5. [代码示例](#代码示例)
6. [配置管理](#配置管理)
7. [常见问题](#常见问题)

## 系统概述

配置系统（Config System）是GameFrameX框架用于管理游戏配置数据的核心系统。它支持多种格式的配置文件（JSON、XML、二进制等），提供了类型安全的配置访问和动态热更新能力。

### 主要特点
- **多格式支持**: JSON、XML、二进制等多种格式
- **类型安全**: 泛型配置访问，编译时类型检查
- **热更新**: 支持配置热更新而无需重启
- **缓存机制**: 自动缓存加载过的配置，提高访问速度
- **验证机制**: 配置加载时自动验证有效性

## 核心概念

### 配置管理器接口
```csharp
public interface IConfigManager
{
    // 加载配置
    void LoadConfig<T>(string configPath) where T : class;
    
    // 获取配置
    T GetConfig<T>() where T : class;
    
    // 卸载配置
    void UnloadConfig<T>() where T : class;
    
    // 重新加载配置
    void ReloadConfig<T>() where T : class;
}
```

### 配置基类
游戏中的所有配置都应继承自配置基类。

```csharp
public interface IConfig
{
    // 验证配置有效性
    bool Validate();
    
    // 获取配置版本
    int GetVersion();
}
```

## 配置文件格式

### 1. JSON格式配置
```json
{
  "version": 1,
  "gameConfig": {
    "gameName": "My Game",
    "gameVersion": "1.0.0",
    "targetFrameRate": 60
  },
  "playerConfig": {
    "defaultPlayerName": "Player",
    "maxHealth": 100,
    "moveSpeed": 5.0
  },
  "levelConfig": {
    "levels": [
      {
        "id": 1,
        "name": "Level 1",
        "difficulty": 1,
        "maxEnemies": 10
      }
    ]
  }
}
```

### 2. XML格式配置
```xml
<?xml version="1.0" encoding="utf-8"?>
<Config version="1">
  <GameConfig>
    <GameName>My Game</GameName>
    <GameVersion>1.0.0</GameVersion>
    <TargetFrameRate>60</TargetFrameRate>
  </GameConfig>
  <PlayerConfig>
    <DefaultPlayerName>Player</DefaultPlayerName>
    <MaxHealth>100</MaxHealth>
    <MoveSpeed>5.0</MoveSpeed>
  </PlayerConfig>
</Config>
```

## 最佳实践

### 1. 配置结构设计

#### 1.1 分层配置设计
```csharp
// 推荐：按功能分层
[Serializable]
public class GameConfig
{
    public GameSettings gameSettings;
    public PlayerSettings playerSettings;
    public LevelSettings levelSettings;
    public UISettings uiSettings;
    public AudioSettings audioSettings;
}

[Serializable]
public class GameSettings
{
    public string gameName;
    public string gameVersion;
    public int targetFrameRate;
    public bool enableDebug;
}

[Serializable]
public class PlayerSettings
{
    public string defaultPlayerName;
    public int maxHealth;
    public float moveSpeed;
    public float jumpForce;
}

// 不推荐：将所有配置放在一个类中
[Serializable]
public class MegaConfig
{
    public string gameName;
    public int maxHealth;
    public float moveSpeed;
    // ... 几百个字段
}
```

#### 1.2 配置类与业务逻辑分离
```csharp
// 推荐：配置类只存储数据
[Serializable]
public class EnemyConfig
{
    public int id;
    public string name;
    public int health;
    public float moveSpeed;
    public int attackDamage;
}

// 业务逻辑在独立的类中
public class EnemyManager
{
    private EnemyConfig m_Config;
    
    public void Initialize(EnemyConfig config)
    {
        m_Config = config;
    }
    
    public void ApplyConfig()
    {
        // 使用配置
    }
}
```

### 2. 配置的加载和使用

#### 2.1 启动时加载所有必需配置
```csharp
public class ConfigInitProcedure : ProcedureBase
{
    private ConfigComponent m_ConfigComponent;
    private int m_LoadedConfigCount = 0;
    private int m_TotalConfigCount = 4;
    
    public override void OnEnter()
    {
        m_ConfigComponent = GameEntry.GetComponent<ConfigComponent>();
        
        // 加载所有必需的配置
        m_ConfigComponent.LoadConfigAsync<GameConfig>(
            "Assets/Configs/GameConfig.json",
            OnGameConfigLoaded
        );
        m_ConfigComponent.LoadConfigAsync<PlayerConfig>(
            "Assets/Configs/PlayerConfig.json",
            OnPlayerConfigLoaded
        );
        m_ConfigComponent.LoadConfigAsync<LevelConfig>(
            "Assets/Configs/LevelConfig.json",
            OnLevelConfigLoaded
        );
        m_ConfigComponent.LoadConfigAsync<AudioConfig>(
            "Assets/Configs/AudioConfig.json",
            OnAudioConfigLoaded
        );
    }
    
    private void OnGameConfigLoaded(bool success)
    {
        m_LoadedConfigCount++;
        if (m_LoadedConfigCount >= m_TotalConfigCount)
        {
            Log.Info("All configs loaded successfully");
            ChangeState<LoginProcedure>();
        }
    }
    
    private void OnPlayerConfigLoaded(bool success) => OnGameConfigLoaded(success);
    private void OnLevelConfigLoaded(bool success) => OnGameConfigLoaded(success);
    private void OnAudioConfigLoaded(bool success) => OnGameConfigLoaded(success);
}
```

#### 2.2 按需延迟加载
```csharp
public class GamePlayProcedure : ProcedureBase
{
    public override void OnEnter()
    {
        // 进入游戏时加载关卡特定配置
        var configComponent = GameEntry.GetComponent<ConfigComponent>();
        configComponent.LoadConfigAsync<LevelSpecificConfig>(
            $"Assets/Configs/Levels/Level{m_CurrentLevel}.json",
            OnLevelConfigLoaded
        );
    }
    
    private void OnLevelConfigLoaded(bool success)
    {
        if (success)
        {
            var config = GameEntry.GetComponent<ConfigComponent>()
                .GetConfig<LevelSpecificConfig>();
            ApplyLevelConfig(config);
        }
    }
}
```

### 3. 配置的版本管理

#### 3.1 配置版本控制
```csharp
[Serializable]
public class VersionedConfig
{
    public int version = 1;
    public string configData;
    
    public bool IsCompatible(int targetVersion)
    {
        return version <= targetVersion;
    }
}

public class ConfigLoader
{
    private const int CURRENT_CONFIG_VERSION = 2;
    
    public bool LoadConfig(string path)
    {
        var config = LoadJsonConfig<VersionedConfig>(path);
        
        if (!config.IsCompatible(CURRENT_CONFIG_VERSION))
        {
            Log.Error($"Config version {config.version} is not compatible");
            return false;
        }
        
        if (config.version < CURRENT_CONFIG_VERSION)
        {
            Log.Info("Migrating config from version {0} to {1}",
                config.version, CURRENT_CONFIG_VERSION);
            MigrateConfig(config);
        }
        
        return true;
    }
    
    private void MigrateConfig(VersionedConfig config)
    {
        // 版本迁移逻辑
    }
}
```

### 4. 配置验证和容错

#### 4.1 配置验证
```csharp
[Serializable]
public class PlayerConfig : IConfig
{
    public string playerName;
    public int maxHealth;
    public float moveSpeed;
    
    public bool Validate()
    {
        // 验证必需字段
        if (string.IsNullOrEmpty(playerName))
        {
            Log.Error("Player name is required");
            return false;
        }
        
        // 验证数值范围
        if (maxHealth <= 0 || maxHealth > 1000)
        {
            Log.Error("Max health must be between 1 and 1000");
            return false;
        }
        
        if (moveSpeed < 0 || moveSpeed > 50)
        {
            Log.Error("Move speed must be between 0 and 50");
            return false;
        }
        
        return true;
    }
    
    public int GetVersion() => 1;
}

public class ConfigManager
{
    public bool LoadConfig<T>(string path) where T : IConfig
    {
        var config = LoadFromFile<T>(path);
        
        if (config == null)
        {
            Log.Error($"Failed to load config from {path}");
            return false;
        }
        
        if (!config.Validate())
        {
            Log.Error($"Config validation failed for {typeof(T).Name}");
            return false;
        }
        
        CacheConfig(config);
        return true;
    }
}
```

#### 4.2 默认值提供
```csharp
public class ConfigProvider
{
    public static GameConfig GetDefaultGameConfig()
    {
        return new GameConfig
        {
            gameName = "Default Game",
            gameVersion = "1.0.0",
            targetFrameRate = 60
        };
    }
    
    public static GameConfig LoadConfigWithFallback(string path)
    {
        try
        {
            return LoadConfigFromFile(path);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load config: {ex.Message}. Using default config.");
            return GetDefaultGameConfig();
        }
    }
}
```

### 5. 配置的热更新

#### 5.1 配置热更新机制
```csharp
public class HotUpdateConfig
{
    private Dictionary<Type, object> m_ConfigCache = new Dictionary<Type, object>();
    private Dictionary<Type, Action> m_UpdateCallbacks = new Dictionary<Type, Action>();
    
    public void RegisterUpdateCallback<T>(Action callback)
    {
        m_UpdateCallbacks[typeof(T)] = callback;
    }
    
    public void ReloadConfig<T>(string path) where T : class, new()
    {
        var config = LoadFromFile<T>(path);
        if (config != null)
        {
            m_ConfigCache[typeof(T)] = config;
            
            // 触发更新回调
            if (m_UpdateCallbacks.TryGetValue(typeof(T), out var callback))
            {
                callback?.Invoke();
            }
            
            Log.Info($"Config {typeof(T).Name} reloaded successfully");
        }
    }
}

// 使用示例
public class GamePlayManager
{
    public void Initialize()
    {
        var configProvider = new HotUpdateConfig();
        configProvider.RegisterUpdateCallback<GameConfig>(OnGameConfigUpdated);
    }
    
    private void OnGameConfigUpdated()
    {
        Log.Info("Game config updated, applying new settings");
        // 应用新配置
    }
}
```

## 代码示例

### 示例1：完整的配置系统集成
```csharp
// 配置类定义
[Serializable]
public class GameplayConfig : IConfig
{
    public PlayerSettings player;
    public EnemySettings enemy;
    public LevelSettings level;
    
    [Serializable]
    public class PlayerSettings
    {
        public int maxHealth;
        public float moveSpeed;
        public float jumpForce;
    }
    
    [Serializable]
    public class EnemySettings
    {
        public int maxEnemies;
        public float spawnInterval;
        public float detectionRange;
    }
    
    [Serializable]
    public class LevelSettings
    {
        public int currentLevel;
        public float levelDuration;
    }
    
    public bool Validate()
    {
        return player.maxHealth > 0 &&
               player.moveSpeed > 0 &&
               enemy.maxEnemies > 0 &&
               level.currentLevel > 0;
    }
    
    public int GetVersion() => 1;
}

// 配置使用
public class GameplayManager : MonoBehaviour
{
    private GameplayConfig m_Config;
    
    private void Start()
    {
        var configComponent = GameEntry.GetComponent<ConfigComponent>();
        m_Config = configComponent.GetConfig<GameplayConfig>();
        
        ApplyConfig();
    }
    
    private void ApplyConfig()
    {
        // 应用玩家配置
        var player = FindObjectOfType<PlayerController>();
        player.SetMaxHealth(m_Config.player.maxHealth);
        player.SetMoveSpeed(m_Config.player.moveSpeed);
        
        // 应用敌人配置
        var spawner = FindObjectOfType<EnemySpawner>();
        spawner.SetMaxEnemies(m_Config.enemy.maxEnemies);
    }
}
```

### 示例2：动态配置加载
```csharp
public class DynamicConfigLoader
{
    private ConfigComponent m_ConfigComponent;
    
    public void LoadLevelConfig(int levelId)
    {
        string configPath = $"Assets/Configs/Levels/Level{levelId}.json";
        
        m_ConfigComponent.LoadConfigAsync<LevelConfig>(
            configPath,
            (success) =>
            {
                if (success)
                {
                    var config = m_ConfigComponent.GetConfig<LevelConfig>();
                    ApplyLevelConfig(levelId, config);
                }
                else
                {
                    Log.Error($"Failed to load config for level {levelId}");
                }
            }
        );
    }
    
    private void ApplyLevelConfig(int levelId, LevelConfig config)
    {
        Log.Info($"Applying configuration for level {levelId}");
        // 应用关卡配置
    }
}
```

### 示例3：配置管理工具类
```csharp
public class ConfigManager
{
    private static ConfigManager s_Instance;
    private ConfigComponent m_ConfigComponent;
    private Dictionary<Type, object> m_CachedConfigs = new Dictionary<Type, object>();
    
    public static ConfigManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ConfigManager();
            }
            return s_Instance;
        }
    }
    
    public T GetConfig<T>() where T : class
    {
        var type = typeof(T);
        if (m_CachedConfigs.TryGetValue(type, out var cached))
        {
            return cached as T;
        }
        
        var config = m_ConfigComponent.GetConfig<T>();
        m_CachedConfigs[type] = config;
        return config;
    }
    
    public void LoadConfigAsync<T>(string path, Action<bool> callback) 
        where T : class, IConfig, new()
    {
        m_ConfigComponent.LoadConfigAsync<T>(path, (success) =>
        {
            if (success)
            {
                var config = m_ConfigComponent.GetConfig<T>();
                if (config.Validate())
                {
                    m_CachedConfigs[typeof(T)] = config;
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }
    
    public void ReloadAllConfigs()
    {
        m_CachedConfigs.Clear();
        Log.Info("All cached configs cleared");
    }
}
```

## 配置管理

### 配置文件存放规范
```
Assets/
├── Configs/
│   ├── GameConfig.json          # 游戏全局配置
│   ├── AudioConfig.json         # 音频配置
│   ├── UIConfig.json            # UI配置
│   ├── Levels/
│   │   ├── Level1.json
│   │   ├── Level2.json
│   │   └── ...
│   └── Enemies/
│       ├── Enemy1.json
│       ├── Enemy2.json
│       └── ...
```

### 配置编辑工具
使用Unity编辑器创建配置编辑窗口：
```csharp
public class ConfigEditor : EditorWindow
{
    private GameConfig m_GameConfig;
    private Vector2 m_ScrollPosition;
    
    [MenuItem("Window/Game Framework/Config Editor")]
    public static void ShowWindow()
    {
        GetWindow<ConfigEditor>("Config Editor");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Game Configuration Editor", EditorStyles.boldLabel);
        
        m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
        
        // 绘制配置编辑界面
        
        GUILayout.EndScrollView();
    }
}
```

## 常见问题

### Q1: 如何选择配置文件格式？

**A:** 根据场景选择：

- **JSON**（推荐）：可读性好，易于编辑，文件体积较小
- **XML**：结构清晰，支持复杂层级
- **二进制**：性能最好，不可读，适合运行时优化

### Q2: 配置文件应该放在哪里？

**A:** 根据发布方式：

- **开发阶段**：放在 `Assets/Resources/Configs` 或 `Assets/Configs`
- **发布版本**：打包为AssetBundle或内置到代码
- **云端配置**：通过网络加载

### Q3: 如何处理配置版本升级？

**A:** 使用版本号和迁移策略：
```csharp
if (config.version < CURRENT_VERSION)
{
    config = MigrateConfig(config, CURRENT_VERSION);
}
```

### Q4: 配置何时应该热更新？

**A:** 
- 在新游戏会话开始前
- 在关键游戏事件发生时
- 通过开发者工具手动触发

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
