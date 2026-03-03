# 游戏事件系统代码审查清单

本清单用于审查代码是否遵循游戏事件系统的最佳实践。

---

## ?? 事件参数定义检查

### 代码结构

- [ ] 类继承自 `GameEventArgs`
- [ ] 类使用 `sealed` 修饰符
- [ ] 定义了 `static readonly string EventId`
- [ ] `EventId` 使用 `nameof` 操作符
- [ ] 实现了 `Clear()` 方法（清空所有字段）
- [ ] 实现了 `Id` 属性（返回 EventId）
- [ ] 定义了 `static Create()` 工厂方法
- [ ] `Create()` 使用 `ReferencePool.Acquire<T>()`

### 代码示例

```csharp
// ? 正确的事件参数定义
public sealed class PlayerLevelUpEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(PlayerLevelUpEventArgs);
    
    public int NewLevel { get; set; }
    public int OldLevel { get; set; }
    
    public override void Clear()
    {
        NewLevel = 0;
        OldLevel = 0;
    }
    
    public override string Id => EventId;
    
    public static PlayerLevelUpEventArgs Create(int newLevel, int oldLevel)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.NewLevel = newLevel;
        args.OldLevel = oldLevel;
        return args;
    }
}
```

### 检查清单

- [ ] 所有字段都在 `Clear()` 中被清空
- [ ] `Create()` 方法设置了所有必要字段
- [ ] 避免在字段中使用复杂对象（如 List）或谨慎处理
- [ ] 参数类是否公开（public）

---

## ?? 事件发送检查

### 代码结构

- [ ] 发送前创建了事件参数
- [ ] 使用 `ReferencePool.Acquire` 或 `Create()` 方法
- [ ] 调用 `GameEntry.GetComponent<EventComponent>().Fire()` 或 `FireNow()`
- [ ] 选择了正确的分发方式（Fire vs FireNow）

### 代码示例

```csharp
// ? 正确的事件发送
public class PlayerManager : MonoBehaviour
{
    public void LevelUp()
    {
        m_Level++;
        
        // 创建事件参数
        var e = PlayerLevelUpEventArgs.Create(m_Level, m_Level - 1);
        
        // 异步发送（推荐）
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// ? 常见错误
public void LevelUp_Bad()
{
    // ? 硬编码字符串
    GameEntry.GetComponent<EventComponent>().Fire(this, "PlayerLevelUp");
    
    // ? 直接 new（无对象池）
    var e = new PlayerLevelUpEventArgs();
    GameEntry.GetComponent<EventComponent>().Fire(this, e);
}
```

### 检查清单

- [ ] 事件发送前是否有必要的逻辑处理
- [ ] 是否使用了对象池
- [ ] 是否选择了合适的分发方式
- [ ] 是否需要立即返回结果（使用 FireNow）还是下一帧处理（Fire）

---

## ?? 事件订阅检查

### 代码结构

- [ ] 在 `OnEnable()` 中订阅
- [ ] 使用 `UIEventSubscriber` 或类似管理器
- [ ] 使用 `CheckSubscribe()` 而非 `Subscribe()`
- [ ] 使用 `EventIds` 常量而非硬编码字符串
- [ ] 处理器方法签名正确：`void Handler(object sender, GameEventArgs e)`

### 代码示例

```csharp
// ? 正确的订阅方式
public class GameUI : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    
    private void OnEnable()
    {
        // 创建管理器
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        
        // 订阅事件（使用常量）
        m_EventSubscriber.CheckSubscribe(
            EventIds.PlayerLevelUp,
            OnPlayerLevelUp
        );
    }
    
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        if (!(e is PlayerLevelUpEventArgs args))
            return;
        
        // 处理事件
    }
}

// ? 常见错误
private void OnEnable()
{
    // ? 没有使用管理器
    GameEntry.GetComponent<EventComponent>().CheckSubscribe(
        "PlayerLevelUp",  // ? 硬编码字符串
        OnPlayerLevelUp
    );
}
```

### 检查清单

- [ ] 是否在生命周期的正确位置订阅（通常 OnEnable）
- [ ] 是否使用了事件订阅管理器
- [ ] 是否使用常量而非硬编码字符串
- [ ] 处理器方法是否用 `try-catch` 保护

