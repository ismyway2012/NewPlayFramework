# 流程系统（Procedure）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [核心概念](#核心概念)
3. [常见使用场景](#常见使用场景)
4. [最佳实践](#最佳实践)
5. [代码示例](#代码示例)
6. [性能优化](#性能优化)
7. [常见问题](#常见问题)

## 系统概述

流程系统（Procedure System）是GameFrameX框架的核心系统之一，用于管理游戏的各个流程状态（如启动、登录、游戏主循环、结算等）。它基于有限状态机（FSM）的思想实现，提供了清晰的状态管理机制。

### 主要特点
- **状态管理**: 使用FSM模式管理游戏流程
- **易于扩展**: 通过继承ProcedureBase创建新流程
- **生命周期管理**: 完整的初始化、进入、离开生命周期
- **状态转换**: 清晰的状态转换流程

## 核心概念

### ProcedureBase
所有流程的基类，提供了流程的生命周期方法。

```csharp
public abstract class ProcedureBase
{
    // 初始化流程
    public virtual void OnInit() { }
    
    // 进入流程
    public virtual void OnEnter() { }
    
    // 流程中的更新
    public virtual void OnUpdate(float elapseSeconds) { }
    
    // 离开流程
    public virtual void OnLeave() { }
    
    // 销毁流程
    public virtual void OnDestroy() { }
}
```

### IProcedureManager
流程管理器接口，用于管理所有流程的转换和状态。

## 常见使用场景

### 1. 游戏启动流程
```
启动检查 → 资源加载 → 配置初始化 → 主菜单
```

### 2. 游戏进入流程
```
主菜单 → 房间选择 → 角色选择 → 进入游戏 → 游戏运行
```

### 3. 游戏结束流程
```
游戏结束 → 结算界面 → 返回主菜单
```

## 最佳实践

### 1. 流程命名规范
使用清晰的命名约定，便于团队理解和维护。

**推荐**：
```csharp
public class SplashProcedure : ProcedureBase { }      // 启动画面
public class LoginProcedure : ProcedureBase { }       // 登录
public class LobbyProcedure : ProcedureBase { }       // 大厅
public class GamePlayProcedure : ProcedureBase { }    // 游戏运行
public class ResultProcedure : ProcedureBase { }      // 结算
```

**不推荐**：
```csharp
public class Proc1 : ProcedureBase { }                // 不清楚含义
public class P : ProcedureBase { }                    // 过度缩写
```

### 2. 职责单一原则
每个流程应该只负责一个明确的游戏状态。

**推荐**：
```csharp
// 只处理加载逻辑
public class LoadingProcedure : ProcedureBase
{
    private float m_Progress = 0f;
    
    public override void OnEnter()
    {
        // 启动资源加载
    }
    
    public override void OnUpdate(float elapseSeconds)
    {
        // 更新进度
        // 达到100%时转换流程
    }
}
```

**不推荐**：
```csharp
// 混杂多个责任
public class MegaProcedure : ProcedureBase
{
    public override void OnUpdate(float elapseSeconds)
    {
        // 同时处理加载、UI更新、网络请求等
    }
}
```

### 3. 流程转换的清晰逻辑
明确定义流程转换的条件，避免状态混乱。

**推荐**：
```csharp
public class GamePlayProcedure : ProcedureBase
{
    private bool m_IsGameOver = false;
    private float m_GameDuration = 0f;
    private const float MAX_GAME_TIME = 300f; // 5分钟
    
    public override void OnUpdate(float elapseSeconds)
    {
        m_GameDuration += elapseSeconds;
        
        if (m_IsGameOver)
        {
            ChangeState<ResultProcedure>();
            return;
        }
        
        if (m_GameDuration >= MAX_GAME_TIME)
        {
            m_IsGameOver = true;
            ChangeState<ResultProcedure>();
        }
    }
}
```

### 4. 使用事件系统协调流程
流程与其他系统的通信应使用事件系统，保持解耦。

**推荐**：
```csharp
public class GamePlayProcedure : ProcedureBase
{
    public override void OnEnter()
    {
        // 订阅相关事件
        GameEntry.GetComponent<EventComponent>()
            .Subscribe<PlayerDeadEventArgs>(OnPlayerDead);
    }
    
    private void OnPlayerDead(PlayerDeadEventArgs args)
    {
        ChangeState<ResultProcedure>();
    }
    
    public override void OnLeave()
    {
        // 取消订阅
        GameEntry.GetComponent<EventComponent>()
            .Unsubscribe<PlayerDeadEventArgs>(OnPlayerDead);
    }
}
```

### 5. 初始化和清理
正确处理流程的初始化和清理，避免内存泄漏。

**推荐**：
```csharp
public class LoginProcedure : ProcedureBase
{
    private HttpRequest m_LoginRequest = null;
    
    public override void OnEnter()
    {
        // 创建请求
        m_LoginRequest = new HttpRequest();
        m_LoginRequest.OnComplete += OnLoginComplete;
    }
    
    public override void OnLeave()
    {
        // 清理资源
        if (m_LoginRequest != null)
        {
            m_LoginRequest.OnComplete -= OnLoginComplete;
            m_LoginRequest.Dispose();
            m_LoginRequest = null;
        }
    }
}
```

## 代码示例

### 示例1：基础流程定义
```csharp
using GameFrameX.Runtime;
using GameFrameX.Procedure;

public class SplashProcedure : ProcedureBase
{
    private float m_ElapsedTime = 0f;
    private const float SPLASH_DURATION = 3f;
    
    public override void OnInit()
    {
        Log.Info("Splash procedure initialized.");
    }
    
    public override void OnEnter()
    {
        Log.Info("Splash procedure entered.");
        m_ElapsedTime = 0f;
    }
    
    public override void OnUpdate(float elapseSeconds)
    {
        m_ElapsedTime += elapseSeconds;
        
        if (m_ElapsedTime >= SPLASH_DURATION)
        {
            // 转换到加载流程
            ChangeState<LoadingProcedure>();
        }
    }
    
    public override void OnLeave()
    {
        Log.Info("Splash procedure left.");
    }
    
    public override void OnDestroy()
    {
        Log.Info("Splash procedure destroyed.");
    }
}
```

### 示例2：复杂流程处理
```csharp
public class LoginProcedure : ProcedureBase
{
    private enum LoginState
    {
        None,
        CheckingVersion,
        DownloadingUpdate,
        Logging,
        Success
    }
    
    private LoginState m_CurrentLoginState = LoginState.None;
    private float m_Timeout = 0f;
    private const float LOGIN_TIMEOUT = 30f;
    
    public override void OnEnter()
    {
        m_CurrentLoginState = LoginState.CheckingVersion;
        m_Timeout = 0f;
        CheckVersion();
    }
    
    public override void OnUpdate(float elapseSeconds)
    {
        m_Timeout += elapseSeconds;
        
        if (m_Timeout > LOGIN_TIMEOUT)
        {
            Log.Error("Login timeout.");
            ChangeState<ErrorProcedure>();
            return;
        }
        
        switch (m_CurrentLoginState)
        {
            case LoginState.CheckingVersion:
                // 版本检查逻辑
                break;
            case LoginState.DownloadingUpdate:
                // 更新下载逻辑
                break;
            case LoginState.Logging:
                // 登录逻辑
                break;
            case LoginState.Success:
                ChangeState<LobbyProcedure>();
                break;
        }
    }
    
    private void CheckVersion()
    {
        // 实现版本检查
        m_CurrentLoginState = LoginState.Logging;
    }
}
```

### 示例3：流程间数据传递
```csharp
public class ResultProcedure : ProcedureBase
{
    private GamePlayData m_GameData;
    
    public override void OnEnter()
    {
        // 获取上一个流程的数据
        var gamePlayProc = ProcedureManager.GetProcedure<GamePlayProcedure>();
        if (gamePlayProc != null)
        {
            m_GameData = gamePlayProc.GetGameData();
        }
        
        // 显示结算界面
        ShowResultUI();
    }
    
    private void ShowResultUI()
    {
        // 创建和显示结算UI
        var ui = UIManager.Open<ResultUI>();
        ui.SetData(m_GameData);
    }
}
```

## 性能优化

### 1. 避免频繁创建对象
```csharp
// 不推荐：每次Update都创建新对象
public override void OnUpdate(float elapseSeconds)
{
    var list = new List<int>(); // 每帧创建
}

// 推荐：复用对象
private List<int> m_TempList = new List<int>();

public override void OnUpdate(float elapseSeconds)
{
    m_TempList.Clear();
    // 使用 m_TempList
}
```

### 2. 减少流程切换频率
```csharp
// 不推荐：每帧都检查切换
public override void OnUpdate(float elapseSeconds)
{
    if (CheckCondition())
        ChangeState<NextProcedure>();
}

// 推荐：使用标志位，合并切换逻辑
private bool m_ShouldChangeState = false;

public override void OnUpdate(float elapseSeconds)
{
    if (m_ShouldChangeState)
    {
        ChangeState<NextProcedure>();
        m_ShouldChangeState = false;
    }
}
```

### 3. 及时清理事件订阅
```csharp
public override void OnLeave()
{
    // 必须在OnLeave中取消事件订阅，防止内存泄漏
    EventManager.Unsubscribe<GameEvent>(OnGameEvent);
}
```

## 常见问题

### Q1: 如何在流程间传递数据？

**A:** 有三种方式：

1. **通过流程引用**：
```csharp
var prevProc = ProcedureManager.GetProcedure<GamePlayProcedure>();
var data = prevProc.GetData();
```

2. **通过事件系统**：
```csharp
EventManager.Fire(new DataChangedEventArgs(data));
```

3. **通过全局容器**：
```csharp
GameEntry.GetComponent<GlobalDataContainer>().SetData("key", data);
```

### Q2: 为什么流程切换不立即生效？

**A:** 流程切换通常在当前帧的`OnUpdate`完成后才生效，这是为了避免在流程更新过程中切换造成的问题。

### Q3: 如何调试流程？

**A:** 使用框架提供的DebugConsole：
```csharp
// 在游戏运行时按下调试控制台快捷键
// 输入命令查看当前流程信息
DebugConsole.Log($"Current Procedure: {ProcedureManager.CurrentProcedure}");
```

### Q4: 如何处理流程初始化错误？

**A:** 使用try-catch和错误流程：
```csharp
public override void OnEnter()
{
    try
    {
        // 初始化逻辑
    }
    catch (Exception ex)
    {
        Log.Error($"Procedure initialization failed: {ex}");
        ChangeState<ErrorProcedure>();
    }
}
```

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
