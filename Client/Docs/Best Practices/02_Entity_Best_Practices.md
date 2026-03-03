# 实体系统（Entity）最佳实践指南

## 目录
1. [系统概述](#系统概述)
2. [核心概念](#核心概念)
3. [实体的生命周期](#实体的生命周期)
4. [最佳实践](#最佳实践)
5. [代码示例](#代码示例)
6. [性能优化](#性能优化)
7. [常见问题](#常见问题)

## 系统概述

实体系统（Entity System）是GameFrameX框架的核心游戏对象管理系统。它提供了统一的实体创建、销毁、管理和回收机制，支持实体分组、生命周期管理等功能。

### 主要特点
- **统一管理**: 所有游戏对象通过实体系统统一管理
- **分组机制**: 支持按类型或功能对实体分组
- **对象复用**: 自动对象复用，减少GC压力
- **事件驱动**: 完整的实体生命周期事件
- **灵活的加载**: 支持同步/异步加载实体资源

## 核心概念

### 实体基类
```csharp
public interface IEntity
{
    int Id { get; }                          // 实体ID
    string EntityAssetName { get; }          // 实体资源名称
    object Handle { get; }                   // 实体实例（通常是GameObject）
    IEntityGroup EntityGroup { get; }        // 所属的实体组
}
```

### 实体逻辑类
```csharp
public abstract class EntityLogic
{
    public virtual void OnInit() { }         // 初始化
    public virtual void OnShow() { }         // 显示
    public virtual void OnHide() { }         // 隐藏
    public virtual void OnAttach() { }       // 挂接
    public virtual void OnUpdate() { }       // 更新
}
```

### 实体组
将相同类型的实体组织在一起，便于批量操作。

```csharp
public interface IEntityGroup
{
    string Name { get; }                     // 组名
    int EntityCount { get; }                 // 组内实体数量
    IEntity GetEntity(int entityId);         // 获取指定ID的实体
}
```

## 实体的生命周期

### 完整生命周期
```
创建 → 初始化(OnInit) → 显示(OnShow) → 运行(OnUpdate) → 隐藏(OnHide) → 销毁
```

### 时间轴示意图
```
时间 →
|
├─ 实体创建: EntityManager.CreateEntity()
|  └─ 从对象池获取或新建GameObject
|
├─ OnInit 执行
|  └─ 一次性初始化逻辑
|
├─ OnShow 执行
|  └─ 每次显示时执行
|
├─ OnUpdate 执行（每帧）
|  └─ 游戏逻辑更新
|
├─ OnHide 执行
|  └─ 隐藏时清理可视化
|
└─ 实体销毁
   └─ 归还对象池
```

## 最佳实践

### 1. 实体类的设计原则

#### 1.1 继承EntityLogic创建实体逻辑
```csharp
// 推荐
public class PlayerEntity : EntityLogic
{
    private int m_PlayerId;
    private PlayerData m_PlayerData;
    
    public override void OnInit()
    {
        // 一次性初始化
    }
    
    public override void OnShow()
    {
        // 显示逻辑
    }
    
    public override void OnUpdate()
    {
        // 每帧更新逻辑
    }
}

// 不推荐：直接在MonoBehaviour中处理所有逻辑
public class PlayerEntityMonoBehaviour : MonoBehaviour
{
    // 所有逻辑混在一起
}
```

#### 1.2 清晰的职责划分
```csharp
// 推荐：行为分离到不同的类
public class PlayerEntity : EntityLogic
{
    private PlayerMovement m_Movement;
    private PlayerCombat m_Combat;
    private PlayerAnimation m_Animation;
    
    public override void OnInit()
    {
        m_Movement = GetComponent<PlayerMovement>();
        m_Combat = GetComponent<PlayerCombat>();
        m_Animation = GetComponent<PlayerAnimation>();
    }
}

// 每个组件专注于自己的职责
public class PlayerMovement : MonoBehaviour { }
public class PlayerCombat : MonoBehaviour { }
public class PlayerAnimation : MonoBehaviour { }
```

### 2. 实体的创建和销毁

#### 2.1 正确的创建流程
```csharp
// 推荐：使用EntityManager创建
public class GamePlayProcedure : ProcedureBase
{
    private int m_PlayerId = 1;
    
    public override void OnEnter()
    {
        var entityManager = GameEntry.GetComponent<EntityComponent>();
        
        // 异步加载实体
        entityManager.CreateEntityAsync(
            "Assets/Resources/Entities/Player.prefab",
            "Player",
            m_PlayerId,
            PlayerCreated
        );
    }
    
    private void PlayerCreated(IEntity entity)
    {
        if (entity != null)
        {
            Log.Info($"Player entity created: {entity.Id}");
        }
    }
}
```

#### 2.2 实体生命周期的完整处理
```csharp
public class EnemyEntity : EntityLogic
{
    private int m_EnemyId;
    private GameObject m_GameObject;
    private Animator m_Animator;
    private Health m_Health;
    
    public override void OnInit()
    {
        // 获取组件引用
        m_GameObject = gameObject;
        m_Animator = GetComponent<Animator>();
        m_Health = GetComponent<Health>();
        
        // 订阅事件
        m_Health.OnDead += OnEnemyDead;
    }
    
    public override void OnShow()
    {
        // 恢复血量等状态
        m_Health.Recover();
        m_GameObject.SetActive(true);
    }
    
    public override void OnUpdate()
    {
        // 更新逻辑
    }
    
    public override void OnHide()
    {
        // 停止动画
        m_Animator.SetBool("IsAlive", false);
        m_GameObject.SetActive(false);
    }
    
    private void OnEnemyDead()
    {
        // 通知实体管理器销毁该实体
        EntityManager.HideEntity(this);
    }
    
    public void OnDestroy()
    {
        // 清理事件订阅
        if (m_Health != null)
        {
            m_Health.OnDead -= OnEnemyDead;
        }
    }
}
```

### 3. 实体分组的最佳实践

#### 3.1 合理使用实体组
```csharp
public class GamePlayProcedure : ProcedureBase
{
    private IEntityGroup m_PlayerGroup;
    private IEntityGroup m_EnemyGroup;
    private IEntityGroup m_ItemGroup;
    
    public override void OnEnter()
    {
        var entityManager = GameEntry.GetComponent<EntityComponent>();
        
        // 创建实体组
        m_PlayerGroup = entityManager.GetEntityGroup("Player");
        m_EnemyGroup = entityManager.GetEntityGroup("Enemy");
        m_ItemGroup = entityManager.GetEntityGroup("Item");
    }
    
    public void RemoveAllEnemies()
    {
        // 批量操作
        var enemies = m_EnemyGroup.GetAllEntities();
        foreach (var enemy in enemies)
        {
            EntityManager.HideEntity(enemy);
        }
    }
}
```

#### 3.2 按需创建实体组
```csharp
// 推荐：只创建需要的分组
public enum EntityGroupType
{
    Player,      // 玩家
    Enemy,       // 敌人
    NPC,         // NPC
    Item,        // 物品
    Projectile,  // 射弹
}

public class EntityGroupManager
{
    private Dictionary<EntityGroupType, IEntityGroup> m_Groups = 
        new Dictionary<EntityGroupType, IEntityGroup>();
    
    public IEntityGroup GetGroup(EntityGroupType type)
    {
        if (!m_Groups.ContainsKey(type))
        {
            var entityManager = GameEntry.GetComponent<EntityComponent>();
            m_Groups[type] = entityManager.GetEntityGroup(type.ToString());
        }
        return m_Groups[type];
    }
}
```

### 4. 与其他系统的协作

#### 4.1 与事件系统的整合
```csharp
public class PlayerEntity : EntityLogic
{
    public override void OnShow()
    {
        // 通知其他系统
        var eventComponent = GameEntry.GetComponent<EventComponent>();
        eventComponent.Fire(this, new PlayerSpawnedEventArgs(m_Id));
    }
    
    public override void OnHide()
    {
        var eventComponent = GameEntry.GetComponent<EventComponent>();
        eventComponent.Fire(this, new PlayerDespawnedEventArgs(m_Id));
    }
}
```

#### 4.2 与资源系统的协作
```csharp
public class GamePlayProcedure : ProcedureBase
{
    public override void OnEnter()
    {
        var entityManager = GameEntry.GetComponent<EntityComponent>();
        var assetManager = GameEntry.GetComponent<AssetComponent>();
        
        // 预加载常用实体资源
        var playerAsset = "Assets/Entities/Player";
        assetManager.LoadAssetAsync(playerAsset, OnPlayerAssetLoaded);
    }
    
    private void OnPlayerAssetLoaded(object asset)
    {
        Log.Info("Player asset loaded, ready to create entity");
    }
}
```

## 代码示例

### 示例1：简单的玩家实体
```csharp
public class SimplePlayerEntity : EntityLogic
{
    private Vector3 m_StartPosition;
    private CharacterController m_CharController;
    
    public override void OnInit()
    {
        m_StartPosition = transform.position;
        m_CharController = GetComponent<CharacterController>();
    }
    
    public override void OnShow()
    {
        transform.position = m_StartPosition;
        gameObject.SetActive(true);
    }
    
    public override void OnUpdate()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(moveX, 0, moveY) * 5f * Time.deltaTime;
        m_CharController.Move(movement);
    }
    
    public override void OnHide()
    {
        gameObject.SetActive(false);
    }
}
```

### 示例2：复杂的敌人实体
```csharp
public class SmartEnemyEntity : EntityLogic
{
    private enum AIState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }
    
    private AIState m_AIState = AIState.Patrol;
    private Transform m_PlayerTransform;
    private NavMeshAgent m_Agent;
    private Health m_Health;
    private float m_DetectionRange = 20f;
    private float m_AttackRange = 2f;
    private float m_AttackCooldown = 0f;
    private const float ATTACK_CD = 1f;
    
    public override void OnInit()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Health = GetComponent<Health>();
        m_Health.OnDead += OnDead;
        
        var playerEntity = EntityManager.GetEntity(1) as SimplePlayerEntity;
        if (playerEntity != null)
        {
            m_PlayerTransform = playerEntity.transform;
        }
    }
    
    public override void OnUpdate()
    {
        m_AttackCooldown -= Time.deltaTime;
        
        switch (m_AIState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
        }
    }
    
    private void UpdatePatrol()
    {
        if (m_PlayerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(
            transform.position, m_PlayerTransform.position);
        
        if (distanceToPlayer <= m_DetectionRange)
        {
            m_AIState = AIState.Chase;
        }
    }
    
    private void UpdateChase()
    {
        if (m_PlayerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(
            transform.position, m_PlayerTransform.position);
        
        if (distanceToPlayer <= m_AttackRange)
        {
            m_AIState = AIState.Attack;
        }
        else
        {
            m_Agent.SetDestination(m_PlayerTransform.position);
        }
    }
    
    private void UpdateAttack()
    {
        if (m_AttackCooldown <= 0)
        {
            // 发起攻击
            m_AttackCooldown = ATTACK_CD;
        }
    }
    
    private void OnDead()
    {
        m_AIState = AIState.Dead;
        EntityManager.HideEntity(this);
    }
    
    public override void OnDestroy()
    {
        if (m_Health != null)
        {
            m_Health.OnDead -= OnDead;
        }
    }
}
```

### 示例3：实体的批量管理
```csharp
public class EntitySpawner
{
    private IEntityGroup m_EnemyGroup;
    private int m_NextEnemyId = 1000;
    
    public void InitializeSpawner()
    {
        var entityManager = GameEntry.GetComponent<EntityComponent>();
        m_EnemyGroup = entityManager.GetEntityGroup("Enemy");
    }
    
    public void SpawnEnemyWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnSingleEnemy();
        }
    }
    
    private void SpawnSingleEnemy()
    {
        var entityManager = GameEntry.GetComponent<EntityComponent>();
        
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        entityManager.CreateEntityAsync(
            "Assets/Entities/Enemy.prefab",
            "Enemy",
            m_NextEnemyId++,
            null,
            spawnPos,
            Quaternion.identity
        );
    }
    
    public void CleanupAllEnemies()
    {
        var allEnemies = m_EnemyGroup.GetAllEntities();
        foreach (var enemy in allEnemies)
        {
            EntityManager.HideEntity(enemy);
        }
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        // 返回随机生成点
        return Vector3.zero;
    }
}
```

## 性能优化

### 1. 使用对象池
```csharp
// 框架自动处理对象池
// 不需要手动Instantiate/Destroy

// 推荐：让EntityManager处理对象的生命周期
var entityManager = GameEntry.GetComponent<EntityComponent>();
entityManager.CreateEntityAsync(...);

// 不推荐：手动创建销毁
var prefab = Resources.Load<GameObject>("Enemy");
Instantiate(prefab);
```

### 2. 减少OnUpdate调用频率
```csharp
// 推荐：按需更新
public class OptimizedEntity : EntityLogic
{
    private float m_UpdateInterval = 0.1f;
    private float m_TimeSinceLastUpdate = 0f;
    
    public override void OnUpdate()
    {
        m_TimeSinceLastUpdate += Time.deltaTime;
        
        if (m_TimeSinceLastUpdate >= m_UpdateInterval)
        {
            m_TimeSinceLastUpdate = 0f;
            DoLogicUpdate();
        }
    }
    
    private void DoLogicUpdate() { }
}
```

### 3. 批量操作优化
```csharp
// 不推荐：逐个操作
foreach (var enemy in m_EnemyGroup.GetAllEntities())
{
    EntityManager.HideEntity(enemy);
}

// 推荐：批量操作（如果框架支持）
m_EnemyGroup.HideAll();
```

## 常见问题

### Q1: 如何在实体间通信？

**A:** 有三种推荐方式：

1. **事件系统**（推荐）：
```csharp
EventManager.Fire(new EntityDamagedEventArgs(sourceId, targetId, damage));
```

2. **直接引用**：
```csharp
var targetEntity = EntityManager.GetEntity(targetId);
targetEntity.TakeDamage(damage);
```

3. **全局管理器**：
```csharp
EntityInteractionManager.NotifyDamage(sourceId, targetId, damage);
```

### Q2: 如何处理异步加载实体？

**A:** 使用CreateEntityAsync方法：
```csharp
var entityManager = GameEntry.GetComponent<EntityComponent>();
entityManager.CreateEntityAsync(
    assetPath,
    groupName,
    entityId,
    (entity) =>
    {
        if (entity != null)
        {
            Log.Info("Entity created successfully");
        }
    }
);
```

### Q3: 如何获取实体的引用？

**A:** 有多种方式：
```csharp
// 通过ID获取
var entity = EntityManager.GetEntity(id);

// 通过组获取
var allEnemies = m_EnemyGroup.GetAllEntities();

// 通过名称查询
var entity = EntityManager.FindEntity(name);
```

### Q4: 实体为什么没有被销毁？

**A:** 常见原因：

1. **事件订阅未清理**：确保在OnDestroy中取消订阅
2. **引用未释放**：检查是否有其他对象持有实体引用
3. **对象池配置**：检查对象池是否正确配置

---

**最后更新时间**: 2025年
**适用版本**: GameFrameX 1.3.6+
**作者**: GameFrameX 开发团队
