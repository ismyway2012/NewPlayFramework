# 游戏启动流程技术文档

## 文档概述

本文档详细说明了NewPlayFramework游戏框架的完整启动流程，适用于新员工入职培训。包括关键组件、执行流程、各个阶段的功能说明以及重要配置点。

---

## 目录

1. [架构概述](#架构概述)
2. [启动流程阶段](#启动流程阶段)
3. [关键组件说明](#关键组件说明)
4. [详细流程时序图](#详细流程时序图)
5. [状态机转换](#状态机转换)
6. [关键代码执行点](#关键代码执行点)
7. [配置与扩展](#配置与扩展)
8. [常见问题](#常见问题)

---

## 架构概述

### 系统分层

NewPlayFramework采用**基于状态机的分层架构**，游戏启动过程采用Procedure（流程）模式：

```
┌─────────────────────────────────────────────┐
│         Game Application (Unity)            │
├─────────────────────────────────────────────┤
│    Procedure System (状态机驱动)             │
├─────────────────────────────────────────────┤
│ ┌─────────────┬──────────────┬────────────┐ │
│ │   UI        │   Resource   │   Config   │ │
│ │ Component   │  Manager     │ Component  │ │
│ └─────────────┴──────────────┴────────────┘ │
├─────────────────────────────────────────────┤
│    GameFrameX Framework (Game Framework)    │
├─────────────────────────────────────────────┤
│    Unity Engine & YooAsset (Package System) │
└─────────────────────────────────────────────┘
```

### 核心设计原则

- **状态机驱动**：使用有限状态机(FSM)管理游戏启动各个阶段
- **异步非阻塞**：全程使用UniTask实现异步操作
- **事件驱动**：通过事件系统解耦各个模块
- **热更新支持**：支持HybridCLR热更新加载

---

## 启动流程阶段

### 阶段概览

```
Stage 1: Engine Init
    ↓
Stage 2: Launcher State (启动状态)
    ↓
Stage 3: Get Global Info (获取全局信息)
    ↓
Stage 4: Get App Version Info (检查应用版本)
    ↓
Stage 5: Patch & Update (资源更新)
    ↓
Stage 6: Initialize Game (初始化游戏)
    ↓
Stage 7: Load Hotfix DLL (加载热更新)
    ↓
Stage 8: Enter Game (进入游戏)
```

### 各阶段详细说明

#### **阶段1: Engine Initialize**

**执行点**: Unity引擎启动时自动执行

**责任**:
- Unity场景初始化
- GameFrameX框架初始化
- ProcedureComponent初始化

**关键组件**:
- `ProcedureComponent`: 流程管理组件（来自GameFrameX框架包）
- 位置: `Packages/com.gameframex.unity.procedure@ca8bbc88b862/`

---

#### **阶段2: ProcedureLauncherState (启动状态)**

**执行点**: 第一个流程状态，作为游戏启动的入口

**责任**:
1. **UI框架初始化**
   - 初始化FairyGUI (如果启用ENABLE_UI_FAIRYGUI)
   - 创建UILauncher加载界面

2. **启动加载UI**
   - 异步打开全屏加载界面
   - 订阅资源下载进度事件

**关键代码流程**:

```csharp
ProcedureLauncherState.OnEnter()
    ├─ 初始化UI框架 (条件编译: ENABLE_UI_FAIRYGUI)
    ├─ LauncherUIHandler.Start()
    │   └─ GameApp.UI.OpenFullScreenAsync<UILauncher>()
    └─ Start() 异步方法
         ├─ await UniTask.NextFrame() (等待一帧)
         └─ ChangeState<ProcedureGetGlobalInfoState>()
```

**输出**: 
- 加载界面显示
- UI事件监听建立
- 准备下一阶段

**文件位置**: `Assets/Scripts/Framework/Procedure/ProcedureLauncherState.cs`

---

#### **阶段3: ProcedureGetGlobalInfoState (获取全局信息)**

**执行点**: 从ProcedureLauncherState转换

**执行模式检查**:

```
EditorSimulateMode (编辑器模拟模式)
    ↓ (直接跳过，转到版本检查)
    
OfflinePlayMode (离线模式)
    ↓ (跳过网络请求，转到资源初始化)
    
生产模式
    ↓ (正常执行所有步骤)
```

**责任**: 从服务器获取游戏全局配置信息

**网络请求信息**:

```
URL: http://127.0.0.1:20808/api/GameGlobalInfo/GetInfo
Method: POST
参数: BaseParams (自定义HTTP参数)

响应格式:
{
    "code": 0,
    "data": {
        "checkAppVersionUrl": "http://...",
        "checkResourceVersionUrl": "http://...",
        "content": {...}
    },
    "msg": "success"
}
```

**错误处理**:
- 服务器返回错误码 (code > 0) → 延迟3秒后重试
- 网络异常 → 延迟3秒后重试
- UI更新为相应的错误提示信息

**关键数据存储**:

```csharp
GlobalConfigComponent globalConfigComponent = GameApp.GlobalConfig;
globalConfigComponent.CheckAppVersionUrl = 版本检查地址;
globalConfigComponent.CheckResourceVersionUrl = 资源版本检查地址;
globalConfigComponent.Content = 服务器配置内容;
```

**文件位置**: `Assets/Scripts/Framework/Procedure/ProcedureGetGlobalInfoState.cs`

---

#### **阶段4-8: 资源更新与热更新加载**

**责任**:
- 检查应用版本并可能触发应用更新
- 资源版本检查与下载
- 热更新DLL加载 (HybridCLR)
- 游戏业务逻辑初始化

**关键点**:
- 使用YooAsset进行资源管理
- HybridCLR支持运行时脚本加载
- 热更新入口函数: `HotfixEntry.StartHotfixLogic()`

**文件位置**: 
- `Assets/Hotfix/HotfixLauncher.cs` - 热更新初始化
- 相关Procedure状态类

---

## 关键组件说明

### 1. **LauncherUIHandler** (加载界面处理器)

**位置**: `Assets/Scripts/Framework/Procedure/LauncherUIHandler.cs`

**职责**: 管理游戏启动过程中的加载界面UI交互

**主要方法**:

| 方法 | 说明 |
|------|------|
| `Start()` | 异步打开加载界面，订阅资源下载事件 |
| `Dispose()` | 关闭加载界面并清理资源 |
| `SetTipText(string text)` | 更新加载界面的提示文本 |
| `SetProgressUpdate()` | 处理资源下载进度更新 |
| `SetProgressUpdateFinish()` | 标记下载完成 |

**使用示例**:

```csharp
// 设置提示文本
LauncherUIHandler.SetTipText("Initializing...");

// 触发进度更新
LauncherUIHandler.SetProgressUpdate(sender, eventArgs);

// 清理
LauncherUIHandler.Dispose();
```

**事件订阅**:
```csharp
GameApp.Event.CheckSubscribe(
    AssetDownloadProgressUpdateEventArgs.EventId, 
    SetProgressUpdate
);
```

### 2. **UILauncher** (加载界面)

**位置**: `Assets/Scripts/Game/Logic/UILauncher/UILauncher.cs` (逻辑) 和 `UILauncher.UI.cs` (UI绑定)

**UI元素**:

| 元素 | 类型 | 说明 |
|------|------|------|
| `BgImage` | Image | 背景图片 |
| `TipText` | Text | 提示文本 |
| `ProgressBar` | Slider | 进度条 |
| `upgrade_Image` | Image | 升级提示图片 |
| `upgrade_TextContent` | Text | 升级文本内容 |
| `upgrade_EnterButton` | Button | 进入按钮 |

**生命周期**:
```csharp
OnAwake()
    └─ 初始化UI Group (UIGroupConstants.Normal)
```

### 3. **ProcedureComponent** (流程组件)

**位置**: `Packages/com.gameframex.unity.procedure@ca8bbc88b862/Runtime/Procedure/ProcedureComponent.cs`

**职责**: 管理游戏的流程状态机

**初始化流程**:

```csharp
Awake()
    ├─ 获取IProcedureManager实例
    └─ 初始化完成

Start() (协程)
    ├─ 从配置加载所有可用的Procedure类型
    ├─ 通过反射创建Procedure实例
    ├─ 标记入口Procedure
    └─ 启动状态机
```

**配置字段** (在Inspector中设置):
- `Available Procedure Type Names`: 所有流程类型的完全限定名
- `Entrance Procedure Type Name`: 入口流程类型

**访问方式**:

```csharp
IProcedureManager procedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
ProcedureBase currentProcedure = procedureManager.CurrentProcedure;
float procedureTime = procedureManager.CurrentProcedureTime;
```

### 4. **HotfixLauncher** (热更新启动器)

**位置**: `Assets/Hotfix/HotfixLauncher.cs`

**职责**: 处理热更新DLL的加载和游戏逻辑的启动

**主要方法**:

```csharp
Main()
    └─ 初始化Proto消息ID处理器
    └─ Load() 异步加载
        ├─ LoadConfig() - 加载配置表
        └─ StartGame() - 启动游戏

LoadConfig()
    ├─ 二进制配置: LoadAsync(ConfigBufferLoader)
    └─ JSON配置: LoadAsync(ConfigLoader)

StartGame()
    └─ 反射调用 HotfixEntry.StartHotfixLogic(true)
```

**配置表加载支持**:
- **二进制格式** (启用ENABLE_BINARY_CONFIG)
- **JSON格式** (默认)

**配置路径**: 
- 路径构造: `Utility.Asset.Path.GetConfigPath(file, fileNameSuffix)`
- 从TextAsset资源加载

---

## 详细流程时序图

### 总体启动时序

```
┌──────────────┐
│  Unity Start │
└──────┬───────┘
       │
       ▼
┌──────────────────────────┐
│ ProcedureComponent Awake │
│ - 初始化Procedure管理器   │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│ ProcedureComponent Start │
│ - 加载所有Procedure类型   │
│ - 创建实例                │
│ - 启动入口Procedure      │
└──────┬───────────────────┘
       │
       ▼
┌────────────────────────────────────┐
│ ProcedureLauncherState.OnEnter()   │
│ 1. 初始化UI框架(FairyGUI)          │
│ 2. LauncherUIHandler.Start()       │
│    - 打开加载界面                   │
│    - 订阅下载事件                   │
│ 3. 延迟一帧后转到下一状态          │
└──────┬───────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ ProcedureGetGlobalInfoState.OnEnter()  │
│ 1. 检查游戏运行模式                    │
│    - EditorSimulateMode: 跳过           │
│    - OfflinePlayMode: 跳过              │
│    - 正常模式: 执行                    │
│ 2. GetGlobalInfo()                     │
│    - POST 请求获取全局配置             │
│    - 错误重试机制 (3秒延迟)           │
│    - 保存全局配置到GlobalConfig       │
│ 3. 转到版本检查状态                    │
└──────┬───────────────────────────┘
       │
       ▼
┌────────────────────────────────────┐
│ 后续流程 (版本检查 → 资源更新)      │
│ - ProcedureGetAppVersionInfoState   │
│ - ProcedurePatchInit                │
│ - ProcedureInitializeGame           │
│ - ... (其他流程)                    │
└──────┬───────────────────────────┘
       │
       ▼
┌────────────────────────────────────┐
│ HotfixLauncher (热更新加载)         │
│ 1. 初始化Proto消息处理              │
│ 2. LoadConfig()                     │
│    - 加载配置表 (JSON或二进制)      │
│ 3. StartGame()                      │
│    - 反射调用HotfixEntry入口        │
│    - 启动热更新逻辑                  │
└──────┬───────────────────────────┘
       │
       ▼
┌────────────────────────────────────┐
│ 游戏业务逻辑启动                    │
│ - UI初始化                           │
│ - 场景加载                           │
│ - 游戏循环开始                       │
└────────────────────────────────────┘
```

### 资源下载进度更新流程

```
YooAsset下载事件触发
    │
    ▼
AssetDownloadProgressUpdateEventArgs
    │
    ▼
LauncherUIHandler.SetProgressUpdate()
    ├─ 计算下载进度
    │  progress = currentSize / totalSize
    ├─ 格式化大小显示
    │  "Downloading 50MB/100MB"
    └─ 更新UI元素
       ├─ m_ProgressBar.value = progress * 100
       └─ m_TipText.text = 提示文本
```

### 错误重试流程

```
请求失败或返回错误
    │
    ▼
┌─────────────────────────┐
│ 检查错误类型            │
├─────────────────────────┤
│ 网络异常   ──┐          │
│ 服务器错误 ──┼─→ 延迟3秒 │
│ 其他异常   ──┤          │
└────────┬────────────────┘
         │
         ▼
    更新UI错误提示
         │
         ▼
    重新发起请求
         │
         ▼
    ├─ 成功 → 继续流程
    └─ 失败 → 再次重试
```

---

## 状态机转换

### 状态机框架

游戏启动使用**有限状态机(FSM)**进行状态管理：

```csharp
interface IProcedureManager
{
    ProcedureBase CurrentProcedure { get; }
    float CurrentProcedureTime { get; }
    void ChangeState<TProcedure>() where TProcedure : ProcedureBase;
}
```

### 启动阶段状态转换

```
ProcedureLauncherState
    ↓ (ChangeState<ProcedureGetGlobalInfoState>)
ProcedureGetGlobalInfoState
    ├─ EditorSimulateMode → ProcedureGetAppVersionInfoState
    ├─ OfflinePlayMode → ProcedurePatchInit
    └─ 正常模式 → ProcedureGetAppVersionInfoState
        ↓
ProcedureGetAppVersionInfoState (检查应用版本)
        ↓
ProcedurePatchInit (资源初始化与更新)
        ↓
ProcedureInitializeGame (游戏初始化)
        ↓
[加载热更新DLL]
        ↓
ProcedureLoadHotfix (热更新加载)
        ↓
[启动游戏业务逻辑]
        ↓
进入游戏场景
```

### 状态转换方法

```csharp
// 在任何Procedure中，使用以下方法转换状态:
ChangeState<NextProcedureType>(procedureOwner);

// 例如:
ChangeState<ProcedureGetGlobalInfoState>(procedureOwner);
```

---

## 关键代码执行点

### 入口点 (Entry Points)

#### 1. **Unity场景启动 (自动)**

```csharp
// 位置: Packages/com.gameframex.unity.procedure/Runtime/ProcedureComponent.cs
void Awake()
{
    // GameFrameX框架初始化
    ImplementationComponentType = Utility.Assembly.GetType(componentType);
    InterfaceComponentType = typeof(IProcedureManager);
    base.Awake();
}

IEnumerator Start()
{
    // 加载并初始化所有Procedure
    // 启动入口Procedure
}
```

#### 2. **启动UI显示**

```csharp
// 位置: Assets/Scripts/Framework/Procedure/ProcedureLauncherState.cs
protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
{
#if ENABLE_UI_FAIRYGUI
    FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(FairyGuiExtensionLoader));
#endif
    base.OnEnter(procedureOwner);
    LauncherUIHandler.Start(); // ← 关键：启动加载UI
    Start(procedureOwner);     // ← 关键：异步延迟转换
}
```

#### 3. **加载UI异步操作**

```csharp
// 位置: Assets/Scripts/Framework/Procedure/LauncherUIHandler.cs
public static async void Start()
{
    // 打开全屏加载界面
    _ui = await GameApp.UI.OpenFullScreenAsync<UILauncher>(
        "UI/UILauncher/UILauncher", 
        UIGroupConstants.Loading
    );
    
    // 订阅资源下载进度事件
    GameApp.Event.CheckSubscribe(
        AssetDownloadProgressUpdateEventArgs.EventId, 
        SetProgressUpdate
    );
}
```

#### 4. **获取全局信息 (网络请求)**

```csharp
// 位置: Assets/Scripts/Framework/Procedure/ProcedureGetGlobalInfoState.cs
private async void GetGlobalInfo(IFsm<IProcedureManager> procedureOwner)
{
    try
    {
        // 构造请求
        string rootUrl = "http://127.0.0.1:20808/api/GameGlobalInfo/GetInfo";
        var jsonParams = HttpHelper.GetBaseParams();
        
        // 发送请求
        var json = await GameApp.Web.PostToString(rootUrl, jsonParams);
        
        // 解析响应
        HttpJsonResult httpJsonResult = Utility.Json.ToObject<HttpJsonResult>(json.Result);
        
        if (httpJsonResult.Code > 0)
        {
            // 服务器错误处理
            LauncherUIHandler.SetTipText("Server error, retrying...");
            await UniTask.Delay(3000);
            GetGlobalInfo(procedureOwner); // 递归重试
        }
        else
        {
            // 解析全局配置
            ResponseGlobalInfo responseGlobalInfo = 
                Utility.Json.ToObject<ResponseGlobalInfo>(httpJsonResult.Data);
            
            // 保存配置
            GlobalConfigComponent globalConfigComponent = GameApp.GlobalConfig;
            globalConfigComponent.CheckAppVersionUrl = responseGlobalInfo.CheckAppVersionUrl;
            globalConfigComponent.CheckResourceVersionUrl = responseGlobalInfo.CheckResourceVersionUrl;
            globalConfigComponent.Content = responseGlobalInfo.Content;
            
            // 转到下一状态
            ChangeState<ProcedureGetAppVersionInfoState>(procedureOwner);
        }
    }
    catch (Exception e)
    {
        // 网络异常处理
        LauncherUIHandler.SetTipText("Network error, retrying...");
        await UniTask.Delay(3000);
        GetGlobalInfo(procedureOwner); // 递归重试
    }
}
```

#### 5. **热更新启动**

```csharp
// 位置: Assets/Hotfix/HotfixLauncher.cs
public static void Main()
{
    Log.Info("Hello World HybridCLR");
    
    // 初始化Proto消息处理
    ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);
    
    // 启动异步加载
    Load().Forget();
}

public static async UniTask Load()
{
    // 加载配置表
    await LoadConfig();
    
    // 启动游戏
    StartGame();
}

private static void StartGame()
{
    // 反射获取热更新入口函数
    var entryFunc = Utility.Assembly
        .GetType("HotfixEntry")
        ?.GetMethod(
            "StartHotfixLogic", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
        );
    
    if (entryFunc == null)
    {
        Log.Fatal("游戏启动失败, 未找到HotfixEntry.StartHotfixLogic入口函数");
        return;
    }
    
    // 调用热更新逻辑入口
    entryFunc.Invoke(null, new object[] { true });
}
```

---

## 配置与扩展

### 1. **ProcedureComponent Inspector配置**

在Unity Scene中，ProcedureComponent脚本需要在Inspector中配置以下参数：

```
Available Procedure Type Names (可用的流程类型)
[
    "Unity.Startup.Procedure.ProcedureLauncherState",
    "Unity.Startup.Procedure.ProcedureGetGlobalInfoState",
    "Unity.Startup.Procedure.ProcedureGetAppVersionInfoState",
    "Unity.Startup.Procedure.ProcedurePatchInit",
    "Unity.Startup.Procedure.ProcedureInitializeGame",
    "Unity.Startup.Procedure.ProcedureLoadHotfix",
    ...
]

Entrance Procedure Type Name (入口流程)
"Unity.Startup.Procedure.ProcedureLauncherState"
```

### 2. **UILauncher资源路径**

```
UI资源路径: "UI/UILauncher/UILauncher"
UI Group: UIGroupConstants.Loading
打开模式: OpenFullScreenAsync<UILauncher>
```

### 3. **全局配置 (GlobalConfigComponent)**

全局配置组件存储以下关键信息：

```csharp
public class GlobalConfigComponent
{
    // 版本检查URL
    public string CheckAppVersionUrl { get; set; }
    
    // 资源版本检查URL
    public string CheckResourceVersionUrl { get; set; }
    
    // 服务器返回的内容 (包含主机地址等)
    public string Content { get; set; }
}
```

### 4. **条件编译符号**

游戏启动支持以下条件编译符号：

| 符号 | 说明 | 影响 |
|------|------|------|
| `ENABLE_UI_FAIRYGUI` | 启用FairyGUI支持 | ProcedureLauncherState中UI框架初始化 |
| `ENABLE_BINARY_CONFIG` | 使用二进制配置表 | HotfixLauncher配置表加载方式 |
| 无符号 | 默认使用JSON配置 | 使用JSON格式配置表 |

### 5. **扩展新的Procedure**

创建新的启动流程步骤的模板：

```csharp
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;

namespace Unity.Startup.Procedure
{
    public class ProcedureCustomState : ProcedureBase
    {
        // 进入状态时调用
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            // 执行初始化逻辑
            DoInitialization(procedureOwner);
        }
        
        // 更新逻辑 (每帧调用)
        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            // 更新逻辑
        }
        
        // 离开状态时调用
        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnLeave(procedureOwner);
            // 清理逻辑
        }
        
        private async void DoInitialization(IFsm<IProcedureManager> procedureOwner)
        {
            // 异步操作
            // ...
            
            // 转到下一个状态
            ChangeState<ProcedureNextState>(procedureOwner);
        }
    }
}
```

### 6. **UI Group Constants**

```csharp
public static class UIGroupConstants
{
    // 加载界面组
    public static string Loading => "Loading";
    
    // 普通UI组
    public static class Normal
    {
        public static string Name => "Normal";
    }
    
    // 更多自定义groups...
}
```

---

## 常见问题

### Q1: 游戏启动时加载界面不显示？

**可能原因**:
1. UILauncher预制体不在指定路径
2. UI Group配置错误
3. FairyGUI初始化失败

**排查步骤**:
- 检查资源是否在 `Assets/Resources/UI/UILauncher/` 目录
- 验证UIGroupConstants.Loading配置
- 查看编辑器日志中的UGUI初始化输出

**解决方案**:
```csharp
// 检查资源加载
GameApp.UI.OpenFullScreenAsync<UILauncher>(
    "UI/UILauncher/UILauncher",  // ← 确保路径正确
    UIGroupConstants.Loading       // ← 确保Group存在
);
```

---

### Q2: 如何跳过全局信息获取进行离线开发？

**方案1: 使用OfflinePlayMode**

```csharp
// 在ProcedureGetGlobalInfoState中检查:
if (GameApp.Asset.GamePlayMode == EPlayMode.OfflinePlayMode)
{
    Debug.Log("当前为离线模式，直接启动");
    ChangeState<ProcedurePatchInit>(procedureOwner);
    return;
}
```

**方案2: 使用EditorSimulateMode**

```csharp
// 编辑器模拟模式直接跳过
if (GameApp.Asset.GamePlayMode == EPlayMode.EditorSimulateMode)
{
    Debug.Log("当前为编辑器模式");
    ChangeState<ProcedureGetAppVersionInfoState>(procedureOwner);
    return;
}
```

---

### Q3: 全局信息请求一直失败重试，如何调试？

**调试步骤**:

1. **检查服务器连接**
   ```csharp
   string rootUrl = "http://127.0.0.1:20808/api/GameGlobalInfo/GetInfo";
   // 验证URL是否正确
   // 检查本地服务器是否运行
   ```

2. **检查网络请求参数**
   ```csharp
   var jsonParams = HttpHelper.GetBaseParams();
   Debug.Log($"Request Params: {jsonParams}");
   ```

3. **查看响应内容**
   ```csharp
   var json = await GameApp.Web.PostToString(rootUrl, jsonParams);
   Debug.Log($"Response: {json.Result}");
   ```

4. **检查错误处理**
   - UI显示的错误提示文本是什么？
   - 控制台是否有异常堆栈？

---

### Q4: 如何修改重试延迟时间？

**当前配置** (3秒延迟):

```csharp
await UniTask.Delay(3000); // 毫秒
```

**修改方式** (改为5秒):

```csharp
// 位置: ProcedureGetGlobalInfoState.cs
await UniTask.Delay(5000); // 5秒
```

**推荐**: 改为配置常数，便于统一管理
```csharp
private const int RETRY_DELAY_MS = 3000;
// 使用
await UniTask.Delay(RETRY_DELAY_MS);
```

---

### Q5: 热更新DLL如何确保正确加载？

**关键检查**:

1. **确保HotfixEntry入口存在**
   ```csharp
   var entryFunc = Utility.Assembly.GetType("HotfixEntry")
       ?.GetMethod("StartHotfixLogic", ...);
   
   if (entryFunc == null)
   {
       Log.Fatal("HotfixEntry.StartHotfixLogic not found!");
   }
   ```

2. **验证Pro消息处理**
   ```csharp
   ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);
   ```

3. **确保配置表加载完成**
   ```csharp
   await LoadConfig(); // 必须等待完成
   StartGame();        // 才能启动游戏
   ```

---

### Q6: UI进度条如何更新不及时？

**可能原因**:
1. 事件订阅未成功
2. 下载事件未触发
3. UI更新代码有阻塞

**优化方案**:

```csharp
// 确保事件订阅成功
GameApp.Event.CheckSubscribe(
    AssetDownloadProgressUpdateEventArgs.EventId,
    SetProgressUpdate
);

// 进度更新方法
public static void SetProgressUpdate(object sender, GameEventArgs gameEventArgs)
{
    var message = (AssetDownloadProgressUpdateEventArgs)gameEventArgs;
    
    // 使用UniTask确保主线程更新UI
    UniTask.NextFrame().Forget();
    
    // 更新UI
    float progress = message.CurrentDownloadSizeBytes / 
                    (message.TotalDownloadSizeBytes * 1f);
    _ui.m_ProgressBar.value = progress * 100;
}
```

---

### Q7: 如何自定义启动流程的状态？

**步骤**:

1. **创建新的Procedure类** (继承ProcedureBase)
2. **在ProcedureComponent中注册** (Available Procedure Type Names)
3. **通过ChangeState转换** (从其他Procedure转向新Procedure)

**示例** - 自定义加载界面等待流程：

```csharp
public class ProcedureLoadingWait : ProcedureBase
{
    private float m_ElapsedTime;
    private const float WAIT_TIME = 2f;
    
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        m_ElapsedTime = 0f;
        LauncherUIHandler.SetTipText("Please wait...");
    }
    
    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, 
                                     float elapseSeconds, 
                                     float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        
        m_ElapsedTime += elapseSeconds;
        if (m_ElapsedTime >= WAIT_TIME)
        {
            ChangeState<ProcedureGetGlobalInfoState>(procedureOwner);
        }
    }
}
```

---

## 附录

### 关键类名速查表

| 类名 | 所在文件 | 用途 |
|------|--------|------|
| `ProcedureLauncherState` | `Assets/Scripts/Framework/Procedure/` | 启动阶段入口 |
| `ProcedureGetGlobalInfoState` | `Assets/Scripts/Framework/Procedure/` | 获取全局配置 |
| `LauncherUIHandler` | `Assets/Scripts/Framework/Procedure/` | 加载UI管理 |
| `UILauncher` | `Assets/Scripts/Game/` | 加载界面 |
| `ProcedureComponent` | `Packages/com.gameframex.unity.procedure/` | 流程组件 |
| `HotfixLauncher` | `Assets/Hotfix/` | 热更新启动 |
| `GlobalConfigComponent` | Framework | 全局配置存储 |

### 重要常量位置

```
UI资源路径: Assets/Resources/UI/UILauncher/UILauncher.prefab
服务器API: http://127.0.0.1:20808/api/GameGlobalInfo/GetInfo
重试延迟: 3000 ms
资源下载事件: AssetDownloadProgressUpdateEventArgs.EventId
```

### 相关命名空间

```csharp
using Unity.Startup.Procedure;              // 启动流程
using GameFrameX.Procedure.Runtime;         // 流程基础框架
using GameFrameX.Fsm.Runtime;               // 状态机
using GameFrameX.UI.Runtime;                // UI框架
using GameFrameX.Runtime;                   // GameApp入口
using Cysharp.Threading.Tasks;              // 异步库UniTask
using GameFrameX.Event.Runtime;             // 事件系统
using GameFrameX.GlobalConfig.Runtime;      // 全局配置
using GameFrameX.Web.Runtime;               // 网络请求
```

---

## 总结

NewPlayFramework的游戏启动流程是一个**分层递进的状态机驱动系统**：

1. **入口层**: Unity场景启动 → ProcedureComponent初始化
2. **UI层**: 启动加载界面 → 显示进度条
3. **配置层**: 获取全局信息 → 版本检查 → 资源更新
4. **加载层**: 热更新DLL加载 → 配置表加载
5. **逻辑层**: 启动游戏业务逻辑 → 进入游戏场景

每个阶段都是高度模块化的，支持通过继承`ProcedureBase`创建新的流程阶段，通过`ChangeState`进行灵活的状态转换。

---

**文档版本**: 1.0  
**最后更新**: 2024年  
**适用框架**: NewPlayFramework with GameFrameX + YooAsset
