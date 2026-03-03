# 游戏事件系统技术文档

## 目录
1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [核心组件](#核心组件)
4. [系统优点](#系统优点)
5. [存在问题](#存在问题)
6. [改进方案](#改进方案)
7. [最佳实践](#最佳实践)
8. [使用示例](#使用示例)
9. [常见陷阱](#常见陷阱)
10. [性能优化](#性能优化)

---

## 系统概述

### 定义
GameFrameX 事件系统是一个基于**发布-订阅（Pub/Sub）模式**的分布式事件处理框架，用于实现游戏中各个模块之间的**松耦合通信**。

### 核心特性
- ? **线程安全**：事件可从任意线程触发，回调在主线程执行
- ? **异步分发**：事件在触发帧的下一帧分发（`Fire`）或立即分发（`FireNow`）
- ? **类型安全**：强类型事件参数继承自 `GameEventArgs`
- ? **对象池机制**：事件和处理器支持对象池复用
- ? **灵活订阅**：支持多对多的订阅关系

---

## 架构设计

### 核心层级架构

```
┌─────────────────────────────────────────────────────┐
│         使用层（User Code）                          │
│  EventComponent 的公开 API 调用                      │
└─────────────────────┬───────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────┐
│         组件层（EventComponent）                      │
│  - CheckSubscribe/Unsubscribe                        │
│  - Fire/FireNow                                      │
│  - SetDefaultHandler                                │
└─────────────────────┬───────────────────────────────┘
                      │ implements
┌─────────────────────▼───────────────────────────────┐
│         接口层（IEventManager）                       │
│  - Count(id)                                         │
│  - Check(id, handler)                               │
│  - Subscribe/Unsubscribe                            │
│  - Fire/FireNow                                      │
└─────────────────────┬───────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────┐
│         实现层（EventManager）                        │
│  - GameFrameworkModule 继承                          │
│  - Update/Shutdown 生命周期                          │
└─────────────────────┬───────────────────────────────┘
                      │ uses
┌─────────────────────▼───────────────────────────────┐
│         事件池层（EventPool<T>）                      │
│  - 事件和处理器的存储与管理                           │
│  - 并发队列管理                                       │
│  - 帧周期事件分发                                     │
└─────────────────────┬───────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────┐
│         事件参数层（GameEventArgs）                   │
│  - 基类：BaseEventArgs                               │
│  - 派生：具体事件参数类型                             │
│  - 实现：Id 属性、Clear 方法（对象池）                 │
└─────────────────────────────────────────────────────┘
```

### 核心数据流

```
发送事件流程：
┌──────────────────────────────────────┐
│  EventComponent.Fire(sender, e)      │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│  EventManager.Fire(sender, e)        │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│  EventPool.Fire(sender, e)           │
│  将事件加入并发队列                   │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│  下一帧 EventPool.Update()            │
│  处理并发队列中的事件                 │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│  遍历所有订阅者并回调处理函数        │
│  (线程安全：在主线程执行)             │
└──────────────────────────────────────┘
```

---

## 核心组件

### 1. EventComponent（组件层）

**文件路径**：`Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/EventComponent.cs`

**职责**：
- 提供 MonoBehaviour 界面接口
- 代理所有事件操作到 EventManager
- 生命周期管理

**关键 API**：
```csharp
public class EventComponent : GameFrameworkComponent
{
    // 查询
    public int EventHandlerCount { get; }           // 处理器总数
    public int EventCount { get; }                  // 待处理事件总数
    public int Count(string id)                     // 指定事件的处理器数
    public bool Check(string id, EventHandler<GameEventArgs> handler)  // 检查处理器是否存在

    // 订阅管理
    public void CheckSubscribe(string id, EventHandler<GameEventArgs> handler)  // 自动重复订阅检测
    public void Subscribe(string id, EventHandler<GameEventArgs> handler)       // [已弃用] 直接订阅
    public void Unsubscribe(string id, EventHandler<GameEventArgs> handler)     // 取消订阅
    public void SetDefaultHandler(EventHandler<GameEventArgs> handler)          // 设置默认处理器

    // 事件触发
    public void Fire(object sender, GameEventArgs e)        // 异步触发（下一帧分发）
    public void Fire(object sender, string eventId)         // 异步触发空事件
    public void FireNow(object sender, GameEventArgs e)     // 同步立即分发
}
```

**优点**：
- 暴露简洁的公开 API
- 支持自动重复订阅检测（`CheckSubscribe`）
- 两种分发模式（异步/同步）

### 2. IEventManager（接口层）

**文件路径**：`Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/Event/IEventManager.cs`

**设计原则**：接口隔离原则（ISP）
```csharp
public interface IEventManager
{
    int EventHandlerCount { get; }
    int EventCount { get; }
    int Count(string id);
    bool Check(string id, EventHandler<GameEventArgs> handler);
    void Subscribe(string id, EventHandler<GameEventArgs> handler);
    void Unsubscribe(string id, EventHandler<GameEventArgs> handler);
    void SetDefaultHandler(EventHandler<GameEventArgs> handler);
    void Fire(object sender, GameEventArgs e);
    void FireNow(object sender, GameEventArgs e);
}
```

### 3. EventManager（实现层）

**文件路径**：`Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/Event/EventManager.cs`

**职责**：
- 事件池生命周期管理
- 实现 IEventManager 接口
- 框架模块集成

**关键实现**：
```csharp
public sealed class EventManager : GameFrameworkModule, IEventManager
{
    private readonly EventPool<GameEventArgs> m_EventPool;

    public EventManager()
    {
        // 允许没有处理器的事件、允许多个处理器
        m_EventPool = new EventPool<GameEventArgs>(
            EventPoolMode.AllowNoHandler | EventPoolMode.AllowMultiHandler
        );
    }

    protected override int Priority => 7;  // 优先级7，较高

    protected override void Update(float elapseSeconds, float realElapseSeconds)
    {
        m_EventPool.Update(elapseSeconds, realElapseSeconds);
    }

    protected override void Shutdown()
    {
        m_EventPool.Shutdown();
    }
}
```

### 4. EventPool<T>（事件池层）

**文件路径**：`Packages/com.gameframex.unity@d91e788909f3/Runtime/Base/EventPool/`

**核心数据结构**：
```csharp
public sealed partial class EventPool<T> where T : BaseEventArgs
{
    // 1. 事件处理器存储（string id => 处理器集合）
    private readonly GameFrameworkMultiDictionary<string, EventHandler<T>> _eventHandlers;

    // 2. 待处理事件队列（线程安全）
    private readonly ConcurrentQueue<EventNode> _events;

    // 3. 缓存节点字典（性能优化）
    private readonly Dictionary<object, LinkedListNode<EventHandler<T>>> _cachedNodes;

    // 4. 默认处理器
    private EventHandler<T> _defaultHandler;

    // 5. 线程同步锁
    private readonly object _lock = new object();
}
```

**关键方法**：
- `Subscribe(string id, EventHandler<T> handler)`：添加事件订阅
- `Unsubscribe(string id, EventHandler<T> handler)`：移除事件订阅
- `Fire(object sender, T e)`：异步触发事件
- `FireNow(object sender, T e)`：同步触发事件
- `Update(float elapseSeconds, float realElapseSeconds)`：处理待发事件

### 5. GameEventArgs（事件参数基类）

**文件路径**：`Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/EventArgs/GameEventArgs.cs`

```csharp
public abstract class GameEventArgs : BaseEventArgs
{
    // 继承自 BaseEventArgs
    // 需要实现：
    //   string Id { get; }           // 事件唯一标识
    //   void Clear()                 // 对象池清空时调用
}
```

**示例实现**：
```csharp
public sealed class BagChangedEventArgs : GameEventArgs
{
    public static readonly string EventId = typeof(BagChangedEventArgs).FullName;

    public override void Clear()
    {
        // 清空数据
    }

    public override string Id => EventId;

    public static BagChangedEventArgs Create()
    {
        return ReferencePool.Acquire<BagChangedEventArgs>();
    }
}
```

---

## 系统优点

### 1. 松耦合架构
**描述**：模块间通过事件通信，不直接持有引用
```csharp
// ? 紧耦合
class PlayerManager
{
    private UIManager uiManager;
    public void LevelUp()
    {
        uiManager.ShowLevelUpUI();  // 直接调用
    }
}

// ? 松耦合
class PlayerManager
{
    public void LevelUp()
    {
        var e = PlayerLevelUpEventArgs.Create();
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}
```

### 2. 线程安全的跨线程通信
**特性**：事件可从子线程触发，回调自动在主线程执行
```csharp
// 网络线程中安全触发
ThreadPool.QueueUserWorkItem(_ =>
{
    var e = NetworkDataReceivedEventArgs.Create();
    GameEntry.GetComponent<EventComponent>().Fire(this, e);
});
// 回调自动在主线程执行
```

### 3. 异步事件分发机制
**优势**：避免事件链式调用导致的栈溢出
```csharp
// 事件 A 触发时立即触发事件 B，而不是等到下一帧
// 这可能导致复杂的调用栈，但异步分发避免了这一问题
```

### 4. 对象池支持
**效能**：减少 GC 压力
```csharp
public static BagChangedEventArgs Create()
{
    var eventArgs = ReferencePool.Acquire<BagChangedEventArgs>();
    return eventArgs;
}
// 事件分发后自动回收到对象池
```

### 5. 灵活的订阅模式
**特性**：多对多订阅关系
```csharp
// 一个事件可以有多个订阅者
GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    PlayerLevelUpEventArgs.EventId, 
    OnPlayerLevelUp_1
);
GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    PlayerLevelUpEventArgs.EventId, 
    OnPlayerLevelUp_2
);
GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    PlayerLevelUpEventArgs.EventId, 
    OnPlayerLevelUp_3
);
```

### 6. 优先级管理
**特性**：EventManager 优先级为 7，确保事件在其他系统之前处理
```
Module Priority:
- 优先级 10：最高优先级
- ...
- 优先级 7：EventManager（较高）
- 优先级 5：UserCode
- 优先级 1：最低优先级
```

---

## 存在问题

### 问题 1：字符串作为事件 ID 的类型不安全性

**问题**：
```csharp
// 字符串匹配错误导致订阅失败，编译期无法检测
GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    "PlayerLevelUp",  // ? 硬编码字符串
    OnPlayerLevelUp
);

GameEntry.GetComponent<EventComponent>().Fire(this, 
    "PlayerLevelUpp"  // ? 拼写错误，无法捕获
);
```

**影响**：
- ?? 严重的运行时 Bug，编译期无法检测
- ?? 事件订阅可能无效，难以调试
- ?? 重构时容易引入错误

**建议**：使用 EventId 常量
```csharp
public static class EventIds
{
    public const string PlayerLevelUp = nameof(PlayerLevelUp);
    public const string BagChanged = nameof(BagChanged);
}

// ? 类型安全的使用
GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    EventIds.PlayerLevelUp,
    OnPlayerLevelUp
);
```

---

### 问题 2：内存泄漏风险

**问题**：
```csharp
public class UIPanel : MonoBehaviour
{
    private void OnEnable()
    {
        // 订阅事件
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(
            "BagChanged",
            OnBagChanged
        );
    }

    private void OnDisable()
    {
        // ? 忘记取消订阅
        // 面板销毁后，处理器仍然被事件系统持有
        // 导致内存泄漏
    }

    private void OnBagChanged(object sender, GameEventArgs e)
    {
        // 即使面板已销毁，此方法仍可能被调用
    }
}
```

**影响**：
- ?? 内存泄漏
- ?? 性能下降
- ?? 不必要的回调触发

**解决方案**：
```csharp
public class UIPanel : MonoBehaviour
{
    private void OnEnable()
    {
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(
            EventIds.BagChanged,
            OnBagChanged
        );
    }

    private void OnDisable()
    {
        // ? 显式取消订阅
        GameEntry.GetComponent<EventComponent>().Unsubscribe(
            EventIds.BagChanged,
            OnBagChanged
        );
    }
}
```

---

### 问题 3：事件处理异常传播不清晰

**问题**：
```csharp
// 如果事件处理器抛出异常，会发生什么？
private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    throw new Exception("处理器出错");  // ? 异常可能被吞没
}
```

**影响**：
- ?? 异常处理不明确
- ?? 调试困难

**当前实现**（UIEventSubscriber.cs）：
```csharp
foreach (var eventHandler in handlers)
{
    try
    {
        eventHandler.Invoke(this, e);
    }
    catch (Exception exception)
    {
        Log.Error(exception);  // 记录异常但继续
    }
}
```

---

### 问题 4：缺乏事件订阅生命周期管理

**问题**：
```csharp
// 复杂的订阅/取消订阅管理
public class UserModule
{
    public void Init()
    {
        GameEntry.GetComponent<EventComponent>().CheckSubscribe("Event1", Handler1);
        GameEntry.GetComponent<EventComponent>().CheckSubscribe("Event2", Handler2);
        GameEntry.GetComponent<EventComponent>().CheckSubscribe("Event3", Handler3);
    }

    public void Cleanup()
    {
        // ? 手动管理，容易遗漏
        GameEntry.GetComponent<EventComponent>().Unsubscribe("Event1", Handler1);
        GameEntry.GetComponent<EventComponent>().Unsubscribe("Event2", Handler2);
        // ? 忘记了 Handler3
    }
}
```

**改进**：使用 UIEventSubscriber 模式
```csharp
public class UserModule
{
    private UIEventSubscriber m_EventSubscriber;

    public void Init()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(EventIds.Event1, Handler1);
        m_EventSubscriber.CheckSubscribe(EventIds.Event2, Handler2);
        m_EventSubscriber.CheckSubscribe(EventIds.Event3, Handler3);
    }

    public void Cleanup()
    {
        // ? 一次清空所有
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }
}
```

---

### 问题 5：默认处理器机制使用不明确

**问题**：
```csharp
// 什么是默认处理器？何时使用？
GameEntry.GetComponent<EventComponent>().SetDefaultHandler(
    (sender, e) => Log.Warning($"Unhandled event: {e.Id}")
);
```

**当前文档缺少**：
- 默认处理器的调用时机
- 默认处理器的优先级
- 何时应该使用默认处理器

---

### 问题 6：混合异步和同步分发导致的问题

**问题**：
```csharp
// 混合使用 Fire（异步）和 FireNow（同步）
// 可能导致事件处理顺序混乱

// 线程 A：异步触发
GameEntry.GetComponent<EventComponent>().Fire(this, eventA);

// 线程 B：同步触发（立即执行）
GameEntry.GetComponent<EventComponent>().FireNow(this, eventB);

// 处理顺序不确定，难以调试
```

---

## 改进方案

### 方案 1：强类型事件系统

**目标**：编译期类型检查，消除字符串错误

**实现**：
```csharp
/// <summary>
/// 强类型事件发送器
/// 在编译期检查事件 ID 和参数类型
/// </summary>
public sealed class TypedEventSystem
{
    private static readonly Dictionary<string, Type> RegisteredEvents = 
        new Dictionary<string, Type>();

    /// <summary>
    /// 注册事件类型
    /// </summary>
    public static void RegisterEvent<T>(string eventId) where T : GameEventArgs
    {
        if (!RegisteredEvents.ContainsKey(eventId))
        {
            RegisteredEvents[eventId] = typeof(T);
        }
    }

    /// <summary>
    /// 类型安全的订阅
    /// </summary>
    public static void Subscribe<T>(
        string eventId, 
        EventHandler<T> handler) where T : GameEventArgs
    {
        if (!RegisteredEvents.TryGetValue(eventId, out var eventType))
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' is not registered");
        }

        if (eventType != typeof(T))
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' expects type {eventType.Name}, " +
                $"but got {typeof(T).Name}");
        }

        var untyped = (EventHandler<GameEventArgs>)(object)handler;
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(eventId, untyped);
    }

    /// <summary>
    /// 类型安全的事件触发
    /// </summary>
    public static void Fire<T>(object sender, string eventId, T e) 
        where T : GameEventArgs
    {
        if (!RegisteredEvents.TryGetValue(eventId, out var eventType))
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' is not registered");
        }

        if (eventType != typeof(T))
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' expects type {eventType.Name}, " +
                $"but got {e.GetType().Name}");
        }

        GameEntry.GetComponent<EventComponent>().Fire(sender, e);
    }
}
```

**使用示例**：
```csharp
// 初始化时注册事件类型
TypedEventSystem.RegisterEvent<PlayerLevelUpEventArgs>(
    EventIds.PlayerLevelUp
);

// ? 编译期类型检查
TypedEventSystem.Subscribe<PlayerLevelUpEventArgs>(
    EventIds.PlayerLevelUp,
    OnPlayerLevelUp
);

// ? 编译错误或运行时异常
TypedEventSystem.Subscribe<BagChangedEventArgs>(
    EventIds.PlayerLevelUp,  // 类型不匹配
    OnBagChanged
);
```

---

### 方案 2：自动生命周期管理

**目标**：简化订阅/取消订阅管理，防止内存泄漏

**实现**：
```csharp
/// <summary>
/// 事件订阅自动管理器
/// 通过 using 语句自动取消订阅
/// </summary>
public sealed class EventSubscriptionManager : IDisposable
{
    private readonly List<(string eventId, Delegate handler)> m_Subscriptions;

    public EventSubscriptionManager()
    {
        m_Subscriptions = new List<(string, Delegate)>();
    }

    /// <summary>
    /// 订阅事件（自动管理生命周期）
    /// </summary>
    public void Subscribe(string eventId, EventHandler<GameEventArgs> handler)
    {
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(eventId, handler);
        m_Subscriptions.Add((eventId, handler));
    }

    /// <summary>
    /// 自动清理所有订阅
    /// </summary>
    public void Dispose()
    {
        foreach (var (eventId, handler) in m_Subscriptions)
        {
            GameEntry.GetComponent<EventComponent>().Unsubscribe(
                eventId, 
                (EventHandler<GameEventArgs>)handler
            );
        }
        m_Subscriptions.Clear();
    }
}
```

**使用示例**：
```csharp
public class MySystem : IDisposable
{
    private EventSubscriptionManager m_EventManager;

    public void Initialize()
    {
        m_EventManager = new EventSubscriptionManager();
        m_EventManager.Subscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
        m_EventManager.Subscribe(EventIds.BagChanged, OnBagChanged);
    }

    public void Dispose()
    {
        // ? 自动取消所有订阅
        m_EventManager?.Dispose();
    }

    private void OnPlayerLevelUp(object sender, GameEventArgs e) { }
    private void OnBagChanged(object sender, GameEventArgs e) { }
}

// 使用
using (var system = new MySystem())
{
    system.Initialize();
    // 自动清理
}
```

---

### 方案 3：事件优先级系统

**目标**：控制事件处理顺序

**实现**：
```csharp
/// <summary>
/// 事件优先级
/// </summary>
public enum EventPriority
{
    /// <summary>最高优先级</summary>
    Highest = 100,
    
    /// <summary>高优先级</summary>
    High = 50,
    
    /// <summary>普通优先级</summary>
    Normal = 0,
    
    /// <summary>低优先级</summary>
    Low = -50,
    
    /// <summary>最低优先级</summary>
    Lowest = -100
}

/// <summary>
/// 优先级事件处理器包装
/// </summary>
public sealed class PrioritizedEventHandler
{
    public EventHandler<GameEventArgs> Handler { get; set; }
    public EventPriority Priority { get; set; }
}

/// <summary>
/// 优先级事件管理器
/// </summary>
public sealed class PrioritizedEventSystem
{
    private static readonly Dictionary<string, List<PrioritizedEventHandler>> 
        Handlers = new Dictionary<string, List<PrioritizedEventHandler>>();

    /// <summary>
    /// 按优先级订阅事件
    /// </summary>
    public static void Subscribe(
        string eventId,
        EventHandler<GameEventArgs> handler,
        EventPriority priority = EventPriority.Normal)
    {
        if (!Handlers.TryGetValue(eventId, out var list))
        {
            list = new List<PrioritizedEventHandler>();
            Handlers[eventId] = list;
        }

        list.Add(new PrioritizedEventHandler 
        { 
            Handler = handler, 
            Priority = priority 
        });

        // 按优先级排序
        list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// 触发事件（按优先级顺序）
    /// </summary>
    public static void Fire(object sender, GameEventArgs e)
    {
        if (Handlers.TryGetValue(e.Id, out var list))
        {
            foreach (var handler in list)
            {
                try
                {
                    handler.Handler.Invoke(sender, e);
                }
                catch (Exception ex)
                {
                    Log.Error($"Event handler error: {ex}");
                }
            }
        }
    }
}
```

**使用示例**：
```csharp
// 订阅，指定优先级
PrioritizedEventSystem.Subscribe(
    EventIds.PlayerLevelUp,
    OnPlayerLevelUp_UIUpdate,
    EventPriority.High  // UI 更新优先级高
);

PrioritizedEventSystem.Subscribe(
    EventIds.PlayerLevelUp,
    OnPlayerLevelUp_AudioEffect,
    EventPriority.Normal  // 音效优先级普通
);

// 事件处理顺序：UI 更新 -> 音效
PrioritizedEventSystem.Fire(this, new PlayerLevelUpEventArgs());
```

---

### 方案 4：事件监听器属性标记

**目标**：简化事件处理器注册

**实现**：
```csharp
/// <summary>
/// 事件监听器标记特性
/// 标记方法为事件处理器
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventListenerAttribute : Attribute
{
    public string EventId { get; }
    public EventPriority Priority { get; }

    public EventListenerAttribute(
        string eventId, 
        EventPriority priority = EventPriority.Normal)
    {
        EventId = eventId;
        Priority = priority;
    }
}

/// <summary>
/// 事件监听器自动注册器
/// </summary>
public static class EventListenerAutoRegister
{
    /// <summary>
    /// 自动扫描和注册事件监听器
    /// </summary>
    public static void RegisterAll<T>(T instance) where T : class
    {
        var type = typeof(T);
        var methods = type.GetMethods(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance
        );

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<EventListenerAttribute>();
            if (attr == null) continue;

            if (!typeof(EventHandler<GameEventArgs>).IsAssignableFrom(
                System.Reflection.MethodInfoExtensions
                    .CreateDelegate(method.GetType(), instance, method)
                    .GetType()))
            {
                continue;
            }

            var handler = (EventHandler<GameEventArgs>)
                System.Reflection.MethodInfoExtensions
                    .CreateDelegate(typeof(EventHandler<GameEventArgs>), instance, method);

            GameEntry.GetComponent<EventComponent>().CheckSubscribe(
                attr.EventId,
                handler
            );
        }
    }

    /// <summary>
    /// 自动取消注册所有事件监听器
    /// </summary>
    public static void UnregisterAll<T>(T instance) where T : class
    {
        var type = typeof(T);
        var methods = type.GetMethods(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance
        );

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<EventListenerAttribute>();
            if (attr == null) continue;

            var handler = (EventHandler<GameEventArgs>)
                System.Reflection.MethodInfoExtensions
                    .CreateDelegate(typeof(EventHandler<GameEventArgs>), instance, method);

            GameEntry.GetComponent<EventComponent>().Unsubscribe(
                attr.EventId,
                handler
            );
        }
    }
}
```

**使用示例**：
```csharp
public class UIPanel
{
    [EventListener(EventIds.PlayerLevelUp, EventPriority.High)]
    public void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        // 自动注册
    }

    [EventListener(EventIds.BagChanged)]
    private void OnBagChanged(object sender, GameEventArgs e)
    {
        // 自动注册
    }

    private void OnEnable()
    {
        EventListenerAutoRegister.RegisterAll(this);
    }

    private void OnDisable()
    {
        EventListenerAutoRegister.UnregisterAll(this);
    }
}
```

---

### 方案 5：事件链式调用的循环检测

**目标**：防止无限循环事件链

**实现**：
```csharp
/// <summary>
/// 事件链式调用检测器
/// 防止无限循环的事件链
/// </summary>
public sealed class EventChainDetector
{
    private static readonly ThreadLocal<Stack<string>> EventStack = 
        new ThreadLocal<Stack<string>>(() => new Stack<string>());

    private const int MaxChainDepth = 10;

    /// <summary>
    /// 开始追踪事件
    /// </summary>
    public static IDisposable BeginTrack(string eventId)
    {
        var stack = EventStack.Value;

        if (stack.Contains(eventId))
        {
            var chain = string.Join(" -> ", stack.Reverse().Select(s => s)) 
                + " -> " + eventId;
            Log.Warning($"Potential event loop detected: {chain}");
        }

        if (stack.Count >= MaxChainDepth)
        {
            throw new InvalidOperationException(
                $"Event chain too deep (max: {MaxChainDepth}). " +
                $"Chain: {string.Join(" -> ", stack.Reverse())} -> {eventId}"
            );
        }

        stack.Push(eventId);
        return new EventTracker(eventId);
    }

    private sealed class EventTracker : IDisposable
    {
        private readonly string m_EventId;

        public EventTracker(string eventId)
        {
            m_EventId = eventId;
        }

        public void Dispose()
        {
            EventStack.Value.Pop();
        }
    }
}
```

**使用示例**：
```csharp
public class EventManager
{
    public void Fire(object sender, GameEventArgs e)
    {
        using (EventChainDetector.BeginTrack(e.Id))
        {
            // 事件处理
            m_EventPool.Fire(sender, e);
        }
    }
}
```

---

## 最佳实践

### 1. 事件 ID 管理

**原则**：集中管理，避免字符串硬编码

```csharp
// ? 集中管理事件 ID
public static class EventIds
{
    // 玩家相关
    public const string PlayerLevelUp = nameof(PlayerLevelUp);
    public const string PlayerExpChanged = nameof(PlayerExpChanged);
    public const string PlayerHealthChanged = nameof(PlayerHealthChanged);

    // UI 相关
    public const string UIOpenComplete = nameof(UIOpenComplete);
    public const string UICloseComplete = nameof(UICloseComplete);

    // 背包相关
    public const string BagItemAdded = nameof(BagItemAdded);
    public const string BagItemRemoved = nameof(BagItemRemoved);
    public const string BagChanged = nameof(BagChanged);
}
```

**优点**：
- ?? 编译期检查（使用 nameof）
- ?? 集中管理，便于查找
- ?? 便于重构

---

### 2. 事件参数设计

**原则**：保持参数简洁，清晰表达事件含义

```csharp
// ? 清晰的事件参数设计
public sealed class PlayerLevelUpEventArgs : GameEventArgs
{
    /// <summary>事件 ID</summary>
    public static readonly string EventId = nameof(PlayerLevelUpEventArgs);

    /// <summary>玩家 ID</summary>
    public long PlayerId { get; set; }

    /// <summary>新等级</summary>
    public int NewLevel { get; set; }

    /// <summary>旧等级</summary>
    public int OldLevel { get; set; }

    /// <summary>获得的奖励</summary>
    public int RewardExp { get; set; }

    public override void Clear()
    {
        PlayerId = 0;
        NewLevel = 0;
        OldLevel = 0;
        RewardExp = 0;
    }

    public override string Id => EventId;

    /// <summary>工厂方法</summary>
    public static PlayerLevelUpEventArgs Create(
        long playerId,
        int newLevel,
        int oldLevel,
        int rewardExp)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.PlayerId = playerId;
        args.NewLevel = newLevel;
        args.OldLevel = oldLevel;
        args.RewardExp = rewardExp;
        return args;
    }
}

// ? 设计不好的参数（过于通用）
public sealed class GenericEventArgs : GameEventArgs
{
    public object Data1 { get; set; }
    public object Data2 { get; set; }
    public object Data3 { get; set; }
    // 何时应该使用 Data1 还是 Data2？不清楚
}
```

---

### 3. 订阅和取消订阅对称性

**原则**：订阅和取消订阅成对出现，保证平衡

```csharp
// ? 对称的生命周期管理
public class GameUI : MonoBehaviour
{
    private void OnEnable()
    {
        // 订阅所有事件
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(
            EventIds.PlayerLevelUp,
            OnPlayerLevelUp
        );
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(
            EventIds.PlayerHealthChanged,
            OnPlayerHealthChanged
        );
    }

    private void OnDisable()
    {
        // 取消订阅所有事件（需要顺序一致）
        GameEntry.GetComponent<EventComponent>().Unsubscribe(
            EventIds.PlayerLevelUp,
            OnPlayerLevelUp
        );
        GameEntry.GetComponent<EventComponent>().Unsubscribe(
            EventIds.PlayerHealthChanged,
            OnPlayerHealthChanged
        );
    }
}
```

**更好的方案**：使用事件订阅管理器
```csharp
// ? 自动管理的生命周期
public class GameUI : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
        m_EventSubscriber.CheckSubscribe(EventIds.PlayerHealthChanged, OnPlayerHealthChanged);
    }

    private void OnDisable()
    {
        // ? 一次性清理所有订阅
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }
}
```

---

### 4. 异步和同步事件的区分使用

**原则**：明确选择异步（Fire）还是同步（FireNow）分发

```csharp
// ? 异步分发：大多数场景
// 优点：避免栈溢出，处理顺序清晰
public class PlayerManager
{
    public void LevelUp()
    {
        m_Level++;
        var e = PlayerLevelUpEventArgs.Create(m_Id, m_Level, m_Level - 1, 100);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);  // 下一帧分发
    }
}

// ? 同步分发：需要立即得到结果的场景
// 用途：如需要在同一帧内立即知道处理结果
public class InputManager
{
    public void OnUIButtonClicked()
    {
        var e = UIButtonClickedEventArgs.Create("LevelUpButton");
        GameEntry.GetComponent<EventComponent>().FireNow(this, e);  // 立即分发
    }
}
```

---

### 5. 错误处理和日志

**原则**：在事件处理器中添加必要的错误处理

```csharp
// ? 完善的错误处理
private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    try
    {
        if (!(e is PlayerLevelUpEventArgs args))
        {
            Log.Error("Invalid event args type");
            return;
        }

        // 处理事件
        UpdateUILevel(args.NewLevel);
        PlayLevelUpAnimation();
    }
    catch (Exception ex)
    {
        Log.Error($"Error handling PlayerLevelUp event: {ex.Message}\n{ex.StackTrace}");
    }
}
```

---

### 6. 事件优先级管理

**原则**：明确事件处理的优先级顺序

```csharp
// 事件订阅顺序表示优先级（先订阅先执行）
public class GameInitializer
{
    public void InitializeEventHandlers()
    {
        var eventComponent = GameEntry.GetComponent<EventComponent>();

        // 最高优先级：系统事件处理
        eventComponent.CheckSubscribe(EventIds.PlayerLevelUp, SystemOnPlayerLevelUp);

        // 高优先级：UI 更新
        eventComponent.CheckSubscribe(EventIds.PlayerLevelUp, UIOnPlayerLevelUp);

        // 普通优先级：音效播放
        eventComponent.CheckSubscribe(EventIds.PlayerLevelUp, AudioOnPlayerLevelUp);

        // 低优先级：日志记录
        eventComponent.CheckSubscribe(EventIds.PlayerLevelUp, LoggingOnPlayerLevelUp);
    }
}
```

---

### 7. 事件系统性能优化

**原则**：在大量事件的场景下优化性能

```csharp
// ? 性能优化：使用对象池
public sealed class BagChangedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(BagChangedEventArgs);

    public override void Clear() { }
    public override string Id => EventId;

    // ? 使用对象池而非 new
    public static BagChangedEventArgs Create()
    {
        return ReferencePool.Acquire<BagChangedEventArgs>();
    }
}

// ? 触发事件时使用对象池
public void OnBagItemChange()
{
    var e = BagChangedEventArgs.Create();  // 从对象池获取
    GameEntry.GetComponent<EventComponent>().Fire(this, e);
    // 事件分发后自动回收到对象池
}

// ? 避免频繁创建事件参数
public void OnBagItemChange_Bad()
{
    var e = new BagChangedEventArgs();  // ? 频繁创建，增加 GC
    GameEntry.GetComponent<EventComponent>().Fire(this, e);
}
```

---

## 使用示例

### 完整的事件系统使用示例

```csharp
// 1. 定义事件参数
public sealed class BagItemAddedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(BagItemAddedEventArgs);

    public int ItemId { get; set; }
    public int Quantity { get; set; }

    public override void Clear()
    {
        ItemId = 0;
        Quantity = 0;
    }

    public override string Id => EventId;

    public static BagItemAddedEventArgs Create(int itemId, int quantity)
    {
        var args = ReferencePool.Acquire<BagItemAddedEventArgs>();
        args.ItemId = itemId;
        args.Quantity = quantity;
        return args;
    }
}

// 2. 发送事件
public class BagManager
{
    public void AddItem(int itemId, int quantity)
    {
        // 逻辑处理
        m_Items[itemId] = (m_Items.TryGetValue(itemId, out var count) ? count : 0) + quantity;

        // 发送事件
        var e = BagItemAddedEventArgs.Create(itemId, quantity);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// 3. 订阅和处理事件
public class BagUI : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            BagItemAddedEventArgs.EventId,
            OnBagItemAdded
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnBagItemAdded(object sender, GameEventArgs e)
    {
        if (!(e is BagItemAddedEventArgs args))
            return;

        // 更新 UI
        RefreshBagUI(args.ItemId, args.Quantity);
    }

    private void RefreshBagUI(int itemId, int quantity)
    {
        // UI 更新逻辑
    }
}
```

---

## 常见陷阱

### 陷阱 1：忘记取消订阅导致内存泄漏

```csharp
// ? 陷阱：忘记取消订阅
public class BadUI : MonoBehaviour
{
    private void Start()
    {
        GameEntry.GetComponent<EventComponent>().CheckSubscribe(
            EventIds.PlayerLevelUp,
            OnPlayerLevelUp
        );
        // ? 没有在 OnDestroy 中取消订阅
    }

    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        Debug.Log("Player level up!");
    }
}

// ? 正确做法
public class GoodUI : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        Debug.Log("Player level up!");
    }
}
```

---

### 陷阱 2：字符串 ID 拼写错误

```csharp
// ? 陷阱：拼写错误
GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    "PlayerLevelUp",  // 硬编码字符串
    OnPlayerLevelUp
);

GameEntry.GetComponent<EventComponent>().Fire(this,
    "PlayerLevelUpp"  // ? 拼写错误！
);

// ? 使用常量
public static class EventIds
{
    public const string PlayerLevelUp = nameof(PlayerLevelUp);
}

GameEntry.GetComponent<EventComponent>().CheckSubscribe(
    EventIds.PlayerLevelUp,
    OnPlayerLevelUp
);

GameEntry.GetComponent<EventComponent>().Fire(this, 
    new PlayerLevelUpEventArgs()
);
```

---

### 陷阱 3：事件处理器异常导致后续处理器无法执行

```csharp
// ? 陷阱：异常处理不当
private void OnPlayerLevelUp_Bad(object sender, GameEventArgs e)
{
    // 如果这里抛出异常，后续订阅者无法收到事件
    var itemId = int.Parse(e.ToString());  // 可能抛出异常
}

// ? 正确做法：添加异常处理
private void OnPlayerLevelUp_Good(object sender, GameEventArgs e)
{
    try
    {
        if (!(e is PlayerLevelUpEventArgs args))
            return;

        // 安全的处理
        UpdateUI(args.NewLevel);
    }
    catch (Exception ex)
    {
        Log.Error($"Error in OnPlayerLevelUp: {ex}");
    }
}
```

---

### 陷阱 4：混淆异步和同步事件

```csharp
// ? 陷阱：混淆异步和同步
void TestEventOrder()
{
    GameEntry.GetComponent<EventComponent>().Fire(this, eventA);  // 下一帧执行
    GameEntry.GetComponent<EventComponent>().FireNow(this, eventB);  // 立即执行

    // 执行顺序：eventB -> ... (下一帧) ... -> eventA
    // 可能导致调试困难
}

// ? 正确做法：保持一致
void TestEventOrder_Good()
{
    // 都用异步
    GameEntry.GetComponent<EventComponent>().Fire(this, eventA);
    GameEntry.GetComponent<EventComponent>().Fire(this, eventB);

    // 都用同步
    GameEntry.GetComponent<EventComponent>().FireNow(this, eventA);
    GameEntry.GetComponent<EventComponent>().FireNow(this, eventB);
}
```

---

### 陷阱 5：事件参数重复使用

```csharp
// ? 陷阱：重复使用事件参数
public class BadEventManager
{
    private PlayerLevelUpEventArgs m_CachedEventArgs = 
        PlayerLevelUpEventArgs.Create(0, 0, 0, 0);

    public void OnPlayerLevelUp(long playerId, int newLevel)
    {
        m_CachedEventArgs.PlayerId = playerId;
        m_CachedEventArgs.NewLevel = newLevel;

        GameEntry.GetComponent<EventComponent>().Fire(this, m_CachedEventArgs);
        // ? 事件是异步分发，参数可能在下一帧被修改
    }
}

// ? 正确做法：每次创建新参数
public class GoodEventManager
{
    public void OnPlayerLevelUp(long playerId, int newLevel, int oldLevel)
    {
        var e = PlayerLevelUpEventArgs.Create(playerId, newLevel, oldLevel, 100);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
        // ? 参数在对象池管理下安全
    }
}
```

---

## 性能优化

### 1. 对象池优化

```csharp
// ? 使用对象池减少 GC
public sealed class HighFrequencyEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(HighFrequencyEventArgs);

    public float Value { get; set; }

    public override void Clear() => Value = 0;
    public override string Id => EventId;

    public static HighFrequencyEventArgs Create(float value)
    {
        var args = ReferencePool.Acquire<HighFrequencyEventArgs>();
        args.Value = value;
        return args;
    }
}

// 每帧触发多次，对象池避免频繁分配
public void OnUpdate()
{
    for (int i = 0; i < 100; i++)
    {
        var e = HighFrequencyEventArgs.Create(Time.deltaTime);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
        // 事件分发后自动回收
    }
}
```

---

### 2. 事件订阅者缓存

```csharp
// ? 缓存 EventComponent 引用
public class OptimizedSubscriber : MonoBehaviour
{
    private EventComponent m_EventComponent;

    private void OnEnable()
    {
        // 缓存引用
        if (m_EventComponent == null)
            m_EventComponent = GameEntry.GetComponent<EventComponent>();

        m_EventComponent.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
    }
}
```

---

### 3. 批量事件处理

```csharp
// ? 批量处理而非频繁小事件
// ? 不好的做法：频繁触发小事件
public void OnItemCountChange_Bad()
{
    // 每次数量变化都触发
    GameEntry.GetComponent<EventComponent>().Fire(this, 
        ItemCountChangedEventArgs.Create(m_ItemId, m_Count));
}

// ? 好的做法：批量处理
public void AddMultipleItems(List<(int itemId, int count)> items)
{
    foreach (var (itemId, count) in items)
    {
        m_Bag[itemId] = (m_Bag.TryGetValue(itemId, out var c) ? c : 0) + count;
    }

    // 一次性触发事件
    var e = BagBatchChangedEventArgs.Create(items);
    GameEntry.GetComponent<EventComponent>().Fire(this, e);
}
```

---

### 4. 事件处理器性能监控

```csharp
// ? 监控事件处理器的执行时间
public sealed class PerformanceMonitoringEventHandler
{
    private readonly EventHandler<GameEventArgs> m_Handler;
    private readonly string m_EventId;
    private const long SlowThresholdMs = 16;  // 超过 16ms 判定为慢

    public PerformanceMonitoringEventHandler(
        string eventId,
        EventHandler<GameEventArgs> handler)
    {
        m_EventId = eventId;
        m_Handler = handler;
    }

    public void Invoke(object sender, GameEventArgs e)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            m_Handler(sender, e);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowThresholdMs)
            {
                Log.Warning(
                    $"Slow event handler: {m_EventId} " +
                    $"({watch.ElapsedMilliseconds}ms)"
                );
            }
        }
    }
}
```

---

## 总结

### 核心要点

| 方面 | 推荐做法 | 避免 |
|------|--------|------|
| **事件 ID** | 使用常量集中管理 | 硬编码字符串 |
| **订阅生命周期** | 使用 UIEventSubscriber 或自动管理 | 手动管理导致遗漏 |
| **事件参数** | 每次创建新对象，使用对象池 | 重复使用同一对象 |
| **异步 vs 同步** | 统一风格，默认用异步 | 混淆使用 |
| **错误处理** | 在处理器中添加 try-catch | 忽视异常 |
| **性能** | 使用对象池，批量处理 | 频繁小事件分配 |
| **优先级** | 明确定义处理顺序 | 依赖隐含顺序 |

### 新员工入门清单

1. ? 了解事件系统基本架构
2. ? 学会定义事件参数（继承 GameEventArgs）
3. ? 学会发送事件（使用 Fire 或 FireNow）
4. ? 学会订阅事件（使用 CheckSubscribe）
5. ? 学会取消订阅（使用 Unsubscribe 或 UIEventSubscriber）
6. ? 理解对象池机制
7. ? 掌握最佳实践，避免常见陷阱
8. ? 进行性能优化

---

## 参考资源

- **GameFrameX 官方文档**：https://gameframex.doc.alianblank.com/
- **GitHub 仓库**：https://github.com/GameFrameX
- **相关代码**：
  - EventComponent: `Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/EventComponent.cs`
  - EventManager: `Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/Event/EventManager.cs`
  - UIEventSubscriber: `Packages/com.gameframex.unity.ui@f8e41afb311e/Runtime/UIEventSubscriber.cs`

---

**文档版本**：1.0  
**更新日期**：2024  
**适用版本**：GameFrameX Latest
