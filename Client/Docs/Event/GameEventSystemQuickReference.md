# 游戏事件系统快速参考卡

## ?? 快速开始（5分钟）

### 1. 定义事件参数
```csharp
public sealed class PlayerLevelUpEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(PlayerLevelUpEventArgs);
    
    public int NewLevel { get; set; }
    
    public override void Clear() => NewLevel = 0;
    public override string Id => EventId;
    
    public static PlayerLevelUpEventArgs Create(int level)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.NewLevel = level;
        return args;
    }
}
```

### 2. 发送事件
```csharp
var e = PlayerLevelUpEventArgs.Create(5);
GameEntry.GetComponent<EventComponent>().Fire(this, e);
```

### 3. 接收事件
```csharp
private UIEventSubscriber m_EventSubscriber;

private void OnEnable()
{
    m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
    m_EventSubscriber.CheckSubscribe(PlayerLevelUpEventArgs.EventId, OnPlayerLevelUp);
}

private void OnDisable()
{
    m_EventSubscriber.UnSubscribeAll();
    ReferencePool.Release(m_EventSubscriber);
}

private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    if (!(e is PlayerLevelUpEventArgs args)) return;
    Debug.Log($"Player level up to {args.NewLevel}");
}
```

---

## ?? 核心 API 速查

### EventComponent（主要接口）

```csharp
// 查询
int EventHandlerCount                          // 处理器总数
int EventCount                                 // 待处理事件数
int Count(string eventId)                      // 指定事件的处理器数
bool Check(string eventId, handler)            // 检查处理器是否存在

// 订阅/取消
void CheckSubscribe(string eventId, handler)   // 订阅（自动检测重复）
void Subscribe(string eventId, handler)        // [弃用] 直接订阅
void Unsubscribe(string eventId, handler)      // 取消订阅

// 发送事件
void Fire(object sender, GameEventArgs e)      // 异步分发（推荐）
void Fire(object sender, string eventId)       // 异步分发空事件
void FireNow(object sender, GameEventArgs e)   // 同步立即分发

// 高级
void SetDefaultHandler(handler)                // 设置默认处理器
```

### UIEventSubscriber（管理器）

```csharp
void CheckSubscribe(string eventId, handler)   // 订阅
void UnSubscribe(string eventId, handler)      // 取消单个订阅
void Fire(string eventId, GameEventArgs e)     // 直接触发
void UnSubscribeAll(List<string> ignoreList)   // 取消所有订阅
```

---

## ? 常用代码片段

### 基础订阅（推荐）

```csharp
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
    if (!(e is PlayerLevelUpEventArgs args)) return;
    // 处理事件
}
```

### 多事件订阅

```csharp
private void OnEnable()
{
    m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
    m_EventSubscriber.CheckSubscribe(EventIds.Event1, OnEvent1);
    m_EventSubscriber.CheckSubscribe(EventIds.Event2, OnEvent2);
    m_EventSubscriber.CheckSubscribe(EventIds.Event3, OnEvent3);
}
```

### 条件订阅

```csharp
public void EnableMonitoring()
{
    m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
    m_EventSubscriber.CheckSubscribe(EventIds.HealthChanged, OnHealthChanged);
}

public void DisableMonitoring()
{
    GameEntry.GetComponent<EventComponent>().Unsubscribe(
        EventIds.HealthChanged, 
        OnHealthChanged
    );
}
```

### 异步vs同步

```csharp
// ? 异步（推荐，默认）- 下一帧执行
var e = PlayerLevelUpEventArgs.Create(5);
GameEntry.GetComponent<EventComponent>().Fire(this, e);

// 同步 - 立即执行
GameEntry.GetComponent<EventComponent>().FireNow(this, e);
```

---

## ?? 事件 ID 管理

### 集中定义（最佳实践）

