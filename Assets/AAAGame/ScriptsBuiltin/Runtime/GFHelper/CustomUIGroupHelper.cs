using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine.UI;
using GameFrameX.UI.UGUI.Runtime;

//[Obfuz.ObfuzIgnore]
public class CustomUIGroupHelper : UIGroupHelperBase
{
    private Canvas m_CachedCanvas = null;

    /// <summary>
    /// 设置界面组深度。
    /// </summary>
    /// <param name="depth">界面组深度。</param>
    public override void SetDepth(int depth)
    {
        m_CachedCanvas.overrideSorting = true;
        m_CachedCanvas.sortingOrder = depth;
    }

    private void Awake()
    {
        m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
        gameObject.GetOrAddComponent<GraphicRaycaster>();
    }

    private void Start()
    {
        RectTransform transform = gameObject.GetOrAddComponent<RectTransform>();
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
    }

    public override IUIGroupHelper Handler(Transform root, string groupName, string uiGroupHelperTypeName, IUIGroupHelper customUIGroupHelper)
    {
        GameObject component = new GameObject();
        var comName = groupName;
        component.name = comName;
        component.transform.SetParent(root, false);
        component.SetLayerRecursively(LayerMask.NameToLayer("UI"));
        RectTransform rectTransform = component.GetOrAddComponent<RectTransform>();
        rectTransform.MakeFullScreen();
        return GameFrameX.Runtime.Helper.CreateHelper(component, uiGroupHelperTypeName, (UIGroupHelperBase)customUIGroupHelper, 0);
    }
}
