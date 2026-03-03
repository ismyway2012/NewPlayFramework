# GameFrameX 实体系统代码示例

## 目录
1. [基础示例](#基础示例)
2. [进阶示例](#进阶示例)
3. [常见操作](#常见操作)
4. [完整项目示例](#完整项目示例)

---

## 基础示例

### 示例 1: 最简单的实体

```csharp
/// <summary>
/// 最简单的实体实现 - 仅有显示和隐藏功能
/// </summary>
public class SimpleEntity : EntityLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Debug.Log($"Entity {Entity.Id} initialized");
    }
    
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        gameObject.SetActive(true);
    }
    
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        gameObject.SetActive(false);
    }
}
```

### 示例 2: 创建实体的基本流程

```csharp
public class EntityCreationExample : MonoBehaviour
{
    private IEntityManager m_EntityManager;
    
    private async void Start()
    {
        // 1. 获取实体管理器
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
        
        // 2. 创建实体组（通常在启动时做）
        m_EntityManager.CreateEntityGroup("Players");
        m_EntityManager.CreateEntityGroup("Enemies");
        
        // 3. 显示实体（简单方式）
        m_EntityManager.ShowEntity("Players", "Assets/Prefabs/Player.prefab", null);
        
        // 4. 显示实体（带数据）
        var playerData = new { Level = 10, Health = 100 };
        m_EntityManager.ShowEntity("Players", "Assets/Prefabs/Player.prefab", playerData);
        
        // 5. 异步显示实体
        await m_EntityManager.ShowEntityAsync(
            "Enemies", 
            "Assets/Prefabs/Enemy.prefab", 
            new { Level = 5 }
        );
    }
}
```

### 示例 3: 隐藏和查询实体

```csharp
public class EntityQueryExample : MonoBehaviour
{
    private IEntityManager m_EntityManager;
    
    private void Start()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
    }
    
    private void Update()
    {
        // 1. 按 ID 获取实体
        if (m_EntityManager.HasEntity(1))
        {
            var entity = m_EntityManager.GetEntity(1);
            Debug.Log($"Found entity: {entity.EntityAssetName}");
        }
        
        // 2. 按组获取所有实体
        var enemies = m_EntityManager.GetEntitiesInGroup("Enemies");
        Debug.Log($"Enemy count: {enemies.Count()}");
        
        // 3. 获取实体的业务逻辑
        var entity = m_EntityManager.GetEntity(1);
        var entityLogic = entity as EntityLogic;
        if (entityLogic != null)
        {
            Debug.Log($"Entity visible: {entityLogic.Visible}");
        }
        
        // 4. 隐藏实体
        if (m_EntityManager.HasEntity(1))
        {
            var entity = m_EntityManager.GetEntity(1);
            m_EntityManager.HideEntity(entity);
        }
    }
}
```

---

## 进阶示例

### 示例 4: 带生命周期的完整实体

```csharp
/// <summary>
/// 带完整生命周期的怪物实体
/// </summary>
public class MonsterEntity : EntityLogic
{
    // === 配置参数 ===
    [SerializeField] private float m_MoveSpeed = 5f;
    [SerializeField] private float m_AttackRange = 2f;
    [SerializeField] private int m_AttackDamage = 10;
    
    // === 运行时状态 ===
    private int m_CurrentHp;
    private int m_MaxHp;
    private bool m_IsAlive;
    private Animator m_Animator;
    private Transform m_PlayerTransform;
    
    // === 计时器 ===
    private float m_AttackCooldown;
    private const float m_AttackCooldownMax = 2f;
    
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        // 获取必要的组件
        m_Animator = GetComponent<Animator>();
        if (m_Animator == null)
        {
            Log.Error($"MonsterEntity requires Animator component");
            return;
        }
        
        // 初始化数据
        if (userData is MonsterInitData initData)
        {
            m_MaxHp = initData.MaxHp;
            m_CurrentHp = initData.MaxHp;
            m_AttackDamage = initData.AttackDamage;
        }
        else
        {
            m_MaxHp = 100;
            m_CurrentHp = 100;
        }
        
        m_IsAlive = true;
        m_AttackCooldown = 0;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[Monster] Initialized with HP: {m_MaxHp}");
        #endif
    }
    
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        
        // 应用初始位置
        if (userData is MonsterShowData showData)
        {
            CachedTransform.position = showData.Position;
        }
        
        // 重置状态
        m_CurrentHp = m_MaxHp;
        m_IsAlive = true;
        m_AttackCooldown = 0;
        
        // 启用可见性
        Visible = true;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[Monster] Shown at position: {CachedTransform.position}");
        #endif
    }
    
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        
        // 停止动画
        m_Animator.SetBool("IsWalking", false);
        m_Animator.SetBool("IsAttacking", false);
        
        // 隐藏视觉
        Visible = false;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[Monster] Hidden (isShutdown={isShutdown})");
        #endif
    }
    
    protected override void OnRecycle()
    {
        base.OnRecycle();
        
        // 清理引用
        m_Animator = null;
        m_PlayerTransform = null;
        m_CurrentHp = 0;
        m_MaxHp = 0;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log.Info($"[Monster] Recycled");
        #endif
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        if (!m_IsAlive) return;
        
        // 1. 寻找玩家（如果没有找到）
        if (m_PlayerTransform == null)
        {
            FindPlayer();
            if (m_PlayerTransform == null) return;
        }
        
        // 2. 计算与玩家的距离
        float distanceToPlayer = Vector3.Distance(
            CachedTransform.position, 
            m_PlayerTransform.position
        );
        
        // 3. 判断是否在攻击范围内
        if (distanceToPlayer <= m_AttackRange)
        {
            // 执行攻击
            if (m_AttackCooldown <= 0)
            {
                AttackPlayer();
                m_AttackCooldown = m_AttackCooldownMax;
            }
            else
            {
                m_AttackCooldown -= elapseSeconds;
            }
            
            m_Animator.SetBool("IsWalking", false);
        }
        else
        {
            // 移动靠近玩家
            Vector3 directionToPlayer = (m_PlayerTransform.position - CachedTransform.position).normalized;
            Vector3 newPosition = CachedTransform.position + directionToPlayer * m_MoveSpeed * elapseSeconds;
            
            CachedTransform.position = newPosition;
            CachedTransform.LookAt(m_PlayerTransform.position);
            
            m_Animator.SetBool("IsWalking", true);
            m_AttackCooldown -= elapseSeconds;
        }
    }
    
    private void FindPlayer()
    {
        // 在所有实体中查找玩家
        var players = Entity.EntityGroup.GetEntities()
            .Where(e => e is PlayerEntity);
        
        if (players.Any())
        {
            m_PlayerTransform = (players.First() as EntityLogic)?.CachedTransform;
        }
    }
    
    private void AttackPlayer()
    {
        m_Animator.SetBool("IsAttacking", true);
        
        // 触发伤害事件
        EventManager.Send(new DamageEvent
        {
            Attacker = Entity.Id,
            Target = /* player entity id */,
            Damage = m_AttackDamage
        });
        
        // 动画结束后重置
        StartCoroutine(ResetAttackAnimation());
    }
    
    private IEnumerator ResetAttackAnimation()
    {
        yield return new WaitForSeconds(m_Animator.GetCurrentAnimatorStateInfo(0).length);
        m_Animator.SetBool("IsAttacking", false);
    }
    
    public void TakeDamage(int damage)
    {
        if (!m_IsAlive) return;
        
        m_CurrentHp -= damage;
        
        if (m_CurrentHp <= 0)
        {
            m_IsAlive = false;
            Die();
        }
    }
    
    private void Die()
    {
        m_Animator.SetTrigger("Die");
        m_AttackCooldown = float.MaxValue;  // 禁止攻击
        
        // 延迟隐藏
        StartCoroutine(DelayedHide());
    }
    
    private IEnumerator DelayedHide()
    {
        yield return new WaitForSeconds(2f);
        Entity.EntityGroup.HideEntity(Entity);
    }
}

// === 数据类 ===
public class MonsterInitData
{
    public int MaxHp { get; set; } = 100;
    public int AttackDamage { get; set; } = 10;
}

public class MonsterShowData
{
    public Vector3 Position { get; set; }
}
```

### 示例 5: 父子实体关系

```csharp
/// <summary>
/// 武器实体 - 可作为其他实体的子物体
/// </summary>
public class WeaponEntity : EntityLogic
{
    [SerializeField] private ParticleSystem m_AttackEffect;
    
    protected override void OnAttachTo(EntityLogic parentLogic, Transform parentTransform, object userData)
    {
        base.OnAttachTo(parentLogic, parentTransform, userData);
        
        // 设置相对位置
        CachedTransform.SetParent(parentTransform);
        CachedTransform.localPosition = Vector3.zero;
        CachedTransform.localRotation = Quaternion.identity;
        
        Log.Info($"Weapon attached to {parentLogic.Name}");
    }
    
    protected override void OnDetachFrom(EntityLogic parentLogic, object userData)
    {
        base.OnDetachFrom(parentLogic, userData);
        
        Log.Info($"Weapon detached from {parentLogic.Name}");
    }
    
    public void PlayAttackEffect()
    {
        if (m_AttackEffect != null)
        {
            m_AttackEffect.Play();
        }
    }
}

/// <summary>
/// 角色实体 - 可以装备武器
/// </summary>
public class CharacterEntity : EntityLogic
{
    private IEntityManager m_EntityManager;
    private WeaponEntity m_EquippedWeapon;
    private Transform m_WeaponSocket;  // 武器插槽
    
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
        m_WeaponSocket = CachedTransform.Find("WeaponSocket");
        
        if (m_WeaponSocket == null)
        {
            Log.Warning("CharacterEntity: WeaponSocket not found");
        }
    }
    
    /// <summary>
    /// 装备武器
    /// </summary>
    public async void EquipWeapon(string weaponAssetName)
    {
        // 1. 加载武器
        await m_EntityManager.ShowEntityAsync(
            "Weapons",
            weaponAssetName,
            null
        );
        
        // 2. 附加到角色
        var weaponEntity = m_EntityManager.GetEntitiesInGroup("Weapons").LastOrDefault() as EntityLogic;
        if (weaponEntity != null)
        {
            m_EntityManager.AttachEntity(
                weaponEntity.Entity,
                Entity,
                m_WeaponSocket
            );
            m_EquippedWeapon = weaponEntity as WeaponEntity;
        }
    }
    
    /// <summary>
    /// 卸载武器
    /// </summary>
    public void UnequipWeapon()
    {
        if (m_EquippedWeapon != null)
        {
            m_EntityManager.DetachEntity(m_EquippedWeapon.Entity);
            m_EntityManager.HideEntity(m_EquippedWeapon.Entity);
            m_EquippedWeapon = null;
        }
    }
    
    /// <summary>
    /// 攻击
    /// </summary>
    public void Attack()
    {
        if (m_EquippedWeapon != null)
        {
            m_EquippedWeapon.PlayAttackEffect();
            
            // 造成伤害等逻辑...
        }
    }
}
```

---

## 常见操作

### 操作 1: 实体池预热

```csharp
public class EntityPoolWarmer : MonoBehaviour
{
    [SerializeField] private string m_EntityGroupName = "Enemies";
    [SerializeField] private string m_EntityAssetName = "Assets/Prefabs/Enemy.prefab";
    [SerializeField] private int m_PoolSize = 20;
    
    private IEntityManager m_EntityManager;
    
    private async void Start()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
        
        // 创建实体组
        m_EntityManager.CreateEntityGroup(m_EntityGroupName);
        
        // 预热对象池
        await WarmupPool();
    }
    
    private async Task WarmupPool()
    {
        var tasks = new List<Task>();
        
        for (int i = 0; i < m_PoolSize; i++)
        {
            tasks.Add(m_EntityManager.ShowEntityAsync(
                m_EntityGroupName,
                m_EntityAssetName,
                null
            ));
        }
        
        await Task.WhenAll(tasks);
        
        // 立即隐藏所有实体，返回到池中
        var entities = m_EntityManager.GetEntitiesInGroup(m_EntityGroupName).ToList();
        foreach (var entity in entities)
        {
            m_EntityManager.HideEntity(entity);
        }
        
        Debug.Log($"Entity pool warmed up with {m_PoolSize} instances");
    }
}
```

### 操作 2: 批量管理实体

```csharp
public class EntityBatchManager : MonoBehaviour
{
    private IEntityManager m_EntityManager;
    private List<int> m_ActiveEntityIds = new();
    
    private void Start()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
    }
    
    /// <summary>
    /// 隐藏所有活跃实体
    /// </summary>
    public void HideAllEntities()
    {
        foreach (var entityId in m_ActiveEntityIds)
        {
            if (m_EntityManager.HasEntity(entityId))
            {
                var entity = m_EntityManager.GetEntity(entityId);
                m_EntityManager.HideEntity(entity);
            }
        }
        m_ActiveEntityIds.Clear();
    }
    
    /// <summary>
    /// 统计实体统计信息
    /// </summary>
    public void PrintEntityStats()
    {
        int totalEntityCount = m_EntityManager.EntityCount;
        int totalGroupCount = m_EntityManager.EntityGroupCount;
        
        Debug.Log($"Total Entities: {totalEntityCount}");
        Debug.Log($"Total Groups: {totalGroupCount}");
        
        foreach (var groupName in GetAllGroupNames())
        {
            var count = m_EntityManager.GetEntitiesInGroup(groupName).Count();
            Debug.Log($"  Group '{groupName}': {count} entities");
        }
    }
    
    private List<string> GetAllGroupNames()
    {
        // 这需要 EntityManager 提供 GetGroupNames 方法
        // 或通过其他方式获取
        return new List<string>();
    }
}
```

### 操作 3: 实体事件响应

```csharp
public class EntityEventHandler : MonoBehaviour
{
    private EntityComponent m_EntityComponent;
    
    private void OnEnable()
    {
        m_EntityComponent = FindObjectOfType<EntityComponent>();
        
        // 订阅实体事件
        m_EntityComponent.EntityManager.ShowEntitySuccess += OnShowEntitySuccess;
        m_EntityComponent.EntityManager.ShowEntityFailure += OnShowEntityFailure;
        m_EntityComponent.EntityManager.HideEntityComplete += OnHideEntityComplete;
    }
    
    private void OnDisable()
    {
        if (m_EntityComponent == null) return;
        
        m_EntityComponent.EntityManager.ShowEntitySuccess -= OnShowEntitySuccess;
        m_EntityComponent.EntityManager.ShowEntityFailure -= OnShowEntityFailure;
        m_EntityComponent.EntityManager.HideEntityComplete -= OnHideEntityComplete;
    }
    
    private void OnShowEntitySuccess(object sender, ShowEntitySuccessEventArgs e)
    {
        Debug.Log($"Entity shown successfully: {e.Entity.EntityAssetName}");
    }
    
    private void OnShowEntityFailure(object sender, ShowEntityFailureEventArgs e)
    {
        Debug.LogError($"Failed to show entity: {e.EntityAssetName}, Error: {e.ErrorMessage}");
    }
    
    private void OnHideEntityComplete(object sender, HideEntityCompleteEventArgs e)
    {
        Debug.Log($"Entity hidden: {e.EntityId}");
    }
}
```

---

## 完整项目示例

### 示例 6: 简单的塔防游戏实体系统

```csharp
/// <summary>
/// 塔防游戏 - 完整实体系统示例
/// </summary>

// ========== 实体逻辑 ==========

/// <summary>
/// 怪物基类
/// </summary>
public abstract class EnemyEntity : EntityLogic
{
    protected int m_CurrentHp;
    protected int m_MaxHp;
    protected float m_MoveSpeed;
    protected List<Vector3> m_PathPoints = new();
    protected int m_CurrentPathIndex = 0;
    protected float m_PathProgress = 0f;
    
    protected virtual void Initialize(EnemyConfig config)
    {
        m_MaxHp = config.MaxHp;
        m_CurrentHp = config.MaxHp;
        m_MoveSpeed = config.MoveSpeed;
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        if (!Available) return;
        
        MoveAlongPath(elapseSeconds);
    }
    
    protected virtual void MoveAlongPath(float deltaTime)
    {
        if (m_PathPoints.Count == 0) return;
        
        if (m_CurrentPathIndex >= m_PathPoints.Count)
        {
            ReachedEnd();
            return;
        }
        
        Vector3 targetPoint = m_PathPoints[m_CurrentPathIndex];
        Vector3 currentPos = CachedTransform.position;
        
        m_PathProgress += (m_MoveSpeed * deltaTime) / 
            Vector3.Distance(currentPos, targetPoint);
        
        if (m_PathProgress >= 1f)
        {
            m_PathProgress = 0f;
            m_CurrentPathIndex++;
            return;
        }
        
        Vector3 nextPos = Vector3.Lerp(currentPos, targetPoint, m_PathProgress);
        CachedTransform.position = nextPos;
    }
    
    public virtual void TakeDamage(int damage)
    {
        m_CurrentHp -= damage;
        
        if (m_CurrentHp <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        Entity.EntityGroup.HideEntity(Entity);
    }
    
    protected virtual void ReachedEnd()
    {
        // 到达路径终点
        EventManager.Send(new EnemyReachedEndEvent { EnemyId = Entity.Id });
        Entity.EntityGroup.HideEntity(Entity);
    }
}

/// <summary>
/// 炮塔
/// </summary>
public class TowerEntity : EntityLogic
{
    [SerializeField] private float m_AttackRange = 5f;
    [SerializeField] private int m_AttackDamage = 10;
    [SerializeField] private float m_AttackCooldown = 1f;
    [SerializeField] private Transform m_TurretBase;
    [SerializeField] private Transform m_TurretGun;
    
    private float m_AttackTimer;
    private EnemyEntity m_CurrentTarget;
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        if (!Available) return;
        
        // 1. 查找目标
        FindTarget();
        
        // 2. 瞄准和攻击
        if (m_CurrentTarget != null)
        {
            AimAtTarget();
            
            m_AttackTimer -= elapseSeconds;
            if (m_AttackTimer <= 0)
            {
                AttackTarget();
                m_AttackTimer = m_AttackCooldown;
            }
        }
    }
    
    private void FindTarget()
    {
        // 从敌人组中找最近的敌人
        var enemies = Entity.EntityGroup.GetEntities()
            .OfType<EnemyEntity>()
            .Where(e => e.Visible)
            .OrderBy(e => Vector3.Distance(CachedTransform.position, e.CachedTransform.position))
            .FirstOrDefault();
        
        m_CurrentTarget = enemies;
    }
    
    private void AimAtTarget()
    {
        if (m_CurrentTarget == null) return;
        
        Vector3 directionToTarget = 
            (m_CurrentTarget.CachedTransform.position - m_TurretGun.position).normalized;
        
        if (m_TurretGun != null)
        {
            m_TurretGun.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }
    
    private void AttackTarget()
    {
        if (m_CurrentTarget == null) return;
        
        // 计算距离，检查是否在范围内
        float distance = Vector3.Distance(
            CachedTransform.position,
            m_CurrentTarget.CachedTransform.position
        );
        
        if (distance <= m_AttackRange)
        {
            m_CurrentTarget.TakeDamage(m_AttackDamage);
            
            // 播放攻击特效
            PlayAttackEffect();
        }
    }
    
    private void PlayAttackEffect()
    {
        // 实现攻击特效
    }
}

// ========== 管理系统 ==========

/// <summary>
/// 波次管理器
/// </summary>
public class WaveManager : MonoBehaviour
{
    [SerializeField] private int m_TotalWaves = 5;
    [SerializeField] private float m_WaveInterval = 30f;
    [SerializeField] private float m_EnemySpawnInterval = 0.5f;
    [SerializeField] private List<EnemyConfig> m_EnemyConfigs = new();
    
    private IEntityManager m_EntityManager;
    private int m_CurrentWave = 0;
    private float m_WaveTimer = 0f;
    private float m_SpawnTimer = 0f;
    private Queue<EnemyConfig> m_SpawnQueue = new();
    
    private void Start()
    {
        m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
        m_EntityManager.CreateEntityGroup("Enemies");
        
        StartNextWave();
    }
    
    private void Update()
    {
        if (m_CurrentWave >= m_TotalWaves) return;
        
        m_SpawnTimer -= Time.deltaTime;
        if (m_SpawnTimer <= 0 && m_SpawnQueue.Count > 0)
        {
            SpawnEnemy();
            m_SpawnTimer = m_EnemySpawnInterval;
        }
        
        // 检查波次是否完成
        if (m_SpawnQueue.Count == 0 && m_EntityManager.GetEntitiesInGroup("Enemies").Count() == 0)
        {
            m_WaveTimer -= Time.deltaTime;
            if (m_WaveTimer <= 0)
            {
                StartNextWave();
            }
        }
    }
    
    private void StartNextWave()
    {
        m_CurrentWave++;
        m_WaveTimer = m_WaveInterval;
        m_SpawnQueue.Clear();
        
        // 根据波次难度调整敌人配置
        foreach (var config in m_EnemyConfigs)
        {
            var scaledConfig = new EnemyConfig(config)
            {
                MaxHp = config.MaxHp + (m_CurrentWave - 1) * 10,
                MoveSpeed = config.MoveSpeed + (m_CurrentWave - 1) * 0.5f
            };
            m_SpawnQueue.Enqueue(scaledConfig);
        }
        
        Debug.Log($"Wave {m_CurrentWave} started!");
    }
    
    private void SpawnEnemy()
    {
        var config = m_SpawnQueue.Dequeue();
        m_EntityManager.ShowEntity("Enemies", config.PrefabPath, config);
    }
}

// ========== 配置和事件 ==========

public class EnemyConfig
{
    public string PrefabPath { get; set; }
    public int MaxHp { get; set; }
    public float MoveSpeed { get; set; }
    
    public EnemyConfig() { }
    
    public EnemyConfig(EnemyConfig source)
    {
        PrefabPath = source.PrefabPath;
        MaxHp = source.MaxHp;
        MoveSpeed = source.MoveSpeed;
    }
}

public class EnemyReachedEndEvent
{
    public int EnemyId { get; set; }
}
```

---

## 总结

这些示例涵盖了从最简单到完整的实体系统使用场景。根据您的具体需求选择适当的模式进行扩展。

**关键要点：**
- 始终遵循生命周期方法的规范
- 及时释放资源
- 合理使用父子关系
- 充分利用对象池机制
- 通过事件系统解耦实体逻辑