---

## ?? 事件取消订阅检查

### 代码结构

- [ ] 在 `OnDisable()` 中取消订阅
- [ ] 使用 `UnSubscribeAll()` 或 `Unsubscribe()`
- [ ] 释放事件订阅管理器回到对象池
- [ ] 在 `OnDestroy()` 中做最终清理

### 代码示例

```csharp
// ? 正确的取消订阅方式
public class GameUI : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    
    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
    }
    
    private void OnDisable()
    {
        // 取消所有订阅
        m_EventSubscriber.UnSubscribeAll();
        
        // 释放管理器
        ReferencePool.Release(m_EventSubscriber);
    }
    
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        // 处理
    }
}

// ? 常见错误
private void OnDisable()
{
    // ? 忘记取消订阅
    // 这导致内存泄漏
}

// ? 只取消部分订阅
private void OnDisable()
{
    GameEntry.GetComponent<EventComponent>().Unsubscribe(
        EventIds.PlayerLevelUp,
        OnPlayerLevelUp
    );
    // ? 如果有多个订阅，可能遗漏其他的
}
```

### 检查清单

- [ ] 是否确实在 `OnDisable()` 中取消了订阅
- [ ] 是否释放了事件订阅管理器
- [ ] 是否有可能在其他地方仍然持有订阅
- [ ] 对象销毁时是否正确清理

---

## ?? 事件处理器检查

### 代码结构

- [ ] 方法签名为 `private void Handler(object sender, GameEventArgs e)`
- [ ] 添加了 `try-catch` 异常处理
- [ ] 检查了事件参数类型
- [ ] 验证了参数数据有效性
- [ ] 避免在处理器中做过多工作

### 代码示例

```csharp
// ? 正确的事件处理器
private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    try
    {
        // 类型检查
        if (!(e is PlayerLevelUpEventArgs args))
        {
            Log.Warning("Invalid event type");
            return;
        }
        
        // 数据验证
        if (args.NewLevel <= 0)
        {
            Log.Warning("Invalid level");
            return;
        }
        
        // 处理事件
        UpdateUI(args.NewLevel);
    }
    catch (Exception ex)
    {
        Log.Error($"Error in OnPlayerLevelUp: {ex.Message}");
    }
}

// ? 常见错误
private void OnPlayerLevelUp(object sender, GameEventArgs e)
{
    // ? 没有类型检查
    var args = (PlayerLevelUpEventArgs)e;
    
    // ? 没有异常处理
    DoComplexLogic(args);
    
    // ? 做了太多工作（应该异步处理）
    for (int i = 0; i < 10000; i++)
    {
        ExpensiveOperation(i);
    }
}
```

### 检查清单

- [ ] 是否添加了 `try-catch` 块
- [ ] 是否检查了参数类型
- [ ] 是否验证了参数数据
- [ ] 处理器是否过于复杂
- [ ] 是否在处理器中引发了其他事件（可能的链式调用）
- [ ] 处理器是否会导致性能问题

---

## ?? 事件 ID 管理检查

### 代码结构

- [ ] 所有事件 ID 集中定义在一个类中
- [ ] 使用 `const string` 和 `nameof`
- [ ] 命名清晰，表明事件的来源
- [ ] 注释说明事件的含义
- [ ] 避免拼写错误

### 代码示例

```csharp
// ? 正确的事件 ID 管理
public static class EventIds
{
    // 玩家事件
    public const string PlayerLevelUp = nameof(PlayerLevelUp);
    public const string PlayerDead = nameof(PlayerDead);
    public const string PlayerHealthChanged = nameof(PlayerHealthChanged);
    
    // UI 事件
    public const string UIOpened = nameof(UIOpened);
    public const string UIClosed = nameof(UIClosed);
    
    // 背包事件
    public const string BagItemAdded = nameof(BagItemAdded);
    public const string BagItemRemoved = nameof(BagItemRemoved);
}

// ? 使用事件 ID
m_EventSubscriber.CheckSubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);

// ? 常见错误
// ? 硬编码字符串
m_EventSubscriber.CheckSubscribe("PlayerLevelUp", OnPlayerLevelUp);

// ? 分散定义
public const string EVENT_PLAYER_LEVEL_UP = "PlayerLevelUp";
// ... 另一个文件中 ...
public const string EVENT_PLAYER_LEVEL_UP = "PlayerLevelUp";  // 重复定义
```

