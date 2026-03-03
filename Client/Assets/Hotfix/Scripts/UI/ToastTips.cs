using Cysharp.Threading.Tasks;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using System;
using UnityEngine;
//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[OptionUIGroup(UIGroupNameConstants.Tip)]
public partial class ToastTips : UIFormBase
{
    public const string P_Duration = "Duration";
    public const string P_Text = "Text";
    public const string P_Style = "Style";

    float m_Duration;
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_Duration = Params.Get<VarFloat>(P_Duration);
        varContentText.text = Params.Get<VarString>(P_Text);
        var style = Params.Get<VarUInt32>(P_Style);
        SetToastStyle(style);
    }
    protected override void OnOpenAnimationComplete()
    {
        base.OnOpenAnimationComplete();
        ScheduleStart();
    }
    void SetToastStyle(uint style)
    {
        style = (uint)Mathf.Clamp(style, 0, (uint)UIExtension.ToastStyle.White);
        for (int i = 0; i < varToastMessageArr.Length; i++)
        {
            varToastMessageArr[i].SetActive(i == style);
        }
    }
    private void ScheduleStart()
    {
        UniTask.Delay(TimeSpan.FromSeconds(m_Duration), true).ContinueWith(() =>
        {
            GameApp.UI.Close(this);
        }).Forget();
    }
}
