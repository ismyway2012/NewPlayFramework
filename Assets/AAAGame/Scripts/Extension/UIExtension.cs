using UnityEngine;
using GameFrameX.Runtime;

using UnityEngine.U2D;
using DG.Tweening;
using System;
using UnityEngine.UI;
using GameFrameX.UI.Runtime;
using System.Threading.Tasks;
using Hotfix.Config.Tables.Core;
using Cysharp.Threading.Tasks;

public static class UIExtension
{
    /// <summary>
    /// 异步加载并设置Sprite
    /// </summary>
    /// <param name="image"></param>
    /// <param name="spriteName"></param>
    public static void SetSprite(this Image image, string spriteName, bool resize = false)
    {
        spriteName = UtilityBuiltin.AssetsPath.GetSpritesPath(spriteName);
        GF.UI.LoadSprite(spriteName, sp =>
        {
            if (sp != null)
            {
                image.sprite = sp;
                if (resize) image.SetNativeSize();
            }
        });
    }
    /// <summary>
    /// 异步加载并设置Texture
    /// </summary>
    /// <param name="rawImage"></param>
    /// <param name="spriteName"></param>
    public static void SetTexture(this RawImage rawImage, string spriteName, bool resize = false)
    {
        spriteName = UtilityBuiltin.AssetsPath.GetTexturePath(spriteName);
        GF.UI.LoadTexture(spriteName, tex =>
        {
            if (tex != null)
            {
                rawImage.texture = tex;
                if (resize) rawImage.SetNativeSize();
            }
        });
    }
    /// <summary>
    /// 判断是否点击在UI上
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    public static bool IsPointerOverUIObject(this UIComponent uiCom, Vector3 mousePosition)
    {
        return UtilityEx.IsPointerOverUIObject(mousePosition);
    }

    /// <summary>
    /// 世界坐标转换到UI屏幕坐标
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="worldPos">世界坐标点</param>
    /// <returns></returns>
    public static Vector3 PositionWorldToUI(this UIComponent uiCom, Vector3 worldPos, RectTransform targetRect)
    {
        var viewPos = GF.Scene.MainCamera.WorldToViewportPoint(worldPos);
        var uiPos = GF.UICamera.ViewportToScreenPoint(viewPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, uiPos, GF.UICamera, out var localPoint);
        return localPoint;
    }

    /// <summary>
    /// 异步加载Sprite
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="spriteName"></param>
    /// <param name="onSpriteLoaded"></param>
    public static async void LoadSprite(this UIComponent uiCom, string spriteName, System.Action<Sprite> onSpriteLoaded)
    {
        if (GF.Resource.HasAssetPath(spriteName) == false)
        {
            Log.Warning("UIExtension.SetSprite()失败, 资源不存在:{0}", spriteName);
            return;
        }
        var asset = await GF.Resource.LoadAssetAsync<Sprite>(spriteName);
        Sprite resultSp = asset.GetAssetObject<Sprite>();
        onSpriteLoaded.Invoke(resultSp);
    }
    /// <summary>
    /// 异步加载Texture
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="spriteName"></param>
    /// <param name="onSpriteLoaded"></param>
    public static async void LoadTexture(this UIComponent uiCom, string spriteName, System.Action<Texture2D> onSpriteLoaded)
    {
        if (GF.Resource.HasAssetPath(spriteName) == false)
        {
            Log.Warning("UIExtension.LoadTexture()失败, 资源不存在:{0}", spriteName);
            return;
        }
        var asset = await GF.Resource.LoadAssetAsync<Texture2D>(spriteName);
        Texture2D resultSp = asset.GetAssetObject<Texture2D>();
        onSpriteLoaded.Invoke(resultSp);
    }
    /// <summary>
    /// Destory指定根节点下的所有子节点
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="parent"></param>
    public static void RemoveAllChildren(this UIComponent ui, Transform parent)
    {
        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    /// <summary>
    /// 显示Toast提示
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="text"></param>
    /// <param name="duration"></param>
    public static void ShowToast(this UIComponent ui, string text, ToastStyle style = ToastStyle.Blue, float duration = 2)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarString>(ToastTips.P_Text, text);
        uiParams.Set<VarFloat>(ToastTips.P_Duration, duration);
        uiParams.Set<VarUInt32>(ToastTips.P_Style, (uint)style);
        var tipsGroup = ui.GetUIGroup(Const.UIGroup.Tips.ToString());
        if (tipsGroup.CurrentUIForm != null)
        {
            uiParams.SortOrder = ((tipsGroup.CurrentUIForm as UIFormBase)).Params.SortOrder + 1;
        }
        ui.OpenUIForm(UIViews.ToastTips, uiParams).Forget();
    }

