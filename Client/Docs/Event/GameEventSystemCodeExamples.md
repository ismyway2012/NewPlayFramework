# 游戏事件系统实战代码示例集

本文档提供在 GameFrameX 事件系统中的实用代码示例，供新员工学习参考。

---

## 目录

1. [基础示例](#基础示例)
2. [高级模式](#高级模式)
3. [常见业务场景](#常见业务场景)
4. [性能优化示例](#性能优化示例)
5. [错误处理示例](#错误处理示例)

---

## 基础示例

### 示例 1：最简单的事件发送和接收

**场景**：玩家升级时，显示升级提示

```csharp
// ========== 事件参数定义 ==========
public sealed class PlayerLevelUpEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(PlayerLevelUpEventArgs);

    /// <summary>新等级</summary>
    public int NewLevel { get; set; }

    public override void Clear()
    {
        NewLevel = 0;
    }

    public override string Id => EventId;

    public static PlayerLevelUpEventArgs Create(int newLevel)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.NewLevel = newLevel;
        return args;
    }
}

// ========== 事件发送者 ==========
public class PlayerManager : MonoBehaviour
{
    private int m_CurrentLevel = 1;

    /// <summary>玩家升级</summary>
    public void LevelUp()
    {
        m_CurrentLevel++;
        
        // 发送事件
        var eventArgs = PlayerLevelUpEventArgs.Create(m_CurrentLevel);
        GameEntry.GetComponent<EventComponent>().Fire(this, eventArgs);

        Debug.Log($"Player leveled up to {m_CurrentLevel}");
    }
}

// ========== 事件接收者 ==========
public class UINotificationPanel : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        // 创建事件订阅器
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();

        // 订阅事件
        m_EventSubscriber.CheckSubscribe(
            PlayerLevelUpEventArgs.EventId,
            OnPlayerLevelUp
        );
    }

    private void OnDisable()
    {
        // 取消所有订阅
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    /// <summary>处理玩家升级事件</summary>
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        if (!(e is PlayerLevelUpEventArgs args))
            return;

        // 显示升级提示
        ShowLevelUpNotification(args.NewLevel);
    }

    private void ShowLevelUpNotification(int newLevel)
    {
        Debug.Log($"[UI] Congratulations! You reached level {newLevel}");
        // 实际的 UI 显示逻辑
    }
}
```

---

### 示例 2：多个订阅者监听同一事件

**场景**：玩家升级时，多个模块需要响应

```csharp
// ========== 多个订阅者 ==========
public class AudioManager : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerLevelUpEventArgs.EventId,
            OnPlayerLevelUp
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        if (!(e is PlayerLevelUpEventArgs args))
            return;

        // 播放升级音效
        PlayLevelUpSound(args.NewLevel);
    }

    private void PlayLevelUpSound(int level)
    {
        Debug.Log($"[Audio] Playing level up sound for level {level}");
    }
}

public class ParticleEffectManager : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerLevelUpEventArgs.EventId,
            OnPlayerLevelUp
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        if (!(e is PlayerLevelUpEventArgs args))
            return;

        // 播放升级特效
        PlayLevelUpEffect(args.NewLevel);
    }

    private void PlayLevelUpEffect(int level)
    {
        Debug.Log($"[Effect] Playing level up particles for level {level}");
    }
}

// ========== 测试代码 ==========
public class GameTest : MonoBehaviour
{
    private PlayerManager m_PlayerManager;

    private void Start()
    {
        m_PlayerManager = GetComponent<PlayerManager>();
        
        // 升级时，多个模块同时响应
        m_PlayerManager.LevelUp();
        // 输出：
        // [UI] Congratulations! You reached level 2
        // [Audio] Playing level up sound for level 2
        // [Effect] Playing level up particles for level 2
    }
}
```

---

### 示例 3：空事件（无参数）

**场景**：游戏暂停/恢复

```csharp
// ========== 空事件参数 ==========
public sealed class GamePausedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(GamePausedEventArgs);

    public override void Clear() { }

    public override string Id => EventId;

    public static GamePausedEventArgs Create()
    {
        return ReferencePool.Acquire<GamePausedEventArgs>();
    }
}

public sealed class GameResumedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(GameResumedEventArgs);

    public override void Clear() { }

    public override string Id => EventId;

    public static GameResumedEventArgs Create()
    {
        return ReferencePool.Acquire<GameResumedEventArgs>();
    }
}

// ========== 游戏暂停管理器 ==========
public class GamePauseManager : MonoBehaviour
{
    private bool m_IsPaused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (m_IsPaused)
                Resume();
            else
                Pause();
        }
    }

    private void Pause()
    {
        m_IsPaused = true;
        Time.timeScale = 0;

        var e = GamePausedEventArgs.Create();
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }

    private void Resume()
    {
        m_IsPaused = false;
        Time.timeScale = 1;

        var e = GameResumedEventArgs.Create();
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// ========== 响应暂停事件 ==========
public class CharacterController : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    private bool m_CanMove = true;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            GamePausedEventArgs.EventId,
            OnGamePaused
        );
        m_EventSubscriber.CheckSubscribe(
            GameResumedEventArgs.EventId,
            OnGameResumed
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnGamePaused(object sender, GameEventArgs e)
    {
        m_CanMove = false;
        Debug.Log("Character movement disabled");
    }

    private void OnGameResumed(object sender, GameEventArgs e)
    {
        m_CanMove = true;
        Debug.Log("Character movement enabled");
    }

    private void Update()
    {
        if (!m_CanMove)
            return;

        // 移动逻辑
    }
}
```

---

## 高级模式

### 模式 1：使用事件优先级

**场景**：多个系统都需要响应玩家死亡事件，但需要按顺序处理

```csharp
// ========== 事件参数 ==========
public sealed class PlayerDeadEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(PlayerDeadEventArgs);

    public Vector3 DeathPosition { get; set; }

    public override void Clear()
    {
        DeathPosition = Vector3.zero;
    }

    public override string Id => EventId;

    public static PlayerDeadEventArgs Create(Vector3 position)
    {
        var args = ReferencePool.Acquire<PlayerDeadEventArgs>();
        args.DeathPosition = position;
        return args;
    }
}

// ========== 优先级管理器（推荐使用） ==========
public static class EventPriorityManager
{
    public enum Priority
    {
        /// <summary>最高优先级：游戏状态管理</summary>
        GameState = 100,

        /// <summary>高优先级：保存数据</summary>
        SaveData = 50,

        /// <summary>普通优先级：UI 更新</summary>
        UI = 0,

        /// <summary>低优先级：音效/特效</summary>
        Effects = -50,

        /// <summary>最低优先级：日志</summary>
        Logging = -100
    }
}

// ========== 各个模块 ==========
public class GameStateManager : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerDeadEventArgs.EventId,
            OnPlayerDead
        );
        Debug.Log("[Priority 100] GameStateManager subscribed");
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    // 最先执行：处理游戏状态
    private void OnPlayerDead(object sender, GameEventArgs e)
    {
        if (!(e is PlayerDeadEventArgs args))
            return;

        Debug.Log("[Priority 100] Setting game to GameOver state");
        // 设置游戏状态为 GameOver
    }
}

public class DataSaveManager : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerDeadEventArgs.EventId,
            OnPlayerDead
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    // 次序执行：保存数据
    private void OnPlayerDead(object sender, GameEventArgs e)
    {
        if (!(e is PlayerDeadEventArgs args))
            return;

        Debug.Log("[Priority 50] Saving player data");
        // 保存死亡数据
    }
}

public class DeathUIPanel : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerDeadEventArgs.EventId,
            OnPlayerDead
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    // 然后执行：显示 UI
    private void OnPlayerDead(object sender, GameEventArgs e)
    {
        if (!(e is PlayerDeadEventArgs args))
            return;

        Debug.Log("[Priority 0] Showing death panel UI");
        // 显示死亡面板
    }
}

public class DeathAudioManager : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerDeadEventArgs.EventId,
            OnPlayerDead
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    // 最后执行：播放音效
    private void OnPlayerDead(object sender, GameEventArgs e)
    {
        if (!(e is PlayerDeadEventArgs args))
            return;

        Debug.Log("[Priority -50] Playing death sound");
        // 播放死亡音效
    }
}
```

---

### 模式 2：条件订阅

**场景**：根据条件动态订阅和取消订阅事件

```csharp
// ========== 事件参数 ==========
public sealed class EnemyDetectedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(EnemyDetectedEventArgs);

    public Vector3 EnemyPosition { get; set; }

    public override void Clear()
    {
        EnemyPosition = Vector3.zero;
    }

    public override string Id => EventId;

    public static EnemyDetectedEventArgs Create(Vector3 position)
    {
        var args = ReferencePool.Acquire<EnemyDetectedEventArgs>();
        args.EnemyPosition = position;
        return args;
    }
}

// ========== 条件订阅示例 ==========
public class AIAlertSystem : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    private bool m_IsAlerted = false;

    private void Start()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
    }

    private void OnDestroy()
    {
        m_EventSubscriber?.UnSubscribeAll();
        if (m_EventSubscriber != null)
            ReferencePool.Release(m_EventSubscriber);
    }

    public void EnterAlertMode()
    {
        if (m_IsAlerted)
            return;

        m_IsAlerted = true;

        // 进入警戒模式，开始监听敌人检测
        m_EventSubscriber.CheckSubscribe(
            EnemyDetectedEventArgs.EventId,
            OnEnemyDetected
        );

        Debug.Log("AI Alert Mode: ACTIVATED");
    }

    public void ExitAlertMode()
    {
        if (!m_IsAlerted)
            return;

        m_IsAlerted = false;

        // 退出警戒模式，停止监听
        GameEntry.GetComponent<EventComponent>().Unsubscribe(
            EnemyDetectedEventArgs.EventId,
            OnEnemyDetected
        );

        Debug.Log("AI Alert Mode: DEACTIVATED");
    }

    private void OnEnemyDetected(object sender, GameEventArgs e)
    {
        if (!(e is EnemyDetectedEventArgs args))
            return;

        Debug.Log($"Enemy detected at {args.EnemyPosition}");
        // 执行警戒逻辑
    }
}
```

---

### 模式 3：事件链（一个事件触发另一个事件）

**场景**：玩家击杀敌人 -> 获得经验 -> 升级

```csharp
// ========== 事件参数 ==========
public sealed class EnemyKilledEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(EnemyKilledEventArgs);

    public int EnemyId { get; set; }
    public int RewardExp { get; set; }

    public override void Clear()
    {
        EnemyId = 0;
        RewardExp = 0;
    }

    public override string Id => EventId;

    public static EnemyKilledEventArgs Create(int enemyId, int rewardExp)
    {
        var args = ReferencePool.Acquire<EnemyKilledEventArgs>();
        args.EnemyId = enemyId;
        args.RewardExp = rewardExp;
        return args;
    }
}

public sealed class PlayerGainExpEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(PlayerGainExpEventArgs);

    public int ExpAmount { get; set; }
    public int CurrentExp { get; set; }

    public override void Clear()
    {
        ExpAmount = 0;
        CurrentExp = 0;
    }

    public override string Id => EventId;

    public static PlayerGainExpEventArgs Create(int expAmount, int currentExp)
    {
        var args = ReferencePool.Acquire<PlayerGainExpEventArgs>();
        args.ExpAmount = expAmount;
        args.CurrentExp = currentExp;
        return args;
    }
}

// ========== 事件链：击杀敌人 -> 获得经验 ==========
public class ExperienceManager : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    private int m_CurrentExp = 0;
    private const int ExpToLevelUp = 100;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            EnemyKilledEventArgs.EventId,
            OnEnemyKilled
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    /// <summary>响应敌人被击杀事件</summary>
    private void OnEnemyKilled(object sender, GameEventArgs e)
    {
        if (!(e is EnemyKilledEventArgs args))
            return;

        Debug.Log($"Enemy {args.EnemyId} killed, reward exp: {args.RewardExp}");

        // 增加经验
        m_CurrentExp += args.RewardExp;

        // 发送获得经验事件（链式调用第一步）
        var gainExpEvent = PlayerGainExpEventArgs.Create(args.RewardExp, m_CurrentExp);
        GameEntry.GetComponent<EventComponent>().Fire(this, gainExpEvent);

        // 检查是否升级
        if (m_CurrentExp >= ExpToLevelUp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        m_CurrentExp -= ExpToLevelUp;
        // 发送升级事件（链式调用第二步）
        // 注意：这可能触发更多事件，形成事件链
    }
}

// ========== 响应获得经验事件 ==========
public class UIExpBar : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            PlayerGainExpEventArgs.EventId,
            OnPlayerGainExp
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnPlayerGainExp(object sender, GameEventArgs e)
    {
        if (!(e is PlayerGainExpEventArgs args))
            return;

        Debug.Log($"[UI] Player gained {args.ExpAmount} exp, total: {args.CurrentExp}");
        // 更新 UI 经验条
    }
}
```

---

## 常见业务场景

### 场景 1：背包系统事件

```csharp
// ========== 事件参数 ==========
public sealed class ItemAddedToBackEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(ItemAddedToBackEventArgs);

    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public int Quality { get; set; }  // 品质

    public override void Clear()
    {
        ItemId = 0;
        Quantity = 0;
        Quality = 0;
    }

    public override string Id => EventId;

    public static ItemAddedToBackEventArgs Create(int itemId, int quantity, int quality)
    {
        var args = ReferencePool.Acquire<ItemAddedToBackEventArgs>();
        args.ItemId = itemId;
        args.Quantity = quantity;
        args.Quality = quality;
        return args;
    }
}

public sealed class ItemRemovedFromBackEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(ItemRemovedFromBackEventArgs);

    public int ItemId { get; set; }
    public int Quantity { get; set; }

    public override void Clear()
    {
        ItemId = 0;
        Quantity = 0;
    }

    public override string Id => EventId;

    public static ItemRemovedFromBackEventArgs Create(int itemId, int quantity)
    {
        var args = ReferencePool.Acquire<ItemRemovedFromBackEventArgs>();
        args.ItemId = itemId;
        args.Quantity = quantity;
        return args;
    }
}

// ========== 背包管理器 ==========
public class BackpackManager : MonoBehaviour
{
    private Dictionary<int, (int quantity, int quality)> m_Items = 
        new Dictionary<int, (int, int)>();

    /// <summary>添加物品到背包</summary>
    public void AddItem(int itemId, int quantity, int quality = 0)
    {
        if (!m_Items.ContainsKey(itemId))
        {
            m_Items[itemId] = (quantity, quality);
        }
        else
        {
            var (existingQty, existingQuality) = m_Items[itemId];
            m_Items[itemId] = (existingQty + quantity, existingQuality);
        }

        // 发送物品添加事件
        var e = ItemAddedToBackEventArgs.Create(itemId, quantity, quality);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);

        Debug.Log($"Item {itemId} added to backpack: {quantity}x");
    }

    /// <summary>从背包移除物品</summary>
    public bool RemoveItem(int itemId, int quantity)
    {
        if (!m_Items.TryGetValue(itemId, out var item))
            return false;

        var (currentQty, quality) = item;
        if (currentQty < quantity)
            return false;

        m_Items[itemId] = (currentQty - quantity, quality);
        if (m_Items[itemId].quantity <= 0)
        {
            m_Items.Remove(itemId);
        }

        // 发送物品移除事件
        var e = ItemRemovedFromBackEventArgs.Create(itemId, quantity);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);

        Debug.Log($"Item {itemId} removed from backpack: {quantity}x");
        return true;
    }
}

// ========== 背包 UI ==========
public class BackpackUI : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            ItemAddedToBackEventArgs.EventId,
            OnItemAdded
        );
        m_EventSubscriber.CheckSubscribe(
            ItemRemovedFromBackEventArgs.EventId,
            OnItemRemoved
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnItemAdded(object sender, GameEventArgs e)
    {
        if (!(e is ItemAddedToBackEventArgs args))
            return;

        Debug.Log($"[UI] Item added: {args.ItemId} x{args.Quantity}");
        RefreshBackpackDisplay();
    }

    private void OnItemRemoved(object sender, GameEventArgs e)
    {
        if (!(e is ItemRemovedFromBackEventArgs args))
            return;

        Debug.Log($"[UI] Item removed: {args.ItemId} x{args.Quantity}");
        RefreshBackpackDisplay();
    }

    private void RefreshBackpackDisplay()
    {
        // 刷新背包 UI 显示
    }
}
```

---

### 场景 2：任务系统事件

```csharp
// ========== 事件参数 ==========
public sealed class QuestStartedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(QuestStartedEventArgs);

    public int QuestId { get; set; }
    public string QuestName { get; set; }

    public override void Clear()
    {
        QuestId = 0;
        QuestName = null;
    }

    public override string Id => EventId;

    public static QuestStartedEventArgs Create(int questId, string questName)
    {
        var args = ReferencePool.Acquire<QuestStartedEventArgs>();
        args.QuestId = questId;
        args.QuestName = questName;
        return args;
    }
}

public sealed class QuestCompletedEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(QuestCompletedEventArgs);

    public int QuestId { get; set; }
    public int RewardExp { get; set; }
    public int RewardGold { get; set; }

    public override void Clear()
    {
        QuestId = 0;
        RewardExp = 0;
        RewardGold = 0;
    }

    public override string Id => EventId;

    public static QuestCompletedEventArgs Create(int questId, int rewardExp, int rewardGold)
    {
        var args = ReferencePool.Acquire<QuestCompletedEventArgs>();
        args.QuestId = questId;
        args.RewardExp = rewardExp;
        args.RewardGold = rewardGold;
        return args;
    }
}

// ========== 任务管理器 ==========
public class QuestManager : MonoBehaviour
{
    private Dictionary<int, bool> m_CompletedQuests = new Dictionary<int, bool>();

    public void StartQuest(int questId, string questName)
    {
        var e = QuestStartedEventArgs.Create(questId, questName);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }

    public void CompleteQuest(int questId, int rewardExp, int rewardGold)
    {
        m_CompletedQuests[questId] = true;

        var e = QuestCompletedEventArgs.Create(questId, rewardExp, rewardGold);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// ========== 任务跟踪器 ==========
public class QuestTracker : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    private List<int> m_ActiveQuests = new List<int>();

    private void OnEnable()
    {
        m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
        m_EventSubscriber.CheckSubscribe(
            QuestStartedEventArgs.EventId,
            OnQuestStarted
        );
        m_EventSubscriber.CheckSubscribe(
            QuestCompletedEventArgs.EventId,
            OnQuestCompleted
        );
    }

    private void OnDisable()
    {
        m_EventSubscriber.UnSubscribeAll();
        ReferencePool.Release(m_EventSubscriber);
    }

    private void OnQuestStarted(object sender, GameEventArgs e)
    {
        if (!(e is QuestStartedEventArgs args))
            return;

        m_ActiveQuests.Add(args.QuestId);
        Debug.Log($"[Quest Tracker] Started: {args.QuestName}");
    }

    private void OnQuestCompleted(object sender, GameEventArgs e)
    {
        if (!(e is QuestCompletedEventArgs args))
            return;

        m_ActiveQuests.Remove(args.QuestId);
        Debug.Log($"[Quest Tracker] Completed quest {args.QuestId}, " +
                  $"Reward: {args.RewardExp} exp, {args.RewardGold} gold");
    }
}
```

---

## 性能优化示例

### 优化 1：批量事件处理

```csharp
// ========== 不推荐：频繁触发小事件 ==========
public class BadBulletSystem : MonoBehaviour
{
    private void OnBulletHit(Collider other)
    {
        // ? 每发子弹击中都触发事件
        var e = BulletHitEventArgs.Create(other.gameObject.name);
        GameEntry.GetComponent<EventComponent>().Fire(this, e);
    }
}

// ========== 推荐：批量事件处理 ==========
public sealed class BulletHitBatchEventArgs : GameEventArgs
{
    public static readonly string EventId = nameof(BulletHitBatchEventArgs);

    public List<string> HitTargets { get; set; } = new List<string>();

    public override void Clear()
    {
        HitTargets.Clear();
    }

    public override string Id => EventId;

    public static BulletHitBatchEventArgs Create(List<string> targets)
    {
        var args = ReferencePool.Acquire<BulletHitBatchEventArgs>();
        args.HitTargets = targets;
        return args;
    }
}

public class GoodBulletSystem : MonoBehaviour
{
    private List<string> m_HitTargets = new List<string>();
    private float m_BatchTime = 0.1f;  // 每 0.1 秒批量处理一次
    private float m_Timer = 0;

    private void OnBulletHit(Collider other)
    {
        // ? 收集击中的目标
        m_HitTargets.Add(other.gameObject.name);
    }

    private void Update()
    {
        m_Timer += Time.deltaTime;
        if (m_Timer >= m_BatchTime && m_HitTargets.Count > 0)
        {
            m_Timer = 0;

            // 批量发送事件
            var e = BulletHitBatchEventArgs.Create(m_HitTargets);
            GameEntry.GetComponent<EventComponent>().Fire(this, e);

            m_HitTargets.Clear();
        }
    }
}
```

---

### 优化 2：有条件的事件订阅

```csharp
// ========== 优化：不需要时不订阅 ==========
public class OptimizedHealthDisplay : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;
    private bool m_IsVisible = false;

    private void OnEnable()
    {
        // 面板显示时才订阅
        if (!m_IsVisible)
        {
            m_IsVisible = true;
            m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
            m_EventSubscriber.CheckSubscribe(
                PlayerHealthChangedEventArgs.EventId,
                OnHealthChanged
            );
        }
    }

    private void OnDisable()
    {
        // 面板隐藏时立即取消订阅
        if (m_IsVisible)
        {
            m_IsVisible = false;
            m_EventSubscriber.UnSubscribeAll();
            ReferencePool.Release(m_EventSubscriber);
        }
    }

    private void OnHealthChanged(object sender, GameEventArgs e)
    {
        // 只在面板可见时才处理事件
    }
}
```

---

## 错误处理示例

### 完善的错误处理

```csharp
// ========== 完善的错误处理 ==========
public class RobustEventHandler : MonoBehaviour
{
    private UIEventSubscriber m_EventSubscriber;

    private void OnEnable()
    {
        try
        {
            m_EventSubscriber = ReferencePool.Acquire<UIEventSubscriber>();
            if (m_EventSubscriber == null)
            {
                Log.Error("Failed to acquire UIEventSubscriber");
                return;
            }

            m_EventSubscriber.CheckSubscribe(
                PlayerLevelUpEventArgs.EventId,
                OnPlayerLevelUp
            );
        }
        catch (Exception ex)
        {
            Log.Error($"Error subscribing to event: {ex.Message}");
        }
    }

    private void OnDisable()
    {
        try
        {
            m_EventSubscriber?.UnSubscribeAll();
            if (m_EventSubscriber != null)
            {
                ReferencePool.Release(m_EventSubscriber);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error unsubscribing from events: {ex.Message}");
        }
    }

    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        try
        {
            // 类型检查
            if (!(e is PlayerLevelUpEventArgs args))
            {
                Log.Warning($"Invalid event type: {e.GetType().Name}");
                return;
            }

            // 数据验证
            if (args.NewLevel <= 0)
            {
                Log.Warning("Invalid level: " + args.NewLevel);
                return;
            }

            // 安全的处理
            UpdateUI(args.NewLevel);
        }
        catch (Exception ex)
        {
            Log.Error($"Error handling PlayerLevelUp event: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void UpdateUI(int newLevel)
    {
        // UI 更新逻辑
    }
}
```

---

## 总结

### 快速参考表

| 任务 | 代码 | 说明 |
|------|------|------|
| 定义事件 | `public sealed class XxxEventArgs : GameEventArgs` | 继承 GameEventArgs |
| 创建参数 | `var e = XxxEventArgs.Create(...)` | 使用工厂方法，支持对象池 |
| 订阅事件 | `m_EventSubscriber.CheckSubscribe(eventId, handler)` | 使用 UIEventSubscriber |
| 取消订阅 | `m_EventSubscriber.UnSubscribeAll()` | 一次清空所有订阅 |
| 发送事件 | `GameEntry.GetComponent<EventComponent>().Fire(this, e)` | 异步发送（推荐） |
| 同步发送 | `GameEntry.GetComponent<EventComponent>().FireNow(this, e)` | 立即执行（特殊情况） |

---

**文档版本**：1.0  
**更新日期**：2024  
**适用版本**：GameFrameX Latest
