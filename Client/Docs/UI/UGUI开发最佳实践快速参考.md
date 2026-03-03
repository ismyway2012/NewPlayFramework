# UGUI开发最佳实践快速参考

本文档是《UGUI系统架构设计文档》的快速参考版本，适合开发过程中快速查阅。

## 1. UI创建的标准流程

### 创建新UI的5个步骤

```
1. 在Unity中设计UI界面 (Prefab)
   └─ 使用UGUI组件（Button, Text, Image等）

2. 运行UGUI代码生成器 (Editor Menu)
   └─ 生成 UIXxx.UI.cs (UI元素绑定代码)

3. 创建 UIXxx.cs (逻辑脚本)
   └─ 继承自 partial class UIXxx 或 UGUI

4. 实现必要的生命周期方法
   └─ OnAwake, OnOpen, BindEvent, LoadData, etc

5. 配置UI信息
   └─ 设置 OptionUIConfig 特性、UIGroup等
```

### 最小化的UI实现模板

```csharp
// ? UIExample.cs
[OptionUIConfig(UIGroupConstants.Normal.Name, "Assets/Bundles/UI/UIExample")]
public partial class UIExample
{
    public override void OnAwake()
    {
        UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Normal.Name);
        base.OnAwake();
    }
    
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        BindUIEvents();
        LoadData();
    }
    
    private void BindUIEvents()
    {
        m_closeButton.onClick.Set(OnCloseClick);
    }
    
    public override void LoadData()
    {
        // 加载数据
    }
    
    private void OnCloseClick()
    {
        GameApp.UI.CloseUIForm(this);
    }
    
    public override void UnBindEvent()
    {
        m_closeButton.onClick.Clear();
        base.UnBindEvent();
    }
}
```

## 2. 生命周期速查

| 阶段 | 方法 | 用途 | 注意事项 |
|------|------|------|--------|
| **初始化** | `OnAwake()` | 获取UIGroup、缓存组件 | 必须调用base.OnAwake() |
| **打开** | `OnOpen(userData)` | 初始化显示逻辑、传入数据 | 在Show之前调用 |
| **事件绑定** | `BindEvent()` | 绑定所有事件监听 | 必须全部实现，避免遗漏 |
| **数据加载** | `LoadData()` | 更新UI显示内容 | 可以异步加载 |
| **本地化** | `UpdateLocalization()` | 更新文本为本地语言 | 可选，根据需求实现 |
| **显示** | `Show()` | 执行进入动画 | 框架自动调用 |
| **运行中** | `Update()` | 每帧更新逻辑 | 可选实现 |
| **隐藏** | `Hide()` | 执行退出动画 | 框架自动调用 |
| **关闭** | `OnClose()` | 清理逻辑 | 可选实现 |
| **事件解绑** | `UnBindEvent()` | 解绑所有事件监听 | 必须对应BindEvent |

## 3. 常用代码片段

### 打开/关闭UI

```csharp
// 打开UI（简单）
await GameApp.UI.OpenAsync<UIPlayerList>(
    Utility.Asset.Path.GetUIPath(nameof(UIPlayerList)), null);

// 打开UI并传递数据
var userData = new PlayerData { PlayerId = 123 };
await GameApp.UI.OpenAsync<UIPlayerDetail>(
    Utility.Asset.Path.GetUIPath(nameof(UIPlayerDetail)), userData);

// 打开全屏UI（暂停下层UI）
await GameApp.UI.OpenFullScreenAsync<UIPlayerCreate>(path, userData);

// 关闭当前UI
GameApp.UI.CloseUIForm(this);

// 关闭指定UI
GameApp.UI.CloseUIForm(uiFormName);
```

### 事件绑定

```csharp
// ? 正确方式1: 使用Set方法（推荐）
m_button.onClick.Set(OnButtonClick);  // Set会自动Clear旧监听

// ? 正确方式2: 显式Clear后Add
m_button.onClick.Clear();
m_button.onClick.Add(OnButtonClick);

// ? 错误方式: 直接Add（多次打开时会重复绑定）
m_button.onClick.AddListener(OnButtonClick);  // 错误！
```

### 数据处理

