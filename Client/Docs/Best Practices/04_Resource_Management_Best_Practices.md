# 资源管理系统（Resource Manager）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [核心概念](#核心概念)
3. [资源加载方式](#资源加载方式)
4. [最佳实践](#最佳实践)
5. [代码示例](#代码示例)
6. [性能优化](#性能优化)
7. [常见问题](#常见问题)

## 系统概述

资源管理系统（Resource Manager System）是GameFrameX框架用于管理游戏资源的核心系统。它集成了YooAsset资源系统，提供了统一的资源加载、卸载、版本管理和热更新能力。

### 主要特点
- **多种加载方式**: 同步/异步加载、预加载、流式加载
- **资源引用计数**: 自动管理资源生命周期
- **版本管理**: 支持资源版本控制和热更新
- **AssetBundle支持**: 完整的AssetBundle工作流
- **内存管理**: 自动内存优化和垃圾回收

## 核心概念

### 资源加载管理器
```csharp
public interface IResourceManager
{
    // 同步加载
    T LoadAsset<T>(string assetPath) where T : Object;
    
    // 异步加载
    void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : Object;
    
    // 预加载
    void PreloadAsset(string assetPath);
    
    // 卸载资源
    void UnloadAsset(string assetPath);
    
    // 卸载所有未使用的资源
    void UnloadUnusedAssets();
}
```

### 资源引用
```csharp
public interface IAssetReference
{
    Object Asset { get; }                   // 获取资源
    bool IsValid { get; }                   // 是否有效
    int ReferenceCount { get; }             // 引用计数
    void Release();                         // 释放引用
}
```

## 资源加载方式

### 1. 同步加载
立即返回资源，阻塞当前线程。

```csharp
var texture = ResourceManager.LoadAsset<Texture2D>("Assets/Textures/UIIcon.png");
```

### 2. 异步加载
不阻塞线程，通过回调返回资源。

```csharp
ResourceManager.LoadAssetAsync<GameObject>(
    "Assets/Prefabs/UI/MainMenu.prefab",
    (prefab) =>
    {
        Instantiate(prefab);
    }
);
```

### 3. 预加载
提前加载资源到内存，避免运行时卡顿。

```csharp
ResourceManager.PreloadAsset("Assets/Prefabs/Player.prefab");
```

## 最佳实践

### 1. 资源路径管理

#### 1.1 使用常量管理路径
```csharp
// 推荐：集中管理资源路径
public static class ResourcePaths
{
    // UI资源
    public const string UI_MAIN_MENU = "Assets/Resources/UI/MainMenu.prefab";
    public const string UI_GAME_HUD = "Assets/Resources/UI/GameHUD.prefab";
    public const string UI_PAUSE_MENU = "Assets/Resources/UI/PauseMenu.prefab";
    
    // 游戏对象
    public const string PREFAB_PLAYER = "Assets/Prefabs/Player.prefab";
    public const string PREFAB_ENEMY = "Assets/Prefabs/Enemy.prefab";
    public const string PREFAB_PROJECTILE = "Assets/Prefabs/Projectile.prefab";
    
    // 音频资源
    public const string AUDIO_BGM_MENU = "Assets/Audio/Music/Menu.mp3";
    public const string AUDIO_BGM_GAMEPLAY = "Assets/Audio/Music/Gameplay.mp3";
    public const string AUDIO_SFX_JUMP = "Assets/Audio/SFX/Jump.mp3";
    
    // 特效资源
    public const string VFX_EXPLOSION = "Assets/VFX/Explosion.prefab";
    public const string VFX_BLOOD = "Assets/VFX/BloodSplash.prefab";
}

// 不推荐：硬编码路径
var prefab = ResourceManager.LoadAsset<GameObject>(
    "Assets/Resources/Prefabs/Player.prefab"
);
```

#### 1.2 资源文件夹结构规范
```
Assets/
├── Resources/
│   ├── UI/
│   │   ├── MainMenu.prefab
│   │   ├── GameHUD.prefab
│   │   └── Icons/
│   ├── Prefabs/
│   │   ├── Player.prefab
│   │   ├── Enemies/
│   │   └── Items/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Textures/
│   │   ├── UI/
│   │   └── Environment/
│   └── Configs/
│       └── GameConfig.json
```

### 2. 异步加载的最佳实践

#### 2.1 批量异步加载
```csharp
// 推荐：使用协程管理多个异步操作
private IEnumerator LoadGameResourcesAsync()
{
    var resourceManager = GameEntry.GetComponent<ResourceComponent>();
    
    // 创建加载任务列表
    var loadTasks = new List<AsyncLoadTask>();
    
    // 添加加载任务
    loadTasks.Add(new AsyncLoadTask(
        ResourcePaths.PREFAB_PLAYER,
        OnPlayerLoaded
    ));
    loadTasks.Add(new AsyncLoadTask(
        ResourcePaths.UI_GAME_HUD,
        OnHUDLoaded
    ));
    
    // 等待所有任务完成
    yield return StartCoroutine(LoadResourcesCoroutine(loadTasks));
    
    Log.Info("All game resources loaded");
}

private IEnumerator LoadResourcesCoroutine(List<AsyncLoadTask> tasks)
{
    foreach (var task in tasks)
    {
        resourceManager.LoadAssetAsync(task.Path, task.Callback);
    }
    
    yield return new WaitForSeconds(1f);
}
```

#### 2.2 超时处理
```csharp
public class ResourceLoadWithTimeout
{
    public void LoadAssetWithTimeout<T>(
        string assetPath,
        float timeout,
        Action<T> onSuccess,
        Action onTimeout) where T : Object
    {
        StartCoroutine(LoadAssetCoroutine(assetPath, timeout, onSuccess, onTimeout));
    }
    
    private IEnumerator LoadAssetCoroutine<T>(
        string assetPath,
        float timeout,
        Action<T> onSuccess,
        Action onTimeout) where T : Object
    {
        T loadedAsset = null;
        bool isLoaded = false;
        
        ResourceManager.LoadAssetAsync<T>(
            assetPath,
            (asset) =>
            {
                loadedAsset = asset;
                isLoaded = true;
            }
        );
        
        float elapsedTime = 0f;
        while (!isLoaded && elapsedTime < timeout)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (isLoaded)
        {
            onSuccess?.Invoke(loadedAsset);
        }
        else
        {
            onTimeout?.Invoke();
        }
    }
}
```

### 3. 资源卸载和内存管理

#### 3.1 及时卸载不需要的资源
```csharp
// 推荐：场景切换时卸载旧资源
public class SceneTransitionManager
{
    public void TransitionToScene(string newScene)
    {
        // 卸载当前场景的资源
        UnloadCurrentSceneResources();
        
        // 加载新场景资源
        LoadSceneResources(newScene);
    }
    
    private void UnloadCurrentSceneResources()
    {
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        
        // 卸载特定资源
        resourceManager.UnloadAsset(ResourcePaths.UI_GAME_HUD);
        resourceManager.UnloadAsset(ResourcePaths.AUDIO_BGM_GAMEPLAY);
        
        // 卸载所有未使用资源
        resourceManager.UnloadUnusedAssets();
    }
}
```

#### 3.2 使用引用计数管理生命周期
```csharp
public class ManagedResourceUser
{
    private IAssetReference m_ResourceRef;
    
    public void UseResource(string assetPath)
    {
        m_ResourceRef = ResourceManager.LoadAssetWithReference(assetPath);
        
        if (m_ResourceRef.IsValid)
        {
            // 使用资源
            var asset = m_ResourceRef.Asset;
        }
    }
    
    public void OnDestroy()
    {
        // 释放引用，当引用计数为0时资源自动卸载
        if (m_ResourceRef != null)
        {
            m_ResourceRef.Release();
            m_ResourceRef = null;
        }
    }
}
```

### 4. 预加载策略

#### 4.1 启动时预加载关键资源
```csharp
public class GameStartProcedure : ProcedureBase
{
    public override void OnEnter()
    {
        PreloadCriticalResources();
    }
    
    private void PreloadCriticalResources()
    {
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        
        // 预加载常用UI
        resourceManager.PreloadAsset(ResourcePaths.UI_MAIN_MENU);
        resourceManager.PreloadAsset(ResourcePaths.UI_GAME_HUD);
        
        // 预加载常用音效
        resourceManager.PreloadAsset(ResourcePaths.AUDIO_BGM_MENU);
        resourceManager.PreloadAsset(ResourcePaths.AUDIO_SFX_JUMP);
        
        // 预加载基础角色
        resourceManager.PreloadAsset(ResourcePaths.PREFAB_PLAYER);
    }
}
```

#### 4.2 按需预加载
```csharp
public class LevelManager
{
    public void PreloadLevelResources(int levelId)
    {
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        
        // 根据关卡ID预加载该关卡的所有资源
        var levelResources = GetLevelResources(levelId);
        
        foreach (var resource in levelResources)
        {
            resourceManager.PreloadAsset(resource);
        }
    }
    
    private List<string> GetLevelResources(int levelId)
    {
        // 返回关卡所需的资源列表
        return new List<string>();
    }
}
```

### 5. AssetBundle管理

#### 5.1 AssetBundle加载
```csharp
public class AssetBundleManager
{
    public void LoadAssetBundleAsync(
        string bundleName,
        Action<AssetBundle> onComplete)
    {
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        
        resourceManager.LoadAssetBundleAsync(
            bundleName,
            (bundle) =>
            {
                if (bundle != null)
                {
                    onComplete?.Invoke(bundle);
                }
                else
                {
                    Log.Error($"Failed to load asset bundle: {bundleName}");
                }
            }
        );
    }
    
    public void UnloadAssetBundle(AssetBundle bundle, bool unloadAllLoadedObjects = false)
    {
        if (bundle != null)
        {
            bundle.Unload(unloadAllLoadedObjects);
        }
    }
}
```

#### 5.2 AssetBundle依赖管理
```csharp
public class AssetBundleDependencyManager
{
    private Dictionary<string, List<string>> m_BundleDependencies = 
        new Dictionary<string, List<string>>();
    
    public void LoadBundleWithDependencies(string bundleName)
    {
        var deps = GetDependencies(bundleName);
        
        // 先加载依赖
        foreach (var dep in deps)
        {
            LoadAssetBundleInternal(dep);
        }
        
        // 再加载目标Bundle
        LoadAssetBundleInternal(bundleName);
    }
    
    private List<string> GetDependencies(string bundleName)
    {
        return m_BundleDependencies.ContainsKey(bundleName)
            ? m_BundleDependencies[bundleName]
            : new List<string>();
    }
    
    private void LoadAssetBundleInternal(string bundleName)
    {
        // 实现加载逻辑
    }
}
```

## 代码示例

### 示例1：完整的资源加载流程
```csharp
public class ResourceLoadManager : MonoBehaviour
{
    private ResourceComponent m_ResourceComponent;
    private Dictionary<string, Object> m_LoadedResources = new Dictionary<string, Object>();
    
    private void Start()
    {
        m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
        InitializeResources();
    }
    
    private void InitializeResources()
    {
        // 预加载关键资源
        PreloadCriticalAssets();
    }
    
    private void PreloadCriticalAssets()
    {
        var assetsToPreload = new[]
        {
            ResourcePaths.PREFAB_PLAYER,
            ResourcePaths.UI_MAIN_MENU,
            ResourcePaths.AUDIO_BGM_MENU
        };
        
        foreach (var asset in assetsToPreload)
        {
            m_ResourceComponent.PreloadAsset(asset);
        }
    }
    
    public T GetCachedResource<T>(string path) where T : Object
    {
        if (m_LoadedResources.TryGetValue(path, out var resource))
        {
            return resource as T;
        }
        
        var loadedAsset = m_ResourceComponent.LoadAsset<T>(path);
        m_LoadedResources[path] = loadedAsset;
        return loadedAsset;
    }
    
    public void LoadResourceAsync<T>(
        string path,
        Action<T> onComplete) where T : Object
    {
        m_ResourceComponent.LoadAssetAsync<T>(path, (asset) =>
        {
            m_LoadedResources[path] = asset;
            onComplete?.Invoke(asset);
        });
    }
    
    public void ClearCache()
    {
        m_LoadedResources.Clear();
        m_ResourceComponent.UnloadUnusedAssets();
    }
}
```

### 示例2：场景资源管理
```csharp
public class SceneResourceManager
{
    private string m_CurrentScene;
    private List<string> m_LoadedSceneResources = new List<string>();
    
    public void LoadSceneAsync(string sceneName, Action onComplete)
    {
        // 卸载旧场景资源
        if (!string.IsNullOrEmpty(m_CurrentScene))
        {
            UnloadSceneResources(m_CurrentScene);
        }
        
        m_CurrentScene = sceneName;
        
        // 加载新场景资源
        StartCoroutine(LoadSceneResourcesCoroutine(sceneName, onComplete));
    }
    
    private IEnumerator LoadSceneResourcesCoroutine(
        string sceneName,
        Action onComplete)
    {
        var sceneResources = GetSceneResources(sceneName);
        m_LoadedSceneResources = sceneResources;
        
        int loadedCount = 0;
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        
        foreach (var resource in sceneResources)
        {
            resourceManager.LoadAssetAsync(resource, (asset) =>
            {
                loadedCount++;
            });
        }
        
        yield return new WaitUntil(() => loadedCount >= sceneResources.Count);
        onComplete?.Invoke();
    }
    
    private void UnloadSceneResources(string sceneName)
    {
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        
        foreach (var resource in m_LoadedSceneResources)
        {
            resourceManager.UnloadAsset(resource);
        }
        
        m_LoadedSceneResources.Clear();
    }
    
    private List<string> GetSceneResources(string sceneName)
    {
        // 返回场景所需的资源列表
        return new List<string>();
    }
}
```

### 示例3：资源对象池整合
```csharp
public class ResourceObjectPool
{
    private Dictionary<string, Queue<Object>> m_ObjectPools = 
        new Dictionary<string, Queue<Object>>();
    
    public void PreallocatePool<T>(string prefabPath, int count) where T : Object
    {
        if (!m_ObjectPools.ContainsKey(prefabPath))
        {
            m_ObjectPools[prefabPath] = new Queue<Object>();
        }
        
        var resourceManager = GameEntry.GetComponent<ResourceComponent>();
        var prefab = resourceManager.LoadAsset<T>(prefabPath);
        
        for (int i = 0; i < count; i++)
        {
            var instance = Object.Instantiate(prefab);
            instance.SetActive(false);
            m_ObjectPools[prefabPath].Enqueue(instance);
        }
    }
    
    public T Spawn<T>(string prefabPath) where T : Object
    {
        if (!m_ObjectPools.ContainsKey(prefabPath))
        {
            PreallocatePool<T>(prefabPath, 5);
        }
        
        var pool = m_ObjectPools[prefabPath];
        T instance;
        
        if (pool.Count > 0)
        {
            instance = pool.Dequeue() as T;
        }
        else
        {
            var resourceManager = GameEntry.GetComponent<ResourceComponent>();
            var prefab = resourceManager.LoadAsset<T>(prefabPath);
            instance = Object.Instantiate(prefab) as T;
        }
        
        (instance as GameObject)?.SetActive(true);
        return instance;
    }
    
    public void Despawn<T>(string prefabPath, T instance) where T : Object
    {
        (instance as GameObject)?.SetActive(false);
        m_ObjectPools[prefabPath].Enqueue(instance);
    }
}
```

## 性能优化

### 1. 避免频繁的资源加载
```csharp
// 不推荐：每次使用都加载
public void UseTexture()
{
    var texture = ResourceManager.LoadAsset<Texture2D>(path);
}

// 推荐：缓存并复用
private Texture2D m_CachedTexture;

public void Initialize()
{
    m_CachedTexture = ResourceManager.LoadAsset<Texture2D>(path);
}

public void UseTexture()
{
    // 使用缓存的texture
}
```

### 2. 异步加载关键资源
```csharp
// 推荐：UI在后台异步加载
ResourceManager.LoadAssetAsync<GameObject>(
    prefabPath,
    (prefab) =>
    {
        UIManager.ShowUI(prefab);
    }
);
```

### 3. 合理使用预加载
```csharp
// 推荐：在合适的时间预加载下一关资源
public void OnLevelEnd()
{
    PreloadNextLevelResources();
}
```

## 常见问题

### Q1: 如何选择同步还是异步加载？

**A:** 
- **同步加载**：用于启动阶段或小型资源
- **异步加载**：用于运行时加载或大型资源，避免卡顿

### Q2: 如何处理资源加载失败？

**A:**
```csharp
if (asset == null)
{
    Log.Error($"Failed to load asset: {path}");
    // 使用默认资源
}
```

### Q3: 如何优化内存占用？

**A:** 
- 及时卸载不需要的资源
- 使用资源压缩
- 合理使用AssetBundle分组

### Q4: 如何进行资源版本管理？

**A:**
```csharp
var version = ResourceManager.GetAssetVersion(assetPath);
```

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
