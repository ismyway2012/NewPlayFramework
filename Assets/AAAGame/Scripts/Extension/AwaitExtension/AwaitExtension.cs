using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFrameX.Runtime;
using GameFrameX.Config.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.Asset.Runtime;
using UnityEngine;
using GameFrameX.UI.Runtime;
using GameFrameX.Entity.Runtime;
using GameFrameX.Download.Runtime;
using GameFrameX.Web.Runtime;
using GameFrameX.Scene.Runtime;
using System.Threading.Tasks;

public static class AwaitExtension
{
    private static readonly Dictionary<int, UniTaskCompletionSource<UIFormBase>> mUIFormTask = new Dictionary<int, UniTaskCompletionSource<UIFormBase>>();
    private static readonly Dictionary<int, UniTaskCompletionSource<EntityLogic>> mEntityTask = new Dictionary<int, UniTaskCompletionSource<EntityLogic>>();
    private static readonly Dictionary<string, UniTaskCompletionSource<bool>> mDataTableTask = new Dictionary<string, UniTaskCompletionSource<bool>>();
    private static readonly Dictionary<string, UniTaskCompletionSource<bool>> mLoadSceneTask = new Dictionary<string, UniTaskCompletionSource<bool>>();
    private static readonly Dictionary<string, UniTaskCompletionSource<bool>> mUnLoadSceneTask = new Dictionary<string, UniTaskCompletionSource<bool>>();
    private static readonly HashSet<int> mWebSerialIds = new HashSet<int>();
    private static readonly List<WebRequestResult> mDelayReleaseWebResult = new List<WebRequestResult>();
    private static readonly HashSet<int> mDownloadSerialIds = new HashSet<int>();
    private static readonly List<DownloadResult> mDelayReleaseDownloadResult = new List<DownloadResult>();

#if UNITY_EDITOR
    private static bool isSubscribeEvent = false;
#endif

    public static void SubscribeEvent()
    {
#if UNITY_EDITOR
        isSubscribeEvent = true;
#endif
    }

#if UNITY_EDITOR
    private static void TipsSubscribeEvent()
    {
        if (!isSubscribeEvent)
        {
            throw new Exception("Use await/async extensions must to subscribe event!");
        }
    }
#endif
    /// <summary>
    /// 打开界面（可等待）
    /// </summary>
    public static async UniTask<IUIForm> OpenUIFormAwait(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        return await uiCom.OpenUIForm(viewId, parms);
    }