### 检查清单

- [ ] 是否有一个统一的 EventIds 类
- [ ] 所有事件 ID 是否都在这个类中定义
- [ ] 是否使用了 `nameof` 避免拼写错误
- [ ] ID 命名是否清晰、一致
- [ ] 是否有文档说明各事件的用途

---

## ?? 性能优化检查

### 代码结构

- [ ] 事件参数使用了对象池
- [ ] 异步分发（Fire）而非同步（FireNow）
- [ ] 高频事件是否考虑了批量处理
- [ ] 不需要的订阅是否已取消
- [ ] 没有事件循环或无限递归

### 代码示例

```csharp
// ? 性能优化：使用对象池
var e = PlayerLevelUpEventArgs.Create(level);
GameEntry.GetComponent<EventComponent>().Fire(this, e);

// ? 性能问题：不使用对象池
var e = new PlayerLevelUpEventArgs();
GameEntry.GetComponent<EventComponent>().Fire(this, e);

// ? 性能优化：批量处理高频事件
public void OnUpdate()
{
    if (m_PositionChangedThisFrame)
    {
        var e = PlayerPositionChangedEventArgs.Create(transform.position);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// ? 性能问题：每帧多次触发相同事件
private void Update()
{
    // ? 每帧都触发
    var e = PlayerPositionChangedEventArgs.Create(transform.position);
    GameEntry.GetComponent<EventComponent>().Fire(this, e);
}

// ? 性能优化：条件订阅
public void EnableMonitoring()
{
    m_EventSubscriber.CheckSubscribe(EventIds.PositionChanged, OnPositionChanged);
}

public void DisableMonitoring()
{
    GameEntry.GetComponent<EventComponent>().Unsubscribe(
        EventIds.PositionChanged,
        OnPositionChanged
    );
}
```

### 检查清单

- [ ] 是否存在频繁触发的小事件（可考虑批量处理）
- [ ] 是否有不必要的订阅（不需要时应取消）
- [ ] 是否有可能的事件循环
- [ ] 处理器中是否有性能敏感的操作
- [ ] 内存分配是否会导致 GC

---

## ?? 线程安全检查

### 代码结构

- [ ] 事件发送可能来自多线程
- [ ] 异步分发（Fire）确保回调在主线程
- [ ] 同步分发（FireNow）仅在主线程使用
- [ ] 事件参数在发送前构建完全

### 代码示例

```csharp
// ? 线程安全：异步分发
public class NetworkManager
{
    private void OnNetworkDataReceived(byte[] data)
    {
        // 网络回调可能在子线程
        var e = NetworkDataReceivedEventArgs.Create(data);
        
        // 异步发送，回调会在主线程执行
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// ? 不安全：同步分发在子线程
private void OnNetworkDataReceived(byte[] data)
{
    var e = NetworkDataReceivedEventArgs.Create(data);
    
    // ? FireNow 在子线程中会不安全
    GameEntry.GetComponent<EventComponent>().FireNow(this, e);
}
```

### 检查清单

- [ ] 是否明确事件发送的来源（哪些线程）
- [ ] 是否选择了正确的分发方式
- [ ] 是否有多线程访问的事件参数
- [ ] 是否正确处理了线程同步

---

## ?? 文档和注释检查

### 代码结构

- [ ] 事件参数类有 XML 文档注释
- [ ] `Create()` 方法有文档
- [ ] 关键事件处理器有注释
- [ ] 复杂的事件逻辑有说明

### 代码示例

