# UI系统（UI/UGUI）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [UI架构设计](#ui架构设计)
3. [最佳实践](#最佳实践)
4. [代码示例](#代码示例)
5. [性能优化](#性能优化)
6. [常见问题](#常见问题)

## 系统概述

UI系统（UI System）是GameFrameX框架用于管理用户界面的核心系统。它提供了统一的UI窗口管理、生命周期控制、事件交互和资源管理机制，支持UGUI和FairyGUI两种渲染方案。

### 主要特点
- **窗口管理**: 统一的UI窗口生命周期管理
- **多层级系统**: 支持多个UI层级的组织
- **事件驱动**: 基于事件系统的交互设计
- **资源管理**: 自动的UI资源加载和卸载
- **动画支持**: 内置UI转换和动画系统

## UI架构设计

### 三层UI架构
```
┌─────────────────────────────────────┐
│   UI Logic Layer                    │  处理业务逻辑和数据
│   (UILogin, UIMain, etc.)          │
├─────────────────────────────────────┤
│   UI Component Layer                │  封装UI组件和交互
│   (UILogin.UI, UIMain.UI, etc.)    │
├─────────────────────────────────────┤
│   UGUI/FairyGUI Rendering          │  渲染层
│   (Canvas, Panel, etc.)            │
└─────────────────────────────────────┘
```

### UI窗口通信路径
```
用户交互 → UI Component → Event System → UI Logic → Event System → 其他系统
```

## 最佳实践

### 1. UI窗口的设计模式

#### 1.1 MVC模式分离（Logic + UI Components）
```csharp
// UI逻辑类 - 处理业务逻辑
public class UIMainLogic : UILogicBase
{
    private int m_CurrentLevel = 1;
    private int m_PlayerScore = 0;
    private UIMainUI m_MainUI;
    
    public override void OnInit()
    {
        m_MainUI = GetUIComponent<UIMainUI>();
    }
    
    public override void OnOpen()
    {
        // 更新UI显示
        m_MainUI.SetLevelText(m_CurrentLevel);
        m_MainUI.SetScoreText(m_PlayerScore);
        
        // 订阅事件
        EventManager.Subscribe<ScoreChangedEventArgs>(OnScoreChanged);
    }
    
    public override void OnClose()
    {
        EventManager.Unsubscribe<ScoreChangedEventArgs>(OnScoreChanged);
    }
    
    private void OnScoreChanged(ScoreChangedEventArgs args)
    {
        m_PlayerScore = args.NewScore;
        m_MainUI.SetScoreText(m_PlayerScore);
    }
}

// UI组件类 - 处理UI交互
public class UIMainUI : UIComponentBase
{
    private Text m_LevelText;
    private Text m_ScoreText;
    private Button m_StartButton;
    
    public override void OnInit()
    {
        m_LevelText = transform.Find("LevelText").GetComponent<Text>();
        m_ScoreText = transform.Find("ScoreText").GetComponent<Text>();
        m_StartButton = transform.Find("StartButton").GetComponent<Button>();
        
        m_StartButton.onClick.AddListener(OnStartClicked);
    }
    
    public void SetLevelText(int level)
    {
        m_LevelText.text = $"Level: {level}";
    }
    
    public void SetScoreText(int score)
    {
        m_ScoreText.text = $"Score: {score}";
    }
    
    private void OnStartClicked()
    {
        // 发布事件而不是直接调用逻辑
        EventManager.Fire(this, new StartGameEventArgs());
    }
    
    public override void OnDestroy()
    {
        if (m_StartButton != null)
        {
            m_StartButton.onClick.RemoveListener(OnStartClicked);
        }
    }
}
```

#### 1.2 数据绑定模式
```csharp
// 推荐：使用数据绑定简化UI更新
public class UIPlayerInfoLogic : UILogicBase
{
    private PlayerData m_PlayerData;
    private UIPlayerInfoUI m_PlayerInfoUI;
    
    public override void OnOpen()
    {
        m_PlayerData = GameEntry.GetData<PlayerData>();
        m_PlayerInfoUI = GetUIComponent<UIPlayerInfoUI>();
        
        // 绑定数据
        m_PlayerInfoUI.BindPlayerData(m_PlayerData);
        
        // 订阅数据变化
        m_PlayerData.OnPropertyChanged += OnPlayerDataChanged;
    }
    
    private void OnPlayerDataChanged(string propertyName)
    {
        m_PlayerInfoUI.UpdateProperty(propertyName);
    }
}

public class UIPlayerInfoUI : UIComponentBase
{
    private PlayerData m_PlayerData;
    
    public void BindPlayerData(PlayerData data)
    {
        m_PlayerData = data;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        SetNameText(m_PlayerData.PlayerName);
        SetLevelText(m_PlayerData.Level);
        SetExpText(m_PlayerData.Experience);
    }
    
    public void UpdateProperty(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(PlayerData.PlayerName):
                SetNameText(m_PlayerData.PlayerName);
                break;
            case nameof(PlayerData.Level):
                SetLevelText(m_PlayerData.Level);
                break;
            case nameof(PlayerData.Experience):
                SetExpText(m_PlayerData.Experience);
                break;
        }
    }
}
```

### 2. UI窗口的生命周期管理

#### 2.1 完整的生命周期处理
```csharp
public class UILoginLogic : UILogicBase
{
    private UILoginUI m_LoginUI;
    private LoginService m_LoginService;
    
    public override void OnInit()
    {
        // 初始化 - 仅执行一次
        m_LoginUI = GetUIComponent<UILoginUI>();
        m_LoginService = new LoginService();
        
        Log.Info("Login UI initialized");
    }
    
    public override void OnOpen()
    {
        // 打开 - 每次显示时执行
        m_LoginUI.ClearInputFields();
        m_LoginUI.SetFocusToUsernameField();
        
        // 订阅UI事件
        m_LoginUI.OnLoginButtonClicked += OnLoginClicked;
        m_LoginUI.OnRegisterButtonClicked += OnRegisterClicked;
        
        Log.Info("Login UI opened");
    }
    
    public override void OnClose()
    {
        // 关闭 - 隐藏时执行
        m_LoginUI.StopAllCoroutines();
        
        // 取消订阅
        m_LoginUI.OnLoginButtonClicked -= OnLoginClicked;
        m_LoginUI.OnRegisterButtonClicked -= OnRegisterClicked;
        
        Log.Info("Login UI closed");
    }
    
    public override void OnDestroy()
    {
        // 销毁 - 永久移除时执行
        m_LoginService?.Dispose();
        
        Log.Info("Login UI destroyed");
    }
    
    private void OnLoginClicked(string username, string password)
    {
        m_LoginService.Login(username, password, OnLoginComplete);
    }
    
    private void OnLoginComplete(bool success)
    {
        if (success)
        {
            UIManager.CloseUI<UILoginLogic>();
            UIManager.OpenUI<UIMainLogic>();
        }
        else
        {
            m_LoginUI.ShowErrorMessage("Login failed");
        }
    }
}
```

### 3. UI窗口间的通信

#### 3.1 使用事件系统通信
```csharp
// 推荐：通过事件系统实现窗口通信
public class UISettingsLogic : UILogicBase
{
    private UISettingsUI m_SettingsUI;
    
    public override void OnOpen()
    {
        m_SettingsUI = GetUIComponent<UISettingsUI>();
        m_SettingsUI.OnSoundVolumeChanged += OnSoundVolumeChanged;
        m_SettingsUI.OnMusicVolumeChanged += OnMusicVolumeChanged;
    }
    
    private void OnSoundVolumeChanged(float volume)
    {
        EventManager.Fire(this, new SoundVolumeChangedEventArgs { Volume = volume });
    }
    
    private void OnMusicVolumeChanged(float volume)
    {
        EventManager.Fire(this, new MusicVolumeChangedEventArgs { Volume = volume });
    }
}

public class AudioManager : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.Subscribe<SoundVolumeChangedEventArgs>(OnSoundVolumeChanged);
        EventManager.Subscribe<MusicVolumeChangedEventArgs>(OnMusicVolumeChanged);
    }
    
    private void OnDisable()
    {
        EventManager.Unsubscribe<SoundVolumeChangedEventArgs>(OnSoundVolumeChanged);
        EventManager.Unsubscribe<MusicVolumeChangedEventArgs>(OnMusicVolumeChanged);
    }
    
    private void OnSoundVolumeChanged(SoundVolumeChangedEventArgs args)
    {
        // 更新音效音量
    }
    
    private void OnMusicVolumeChanged(MusicVolumeChangedEventArgs args)
    {
        // 更新背景音乐音量
    }
}
```

#### 3.2 参数传递
```csharp
// 推荐：通过参数对象传递数据
public class UICharacterDetailsLogic : UILogicBase
{
    private UICharacterDetailsUI m_DetailsUI;
    private CharacterData m_CharacterData;
    
    public override void OnOpen(UIParameter uiParameter)
    {
        if (uiParameter is CharacterUIParameter charParam)
        {
            m_CharacterData = charParam.CharacterData;
            m_DetailsUI.SetCharacterInfo(m_CharacterData);
        }
    }
}

// 打开UI时传递参数
var param = new CharacterUIParameter { CharacterData = selectedCharacter };
UIManager.OpenUI<UICharacterDetailsLogic>(param);
```

### 4. UI性能优化

#### 4.1 UI预加载
```csharp
public class UIPreloadManager
{
    public void PreloadCommonUIs()
    {
        var uiManager = GameEntry.GetComponent<UIComponent>();
        
        // 预加载经常使用的UI
        uiManager.PreloadUI<UIMainLogic>("Assets/UI/Main.prefab");
        uiManager.PreloadUI<UISettingsLogic>("Assets/UI/Settings.prefab");
        uiManager.PreloadUI<UIInventoryLogic>("Assets/UI/Inventory.prefab");
    }
}
```

#### 4.2 UI资源复用
```csharp
// 推荐：共享UI资源
public class UIResourceCache
{
    private Dictionary<string, Object> m_ResourceCache = new Dictionary<string, Object>();
    
    public T GetCachedResource<T>(string path) where T : Object
    {
        if (!m_ResourceCache.ContainsKey(path))
        {
            m_ResourceCache[path] = Resources.Load<T>(path);
        }
        return m_ResourceCache[path] as T;
    }
}
```

#### 4.3 避免频繁的Canvas重建
```csharp
// 不推荐：每次都创建新的Canvas
for (int i = 0; i < 100; i++)
{
    var canvas = Instantiate(canvasPrefab);
}

// 推荐：使用对象池
public class UICanvasPool
{
    private Queue<Canvas> m_CanvasPool = new Queue<Canvas>();
    
    public Canvas GetCanvas()
    {
        return m_CanvasPool.Count > 0
            ? m_CanvasPool.Dequeue()
            : Instantiate(canvasPrefab);
    }
    
    public void ReturnCanvas(Canvas canvas)
    {
        canvas.gameObject.SetActive(false);
        m_CanvasPool.Enqueue(canvas);
    }
}
```

### 5. UI动画和转换

#### 5.1 UI进出动画
```csharp
public class UITransitionController
{
    public static IEnumerator FadeIn(CanvasGroup canvasGroup, float duration = 0.5f)
    {
        canvasGroup.alpha = 0f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    public static IEnumerator FadeOut(CanvasGroup canvasGroup, float duration = 0.5f)
    {
        canvasGroup.alpha = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - elapsedTime / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
}

// 在UI逻辑中使用
public override void OnOpen()
{
    StartCoroutine(UITransitionController.FadeIn(m_CanvasGroup));
}

public override void OnClose()
{
    StartCoroutine(UITransitionController.FadeOut(m_CanvasGroup));
}
```

## 代码示例

### 示例1：完整的UI窗口示例
```csharp
// UI逻辑
public class UIGamePlayHUDLogic : UILogicBase
{
    private UIGamePlayHUDUI m_HudUI;
    private GamePlayManager m_GamePlayManager;
    
    public override void OnInit()
    {
        m_HudUI = GetUIComponent<UIGamePlayHUDUI>();
        m_GamePlayManager = GameEntry.GetComponent<GamePlayComponent>();
    }
    
    public override void OnOpen()
    {
        // 监听游戏事件
        EventManager.Subscribe<PlayerHealthChangedEventArgs>(OnHealthChanged);
        EventManager.Subscribe<PlayerLevelUpEventArgs>(OnLevelUp);
        EventManager.Subscribe<GamePauseEventArgs>(OnGamePaused);
        EventManager.Subscribe<GameResumeEventArgs>(OnGameResumed);
        
        // 初始化UI
        UpdateHUD();
    }
    
    public override void OnClose()
    {
        EventManager.Unsubscribe<PlayerHealthChangedEventArgs>(OnHealthChanged);
        EventManager.Unsubscribe<PlayerLevelUpEventArgs>(OnLevelUp);
        EventManager.Unsubscribe<GamePauseEventArgs>(OnGamePaused);
        EventManager.Unsubscribe<GameResumeEventArgs>(OnGameResumed);
    }
    
    private void UpdateHUD()
    {
        var playerData = m_GamePlayManager.GetPlayerData();
        m_HudUI.SetHealthBar(playerData.CurrentHealth, playerData.MaxHealth);
        m_HudUI.SetLevelText(playerData.Level);
    }
    
    private void OnHealthChanged(PlayerHealthChangedEventArgs args)
    {
        m_HudUI.PlayDamageAnimation();
        m_HudUI.SetHealthBar(args.NewHealth, args.MaxHealth);
    }
    
    private void OnLevelUp(PlayerLevelUpEventArgs args)
    {
        m_HudUI.PlayLevelUpAnimation();
        m_HudUI.SetLevelText(args.NewLevel);
    }
    
    private void OnGamePaused(GamePauseEventArgs args)
    {
        m_HudUI.ShowPauseIndicator();
    }
    
    private void OnGameResumed(GameResumeEventArgs args)
    {
        m_HudUI.HidePauseIndicator();
    }
}

// UI组件
public class UIGamePlayHUDUI : UIComponentBase
{
    private Image m_HealthBar;
    private Text m_LevelText;
    private CanvasGroup m_PauseIndicator;
    private Animator m_Animator;
    
    public override void OnInit()
    {
        m_HealthBar = transform.Find("HealthBar").GetComponent<Image>();
        m_LevelText = transform.Find("LevelText").GetComponent<Text>();
        m_PauseIndicator = transform.Find("PauseIndicator").GetComponent<CanvasGroup>();
        m_Animator = GetComponent<Animator>();
    }
    
    public void SetHealthBar(int currentHealth, int maxHealth)
    {
        m_HealthBar.fillAmount = (float)currentHealth / maxHealth;
    }
    
    public void SetLevelText(int level)
    {
        m_LevelText.text = $"Level {level}";
    }
    
    public void PlayDamageAnimation()
    {
        m_Animator.SetTrigger("Damage");
    }
    
    public void PlayLevelUpAnimation()
    {
        m_Animator.SetTrigger("LevelUp");
    }
    
    public void ShowPauseIndicator()
    {
        m_PauseIndicator.alpha = 1f;
    }
    
    public void HidePauseIndicator()
    {
        m_PauseIndicator.alpha = 0f;
    }
}
```

### 示例2：UI列表实现
```csharp
public class UIPlayerListLogic : UILogicBase
{
    private UIPlayerListUI m_ListUI;
    private List<PlayerData> m_PlayerList;
    
    public override void OnOpen()
    {
        LoadPlayerList();
        m_ListUI.SetListData(m_PlayerList);
        m_ListUI.OnItemSelected += OnPlayerSelected;
    }
    
    private void LoadPlayerList()
    {
        m_PlayerList = GameEntry.GetData<PlayerListData>().Players;
    }
    
    private void OnPlayerSelected(int index)
    {
        var selectedPlayer = m_PlayerList[index];
        EventManager.Fire(this, new PlayerSelectedEventArgs { Player = selectedPlayer });
    }
}

public class UIPlayerListUI : UIComponentBase
{
    private UIPlayerListItem m_ItemPrefab;
    private Transform m_ListContent;
    private List<UIPlayerListItem> m_ListItems = new List<UIPlayerListItem>();
    
    public event Action<int> OnItemSelected;
    
    public void SetListData(List<PlayerData> players)
    {
        ClearList();
        
        foreach (var player in players)
        {
            var item = Instantiate(m_ItemPrefab, m_ListContent);
            item.SetData(player);
            item.OnSelected += OnItemClicked;
            m_ListItems.Add(item);
        }
    }
    
    private void OnItemClicked(int index)
    {
        OnItemSelected?.Invoke(index);
    }
    
    private void ClearList()
    {
        foreach (var item in m_ListItems)
        {
            Destroy(item.gameObject);
        }
        m_ListItems.Clear();
    }
}
```

## 性能优化

### 1. 减少Layout重建
```csharp
// 启用Canvas批处理
Canvas.willRenderCanvases += () =>
{
    if (m_NeedsRebuild)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_RectTransform);
        m_NeedsRebuild = false;
    }
};
```

### 2. 使用Pool管理动态UI元素
```csharp
public class DynamicUIElementPool
{
    private Queue<UIElement> m_Pool = new Queue<UIElement>();
    private UIElement m_Prefab;
    
    public UIElement Get()
    {
        return m_Pool.Count > 0 ? m_Pool.Dequeue() : CreateNew();
    }
    
    public void Return(UIElement element)
    {
        element.gameObject.SetActive(false);
        m_Pool.Enqueue(element);
    }
}
```

## 常见问题

### Q1: 如何处理UI的背包系统？

**A:** 使用数据驱动的列表实现，通过事件更新UI。

### Q2: 如何避免UI内存泄漏？

**A:** 在OnClose/OnDestroy中清理所有事件订阅和资源引用。

### Q3: 如何实现UI动画？

**A:** 使用Animator、Tweens或协程实现平滑的动画转换。

### Q4: 如何管理多个UI窗口的显示顺序？

**A:** 使用UI层级系统和Canvas Sorting Order。

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