```csharp
public static class EventIds
{
    // 玩家事件
    public const string PlayerLevelUp = nameof(PlayerLevelUp);
    public const string PlayerDead = nameof(PlayerDead);
    public const string PlayerHealthChanged = nameof(PlayerHealthChanged);
    
    // UI 事件
    public const string UIOpened = nameof(UIOpened);
    public const string UIClosed = nameof(UIClosed);
    
    // 战斗事件
    public const string EnemyDefeated = nameof(EnemyDefeated);
    public const string AttackLanded = nameof(AttackLanded);
}
```

### 使用方法

```csharp
// ? 推荐
m_EventSubscriber.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);

// ? 避免
m_EventSubscriber.CheckSubscribe("PlayerLevelUp", OnPlayerLevelUp);  // 硬编码
m_EventSubscriber.CheckSubscribe("PlayerLevelUpp", OnPlayerLevelUp); // 易错
```

---

## ?? 事件参数模板

### 标准模板

```csharp
public sealed class YourEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(YourEventArgs);
    
    // 字段
    public int SomeData { get; set; }
    
    public override void Clear()
    {
        SomeData = 0;
    }
    
    public override string Id => EventId;
    
    public static YourEventArgs Create(int someData)
    {
        var args = ReferencePool.Acquire<YourEventArgs>();
        args.SomeData = someData;
        return args;
    }
}
```

### 空事件模板（无参数）

```csharp
public sealed class GameStartedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(GameStartedEventArgs);
    
    public override void Clear() { }  // 无数据
    public override string Id => EventId;
    
    public static GameStartedEventArgs Create()
    {
        return ReferencePool.Acquire<GameStartedEventArgs>();
    }
}
```

### 复杂数据模板

```csharp
public sealed class ItemUsedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(ItemUsedEventArgs);
    
    // 多个字段
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public Vector3 Position { get; set; }
    public List<int> AffectedTargets { get; set; } = new List<int>();
    
    public override void Clear()
    {
        ItemId = 0;
        Quantity = 0;
        Position = Vector3.zero;
        AffectedTargets.Clear();
    }
    
    public override string Id => EventId;
    
    public static ItemUsedEventArgs Create(int itemId, int qty, Vector3 pos)
    {
        var args = ReferencePool.Acquire<ItemUsedEventArgs>();
        args.ItemId = itemId;
        args.Quantity = qty;
        args.Position = pos;
        return args;
    }
}
```

---

## ? 常见错误

| 错误 | ? 错误做法 | ? 正确做法 |
|------|-----------|----------|
| **拼写错误** | `CheckSubscribe("PlayLevelUp", ...)` | `CheckSubscribe(EventIds.PlayerLevelUp, ...)` |
| **内存泄漏** | 订阅后不取消 | `OnDisable()` 中调用 `UnSubscribeAll()` |
| **类型不匹配** | 处理器参数类型错误 | 检查 `e is PlayerLevelUpEventArgs` |
| **异常传播** | 处理器中未处理异常 | 添加 `try-catch` |
| **重复订阅** | 相同处理器订阅多次 | 使用 `CheckSubscribe` |
| **参数重用** | 异步后重用参数 | 每次创建新参数 |
| **忘记取消** | `OnEnable` 订阅但 `OnDisable` 不取消 | 确保配对 |

---

## ?? 性能提示

### ? 做

- ? 使用对象池创建事件参数
- ? 异步分发（Fire）而非同步（FireNow）
- ? 批量处理高频事件
- ? 及时取消不需要的订阅
- ? 使用 EventIds 常量

### ? 不做

- ? `new` 关键字创建事件参数
- ? 在处理器中触发无限循环事件
- ? 大量 FireNow 同步调用
- ? 订阅后忘记取消
- ? 使用硬编码字符串事件 ID

---

## ?? 调试技巧

### 查看当前事件数

```csharp
var eventComponent = GameEntry.GetComponent<EventComponent>();
Debug.Log($"Total handlers: {eventComponent.EventHandlerCount}");
Debug.Log($"Pending events: {eventComponent.EventCount}");
Debug.Log($"Handlers for event: {eventComponent.Count(EventIds.PlayerLevelUp)}");
```