```csharp
// ? 完善的文档
/// <summary>
/// 玩家升级事件参数
/// </summary>
public sealed class PlayerLevelUpEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(PlayerLevelUpEventArgs);
    
    /// <summary>新等级</summary>
    public int NewLevel { get; set; }
    
    /// <summary>旧等级</summary>
    public int OldLevel { get; set; }
    
    public override void Clear()
    {
        NewLevel = 0;
        OldLevel = 0;
    }
    
    public override string Id => EventId;
    
    /// <summary>
    /// 创建玩家升级事件参数
    /// </summary>
    /// <param name="newLevel">新等级</param>
    /// <param name="oldLevel">旧等级</param>
    /// <returns>事件参数</returns>
    public static PlayerLevelUpEventArgs Create(int newLevel, int oldLevel)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.NewLevel = newLevel;
        args.OldLevel = oldLevel;
        return args;
    }
}

// ? 缺少文档
public sealed class XEventArgs : GameEventArgs
{
    // ...
}
```

### 检查清单

- [ ] 是否为事件参数类添加了 XML 文档
- [ ] 是否为公开方法添加了文档
- [ ] 是否有复杂事件流程的注释
- [ ] 文档是否准确无误

---

## ?? 测试检查

### 代码结构

- [ ] 事件参数是否有单元测试
- [ ] 订阅/取消订阅是否正确测试
- [ ] 事件处理器异常是否被捕获
- [ ] 对象池是否正确清理

### 代码示例

```csharp
[TestFixture]
public class EventSystemTests
{
    private EventComponent m_EventComponent;
    private int m_CallCount;
    
    [SetUp]
    public void Setup()
    {
        m_EventComponent = GameEntry.GetComponent<EventComponent>();
        m_CallCount = 0;
    }
    
    [Test]
    public void TestEventFire()
    {
        // 订阅
        m_EventComponent.CheckSubscribe(
            PlayerLevelUpEventArgs.EventId,
            (sender, e) => m_CallCount++
        );
        
        // 发送事件
        var args = PlayerLevelUpEventArgs.Create(5, 4);
        m_EventComponent.Fire(this, args);
        
        // 下一帧处理
        yield return null;
        
        // 验证
        Assert.AreEqual(1, m_CallCount);
    }
    
    [Test]
    public void TestUnsubscribe()
    {
        // 订阅后取消
        m_EventComponent.CheckSubscribe(
            PlayerLevelUpEventArgs.EventId,
            (sender, e) => m_CallCount++
        );
        
        m_EventComponent.Unsubscribe(
            PlayerLevelUpEventArgs.EventId,
            (sender, e) => m_CallCount++
        );
        
        // 发送事件，不应触发回调
        var args = PlayerLevelUpEventArgs.Create(5, 4);
        m_EventComponent.Fire(this, args);
        
        yield return null;
        
        // 验证
        Assert.AreEqual(0, m_CallCount);
    }
}
```

### 检查清单

- [ ] 是否有事件系统的单元测试
- [ ] 是否测试了订阅/取消订阅
- [ ] 是否测试了异常情况
- [ ] 是否测试了对象池是否正确回收

---

## ?? 集成检查清单

使用此清单验证整个事件系统的实现：

### 系统级别

- [ ] 是否定义了 EventIds 常量类
- [ ] 所有事件参数类是否都继承自 GameEventArgs
- [ ] 是否建立了清晰的事件架构文档
- [ ] 是否有事件系统的使用指南
- [ ] 团队是否理解并遵循最佳实践

### 代码质量

- [ ] 是否通过了代码审查
- [ ] 是否通过了静态分析工具（如 ReSharper）
- [ ] 是否有性能基准测试
- [ ] 是否有集成测试

### 项目健康

- [ ] 是否有明显的内存泄漏
- [ ] 是否有过度的 GC
- [ ] 是否有已知的 bug 与事件系统相关
- [ ] 是否有开发者反馈问题

---

## ?? 审查记录

| 审查日期 | 审查者 | 项目 | 结果 | 备注 |
|---------|--------|------|------|------|
| YYYY-MM-DD | Name | ProjectName | ?/??/? | Notes |
| | | | | |

---

## ?? 常见问题反馈

如果发现以下问题，请记录并改进：

```
[ ] 频繁的内存泄漏
[ ] GC 压力过高
[ ] 事件处理缓慢
[ ] 事件 ID 不一致
[ ] 订阅/取消不对称
[ ] 异常处理不当
[ ] 文档不完整
[ ] 新人上手困难
```

---

**检查表版本**：1.0 | **更新日期**：2024
