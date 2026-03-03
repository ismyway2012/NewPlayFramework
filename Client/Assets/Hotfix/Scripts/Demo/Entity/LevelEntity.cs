using GameFrameX.Runtime;
using GameFrameX.Event.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using GameFrameX.Entity.Runtime;
using Hotfix.Config.Tables;

public class LevelEntity : EntityBase
{
    public const string P_LevelData = "LevelData";
    public const string P_LevelReadyCallback = "OnLevelReady";
    public bool IsAllReady { get; private set; }
    private Transform playerSpawnPoint;
    PlayerEntity m_PlayerEntity;

    List<Spawnner> m_Spawnners;

    HashSet<int> m_EntityLoadingList;
    Dictionary<int, CombatUnitEntity> m_Enemies;
    bool m_IsGameOver;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        playerSpawnPoint = transform.Find("PlayerSpawnPoint");
        m_Spawnners = new List<Spawnner>();
        m_EntityLoadingList = new HashSet<int>();
        m_Enemies = new Dictionary<int, CombatUnitEntity>();
    }
    protected override async void OnShow(object userData)
    {
        base.OnShow(userData);
        GameApp.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GameApp.Event.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
        m_PlayerEntity = null;
        m_IsGameOver = false;
        IsAllReady = false;
        m_Spawnners.Clear();
        m_EntityLoadingList.Clear();
        m_Enemies.Clear();
        CachedTransform.Find("EnemySpawnPoints").GetComponentsInChildren<Spawnner>(m_Spawnners);

        var combatUnitTb = GameApp.Config.GetConfig<TbCombatUnitTable>();
        var playerRow = combatUnitTb.Get(0);
        var playerParams = EntityParams.Create(playerSpawnPoint.position, playerSpawnPoint.eulerAngles);
        playerParams.Set(PlayerEntity.P_DataTableRow, playerRow);
        playerParams.Set<VarInt32>(PlayerEntity.P_CombatFlag, (int)CombatUnitEntity.CombatFlag.Player);
        playerParams.Set<VarAction>(PlayerEntity.P_OnBeKilled, (Action)OnPlayerBeKilled);
        var playerPrefab = UtilityBuiltin.AssetsPath.GetPrefab($"Entity/{playerRow.PrefabName}");
        Entity entity = await GameApp.Entity.ShowEntityAsync<PlayerEntity>(playerParams.Id, playerPrefab, Const.EntityGroup.Player.ToString(), playerParams) as Entity;
        m_PlayerEntity = entity.Logic as PlayerEntity;
        CameraController.Instance.SetFollowTarget(m_PlayerEntity.CachedTransform);
        IsAllReady = true;
    }


    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (m_IsGameOver || !IsAllReady) return;
        SpawnEnemiesUpdate();
    }
    protected override void OnHide(bool isShutdown, object userData)
    {
        GameApp.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GameApp.Event.Unsubscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);

        base.OnHide(isShutdown, userData);
    }

    public void StartGame()
    {
        m_PlayerEntity.Ctrlable = true;
    }
    private void SpawnEnemiesUpdate()
    {
        if (m_Spawnners.Count == 0) return;
        Spawnner item = null;
        var playerPos = m_PlayerEntity.CachedTransform.position;
        for (int i = m_Spawnners.Count - 1; i >= 0; i--)
        {
            item = m_Spawnners[i];
            if (item.CheckInBounds(playerPos))
            {
                var ids = item.SpawnAllCombatUnits(m_PlayerEntity);
                m_Spawnners.RemoveAt(i);
                foreach (var entityId in ids)
                {
                    m_EntityLoadingList.Add(entityId);
                }
            }
        }
    }

    private void OnPlayerBeKilled()
    {
        if (m_IsGameOver) return;
        m_IsGameOver = true;
        var eParms = RefParams.Create();
        eParms.Set<VarBoolean>("IsWin", false);
        GameApp.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
    }
    private void CheckGameOver()
    {
        if(m_IsGameOver) return;
        if (m_Spawnners.Count < 1 && m_EntityLoadingList.Count < 1 && m_Enemies.Count < 1)
        {
            m_IsGameOver = true;
            var eParms = RefParams.Create();
            eParms.Set<VarBoolean>("IsWin", true);
            GameApp.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
        }
    }
    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var eArgs = e as ShowEntitySuccessEventArgs;
        int entityId = eArgs.Entity.Id;
        if (m_EntityLoadingList.Contains(entityId))
        {
            m_Enemies.Add(entityId, eArgs.Entity.Logic as CombatUnitEntity);
            m_EntityLoadingList.Remove(entityId);
        }
    }


    private void OnHideEntityComplete(object sender, GameEventArgs e)
    {
        var eArgs = e as HideEntityCompleteEventArgs;
        int entityId = eArgs.EntityId;
        if (m_Enemies.ContainsKey(entityId))
        {
            m_Enemies.Remove(entityId);
        }
        else if (m_EntityLoadingList.Contains(entityId))
        {
            m_EntityLoadingList.Remove(entityId);
        }

        CheckGameOver();
    }
}