### 添加日志调试

```csharp
private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    Debug.Log($"[Event] PlayerLevelUp received from {sender}");
    
    if (!(e is PlayerLevelUpEventArgs args))
    {
        Debug.LogError("Invalid event type");
        return;
    }
    
    Debug.Log($"[Event] New level: {args.NewLevel}");
}
```

### 异常处理调试

```csharp
private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    try
    {
        if (!(e is PlayerLevelUpEventArgs args)) return;
        // 处理逻辑
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Event] Error in OnPlayerLevelUp: {ex.Message}");
        Debug.LogError(ex.StackTrace);
    }
}
```

---

## ?? UI 事件绑定

### 常见 UI 事件模式

```csharp
public class UIPanel : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    
    // OnEnable 时订阅
    private void OnEnable()
    {
        // 方法 1：自动管理
        if (m_EventSubscriber == null)
            m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        
        m_EventSubscriber.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
    }
    
    // OnDisable 时取消
    private void OnDisable()
    {
        m_EventSubscriber?.UnSubscribeAll();
    }
    
    // OnDestroy 时清理
    private void OnDestroy()
    {
        if (m_EventSubscriber != null)
        {
            ReferencePool.Release(m_EventSubscriber);
            m_EventSubscriber = null;
        }
    }
    
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        if (!(e is PlayerLevelUpEventArgs args)) return;
        UpdateDisplay(args.NewLevel);
    }
    
    private void UpdateDisplay(int level)
    {
        // UI 更新逻辑
    }
}
```

---

## ?? 跨模块通信

### 模块 A -> 事件 -> 模块 B

```csharp
// 模块 A：发送事件
public class PlayerManager : MonoBehaviour
{
    public void LevelUp()
    {
        var e = PlayerLevelUpEventArgs.Create(5);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// 模块 B：接收事件
public class AchievementSystem : MonoBehaviour
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
        if (!(e is PlayerLevelUpEventArgs args)) return;
        CheckLevelUpAchievements(args.NewLevel);
    }
    
    private void CheckLevelUpAchievements(int level)
    {
        // 检查成就逻辑
    }
}
```

---

## ?? 学习路径

### 初级（第一天）
1. ? 理解发布-订阅模式
2. ? 定义简单事件参数
3. ? 发送和接收事件
4. ? 掌握订阅/取消订阅

### 中级（第二天）
1. ? 使用 UIEventSubscriber 管理生命周期
2. ? 处理多个事件
3. ? 添加错误处理
4. ? 使用 EventIds 常量

### 高级（第三天）
1. ? 事件优先级管理
2. ? 事件链（链式触发）
3. ? 性能优化（批量处理）
4. ? 调试和监控

---

## ?? 相关资源

| 资源 | 位置 |
|------|------|
| 完整技术文档 | `GameEventSystemTechnicalGuide.md` |
| 代码示例集 | `GameEventSystemCodeExamples.md` |
| EventComponent 源码 | `Packages/com.gameframex.unity.event@7937b4d92d98/Runtime/` |
| 官方文档 | https://gameframex.doc.alianblank.com/ |
| GitHub | https://github.com/GameFrameX |

---

## ?? 快速问答

**Q: Fire 和 FireNow 有什么区别？**  
A: Fire 在下一帧执行（异步），FireNow 立即执行（同步）。推荐使用 Fire。

**Q: 为什么要使用对象池？**  
A: 减少频繁分配内存，降低 GC 压力，提高性能。

**Q: 如何防止内存泄漏？**  
A: 在 OnDisable 中调用 `UnSubscribeAll()`，或使用 UIEventSubscriber 自动管理。

**Q: 可以在处理器中发送其他事件吗？**  
A: 可以，但要注意不要形成无限循环。

**Q: 支持事件优先级吗？**  
A: 原生不支持，但可以通过订阅顺序隐含控制优先级。

---

**版本**: 1.0 | **更新**: 2024 | **作者**: GameFrameX Team
