
using UnityEngine;
using GameFrameX.Event.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using GameFrameX.Fsm.Runtime;
using System.Collections.Generic;
using System;
using GameFrameX.Asset.Runtime;
using Cysharp.Threading.Tasks;
using GameFrameX.Config.Runtime;
using GameFrameX.Localization.Runtime;
using Hotfix.Config.Tables.Core;
using YooAsset;

//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class PreloadProcedure : ProcedureBase
{
    private int totalProgress;
    private int loadedProgress;
    private float smoothProgress;
    private bool preloadAllCompleted;
    private float progressSmoothSpeed = 10f;
    private int m_DataTablesCount;
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GameApp.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
        GameApp.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
        GameApp.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDicSuccess);
        GameApp.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDicFailure);
        //GameApp.BuiltinView.ShowLoadingProgress();
        Log.Info("进入HybridCLR热更流程! 预加载游戏数据...");

        InitAppSettings();
        PreloadAndInitData();
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GameApp.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
        GameApp.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
        GameApp.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDicSuccess);
        GameApp.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDicFailure);
        base.OnLeave(procedureOwner, isShutdown);
    }


    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (totalProgress <= 0 || preloadAllCompleted) return;

        smoothProgress = Mathf.Lerp(smoothProgress, loadedProgress / (float)totalProgress, elapseSeconds * progressSmoothSpeed);

        //GameApp.BuiltinView.SetLoadingProgress(smoothProgress);
        //预加载完成 切换场景
        if (loadedProgress >= totalProgress && smoothProgress >= 0.99f)
        {
            preloadAllCompleted = true;
            InitGameFrameworkSettings();
            Log.Info("预加载完成, 进入游戏场景.");
            procedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, "Game");
            ChangeState<ChangeSceneProcedure>(procedureOwner);
        }
    }
    private void InitAppSettings()
    {
        //if (string.IsNullOrWhiteSpace(GameApp.Setting.GetABTestGroup()))
        //{
        //    GameApp.Setting.SetABTestGroup("B");//设置A/B测试组; 应由服务器分配该新用户所属测试组
        //}
    }
    /// <summary>
    /// 预加载完成之后需要处理的事情
    /// </summary>
    private void InitGameFrameworkSettings()
    {
        //初始化EntityGroup
        var entityGroupTb = GameApp.Config.GetConfig<TbEntityGroupTable>();
        entityGroupTb.ForEach(tb =>
        {
            if (GameApp.Entity.HasEntityGroup(tb.Name))
            {
                var group = GameApp.Entity.GetEntityGroup(tb.Name);
                group.InstanceAutoReleaseInterval = tb.ReleaseInterval;
                group.InstanceCapacity = tb.Capacity;
                group.InstanceExpireTime = tb.ExpireTime;
                group.InstancePriority = tb.Priority;
            }
            else
            {
                GameApp.Entity.AddEntityGroup(tb.Name, tb.ReleaseInterval, tb.Capacity, tb.ExpireTime, tb.Priority);
            }
        });

        Dictionary<string, SoundGroupTable> defaultSoundGroupData = new Dictionary<string, SoundGroupTable>();
        //初始化SoundGroup
        var soundGroupTb = GameApp.Config.GetConfig<TbSoundGroupTable>();
        soundGroupTb.ForEach(tb =>
        {
            if (!defaultSoundGroupData.ContainsKey(tb.Name))
            {
                defaultSoundGroupData.Add(tb.Name, tb);
            }
            if (GameApp.Sound.HasSoundGroup(tb.Name))
            {
                var group = GameApp.Sound.GetSoundGroup(tb.Name);
                group.AvoidBeingReplacedBySamePriority = tb.AvoidBeingReplacedBySamePriority;
                group.Mute = tb.Mute;
                group.Volume = tb.Volume;
                return;
            }
            GameApp.Sound.AddSoundGroup(tb.Name, tb.AvoidBeingReplacedBySamePriority, tb.Mute, tb.Volume, tb.SoundAgentCount);
        });

        //初始化UIGroup
        var uiGroupTb = GameApp.Config.GetConfig<TbUIGroupTable>();
        uiGroupTb.ForEach(tb =>
        {
            if (GameApp.UI.HasUIGroup(tb.Name))
            {
                var group = GameApp.UI.GetUIGroup(tb.Name);
                group.Depth = tb.Depth;
                return;
            }
            GameApp.UI.AddUIGroup(tb.Name, tb.Depth);
        });


        ////初始化音效
        //GameApp.Setting.SetMediaMute(Const.SoundGroup.Music, GameApp.Setting.GetMediaMute(Const.SoundGroup.Music, defaultSoundGroupData[Const.SoundGroup.Music.ToString()].Mute));
        //GameApp.Setting.SetMediaMute(Const.SoundGroup.Sound, GameApp.Setting.GetMediaMute(Const.SoundGroup.Sound, defaultSoundGroupData[Const.SoundGroup.Sound.ToString()].Mute));
        //GameApp.Setting.SetMediaMute(Const.SoundGroup.Vibrate, GameApp.Setting.GetMediaMute(Const.SoundGroup.Vibrate, defaultSoundGroupData[Const.SoundGroup.Vibrate.ToString()].Mute));
        //GameApp.Setting.SetMediaMute(Const.SoundGroup.Joystick, GameApp.Setting.GetMediaMute(Const.SoundGroup.Joystick, defaultSoundGroupData[Const.SoundGroup.Joystick.ToString()].Mute));

        //GameApp.Setting.SetMediaVolume(Const.SoundGroup.Music, GameApp.Setting.GetMediaVolume(Const.SoundGroup.Music, defaultSoundGroupData[Const.SoundGroup.Music.ToString()].Volume));
        //GameApp.Setting.SetMediaVolume(Const.SoundGroup.Sound, GameApp.Setting.GetMediaVolume(Const.SoundGroup.Sound, defaultSoundGroupData[Const.SoundGroup.Sound.ToString()].Volume));
    }
    /// <summary>
    /// 预加载数据表、游戏配置,以及初始化游戏数据
    /// </summary>
    private async void PreloadAndInitData()
    {
        preloadAllCompleted = false;
        smoothProgress = 0;
        totalProgress = 0;
        loadedProgress = 0;
        m_DataTablesCount = -1;
        var appConfig = await AppConfigs.GetInstanceSync();
        totalProgress = 2;//2是加载多语言和创建框架扩展
        CreateGFExtension();
    }
    private async void LoadConfigsAndDataTables()
    {
        var appConfig = await AppConfigs.GetInstanceSync();
        m_DataTablesCount = appConfig.DataTables.Length;
        loadedProgress++;
    }
    private async void CreateGFExtension()
    {
        var ret = await GameApp.Asset.LoadAssetAsync(UtilityBuiltin.AssetsPath.GetPrefab("Core/GFExtension"));
        if (ret.IsSucceed())
        {
            ret.InstantiateSync(GameApp.Base.transform);
        }
        loadedProgress++;
        LoadConfigsAndDataTables();
    }

    private void OnLoadDicSuccess(object sender, GameEventArgs e)
    {
        LoadDictionarySuccessEventArgs args = e as LoadDictionarySuccessEventArgs;
        if (args.UserData != this) return;
        loadedProgress++;
        Log.Info("Load Language Success:{0}", args.DictionaryAssetName);

    }
    /// <summary>
    /// 加载配置成功回调
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadConfigSuccess(object sender, GameEventArgs e)
    {
        var args = e as LoadConfigSuccessEventArgs;
        if (args.UserData != this) return;
        loadedProgress++;
        Log.Info("Load Config Success:{0}", args.ConfigAssetName);
    }

    private void OnLoadDicFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadDictionaryFailureEventArgs;
        if (args.UserData != this) return;

        Log.Error($"Load Dictionary Failed:{args.ErrorMessage}");
    }

    private void OnLoadConfigFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadConfigFailureEventArgs;
        if (args.UserData != this) return;

        Log.Error($"Load Config Failed:{args.ErrorMessage}");
    }
}
