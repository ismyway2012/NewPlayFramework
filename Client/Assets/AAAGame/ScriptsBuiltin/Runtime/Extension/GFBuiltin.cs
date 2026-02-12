using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using GameFrameX.Runtime;
using GameFrameX.Config.Runtime;
using GameFrameX.Entity.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.UI.Runtime;
using GameFrameX.Web.Runtime;
using GameFrameX.Sound.Runtime;
using GameFrameX.Network.Runtime;
using GameFrameX.Asset.Runtime;
using GameFrameX.Setting.Runtime;
using GameFrameX.Scene.Runtime;
using GameFrameX.Download.Runtime;
using GameFrameX.Localization.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;

//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class GFBuiltin : MonoBehaviour
{
    public static GFBuiltin Instance { get; private set; }
    public static Camera UICamera { get; private set; }

    public static Canvas RootCanvas { get; private set; } = null;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //var resCom = GameEntry.GetComponent<AssetComponent>();
            //if (resCom != null)
            //{
            //    var resTp = resCom.GetType();
            //    var m_ResourceMode = resTp.GetField("m_ResourceMode", BindingFlags.Instance | BindingFlags.NonPublic);
            //    m_ResourceMode.SetValue(resCom, AppSettings.Instance.ResourceMode);
            //    GFBuiltin.Log($"------------Set ResourceMode:{AppSettings.Instance.ResourceMode}------------");
            //}
        }
    }

    private void Start()
    {
        RootCanvas = GameApp.UI.GetComponentInChildren<Canvas>();
        GFBuiltin.UICamera = RootCanvas.worldCamera;

        UpdateCanvasScaler();
    }
    public void UpdateCanvasScaler()
    {
        CanvasScaler canvasScaler = RootCanvas.GetComponent<CanvasScaler>();
        //canvasScaler.referenceResolution = AppSettings.Instance.DesignResolution;
        var designRatio = canvasScaler.referenceResolution.x / (float)canvasScaler.referenceResolution.y;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = Screen.width / (float)Screen.height > designRatio ? 1 : 0;
        Log.Info($"----------UI适配Match:{canvasScaler.matchWidthOrHeight}----------");

    }


    /// <summary>
    /// 退出或重启
    /// </summary>
    /// <param name="type"></param>
    public static void Shutdown(ShutdownType type)
    {
        GameEntry.Shutdown(type);
    }
}