```csharp
// 接收传入的数据
public override void LoadData()
{
    var playerData = (PlayerData)UserData;
    if (playerData == null) return;
    
    m_playerNameText.text = playerData.Name;
    m_playerLevelText.text = playerData.Level.ToString();
}

// 发送数据给下一个UI
var context = new { PlayerId = 123, PlayerName = "Hero" };
await GameApp.UI.OpenAsync<UIPlayerDetail>(path, context);
```

### 异步操作

```csharp
// 异步加载资源
private async Task<Sprite> LoadAvatarAsync(string avatarId)
{
    try
    {
        var handle = await GameApp.Asset.LoadAssetAsync<Sprite>(
            $"Assets/Bundles/Avatar/avatar_{avatarId}");
        
        if (handle.IsSucceed())
        {
            return handle.GetAsset<Sprite>();
        }
    }
    catch (Exception ex)
    {
        Log.Error($"加载头像失败: {ex.Message}");
    }
    
    return null;
}

// 异步网络请求
private async void FetchData()
{
    try
    {
        var resp = await GameApp.WebProtoBuff.Post<RespData>(url, request);
        if (ResponseHandler.TryHandleError(resp)) return;
        
        UpdateUI(resp);
    }
    catch (Exception ex)
    {
        Log.Error($"获取数据失败: {ex.Message}");
    }
}
```

## 4. 错误检查清单

| 项目 | 检查方式 | 常见错误 |
|------|--------|--------|
| **UIGroup** | OnAwake中是否设置 | 未设置导致null异常 |
| **事件绑定** | BindEvent是否实现 | 重复绑定、未绑定 |
| **事件解绑** | UnBindEvent是否清理 | 内存泄漏、事件残留 |
| **base调用** | 是否调用base方法 | 生命周期不完整 |
| **异常处理** | 是否有try-catch | 异常导致UI卡死 |
| **null检查** | 是否检查null | 空引用异常 |

## 5. 性能优化速记

### 快速检查清单

- [ ] 对象池是否被复用（多次打开同一UI）
- [ ] 是否进行了异步加载（不阻塞主线程）
- [ ] 大列表是否使用虚拟滚动（不一次性创建所有Item）
- [ ] 事件是否正确解绑（避免内存泄漏）
- [ ] 资源是否及时释放（OnClose中清理）

### 性能问题症状及解决方案

```csharp
// 症状: 打开UI卡顿
// 解决: 使用异步加载
var uiForm = await GameApp.UI.OpenAsync<UIType>(path, data);  // ?

// 症状: 关闭UI后内存不释放
// 解决: 在OnClose中清理大对象
public override void OnClose()
{
    m_largeList?.Clear();
    m_largeList = null;
    base.OnClose();
}

// 症状: 列表滚动时帧率下降
// 解决: 使用虚拟滚动，只显示可见Item
// 见主文档的VirtualScrollView实现

// 症状: 反复打开同一UI导致内存增长
// 解决: 使用对象池复用UI实例（框架已内置）
```

## 6. UIGroup分组管理

### 常用分组

```csharp
// UIGroupConstants.cs
public static class UIGroupConstants
{
    public static class Normal { public const string Name = "Normal"; }      // 普通UI
    public static class Popup { public const string Name = "Popup"; }        // 弹窗
    public static class Dialog { public const string Name = "Dialog"; }      // 对话框
    public static class TopMost { public const string Name = "TopMost"; }    // 最前面
}

// 初始化分组（在UIComponent中）
GameApp.UI.AddUIGroup(UIGroupConstants.Normal.Name, 0, helper);
GameApp.UI.AddUIGroup(UIGroupConstants.Popup.Name, 10, helper);
GameApp.UI.AddUIGroup(UIGroupConstants.Dialog.Name, 20, helper);
GameApp.UI.AddUIGroup(UIGroupConstants.TopMost.Name, 100, helper);

// UI中指定分组
public override void OnAwake()
{
    UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Popup.Name);
    base.OnAwake();
}
```

## 7. API快速速查

### UI管理

```csharp
// 打开/关闭
await GameApp.UI.OpenAsync<T>(path, userData);
await GameApp.UI.OpenFullScreenAsync<T>(path, userData);
GameApp.UI.CloseUIForm(uiForm);
GameApp.UI.CloseUIFormByName(uiFormName);

// UIGroup
GameApp.UI.GetUIGroup(groupName);
GameApp.UI.AddUIGroup(name, depth, helper);

// UI查询
GameApp.UI.GetUIForm(uiFormAssetName);
GameApp.UI.HasUIForm(uiFormAssetName);
```