    private static void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        ShowEntitySuccessEventArgs ne = (ShowEntitySuccessEventArgs)e;
        mEntityTask.TryGetValue(ne.Entity.Id, out var tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(((Entity)ne.Entity).Logic);
            mEntityTask.Remove(ne.Entity.Id);
        }
    }

    private static void OnShowEntityFailure(object sender, GameEventArgs e)
    {
        ShowEntityFailureEventArgs ne = (ShowEntityFailureEventArgs)e;
        mEntityTask.TryGetValue(ne.EntityId, out var tcs);
        if (tcs != null)
        {
            Debug.LogError(ne.ErrorMessage);
            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            mEntityTask.Remove(ne.EntityId);
        }
    }


    /// <summary>
    /// 加载场景（可等待）
    /// </summary>
    public static async UniTask<bool> LoadSceneAwait(this SceneComponent sceneComponent, string sceneAssetName)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<bool>();
        var isUnLoadScene = mUnLoadSceneTask.TryGetValue(sceneAssetName, out var unloadSceneTcs);
        if (isUnLoadScene)
        {
            await unloadSceneTcs.Task;
        }
        mLoadSceneTask.Add(sceneAssetName, tcs);

        try
        {
            sceneComponent.LoadScene(sceneAssetName);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            tcs.TrySetException(e);
            mLoadSceneTask.Remove(sceneAssetName);
        }
        return await tcs.Task;
    }

    private static void OnLoadSceneSuccess(object sender, GameEventArgs e)
    {
        LoadSceneSuccessEventArgs ne = (LoadSceneSuccessEventArgs)e;
        mLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(true);
            mLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    private static void OnLoadSceneFailure(object sender, GameEventArgs e)
    {
        LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
        mLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            Debug.LogError(ne.ErrorMessage);
            tcs.TrySetException(new GameFrameworkException(ne.ErrorMessage));
            mLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    /// <summary>
    /// 卸载场景（可等待）
    /// </summary>
    public static async UniTask<bool> UnLoadSceneAwait(this SceneComponent sceneComponent, string sceneAssetName)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<bool>();
        var isLoadSceneTcs = mLoadSceneTask.TryGetValue(sceneAssetName, out var loadSceneTcs);
        if (isLoadSceneTcs)
        {
            Debug.Log("Unload  loading scene");
            await loadSceneTcs.Task;
        }
        mUnLoadSceneTask.Add(sceneAssetName, tcs);
        try
        {
            sceneComponent.UnloadScene(sceneAssetName);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            tcs.TrySetException(e);
            mUnLoadSceneTask.Remove(sceneAssetName);
        }
        return await tcs.Task;
    }
    private static void OnUnloadSceneSuccess(object sender, GameEventArgs e)
    {
        UnloadSceneSuccessEventArgs ne = (UnloadSceneSuccessEventArgs)e;
        mUnLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            tcs.TrySetResult(true);
            mUnLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }

    private static void OnUnloadSceneFailure(object sender, GameEventArgs e)
    {
        UnloadSceneFailureEventArgs ne = (UnloadSceneFailureEventArgs)e;
        mUnLoadSceneTask.TryGetValue(ne.SceneAssetName, out var tcs);
        if (tcs != null)
        {
            Debug.LogError($"Unload scene {ne.SceneAssetName} failure.");
            tcs.TrySetException(new GameFrameworkException($"Unload scene {ne.SceneAssetName} failure."));
            mUnLoadSceneTask.Remove(ne.SceneAssetName);
        }
    }


    /// <summary>
    /// 增加下载任务（可等待)
    /// </summary>
    public static UniTask<DownloadResult> AddDownloadAwait(this DownloadComponent downloadComponent,
        string downloadPath,
        string downloadUri,
        object userdata = null)
    {
#if UNITY_EDITOR
        TipsSubscribeEvent();
#endif
        var tcs = new UniTaskCompletionSource<DownloadResult>();
        int serialId = downloadComponent.AddDownload(downloadPath, downloadUri,
            AwaitParams<DownloadResult>.Create(userdata, tcs));
        mDownloadSerialIds.Add(serialId);
        return tcs.Task;
    }

    private static void OnDownloadSuccess(object sender, GameEventArgs e)
    {
        DownloadSuccessEventArgs ne = (DownloadSuccessEventArgs)e;
        if (mDownloadSerialIds.Contains(ne.SerialId))
        {
            if (ne.UserData is AwaitParams<DownloadResult> awaitDataWrap)
            {
                DownloadResult result = DownloadResult.Create(false, string.Empty, awaitDataWrap.UserData);
                mDelayReleaseDownloadResult.Add(result);
                awaitDataWrap.Source.TrySetResult(result);
                ReferencePool.Release(awaitDataWrap);
            }

            mDownloadSerialIds.Remove(ne.SerialId);
            if (mDownloadSerialIds.Count == 0)
            {
                for (int i = 0; i < mDelayReleaseDownloadResult.Count; i++)
                {
                    ReferencePool.Release(mDelayReleaseDownloadResult[i]);
                }

                mDelayReleaseDownloadResult.Clear();
            }
        }
    }

    private static void OnDownloadFailure(object sender, GameEventArgs e)
    {
        DownloadFailureEventArgs ne = (DownloadFailureEventArgs)e;
        if (mDownloadSerialIds.Contains(ne.SerialId))
        {
            if (ne.UserData is AwaitParams<DownloadResult> awaitDataWrap)
            {
                DownloadResult result = DownloadResult.Create(true, ne.ErrorMessage, awaitDataWrap.UserData);
                mDelayReleaseDownloadResult.Add(result);
                awaitDataWrap.Source.TrySetResult(result);
                ReferencePool.Release(awaitDataWrap);
            }

            mDownloadSerialIds.Remove(ne.SerialId);
            if (mDownloadSerialIds.Count == 0)
            {
                for (int i = 0; i < mDelayReleaseDownloadResult.Count; i++)
                {
                    ReferencePool.Release(mDelayReleaseDownloadResult[i]);
                }

                mDelayReleaseDownloadResult.Clear();
            }
        }
    }
}