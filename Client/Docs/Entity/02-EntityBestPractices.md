# GameFrameX 实体系统最佳实践指南

## 目录
1. [开发规范](#开发规范)
2. [常见陷阱](#常见陷阱)
3. [性能优化](#性能优化)
4. [测试策略](#测试策略)
5. [实战案例](#实战案例)

---

## 开发规范

### 1. EntityLogic 编写规范

#### 1.1 完整的生命周期实现

```csharp
/// <summary>
/// 示例实体逻辑：玩家角色
/// </summary>
public class PlayerEntity : EntityLogic
{
    // 配置属性
    [SerializeField] private float m_MoveSpeed = 10f;
    [SerializeField] private float m_RotationSpeed = 8f;
    
    // 状态数据
    private PlayerData m_PlayerData;
    private Animator m_Animator;
    private CharacterController m_CharacterController;
    private Vector3 m_MoveInput;
    
    // 资源管理
    private List<IDisposable> m_Resources = new();
    private AudioSource m_AudioSource;
    
    /// <summary>
    /// 初始化阶段 - 仅在第一次创建时调用
    /// 用途: 获取组件引用、初始化数据结构
    /// </summary>
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        // 1. 参数验证
        if (userData == null)
        {
            Log.Error("PlayerEntity requires PlayerInitData");
            return;
        }
        
        var initData = userData as PlayerInitData;
        if (initData == null)
        {
            Log.Error("Invalid userData type for PlayerEntity");
            return;
        }
        
        // 2. 获取组件
        m_CharacterController = gameObject.GetComponent<CharacterController>();
        m_Animator = gameObject.GetComponent<Animator>();
        m_AudioSource = gameObject.GetComponent<AudioSource>();
        
        if (m_CharacterController == null || m_Animator == null)
        {
            Log.Error("Required components missing on PlayerEntity");
            return;
        }
        
        // 3. 初始化数据
        m_PlayerData = new PlayerData
        {
            PlayerId = initData.PlayerId,
            Level = initData.Level,
            MaxHp = initData.MaxHp,
            CurrentHp = initData.MaxHp
        };
        
        m_MoveInput = Vector3.zero;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[PlayerEntity] Initialized: ID={Entity.Id}, Level={initData.Level}");
        #endif
    }
    
    /// <summary>
    /// 显示阶段 - 每次显示时调用（包括从对象池取出）
    /// 用途: 重置状态、播放出场动画、UI 更新
    /// </summary>
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 1. 参数处理
        if (userData is not PlayerShowData showData)
        {
            Log.Warning("Invalid show data for PlayerEntity");
            return;
        }
        
        // 2. 重置物理状态
        m_CharacterController.enabled = true;
        
        // 3. 应用初始位置和方向
        CachedTransform.position = showData.Position;
        CachedTransform.rotation = Quaternion.Euler(0, showData.Rotation, 0);
        
        // 4. 重置状态变量
        m_MoveInput = Vector3.zero;
        m_PlayerData.CurrentHp = m_PlayerData.MaxHp;
        
        // 5. 播放进场动画
        m_Animator.SetBool("IsAlive", true);
        
        // 6. 启用可见性和交互
        Visible = true;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[PlayerEntity] Shown at position {showData.Position}");
        #endif
    }
    
    /// <summary>
    /// 隐藏阶段 - 实体离开时调用
    /// 用途: 停止动画、清理临时状态、保存数据
    /// 注意: 不要释放持久数据（在 OnRecycle 中释放）
    /// </summary>
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        
        // 1. 停止当前动画
        m_Animator.SetBool("IsAlive", false);
        m_MoveInput = Vector3.zero;
        
        // 2. 禁用物理
        m_CharacterController.enabled = false;
        
        // 3. 停止音效
        if (m_AudioSource != null && m_AudioSource.isPlaying)
        {
            m_AudioSource.Stop();
        }
        
        // 4. 隐藏视觉
        Visible = false;
        
        // 5. 保存临时数据（如果需要）
        if (!isShutdown)
        {
            // 保存玩家数据供下次使用
            SavePlayerState();
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[PlayerEntity] Hidden (isShutdown={isShutdown})");
        #endif
    }
    
    /// <summary>
    /// 回收阶段 - 对象返回池中前调用
    /// 用途: 释放资源、取消事件订阅、清理引用
    /// 重要: 必须彻底清理，为下一个生命周期做准备
    /// </summary>
    protected override void OnRecycle()
    {
        base.OnRecycle();
        
        // 1. 释放托管资源
        foreach (var resource in m_Resources)
        {
            resource?.Dispose();
        }
        m_Resources.Clear();
        
        // 2. 取消事件订阅
        EventManager.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventManager.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        
        // 3. 清理数据引用
        m_PlayerData = null;
        m_MoveInput = Vector3.zero;
        
        // 4. 重置配置
        m_Animator = null;
        m_CharacterController = null;
        m_AudioSource = null;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[PlayerEntity] Recycled");
        #endif
    }
    
    /// <summary>
    /// 更新阶段 - 每帧调用
    /// 用途: 输入处理、位置更新、状态检查
    /// 性能提示: 避免频繁 GC、大量计算应该延迟到需要时
    /// </summary>
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        if (!Available) return;
        
        // 1. 更新输入
        UpdateInput();
        
        // 2. 更新位置
        if (m_MoveInput.sqrMagnitude > 0.01f)
        {
            UpdateMovement(elapseSeconds);
        }
        
        // 3. 更新动画
        m_Animator.SetFloat("Speed", m_MoveInput.magnitude);
        
        // 4. 检查生命值
        if (m_PlayerData.CurrentHp <= 0)
        {
            HandleDeath();
        }
    }
    
    /// <summary>
    /// 子实体附加回调
    /// 用途: 武器、装备等子实体挂载时的处理
    /// </summary>
    protected override void OnAttached(EntityLogic childLogic, Transform parentTransform, object userData)
    {
        base.OnAttached(childLogic, parentTransform, userData);
        
        Log.Info($"Child entity attached: {childLogic.Name}");
        
        // 根据子实体类型进行特殊处理
        if (childLogic is WeaponEntity weaponEntity)
        {
            OnWeaponAttached(weaponEntity);
        }
    }
    
    /// <summary>
    /// 子实体分离回调
    /// 用途: 武器、装备等子实体移除时的处理
    /// </summary>
    protected override void OnDetached(EntityLogic childLogic, object userData)
    {
        base.OnDetached(childLogic, userData);
        
        Log.Info($"Child entity detached: {childLogic.Name}");
        
        if (childLogic is WeaponEntity weaponEntity)
        {
            OnWeaponDetached(weaponEntity);
        }
    }
    
    // ==================== 私有辅助方法 ====================
    
    private void UpdateInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        m_MoveInput = new Vector3(horizontal, 0, vertical).normalized;
    }
    
    private void UpdateMovement(float deltaTime)
    {
        Vector3 moveDirection = m_MoveInput * m_MoveSpeed * deltaTime;
        m_CharacterController.Move(moveDirection);
        
        // 旋转角色面向
        if (m_MoveInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(m_MoveInput);
            CachedTransform.rotation = Quaternion.Lerp(
                CachedTransform.rotation,
                targetRotation,
                m_RotationSpeed * Time.deltaTime
            );
        }
    }
    
    private void HandleDeath()
    {
        Visible = false;
        m_Animator.SetBool("IsAlive", false);
        
        // 触发死亡事件
        EventManager.Send(new PlayerDeadEvent { PlayerId = m_PlayerData.PlayerId });
        
        // 延迟隐藏实体
        Invoke(nameof(HideAfterDeath), 2f);
    }
    
    private void HideAfterDeath()
    {
        Entity.EntityGroup.HideEntity(Entity);
    }
    
    private void SavePlayerState()
    {
        // 保存玩家数据到持久层
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        if (e.TargetId == Entity.Id)
        {
            m_PlayerData.CurrentHp -= e.Damage;
            OnDamageReceived(e.Damage, e.SourceId);
        }
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent e)
    {
        if (e.PlayerId == Entity.Id)
        {
            m_PlayerData.Level = e.NewLevel;
            UpdatePowerStats();
        }
    }
    
    private void OnWeaponAttached(WeaponEntity weapon)
    {
        // 武器特殊处理
    }
    
    private void OnWeaponDetached(WeaponEntity weapon)
    {
        // 武器卸载处理
    }
    
    private void OnDamageReceived(int damage, int sourceId)
    {
        // 伤害反馈
    }
    
    private void UpdatePowerStats()
    {
        // 更新战斗属性
    }
}
```

#### 1.2 数据结构定义规范

```csharp
/// <summary>
/// 实体初始化数据
/// </summary>
public class PlayerInitData
{
    public int PlayerId { get; set; }
    public int Level { get; set; }
    public int MaxHp { get; set; }
}

/// <summary>
/// 实体显示数据
/// </summary>
public class PlayerShowData
{
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
}

/// <summary>
/// 运行时数据（不应该在 userData 中传递）
/// </summary>
public class PlayerData
{
    public int PlayerId { get; set; }
    public int Level { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
}
```

### 2. EntityLogic 继承树规范

```csharp
// 基础层
public abstract class EntityLogic { }

// 功能层
public abstract class CombatUnitEntity : EntityLogic
{
    public int Hp { get; protected set; }
    public int MaxHp { get; protected set; }
    public abstract void TakeDamage(int damage);
    public abstract void Die();
}

// 具体实现
public class PlayerEntity : CombatUnitEntity { }
public class EnemyEntity : CombatUnitEntity { }
public class BossEntity : EnemyEntity { }

// 特效和临时对象
public class EffectEntity : EntityLogic { }
public class ProjectileEntity : EntityLogic { }
```

### 3. 命名规范

```csharp
// ? 推荐
public class PlayerEntity : EntityLogic { }           // 玩家实体
public class EnemyEntity : EntityLogic { }           // 敌人实体
public class NPCEntity : EntityLogic { }             // NPC 实体
public class BulletEntity : EntityLogic { }          // 子弹实体
public class EffectEntity : EntityLogic { }          // 特效实体

// ? 不推荐
public class Player : EntityLogic { }                // 名字太短，不清晰
public class EnemyLogic : EntityLogic { }            // 已经继承于 EntityLogic，不需要后缀
public class PlayerView : EntityLogic { }            // 混淆 View 和 Logic 的概念

// 方法命名
protected virtual void OnInit(object userData) { }       // 初始化
protected virtual void OnShow(object userData) { }       // 显示
protected virtual void OnHide(bool isShutdown, object userData) { }  // 隐藏
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds) { }  // 更新
```

---

## 常见陷阱

### 陷阱 1: 资源泄漏

#### ? 错误示例
```csharp
public class EnemyEntity : EntityLogic
{
    private AudioSource m_AudioSource;
    private ParticleSystem m_EffectParticles;
    
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        // 创建了新的 AudioSource 但从未释放
        GameObject audioGO = new GameObject("Audio");
        audioGO.transform.SetParent(CachedTransform);
        m_AudioSource = audioGO.AddComponent<AudioSource>();
    }
    
    // 忘记在 OnHide 或 OnRecycle 中清理
}
```

#### ? 正确示例
```csharp
public class EnemyEntity : EntityLogic
{
    private AudioSource m_AudioSource;
    private ParticleSystem m_EffectParticles;
    
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        // 使用现有的子物体或从 Prefab 继承
        m_AudioSource = GetComponent<AudioSource>();
        m_EffectParticles = GetComponent<ParticleSystem>();
    }
    
    protected override void OnRecycle()
    {
        base.OnRecycle();
        
        // 确保清理所有引用
        m_AudioSource = null;
        m_EffectParticles = null;
    }
}
```

### 陷阱 2: 委托未取消订阅

#### ? 错误示例
```csharp
public class PlayerEntity : EntityLogic
{
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 在 OnShow 中订阅
        EventManager.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
    }
    
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        // 忘记取消订阅！内存泄漏
    }
}
```

#### ? 正确示例
```csharp
public class PlayerEntity : EntityLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        // 在初始化时订阅一次
        EventManager.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
    }
    
    protected override void OnRecycle()
    {
        base.OnRecycle();
        
        // 在回收时统一取消订阅
        EventManager.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        // 处理伤害
    }
}
```

### 陷阱 3: 对象池复用问题

#### ? 错误示例
```csharp
public class EnemyEntity : EntityLogic
{
    private float m_AttackCooldown;  // 没有重置
    private bool m_HasInitialized;
    
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 因为 m_HasInitialized 没有重置，初始化只发生一次
        if (!m_HasInitialized)
        {
            InitializeStats();
            m_HasInitialized = true;
        }
        
        // m_AttackCooldown 保留了上一个实例的值！
    }
}
```

#### ? 正确示例
```csharp
public class EnemyEntity : EntityLogic
{
    private float m_AttackCooldown;
    
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 每次显示时都重置状态
        m_AttackCooldown = 0f;
        InitializeStats(userData);
    }
}
```

### 陷阱 4: userData 类型转换异常

#### ? 错误示例
```csharp
public class EnemyEntity : EntityLogic
{
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 直接转换，如果类型不匹配会抛异常
        var data = (EnemyShowData)userData;  // 可能抛 InvalidCastException
        ApplyShowData(data);
    }
}
```

#### ? 正确示例
```csharp
public class EnemyEntity : EntityLogic
{
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 安全的类型检查
        if (userData is EnemyShowData data)
        {
            ApplyShowData(data);
        }
        else if (userData == null)
        {
            Log.Warning("EnemyEntity.OnShow called with null userData");
            ApplyDefaultShowData();
        }
        else
        {
            Log.Error($"Invalid userData type: {userData.GetType()}");
            ApplyDefaultShowData();
        }
    }
}
```

### 陷阱 5: 父子关系循环

#### ? 错误示例
```csharp
// 在运行时，可能发生循环关系
entityManager.AttachEntity(parentEntity, childEntity);      // A → B
entityManager.AttachEntity(childEntity, parentEntity);      // B → A（循环！）
```

#### ? 正确示例
```csharp
// 验证关系合法性
public bool CanAttach(IEntity parent, IEntity child)
{
    // 检查循环
    if (parent.Id == child.Id) return false;
    
    var current = parent.EntityGroup.GetParentEntity(parent.Id);
    while (current != null)
    {
        if (current.Id == child.Id) return false;
        current = parent.EntityGroup.GetParentEntity(current.Id);
    }
    
    return true;
}
```

---

## 性能优化

### 1. 批量操作优化

```csharp
// ? 低效：逐个创建
for (int i = 0; i < 100; i++)
{
    await entityManager.ShowEntityAsync("Enemies", "Enemy.prefab", null);
}

// ? 高效：批量加载
var tasks = new Task[100];
for (int i = 0; i < 100; i++)
{
    tasks[i] = entityManager.ShowEntityAsync("Enemies", "Enemy.prefab", null);
}
await Task.WhenAll(tasks);
```

### 2. Update 优化

```csharp
// ? 低效：每帧都进行查询
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    var allEnemies = entityManager.GetAllEntities()  // O(n)
        .Where(e => e.EntityGroup.GroupName == "Enemies");
    
    foreach (var enemy in allEnemies)
    {
        UpdateCombat(enemy);
    }
}

// ? 高效：缓存引用
private List<EntityLogic> m_EnemyList;

protected override void OnInit(object userData)
{
    base.OnInit(userData);
    m_EnemyList = entityManager.GetEntitiesInGroup("Enemies").ToList();
}

protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    foreach (var enemy in m_EnemyList)  // O(1) 访问
    {
        UpdateCombat(enemy);
    }
}
```

### 3. 对象池优化

```csharp
// 配置对象池参数以降低内存消耗
public void ConfigureEntityGroup(EntityGroup group)
{
    group.InstanceAutoReleaseInterval = 300f;      // 5分钟自动释放
    group.InstanceCapacity = 50;                   // 预热到 50 个
    group.InstanceExpireTime = 600f;               // 10分钟过期
}
```

### 4. 避免 GC Alloc

```csharp
// ? 在 Update 中分配内存
protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    List<EnemyEntity> enemies = new List<EnemyEntity>();  // GC Alloc!
    
    foreach (var enemy in GetEnemies())
    {
        enemies.Add(enemy);
    }
}

// ? 复用列表
private List<EnemyEntity> m_EnemyBuffer = new();

protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    m_EnemyBuffer.Clear();
    
    foreach (var enemy in GetEnemies())
    {
        m_EnemyBuffer.Add(enemy);
    }
}
```

---

## 测试策略

### 1. 单元测试

```csharp
[TestFixture]
public class PlayerEntityTests
{
    private GameObject m_TestGameObject;
    private PlayerEntity m_PlayerEntity;
    private Entity m_Entity;
    
    [SetUp]
    public void Setup()
    {
        m_TestGameObject = new GameObject("TestPlayer");
        m_Entity = m_TestGameObject.AddComponent<Entity>();
        m_PlayerEntity = m_TestGameObject.AddComponent<PlayerEntity>();
    }
    
    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(m_TestGameObject);
    }
    
    [Test]
    public void OnInit_WithValidData_InitializesCorrectly()
    {
        // Arrange
        var initData = new PlayerInitData { PlayerId = 1, Level = 10, MaxHp = 100 };
        
        // Act
        m_PlayerEntity.OnInit(1, "Player.prefab", null, true, initData);
        
        // Assert
        Assert.AreEqual(m_PlayerEntity.Entity.Id, 1);
        Assert.IsNotNull(m_PlayerEntity.Entity);
    }
    
    [Test]
    public void OnHide_WithoutShutdown_SavesState()
    {
        // Arrange
        var initData = new PlayerInitData { PlayerId = 1, Level = 10, MaxHp = 100 };
        m_PlayerEntity.OnInit(1, "Player.prefab", null, true, initData);
        
        // Act
        m_PlayerEntity.OnShow(new PlayerShowData());
        m_PlayerEntity.OnHide(false, null);
        
        // Assert
        Assert.False((m_TestGameObject as GameObject).activeSelf);
    }
}
```

### 2. 集成测试

```csharp
[TestFixture]
public class EntityManagerIntegrationTests
{
    private EntityComponent m_EntityComponent;
    private IEntityManager m_EntityManager;
    
    [SetUp]
    public void Setup()
    {
        var go = new GameObject();
        m_EntityComponent = go.AddComponent<EntityComponent>();
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
    }
    
    [UnityTest]
    public IEnumerator ShowAndHideEntity_CompletesSuccessfully()
    {
        // Arrange
        string groupName = "TestGroup";
        string assetName = "Assets/Prefabs/TestEntity.prefab";
        
        // Act
        yield return new WaitForSeconds(0.1f);
        m_EntityManager.ShowEntity(groupName, assetName, null);
        
        yield return new WaitForSeconds(0.5f);
        
        // Assert
        Assert.AreEqual(1, m_EntityManager.EntityCount);
        
        // Clean up
        var entity = m_EntityManager.GetEntity(1);
        m_EntityManager.HideEntity(entity);
        
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(0, m_EntityManager.EntityCount);
    }
}
```

---

## 实战案例

### 案例 1: 敌人波次生成系统

```csharp
public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private int m_WaveCount = 5;
    [SerializeField] private int m_EnemiesPerWave = 10;
    [SerializeField] private float m_SpawnInterval = 0.5f;
    [SerializeField] private Vector3 m_SpawnPosition = Vector3.zero;
    
    private IEntityManager m_EntityManager;
    private int m_CurrentWave;
    private float m_SpawnTimer;
    
    private void OnEnable()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
        m_CurrentWave = 0;
        m_SpawnTimer = 0f;
    }
    
    private void Update()
    {
        if (m_CurrentWave >= m_WaveCount) return;
        
        m_SpawnTimer += Time.deltaTime;
        if (m_SpawnTimer >= m_SpawnInterval)
        {
            m_SpawnTimer -= m_SpawnInterval;
            SpawnEnemy();
        }
    }
    
    private void SpawnEnemy()
    {
        int enemyCount = m_EntityManager.GetEntitiesInGroup("Enemies").Count();
        
        if (enemyCount >= m_EnemiesPerWave)
        {
            m_CurrentWave++;
            return;
        }
        
        var showData = new EnemyShowData
        {
            Position = m_SpawnPosition + Random.insideUnitSphere * 5f,
            Level = m_CurrentWave + 1
        };
        
        m_EntityManager.ShowEntity("Enemies", "Assets/Prefabs/Enemy.prefab", showData);
    }
}
```

### 案例 2: 装备系统

```csharp
public class EquipmentSystem : EntityLogic
{
    private Dictionary<EquipmentSlot, WeaponEntity> m_EquipmentMap = 
        new Dictionary<EquipmentSlot, WeaponEntity>();
    
    private IEntityManager m_EntityManager;
    
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
    }
    
    public void EquipWeapon(EquipmentSlot slot, string weaponAssetName)
    {
        // 1. 卸载旧装备
        if (m_EquipmentMap.TryGetValue(slot, out var oldWeapon))
        {
            DetachEquipment(slot, oldWeapon);
        }
        
        // 2. 装备新武器
        var weaponData = new WeaponShowData { Slot = slot };
        m_EntityManager.ShowEntity("Equipment", weaponAssetName, 
            (weapon) => OnWeaponLoaded(weapon, slot));
    }
    
    private void OnWeaponLoaded(IEntity weapon, EquipmentSlot slot)
    {
        var weaponLogic = weapon as WeaponEntity;
        
        // 3. 附加到角色
        m_EntityManager.AttachEntity(weapon, Entity, 
            GetSlotTransform(slot));
        
        m_EquipmentMap[slot] = weaponLogic;
        
        // 4. 触发装备事件
        EventManager.Send(new WeaponEquippedEvent 
        { 
            EquipperId = Entity.Id,
            Slot = slot,
            Weapon = weaponLogic
        });
    }
    
    private void DetachEquipment(EquipmentSlot slot, WeaponEntity weapon)
    {
        m_EntityManager.DetachEntity(weapon.Entity);
        m_EntityManager.HideEntity(weapon.Entity);
        m_EquipmentMap.Remove(slot);
    }
    
    private Transform GetSlotTransform(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.LeftHand => CachedTransform.Find("LeftHand"),
            EquipmentSlot.RightHand => CachedTransform.Find("RightHand"),
            EquipmentSlot.Back => CachedTransform.Find("Back"),
            _ => CachedTransform
        };
    }
}

public enum EquipmentSlot { LeftHand, RightHand, Back }

public class WeaponEntity : EntityLogic
{
    public EquipmentSlot Slot { get; set; }
    
    protected override void OnAttachTo(EntityLogic parentLogic, 
        Transform parentTransform, object userData)
    {
        base.OnAttachTo(parentLogic, parentTransform, userData);
        
        // 设置相对位置
        CachedTransform.localPosition = Vector3.zero;
        CachedTransform.localRotation = Quaternion.identity;
    }
}
```

### 案例 3: 自动清理系统

```csharp
public class EntityAutoCleanupSystem : MonoBehaviour
{
    [SerializeField] private float m_CheckInterval = 5f;
    [SerializeField] private float m_MaxEntityLifetime = 300f;  // 5分钟
    
    private IEntityManager m_EntityManager;
    private Dictionary<int, float> m_EntitySpawnTime;
    private float m_CheckTimer;
    
    private void Start()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
        m_EntitySpawnTime = new Dictionary<int, float>();
        m_CheckTimer = m_CheckInterval;
    }
    
    private void Update()
    {
        m_CheckTimer -= Time.deltaTime;
        if (m_CheckTimer <= 0)
        {
            m_CheckTimer = m_CheckInterval;
            CheckAndCleanupEntities();
        }
    }
    
    private void CheckAndCleanupEntities()
    {
        var currentTime = Time.time;
        var entitiesToHide = new List<int>();
        
        foreach (var kvp in m_EntitySpawnTime)
        {
            float lifetime = currentTime - kvp.Value;
            if (lifetime > m_MaxEntityLifetime)
            {
                entitiesToHide.Add(kvp.Key);
            }
        }
        
        foreach (var entityId in entitiesToHide)
        {
            var entity = m_EntityManager.GetEntity(entityId);
            if (entity != null)
            {
                Log.Warning($"Auto-cleaning entity {entityId} (lifetime exceeded)");
                m_EntityManager.HideEntity(entity);
                m_EntitySpawnTime.Remove(entityId);
            }
        }
    }
}
```

---

## 总结

通过遵循这些最佳实践，您将能够：

? 编写更加健壮的实体逻辑代码  
? 避免常见的陷阱和内存泄漏  
? 优化系统性能  
? 更容易进行测试和维护  
? 提高代码的可读性和可维护性  

记住：**清晰的生命周期管理和资源释放是关键**！
