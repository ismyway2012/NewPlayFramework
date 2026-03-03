# GameFrameX 实体系统 FAQ 和快速参考

## 目录
1. [常见问题](#常见问题)
2. [快速参考](#快速参考)
3. [故障排除](#故障排除)
4. [性能优化建议](#性能优化建议)

---

## 常见问题

### Q1: EntityLogic 和 Entity 的区别是什么？

**A:** 这是理解实体系统的关键：

| 特性 | Entity | EntityLogic |
|------|--------|-------------|
| **类型** | MonoBehaviour | MonoBehaviour |
| **职责** | 生命周期管理和委派 | 业务逻辑实现 |
| **一个 GameObject 中** | 只有 1 个 | 只有 1 个（动态添加） |
| **是否可继承** | 不可以（sealed） | 必须继承 |
| **持有引用** | EntityLogic | Entity（获取 ID 等） |
| **用户编写** | 不需要 | 必须编写 |

```csharp
// 关系示例
GameObject
├─ Entity (框架层)
│  └─ m_EntityLogic
│     └─ PlayerEntity (用户实现)
│        └─ 业务逻辑
```

---

### Q2: 什么时候使用 OnInit、OnShow 和 OnRecycle？

**A:** 三个不同的生命周期阶段：

```csharp
// 对象池生命周期

// 第一次创建实体
OnInit()          // ← 只调用一次，获取组件、初始化数据结构
    ↓
OnShow()          // ← 每次显示时调用，重置状态
    ↓
[实体活跃期间]
    ↓
OnHide()          // ← 每次隐藏时调用，停止动画等
    ↓
[实体隐藏等待]
    ↓
OnShow()          // ← 再次显示（复用对象）
    ↓
OnHide()
    ↓
[最终回收]
    ↓
OnRecycle()       // ← 对象返回池前调用，清理资源

---

// 第二个实体周期（从对象池取出后重复）
OnShow()
    ↓
OnHide()
    ↓
...
```

**使用原则**：
- ? **OnInit**: 一次性初始化（获取组件、创建对象）
- ? **OnShow**: 重置状态（位置、HP、动画等）
- ? **OnHide**: 停止持续行为（动画、音效、定时器）
- ? **OnRecycle**: 彻底清理（释放资源、取消事件）

---

### Q3: 为什么实体复用时状态没有重置？

**A:** 这是最常见的对象池陷阱。问题通常是：

#### ? 常见错误
```csharp
private bool m_HasInitialized;

protected override void OnShow(object userData)
{
    base.OnShow(userData);
    
    // 这个标志在第二次显示时仍然是 true！
    if (!m_HasInitialized)
    {
        Initialize();
        m_HasInitialized = true;
    }
}
```

#### ? 正确做法
```csharp
protected override void OnShow(object userData)
{
    base.OnShow(userData);
    
    // 每次都重置，不用 if 判断
    Initialize(userData);
    m_AttackCooldown = 0f;
    m_CurrentHp = m_MaxHp;
}
```

---

### Q4: 如何处理父子实体的位置同步？

**A:** 父子关系有两种处理方式：

#### 方式 1: 自动同步（推荐）
```csharp
public class ChildEntity : EntityLogic
{
    protected override void OnAttachTo(EntityLogic parentLogic, Transform parentTransform, object userData)
    {
        base.OnAttachTo(parentLogic, parentTransform, userData);
        
        // 将自己设为子物体，自动同步变换
        CachedTransform.SetParent(parentTransform);
        CachedTransform.localPosition = Vector3.zero;
        CachedTransform.localScale = Vector3.one;
    }
}
```

#### 方式 2: 手动同步
```csharp
private Transform m_ParentTransform;

protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    base.OnUpdate(elapseSeconds, realElapseSeconds);
    
    if (m_ParentTransform != null)
    {
        // 手动更新位置
        CachedTransform.position = m_ParentTransform.position + m_Offset;
    }
}
```

---

### Q5: 实体显示失败，如何调试？

**A:** 检查以下几点，按优先级：

1. **检查资源路径**
   ```csharp
   // 确保路径正确
   await entityManager.ShowEntityAsync("Group", "Assets/Prefabs/Enemy.prefab", null);
   //                                               ↑ 路径必须正确
   ```

2. **订阅失败事件**
   ```csharp
   entityManager.ShowEntityFailure += (sender, e) =>
   {
       Log.Error($"Show entity failed: {e.EntityAssetName}");
       Log.Error($"Error: {e.ErrorMessage}");
   };
   ```

3. **检查 EntityLogic 类型**
   ```csharp
   var showData = new ShowEntityInfo
   {
       EntityLogicType = typeof(EnemyEntity),  // 确保类型存在且可实例化
       UserData = null
   };
   ```

4. **检查对象池配置**
   ```csharp
   var group = entityManager.GetEntityGroup("Enemies");
   if (group.InstanceAutoReleaseInterval < 30)
   {
       // 实体可能被自动释放了
   }
   ```

---

### Q6: 内存泄漏怎么排查？

**A:** 常见的泄漏来源和排查方法：

#### 泄漏原因 1: 委托未取消
```csharp
// ? 泄漏代码
protected override void OnInit(object userData)
{
    EventManager.Subscribe<AttackEvent>(OnAttack);
    // 从不取消订阅
}

// ? 修复
protected override void OnRecycle()
{
    base.OnRecycle();
    EventManager.Unsubscribe<AttackEvent>(OnAttack);
}
```

#### 泄漏原因 2: 子物体未销毁
```csharp
// ? 泄漏代码
protected override void OnInit(object userData)
{
    var child = new GameObject("Effect");
    child.transform.SetParent(CachedTransform);
    // 从不销毁
}

// ? 修复
protected override void OnRecycle()
{
    base.OnRecycle();
    foreach (Transform child in CachedTransform)
    {
        Destroy(child.gameObject);
    }
}
```

#### 泄漏原因 3: 缓存引用
```csharp
// ? 泄漏代码
public static EntityLogic s_LastEntity;

protected override void OnInit(object userData)
{
    s_LastEntity = this;  // 静态引用导致 GC 无法回收
}

// ? 修复：避免静态引用，或在 OnRecycle 中清理
protected override void OnRecycle()
{
    base.OnRecycle();
    s_LastEntity = null;
}
```

#### 排查工具：使用 Profiler
```csharp
// 在编辑器中测试
1. Window → Analysis → Profiler
2. 切换到 Memory 标签
3. 点击 "Take Sample"
4. 查看 "EntityLogic" 及其子类的内存占用
5. 如果隐藏实体后内存未释放，说明存在泄漏
```

---

### Q7: userData 应该如何设计？

**A:** userData 的设计原则：

#### ? 错误设计
```csharp
// 混合多个概念
object userData;  // 什么都可以放，容易出错

// 使用方式
await ShowEntity("Group", "Prefab.prefab", new { hp = 100, pos = Vector3.zero, ... });
// 接收时需要多次转换
```

#### ? 正确设计
```csharp
// 分离职责
public class EntityInitData
{
    public int Level { get; set; }
    public int MaxHp { get; set; }
}

public class EntityShowData
{
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
}

// OnInit 中接收初始化数据
protected override void OnInit(object userData)
{
    if (userData is EntityInitData initData)
    {
        InitializeWithData(initData);
    }
}

// OnShow 中接收显示数据
protected override void OnShow(object userData)
{
    if (userData is EntityShowData showData)
    {
        ApplyShowData(showData);
    }
}
```

---

### Q8: 实体数量过多时性能下降，如何优化？

**A:** 性能优化检查清单：

| 问题 | 表现 | 解决方案 |
|------|------|---------|
| **Update 调用过多** | CPU 占用高 | 减少 Update 中的计算，使用 LateUpdate 或定时更新 |
| **查询操作低效** | 实体多时卡顿 | 缓存查询结果，避免频繁遍历 |
| **对象池配置不当** | 内存占用高 | 调整 InstanceCapacity 和 InstanceAutoReleaseInterval |
| **GC.Alloc** | 内存波动 | 避免在 Update 中创建列表、数组 |
| **Physics 检测** | 帧率低 | 使用 JobSystem 或优化碰撞检测范围 |

---

### Q9: 如何实现实体间的通信？

**A:** 推荐三种方式（按推荐度排序）：

#### 1. 事件系统（强烈推荐）
```csharp
// 定义事件
public class DamageEvent
{
    public int Attacker { get; set; }
    public int Target { get; set; }
    public int Damage { get; set; }
}

// 发送方
public class WeaponEntity : EntityLogic
{
    private void OnHitTarget(int targetId)
    {
        EventManager.Send(new DamageEvent
        {
            Attacker = Entity.Id,
            Target = targetId,
            Damage = 10
        });
    }
}

// 接收方
public class CharacterEntity : EntityLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        EventManager.Subscribe<DamageEvent>(OnDamageReceived);
    }
    
    protected override void OnRecycle()
    {
        base.OnRecycle();
        EventManager.Unsubscribe<DamageEvent>(OnDamageReceived);
    }
    
    private void OnDamageReceived(DamageEvent e)
    {
        if (e.Target == Entity.Id)
        {
            TakeDamage(e.Damage);
        }
    }
}
```

#### 2. 直接引用（用于紧密耦合的关系）
```csharp
public class WeaponEntity : EntityLogic
{
    private CharacterEntity m_Owner;
    
    public void SetOwner(CharacterEntity owner)
    {
        m_Owner = owner;
    }
    
    private void OnHitTarget(CharacterEntity target)
    {
        target.TakeDamage(10);
    }
}
```

#### 3. Manager 中介（用于复杂的多方通信）
```csharp
public class CombatManager : MonoBehaviour
{
    public void RequestDamage(int attacker, int target, int damage)
    {
        var targetEntity = entityManager.GetEntity(target);
        if (targetEntity is CharacterEntity character)
        {
            character.TakeDamage(damage);
        }
    }
}
```

---

## 快速参考

### EntityLogic 生命周期速查表

```csharp
public class QuickReferenceEntity : EntityLogic
{
    // ===== 初始化周期 =====
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // ? 获取组件引用
        // ? 初始化数据结构
        // ? 订阅事件（长期订阅）
        // ? 不要修改 GameObject 活跃状态
    }
    
    // ===== 显示周期 =====
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        // ? 重置所有状态变量
        // ? 应用初始位置、旋转、缩放
        // ? 播放进场动画
        // ? 设置 Visible = true
        // ? 不要在这里创建新对象
    }
    
    // ===== 更新周期 =====
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        // ? 输入处理
        // ? 位置更新
        // ? 动画控制
        // ? 不要进行复杂计算（用协程）
        // ? 不要创建临时对象
    }
    
    // ===== 隐藏周期 =====
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        // ? 停止动画
        // ? 停止音效
        // ? 禁用物理
        // ? 设置 Visible = false
        // ? 保存临时状态（如果需要）
        // ? 不要释放 OnInit 中获取的资源
    }
    
    // ===== 回收周期 =====
    protected override void OnRecycle()
    {
        base.OnRecycle();
        // ? 释放托管资源
        // ? 取消所有事件订阅
        // ? 清理所有引用
        // ? 销毁动态创建的子物体
        // ? 不要调用 Destroy
    }
    
    // ===== 父子关系 =====
    protected override void OnAttached(EntityLogic childLogic, Transform parentTransform, object userData)
    {
        base.OnAttached(childLogic, parentTransform, userData);
        // 本实体作为子实体被附加到其他实体
    }
    
    protected override void OnDetached(EntityLogic childLogic, object userData)
    {
        base.OnDetached(childLogic, userData);
        // 子实体从本实体分离
    }
    
    protected override void OnAttachTo(EntityLogic parentLogic, Transform parentTransform, object userData)
    {
        base.OnAttachTo(parentLogic, parentTransform, userData);
        // 本实体附加到其他实体
        // ? 在这里设置 SetParent
        // ? 重置相对位置
    }
    
    protected override void OnDetachFrom(EntityLogic parentLogic, object userData)
    {
        base.OnDetachFrom(parentLogic, userData);
        // 本实体从其他实体分离
    }
}
```

### 常用 API 速查

```csharp
// ===== EntityManager API =====

// 实体创建
await entityManager.ShowEntityAsync("GroupName", "Assets/Prefabs/Entity.prefab", userData);

// 实体查询
bool has = entityManager.HasEntity(entityId);
IEntity entity = entityManager.GetEntity(entityId);
IEnumerable<IEntity> allInGroup = entityManager.GetEntitiesInGroup("GroupName");

// 实体隐藏
entityManager.HideEntity(entity);

// 实体组
IEntityGroup group = entityManager.GetEntityGroup("GroupName");
group.InstanceCapacity = 100;
group.InstanceAutoReleaseInterval = 300f;

// 统计
int totalCount = entityManager.EntityCount;
int groupCount = entityManager.EntityGroupCount;

// ===== EntityLogic API =====

// 属性访问
Entity         // 获取关联的 Entity
Name           // 实体名称
Available      // 是否已初始化
Visible        // 是否可见
CachedTransform // 缓存的 Transform

// 发送数据到实体
public void TestShowEntity()
{
    var data = new { hp = 100, pos = Vector3.zero };
    entityManager.ShowEntity("Group", "Prefab.prefab", data);
}

// ===== 实体组操作 =====

// 创建实体组
entityManager.CreateEntityGroup("EnemyGroup");

// 获取实体组
var group = entityManager.GetEntityGroup("EnemyGroup");

// 配置对象池
group.InstanceCapacity = 50;                    // 初始容量
group.InstanceAutoReleaseInterval = 300f;       // 自动释放间隔
group.InstanceExpireTime = 600f;                // 过期时间
```

---

## 故障排除

### 问题 1: "Entity with ID xxx is already in loading queue"

**原因**: 同一实体被多次 ShowEntity 调用

**解决**:
```csharp
// ? 问题代码
for (int i = 0; i < 5; i++)
{
    await entityManager.ShowEntity("Group", "Prefab.prefab", i);  // 重复调用
}

// ? 修复
var tasks = new List<Task>();
for (int i = 0; i < 5; i++)
{
    tasks.Add(entityManager.ShowEntityAsync("Group", "Prefab.prefab", i));
}
await Task.WhenAll(tasks);
```

---

### 问题 2: "Invalid cast from UserData"

**原因**: userData 类型转换错误

**解决**:
```csharp
// ? 问题代码
protected override void OnShow(object userData)
{
    var data = (PlayerData)userData;  // 可能抛异常
}

// ? 修复
protected override void OnShow(object userData)
{
    if (userData is PlayerData data)
    {
        // 安全处理
    }
    else
    {
        Log.Warning("Invalid userData type");
    }
}
```

---

### 问题 3: 实体显示但看不见

**原因**: 多种可能性

**检查清单**:
```csharp
// 1. 检查 Visible 属性
if (!entityLogic.Visible)
{
    entityLogic.Visible = true;
}

// 2. 检查 GameObject 活跃
if (!gameObject.activeSelf)
{
    gameObject.SetActive(true);
}

// 3. 检查渲染器
var renderer = GetComponent<Renderer>();
if (renderer != null && !renderer.enabled)
{
    renderer.enabled = true;
}

// 4. 检查层级和摄像机
Debug.Log($"Layer: {gameObject.layer}");
Debug.Log($"Camera culling mask: {Camera.main.cullingMask}");

// 5. 检查位置
Debug.Log($"Position: {CachedTransform.position}");
Debug.Log($"Camera position: {Camera.main.transform.position}");
```

---

### 问题 4: OnUpdate 不被调用

**原因**: 实体不可用或分组未启用

**解决**:
```csharp
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    base.OnUpdate(elapseSeconds, realElapseSeconds);
    
    // 首先检查可用性
    if (!Available)
    {
        Debug.Log("Entity not available yet");
        return;
    }
    
    if (!Visible)
    {
        Debug.Log("Entity not visible");
        return;
    }
    
    // 然后执行业务逻辑
    DoUpdate(elapseSeconds);
}
```

---

## 性能优化建议

### 优化 1: 减少 Update 中的操作

```csharp
// ? 低效
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    // 每帧进行复杂计算
    CalculatePath();
    CheckAllEnemiesDistance();
    UpdateAnimation();
}

// ? 高效
private float m_CalculatePathTimer;
private float m_CheckEnemyTimer = 0.5f;  // 0.5秒检查一次

protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    // 使用定时器
    m_CalculatePathTimer -= elapseSeconds;
    if (m_CalculatePathTimer <= 0)
    {
        CalculatePath();
        m_CalculatePathTimer = 1f;  // 1秒计算一次
    }
    
    m_CheckEnemyTimer -= elapseSeconds;
    if (m_CheckEnemyTimer <= 0)
    {
        CheckAllEnemiesDistance();
        m_CheckEnemyTimer = 0.5f;
    }
    
    // 每帧更新动画（必须）
    UpdateAnimation();
}
```

### 优化 2: 使用对象池预热

```csharp
// 在关卡开始前预热对象池
private async Task WarmupPool(string groupName, string assetName, int count)
{
    var tasks = new List<Task>();
    
    for (int i = 0; i < count; i++)
    {
        tasks.Add(entityManager.ShowEntityAsync(groupName, assetName, null));
    }
    
    await Task.WhenAll(tasks);
    
    // 立即隐藏所有实体
    var entities = entityManager.GetEntitiesInGroup(groupName).ToList();
    foreach (var entity in entities)
    {
        entityManager.HideEntity(entity);
    }
}
```

### 优化 3: 缓存查询结果

```csharp
// ? 低效（每帧遍历）
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    var enemies = entityManager.GetEntitiesInGroup("Enemies")  // O(n) 每帧
        .ToList();
    
    foreach (var enemy in enemies)
    {
        // 处理
    }
}

// ? 高效（缓存列表）
private List<EnemyEntity> m_CachedEnemies = new();
private float m_CacheUpdateTimer = 0.1f;

protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    m_CacheUpdateTimer -= elapseSeconds;
    if (m_CacheUpdateTimer <= 0)
    {
        UpdateEnemyCache();
        m_CacheUpdateTimer = 0.1f;
    }
    
    foreach (var enemy in m_CachedEnemies)
    {
        // 处理
    }
}

private void UpdateEnemyCache()
{
    m_CachedEnemies.Clear();
    m_CachedEnemies.AddRange(
        entityManager.GetEntitiesInGroup("Enemies")
            .OfType<EnemyEntity>()
    );
}
```

---

## 推荐学习路径

```
初级→ 01-EntitySystemArchitecture.md (了解架构)
  │
  ├→ 03-EntityCodeExamples (看简单示例)
  │
  └→ 02-EntityBestPractices (学习规范)
  
中级→ 03-EntityCodeExamples (学习进阶示例)
  │
  ├→ 本 FAQ (解决问题)
  │
  └→ 性能优化部分 (优化代码)
  
高级→ 改进建议 (理解限制)
  │
  └→ 自定义扩展 (修改框架)
```

**预计学习时间**: 2-3 小时可以掌握基础，1-2 周可以成为高手。