    /// <summary>
    /// 打开UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="viewId">UI界面id(传入自动生成的UIViews枚举值)</param>
    /// <param name="parms"></param>
    /// <returns></returns>
    public static async UniTask<IUIForm> OpenUIForm(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    {
        var uiTb = GF.Config.GetConfig<TbUITable>();
        var uiGroupTb = GF.Config.GetConfig<TbUIGroupTable>();
        int uiId = (int)viewId;
        if (!uiTb.TryGet(uiId, out var uiRow))
        {
            Log.Error("UITable表不存在id:{0}", uiId);
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return await UniTask.FromResult((UIFormBase)null);
        }
        if (!uiGroupTb.TryGet(uiRow.UIGroupId, out var uiGroupRow))
        {
            Log.Error("UIGroupTable表不存在id:{0}", uiId);
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return await UniTask.FromResult((UIFormBase)null);
        }
        string uiName = UtilityBuiltin.AssetsPath.GetUIFormPath(uiRow.UIPrefab);
        if (uiCom.IsLoadingUIForm(uiName))
        {
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return await UniTask.FromResult((UIFormBase)null);
        }
        parms ??= UIParams.Create();
        parms.AllowEscapeClose ??= uiRow.EscapeClose;
        parms.SortOrder ??= uiRow.SortOrder + uiGroupRow.Depth;
        Type uiFormType = Type.GetType(viewId.ToString());
        return await uiCom.OpenUIAsync(uiName, uiFormType, uiRow.PauseCoveredUI, parms);//, uiGroupRow.Name
        //return parms.Id;
    }

    /// <summary>
    /// 关闭UI界面(关闭前播放UI界面关闭动画)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="ui"></param>
    public static void Close(this UIComponent uiCom, UIForm ui)
    {
        Close(uiCom, ui.SerialId);
    }
    /// <summary>
    /// 关闭UI界面(关闭前播放UI界面关闭动画)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="uiFormId"></param>
    public static void Close(this UIComponent uiCom, int uiFormId)
    {
        if (uiCom.IsLoadingUIForm(uiFormId))
        {
            GF.UI.CloseUIForm(uiFormId);
            return;
        }
        if (!uiCom.HasUIForm(uiFormId))
        {
            return;
        }
        var uiForm = uiCom.GetUIForm(uiFormId);
        UIFormBase logic = uiForm as UIFormBase;
        logic.CloseWithAnimation();
    }
    /// <summary>
    /// 关闭整个UI组的所有UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="groupName"></param>
    public static void CloseUIForms(this UIComponent uiCom, string groupName)
    {
        var group = uiCom.GetUIGroup(groupName);
        var all = group.GetAllUIForms();
        foreach (var item in all)
        {
            uiCom.CloseUIForm(item.SerialId);
        }
    }
    /// <summary>
    /// 判断UI界面是否正在加载队列(还没有实体化)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool IsLoadingUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        return uiCom.IsLoadingUIForm(assetName);
    }
    /// <summary>
    /// 是否已经打开UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool HasUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        return uiCom.HasUIForm(assetName);
    }
    /// <summary>
    /// 获取UI界面的prefab资源名
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static string GetUIFormAssetName(this UIComponent uiCom, UIViews view)
    {
        if (GF.Config == null || !GF.Config.HasConfig<TbUITable>())
        {
            Log.Warning("GetUIFormAssetName is empty.");
            return string.Empty;
        }

        var uiTb = GF.Config.GetConfig<TbUITable>();
        if (!uiTb.TryGet((int)view, out var table))
        {
            return string.Empty;
        }
        string uiName = UtilityBuiltin.AssetsPath.GetUIFormPath(table.UIPrefab);
        return uiName;
    }
    /// <summary>
    /// 关闭所有打开的某个界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <param name="uiGroup"></param>
    public static void CloseUIForms(this UIComponent uiCom, UIViews view, string uiGroup = null)
    {
        string uiAssetName = uiCom.GetUIFormAssetName(view);
        IUIForm[] uIForms;
        if (string.IsNullOrEmpty(uiGroup))
        {
            uIForms = uiCom.GetUIForms(uiAssetName);
        }
        else
        {
            if (!uiCom.HasUIGroup(uiGroup))
            {
                return;
            }
            uIForms = uiCom.GetUIGroup(uiGroup).GetUIForms(uiAssetName);
        }

        foreach (var item in uIForms)
        {
            uiCom.Close(item.SerialId);
        }
    }
    /// <summary>
    /// 刷新所有UI的多语言文本(当语言切换时需调用),用于即时改变多语言文本
    /// </summary>
    /// <param name="uiCom"></param>
    public static void UpdateLocalizationTexts(this UIComponent uiCom)
    {
        //foreach (var item in Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>())
        //{
        //    item.ClearFontAssetData();
        //}
        foreach (UIForm uiForm in uiCom.GetAllLoadedUIForms())
        {
            (uiForm as UIFormBase).InitLocalization();
        }
        var uiObjectPool = GF.ObjectPool.GetObjectPool(pool => pool.FullName == "GameFramework.UI.UIManager+UIFormInstanceObject.UI Instance Pool");
        if (uiObjectPool != null)
        {
            uiObjectPool.ReleaseAllUnused();
        }
    }
    /// <summary>
    /// 获取当前顶层的UI界面id(排除子界面)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <returns></returns>
    public static int GetTopUIFormId(this UIComponent uiCom)
    {
        var groups = uiCom.GetAllUIGroups();
        Array.Sort(groups, (a, b) => b.Depth.CompareTo(a.Depth));
        var dialogGp = uiCom.GetUIGroup(Const.UIGroup.Dialog.ToString());
        int maxSortOrder = -1;
        int maxOrderIndex = -1;
        int topID = -1;

        foreach (var uiFormGp in groups)
        {
            var allUIForms = uiFormGp.GetAllUIForms();
            for (int i = 0; i < allUIForms.Length; i++)
            {
                var uiBase = (allUIForms[i] as UIForm) as UIFormBase;
                if (uiBase == null || uiBase.Params.IsSubUIForm) continue;

                int curOrder = uiBase.SortOrder;
                if (curOrder >= maxSortOrder)
                {
                    maxSortOrder = curOrder;
                    maxOrderIndex = i;
                    topID = uiBase.SerialId;
                }
            }
        }
        if (topID != -1) return topID;
        return -1;
    }
    public static void ShowRewardEffect(this UIComponent uiCom, Vector3 centerPos, Vector3 fly2Pos, float flyDelay = 0.5f, System.Action onAnimComplete = null, int num = 30)
    {
        int coinNum = num;
        DOVirtual.DelayedCall(flyDelay, () =>
        {
            GF.Sound.PlayEffect("add_money.wav");
        });
        var richText = "<sprite name=USD_0>";
        for (int i = 0; i < num; i++)
        {
            var animPrams = EntityParams.Create(centerPos, Vector3.zero, Vector3.one);
            animPrams.OnShowCallback = moneyEntity =>
            {
                moneyEntity.GetComponent<TMPro.TextMeshPro>().text = richText;
                var spawnPos = UnityEngine.Random.insideUnitCircle * 3;
                var expPos = centerPos;
                expPos.x += spawnPos.x;
                expPos.y += spawnPos.y;
                var targetPos = fly2Pos;
                int moneyEntityId = moneyEntity.Entity.Id;
                var expDuration = Vector2.Distance(moneyEntity.transform.position, expPos) * 0.05f;// Mathf.Clamp(Vector3.Distance(moneyEntity.transform.position, expPos)*0.01f, 0.1f, 0.4f);
                var animSeq = DOTween.Sequence();
                animSeq.Append(moneyEntity.transform.DOMove(expPos, expDuration));
                animSeq.AppendInterval(0.25f);
                var moveDuration = Vector2.Distance(expPos, targetPos) * 0.05f;// Mathf.Clamp(Vector3.Distance(expPos, targetPos)*0.01f, 0.1f, 0.8f);
                animSeq.Append(moneyEntity.transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear));
                animSeq.onComplete = () =>
                {
                    GF.Entity.HideEntitySafe(moneyEntityId);
                    coinNum--;
                    if (coinNum <= 0)
                    {
                        onAnimComplete?.Invoke();
                    }
                    GF.Sound.PlayEffect("Collect_Gem_2.wav");
                    GF.Sound.PlayVibrate();
                };
            };
            GF.Entity.ShowEntity<SampleEntity>("Effect/EffectMoney", Const.EntityGroup.Effect, animPrams);
        }
    }


    #region Unity UI Extension
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float anchoredPositionX)
    {
        var value = rectTransform.anchoredPosition;
        value.x = anchoredPositionX;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPositionY(this RectTransform rectTransform, float anchoredPositionY)
    {
        var value = rectTransform.anchoredPosition;
        value.y = anchoredPositionY;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float anchoredPositionZ)
    {
        var value = rectTransform.anchoredPosition3D;
        value.z = anchoredPositionZ;
        rectTransform.anchoredPosition3D = value;
    }
    public static void SetColorAlpha(this UnityEngine.UI.Graphic graphic, float alpha)
    {
        var value = graphic.color;
        value.a = alpha;
        graphic.color = value;
    }
    public static void SetFlexibleSize(this LayoutElement layoutElement, Vector2 flexibleSize)
    {
        layoutElement.flexibleWidth = flexibleSize.x;
        layoutElement.flexibleHeight = flexibleSize.y;
    }
    public static Vector2 GetFlexibleSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.flexibleWidth, layoutElement.flexibleHeight);
    }
    public static void SetMinSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
    }
    public static Vector2 GetMinSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.minWidth, layoutElement.minHeight);
    }
    public static void SetPreferredSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
    }
    public static Vector2 GetPreferredSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
    }
    #endregion
    public enum ToastStyle : uint
    {
        Blue = 0,
        Yellow = 1,
        Green = 2,
        Red = 3,
        White = 4
    }
}
