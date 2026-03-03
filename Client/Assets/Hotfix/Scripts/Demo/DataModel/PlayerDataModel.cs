using GameFrameX.Runtime;
using GameFrameX.Event.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hotfix.Config.Tables;
[Serializable]
public enum PlayerDataType
{
    /// <summary>
    /// 玩家金币
    /// </summary>
    Coins,
    /// <summary>
    /// 玩家钻石
    /// </summary>
    Diamond,
    /// <summary>
    /// 玩家血量
    /// </summary>
    Hp,
    /// <summary>
    /// 玩家能量
    /// </summary>
    Energy,
    /// <summary>
    /// 关卡Id
    /// </summary>
    LevelId
}

/// <summary>
/// 玩家数据类, 金币/血量等
/// </summary>
public class PlayerDataModel : DataModelStorageBase
{
    [JsonProperty]
    private Dictionary<PlayerDataType, int> m_PlayerDataDic;
    public int Hp
    {
        get=>GetData(PlayerDataType.Hp);
        set=>SetData(PlayerDataType.Hp, Mathf.Max(0, value));
    }
    public int Coins
    {
        get => GetData(PlayerDataType.Coins);
        set => SetData(PlayerDataType.Coins, Mathf.Max(0, value));
    }
    /// <summary>
    /// 关卡
    /// </summary>
    public int LevelId
    {
        get => GetData(PlayerDataType.LevelId);
        set
        {
            var lvTb = GameApp.Config.GetConfig<TbLevelTable>();
            int nextLvId = Const.RepeatLevel ? value : Mathf.Clamp(value, lvTb.Min(a => a.ID), lvTb.Max(e => e.ID));
            SetData(PlayerDataType.LevelId, nextLvId);
        }
    }
    public PlayerDataModel()
    {
        m_PlayerDataDic = new Dictionary<PlayerDataType, int>();
    }

    protected override void OnInitialDataModel()
    {
        m_PlayerDataDic[PlayerDataType.Coins] = GameApp.Setting.GetInt("DefaultCoins");
        m_PlayerDataDic[PlayerDataType.Diamond] = GameApp.Setting.GetInt("DefaultDiamonds");
        m_PlayerDataDic[PlayerDataType.Hp] = 100;
        m_PlayerDataDic[PlayerDataType.Energy] = 100;
        m_PlayerDataDic[PlayerDataType.LevelId] = 1;

    }
    
    public int GetData(PlayerDataType tp)
    {
        return m_PlayerDataDic[tp];
    }
    public void SetData(PlayerDataType tp, int value, bool triggerEvent = true)
    {
        int oldValue = m_PlayerDataDic[tp];
        m_PlayerDataDic[tp] = value;

        if (triggerEvent)
            GameApp.Event.Fire(this, PlayerDataChangedEventArgs.Create(tp, oldValue, value));
    }
}
