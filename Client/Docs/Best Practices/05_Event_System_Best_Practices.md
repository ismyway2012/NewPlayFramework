# 事件系统（Event System）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [核心概念](#核心概念)
3. [事件的生命周期](#事件的生命周期)
4. [最佳实践](#最佳实践)
5. [代码示例](#代码示例)
6. [性能优化](#性能优化)
7. [常见问题](#常见问题)

## 系统概述

事件系统（Event System）是GameFrameX框架用于实现模块间解耦通信的核心系统。它基于观察者模式实现，支持事件订阅、发布和自定义事件处理。

### 主要特点
- **类型安全**: 利用泛型实现类型安全的事件
- **解耦通信**: 实现模块间的松耦合
- **灵活扩展**: 支持自定义事件类型
- **高性能**: 优化的事件分发机制
- **内存管理**: 自动事件参数池管理

## 核心概念

### 事件管理器接口
```csharp
public interface IEventManager
{
    // 订阅事件
    void Subscribe<T>(EventHandler<T> handler) where T : EventArgs;
    
    // 取消订阅
    void Unsubscribe<T>(EventHandler<T> handler) where T : EventArgs;
    
    // 发布事件
    void Fire<T>(object sender, T args) where T : EventArgs;
    
    // 检查是否有订阅者
    bool HasSubscriber<T>() where T : EventArgs;
}
```

### 事件参数基类
所有自定义事件都应继承自EventArgs。

```csharp
public class GameEventArgs : EventArgs
{
    public int EventId { get; set; }
    public object Data { get; set; }
}
```

### 事件处理委托
```csharp
public delegate void EventHandler<T>(object sender, T args) where T : EventArgs;
```

## 事件的生命周期

### 完整的事件流程
```
1. 定义事件类 (继承EventArgs)
   ↓
2. 声明事件处理方法
   ↓
3. 订阅事件 (EventManager.Subscribe)
   ↓
4. 发布事件 (EventManager.Fire)
   ↓
5. 事件处理方法被调用
   ↓
6. 取消订阅 (EventManager.Unsubscribe)
```

## 最佳实践

### 1. 事件类的设计

#### 1.1 清晰的事件命名规范
```csharp
// 推荐：事件命名清晰指示发生了什么
public class PlayerSpawnedEventArgs : EventArgs
{
    public int PlayerId { get; set; }
    public Vector3 SpawnPosition { get; set; }
}

public class PlayerDeadEventArgs : EventArgs
{
    public int PlayerId { get; set; }
    public int KillerId { get; set; }
    public string DeathReason { get; set; }
}

public class PlayerHealthChangedEventArgs : EventArgs
{
    public int PlayerId { get; set; }
    public int OldHealth { get; set; }
    public int NewHealth { get; set; }
}

// 不推荐：命名模糊
public class EventArgs1 : EventArgs { }
public class GameEvent : EventArgs { }
```

#### 1.2 事件数据的完整性
```csharp
// 推荐：事件包含所有必要信息，使用者不需要查询其他数据
public class GameOverEventArgs : EventArgs
{
    public int WinnerId { get; set; }
    public int[] AllPlayerIds { get; set; }
    public Dictionary<int, int> FinalScores { get; set; }
    public float GameDuration { get; set; }
    public DateTime GameEndTime { get; set; }
}

// 不推荐：事件只包含最少信息，使用者需要查询其他数据
public class GameOverEventArgs : EventArgs
{
    public int WinnerId { get; set; }
}
```

#### 1.3 使用特定的事件类
```csharp
// 推荐：为不同的事件创建特定的类
public class ItemPickedUpEventArgs : EventArgs
{
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
}

public class ItemUsedEventArgs : EventArgs
{
    public int ItemId { get; set; }
    public int UserId { get; set; }
}

// 不推荐：使用通用的事件类
public class ItemEventArgs : EventArgs
{
    public int ItemId { get; set; }
    public string EventType { get; set; }
    public object Data { get; set; }
}
```

### 2. 事件的订阅和取消

#### 2.1 正确的订阅方式
```csharp
public class PlayerController : MonoBehaviour
{
    private void OnEnable()
    {
        // 在启用时订阅事件
        var eventManager = GameEntry.GetComponent<EventComponent>();
        eventManager.Subscribe<PlayerDeadEventArgs>(OnPlayerDead);
        eventManager.Subscribe<GameOverEventArgs>(OnGameOver);
    }
    
    private void OnDisable()
    {
        // 在禁用时取消订阅
        var eventManager = GameEntry.GetComponent<EventComponent>();
        eventManager.Unsubscribe<PlayerDeadEventArgs>(OnPlayerDead);
        eventManager.Unsubscribe<GameOverEventArgs>(OnGameOver);
    }
    
    private void OnPlayerDead(PlayerDeadEventArgs args)
    {
        Log.Info($"Player {args.PlayerId} is dead");
    }
    
    private void OnGameOver(GameOverEventArgs args)
    {
        Log.Info($"Game over! Winner: {args.WinnerId}");
    }
}
```

#### 2.2 使用事件类管理订阅
```csharp
// 推荐：创建事件管理类集中处理事件订阅
public class EventSubscriptionManager
{
    private EventComponent m_EventComponent;
    private List<(Type, Delegate)> m_Subscriptions = new List<(Type, Delegate)>();
    
    public void Initialize(EventComponent eventComponent)
    {
        m_EventComponent = eventComponent;
    }
    
    public void SubscribeEvent<T>(EventHandler<T> handler) where T : EventArgs
    {
        m_EventComponent.Subscribe(handler);
        m_Subscriptions.Add((typeof(T), handler));
    }
    
    public void UnsubscribeAllEvents()
    {
        foreach (var (eventType, handler) in m_Subscriptions)
        {
            // 使用反射取消所有订阅
            typeof(EventComponent)
                .GetMethod("Unsubscribe")
                .MakeGenericMethod(eventType)
                .Invoke(m_EventComponent, new object[] { handler });
        }
        m_Subscriptions.Clear();
    }
}
```

### 3. 事件的发布

#### 3.1 在正确的时机发布事件
```csharp
public class PlayerEntity : EntityLogic
{
    private int m_Health;
    private int m_MaxHealth = 100;
    private EventComponent m_EventComponent;
    
    public override void OnInit()
    {
        m_EventComponent = GameEntry.GetComponent<EventComponent>();
        m_Health = m_MaxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        int oldHealth = m_Health;
        m_Health -= damage;
        
        // 发布生命值改变事件
        m_EventComponent.Fire(this, new PlayerHealthChangedEventArgs
        {
            PlayerId = Id,
            OldHealth = oldHealth,
            NewHealth = m_Health
        });
        
        // 在生命值为0时发布死亡事件
        if (m_Health <= 0)
        {
            m_EventComponent.Fire(this, new PlayerDeadEventArgs
            {
                PlayerId = Id,
                DeathReason = "Health depleted"
            });
        }
    }
}
```

#### 3.2 避免过度发布事件
```csharp
// 不推荐：每帧都发布事件
public override void OnUpdate()
{
    // 每帧发布位置改变事件
    m_EventComponent.Fire(this, new PlayerPositionChangedEventArgs
    {
        Position = transform.position
    });
}

// 推荐：只在值真正改变时才发布
private Vector3 m_LastPosition;
private const float POSITION_CHANGE_THRESHOLD = 0.01f;

public override void OnUpdate()
{
    if (Vector3.Distance(transform.position, m_LastPosition) > POSITION_CHANGE_THRESHOLD)
    {
        m_LastPosition = transform.position;
        m_EventComponent.Fire(this, new PlayerPositionChangedEventArgs
        {
            Position = transform.position
        });
    }
}
```

### 4. 事件的组织结构

#### 4.1 集中管理事件定义
```csharp
// 推荐：创建事件命名空间和类
namespace GameFrameX.Events
{
    // 玩家相关事件
    namespace Player
    {
        public class PlayerSpawnedEventArgs : EventArgs { }
        public class PlayerDeadEventArgs : EventArgs { }
        public class PlayerHealthChangedEventArgs : EventArgs { }
    }
    
    // 敌人相关事件
    namespace Enemy
    {
        public class EnemySpawnedEventArgs : EventArgs { }
        public class EnemyDeadEventArgs : EventArgs { }
    }
    
    // 游戏相关事件
    namespace Game
    {
        public class GameStartedEventArgs : EventArgs { }
        public class GameOverEventArgs : EventArgs { }
    }
}
```

#### 4.2 事件定义文件结构
```
Assets/
├── Scripts/
│   └── Events/
│       ├── PlayerEvents.cs
│       ├── EnemyEvents.cs
│       ├── GameEvents.cs
│       └── UIEvents.cs
```

### 5. 事件与其他系统的协作

#### 5.1 事件驱动的流程系统
```csharp
public class GamePlayProcedure : ProcedureBase
{
    private EventComponent m_EventComponent;
    private bool m_IsGameOver = false;
    
    public override void OnEnter()
    {
        m_EventComponent = GameEntry.GetComponent<EventComponent>();
        
        // 订阅游戏事件
        m_EventComponent.Subscribe<PlayerDeadEventArgs>(OnPlayerDead);
        m_EventComponent.Subscribe<GameOverEventArgs>(OnGameOver);
        
        // 发布游戏开始事件
        m_EventComponent.Fire(this, new GameStartedEventArgs());
    }
    
    private void OnPlayerDead(PlayerDeadEventArgs args)
    {
        Log.Info($"Player {args.PlayerId} died");
        CheckGameOverCondition();
    }
    
    private void OnGameOver(GameOverEventArgs args)
    {
        m_IsGameOver = true;
        ChangeState<ResultProcedure>();
    }
    
    public override void OnLeave()
    {
        m_EventComponent.Unsubscribe<PlayerDeadEventArgs>(OnPlayerDead);
        m_EventComponent.Unsubscribe<GameOverEventArgs>(OnGameOver);
    }
}
```

#### 5.2 事件驱动的UI更新
```csharp
public class UIHealthDisplay : MonoBehaviour
{
    private Text m_HealthText;
    private EventComponent m_EventComponent;
    
    private void OnEnable()
    {
        m_EventComponent = GameEntry.GetComponent<EventComponent>();
        m_EventComponent.Subscribe<PlayerHealthChangedEventArgs>(OnHealthChanged);
    }
    
    private void OnDisable()
    {
        m_EventComponent.Unsubscribe<PlayerHealthChangedEventArgs>(OnHealthChanged);
    }
    
    private void OnHealthChanged(PlayerHealthChangedEventArgs args)
    {
        m_HealthText.text = $"Health: {args.NewHealth}/{maxHealth}";
        
        // 可以添加动画或特效
        if (args.NewHealth < args.OldHealth)
        {
            PlayDamageAnimation();
        }
    }
}
```

## 代码示例

### 示例1：完整的事件系统使用
```csharp
// 定义事件
public class LevelCompleteEventArgs : EventArgs
{
    public int LevelId { get; set; }
    public float CompletionTime { get; set; }
    public int Score { get; set; }
}

public class LevelFailedEventArgs : EventArgs
{
    public int LevelId { get; set; }
    public string FailReason { get; set; }
}

// 发布事件
public class LevelManager
{
    private EventComponent m_EventComponent;
    private float m_LevelStartTime;
    
    public void StartLevel(int levelId)
    {
        m_LevelStartTime = Time.time;
    }
    
    public void CompleteLevel(int levelId, int score)
    {
        float completionTime = Time.time - m_LevelStartTime;
        
        m_EventComponent.Fire(this, new LevelCompleteEventArgs
        {
            LevelId = levelId,
            CompletionTime = completionTime,
            Score = score
        });
    }
    
    public void FailLevel(int levelId, string reason)
    {
        m_EventComponent.Fire(this, new LevelFailedEventArgs
        {
            LevelId = levelId,
            FailReason = reason
        });
    }
}

// 订阅事件
public class UILevelComplete : MonoBehaviour
{
    private EventComponent m_EventComponent;
    
    private void OnEnable()
    {
        m_EventComponent = GameEntry.GetComponent<EventComponent>();
        m_EventComponent.Subscribe<LevelCompleteEventArgs>(OnLevelComplete);
        m_EventComponent.Subscribe<LevelFailedEventArgs>(OnLevelFailed);
    }
    
    private void OnDisable()
    {
        m_EventComponent.Unsubscribe<LevelCompleteEventArgs>(OnLevelComplete);
        m_EventComponent.Unsubscribe<LevelFailedEventArgs>(OnLevelFailed);
    }
    
    private void OnLevelComplete(LevelCompleteEventArgs args)
    {
        ShowCompletionUI(args.Score, args.CompletionTime);
    }
    
    private void OnLevelFailed(LevelFailedEventArgs args)
    {
        ShowFailureUI(args.FailReason);
    }
}
```

### 示例2：事件链式处理
```csharp
public class EventChainHandler
{
    private EventComponent m_EventComponent;
    private Dictionary<Type, List<Action>> m_EventChain = new Dictionary<Type, List<Action>>();
    
    public void RegisterEventChain<T>(params Action[] handlers) where T : EventArgs
    {
        var eventType = typeof(T);
        if (!m_EventChain.ContainsKey(eventType))
        {
            m_EventChain[eventType] = new List<Action>();
        }
        
        foreach (var handler in handlers)
        {
            m_EventChain[eventType].Add(handler);
        }
    }
    
    public void ExecuteEventChain<T>() where T : EventArgs
    {
        var eventType = typeof(T);
        if (m_EventChain.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler?.Invoke();
            }
        }
    }
}

// 使用示例
public void SetupGameStartChain()
{
    var chainHandler = new EventChainHandler();
    
    chainHandler.RegisterEventChain<GameStartedEventArgs>(
        LoadGameResources,
        InitializeGameState,
        SpawnPlayer,
        StartGameLogic
    );
}
```

### 示例3：事件优先级处理
```csharp
public class PriorityEventHandler<T> where T : EventArgs
{
    private class PriorityHandler
    {
        public int Priority { get; set; }
        public EventHandler<T> Handler { get; set; }
    }
    
    private List<PriorityHandler> m_Handlers = new List<PriorityHandler>();
    
    public void Subscribe(EventHandler<T> handler, int priority = 0)
    {
        m_Handlers.Add(new PriorityHandler { Handler = handler, Priority = priority });
        m_Handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }
    
    public void Fire(object sender, T args)
    {
        foreach (var handler in m_Handlers)
        {
            handler.Handler?.Invoke(sender, args);
        }
    }
}

// 使用示例
private PriorityEventHandler<PlayerDamagedEventArgs> m_DamageHandler = 
    new PriorityEventHandler<PlayerDamagedEventArgs>();

public void SetupDamageHandlers()
{
    // 优先级高的处理器先执行
    m_DamageHandler.Subscribe(ApplyDefense, 100);      // 先处理防御
    m_DamageHandler.Subscribe(CalculateDamage, 50);    // 再计算伤害
    m_DamageHandler.Subscribe(ApplyDamage, 0);         // 最后应用伤害
}
```

## 性能优化

### 1. 减少事件分发开销
```csharp
// 推荐：检查是否有订阅者再发布
private void TakeDamage(int damage)
{
    var eventComponent = GameEntry.GetComponent<EventComponent>();
    
    if (eventComponent.HasSubscriber<PlayerDamagedEventArgs>())
    {
        eventComponent.Fire(this, new PlayerDamagedEventArgs { Damage = damage });
    }
}
```

### 2. 使用事件对象池
```csharp
// 框架通常会自动处理事件参数的对象池
// 开发者需要注意不要在事件处理完成后继续使用事件参数
private void OnPlayerDamaged(PlayerDamagedEventArgs args)
{
    // 使用args数据
    int damage = args.Damage;
    
    // 不要保存args的引用到其他地方
}
```

### 3. 避免在事件处理中再次发布事件
```csharp
// 不推荐：嵌套发布事件
private void OnPlayerDamaged(PlayerDamagedEventArgs args)
{
    var eventComponent = GameEntry.GetComponent<EventComponent>();
    eventComponent.Fire(this, new PlayerHealthChangedEventArgs());
    eventComponent.Fire(this, new GameStateChangedEventArgs());
}

// 推荐：在合适的地方统一发布
public void ProcessDamage(int damage)
{
    TakeDamage(damage);
    // 只发布一个综合事件
    m_EventComponent.Fire(this, new PlayerStateChangedEventArgs());
}
```

## 常见问题

### Q1: 如何确保事件订阅不会造成内存泄漏？

**A:** 始终在卸载或禁用时取消订阅：
```csharp
private void OnDestroy()
{
    m_EventComponent.Unsubscribe<PlayerDeadEventArgs>(OnPlayerDead);
}
```

### Q2: 事件的执行顺序如何保证？

**A:** 事件执行顺序通常按订阅顺序执行，如需特定顺序，使用优先级机制。

### Q3: 如何调试事件问题？

**A:** 添加日志记录：
```csharp
private void OnPlayerDead(PlayerDeadEventArgs args)
{
    Log.Info($"Player dead event received: PlayerId={args.PlayerId}");
}
```

### Q4: 是否应该为每个事件创建一个类？

**A:** 是的。为每个事件创建特定类有以下好处：
- 类型安全
- 易于维护
- 易于调试
- 易于查找事件使用处

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