### 事件系统

```csharp
// 订阅/取消订阅
GameApp.Event.Subscribe(eventId, handler);
GameApp.Event.Unsubscribe(eventId, handler);

// 发送事件
GameApp.Event.Fire(eventId, eventArgs);
```

### 资源管理

```csharp
// 异步加载
var handle = await GameApp.Asset.LoadAssetAsync<T>(path);

// 获取资源
var asset = handle.GetAsset<T>();

// 释放
handle.Release();
```

## 8. 代码规范

### 命名规范

```csharp
// ? 正确
private int m_playerId;               // 私有字段（m_前缀）
public int PlayerId { get; set; }     // 属性（帕斯卡）
private async void OnButtonClick() {} // 事件处理方法
private async Task LoadDataAsync() {} // 异步方法（Async后缀）
```

### 注释规范

```csharp
// ? 简洁注释
private int m_playerId;    // 玩家ID

// 不必过度注释
private string m_playerName;  // ? 自解释的名称就足够了
```

## 9. 常见问题速解

### Q: 如何在UI间传递数据？

```csharp
// A: 使用userData参数
var data = new { PlayerId = 123, PlayerName = "Hero" };
await GameApp.UI.OpenAsync<UIDetail>(path, data);

// 在接收UI中
public override void LoadData()
{
    var data = (dynamic)UserData;
    var playerId = data.PlayerId;
}
```

### Q: 如何返回数据给前一个UI？

```csharp
// A: 使用事件系统
// 在当前UI中发送事件
GameApp.Event.Fire(PlayerSelectEvent.EventId, 
    new PlayerSelectEvent { PlayerId = selectedId });

// 在前一个UI中监听事件
public override void OnAwake()
{
    GameApp.Event.Subscribe(PlayerSelectEvent.EventId, OnPlayerSelected);
}

private void OnPlayerSelected(object sender, GameEventArgs e)
{
    var evt = e as PlayerSelectEvent;
    ProcessPlayerSelection(evt.PlayerId);
}
```

### Q: 如何防止事件重复绑定？

```csharp
// A: 使用Set方法或显式Clear
public override void OnOpen(object userData)
{
    m_button.onClick.Set(OnClick);  // Set会自动Clear
    // 或者
    m_button.onClick.Clear();
    m_button.onClick.Add(OnClick);
}
```

### Q: 如何处理异步操作异常？

```csharp
// A: 使用try-catch包装异步操作
private async void LoadData()
{
    try
    {
        var resp = await GameApp.WebProtoBuff.Post<Response>(url, req);
        if (ResponseHandler.TryHandleError(resp)) return;
        UpdateUI(resp);
    }
    catch (Exception ex)
    {
        Log.Error($"加载失败: {ex.Message}");
        ShowErrorUI("加载失败，请重试");
    }
}
```

## 10. 快速诊断工具

### 内存泄漏检查

```csharp
// 在UI关闭后检查内存是否释放
public override void OnClose()
{
    // 清理大对象
    m_itemList?.Clear();
    m_itemList = null;
    
    // 释放事件监听
    m_button.onClick.Clear();
    
    base.OnClose();
    
    // Log输出用于调试
    Log.Info($"[{GetType().Name}] closed and cleaned");
}
```

### 性能分析输出

```csharp
// 在关键方法中添加性能输出
private async void LoadDataAsync()
{
    var startTime = System.DateTime.Now;
    
    try
    {
        await FetchDataFromServer();
        await UpdateUIDisplay();
    }
    finally
    {
        var duration = (System.DateTime.Now - startTime).TotalMilliseconds;
        Log.Warning($"[Performance] LoadData took {duration}ms");
        
        if (duration > 1000)
        {
            Log.Error($"[Performance] LoadData is too slow! ({duration}ms)");
        }
    }
}
```

---

## 总结

记住这三条黄金法则：

1. ? **Always call base methods** - OnAwake, OnOpen, OnClose等必须调用base
2. ? **Always bind and unbind events** - BindEvent和UnBindEvent必须成对出现
3. ? **Always handle exceptions** - 异步操作必须有try-catch

遵循这个快速参考，可以快速高效地开发高质量的UGUI界面。

更详细的信息请参考《UGUI系统架构设计文档.md》

