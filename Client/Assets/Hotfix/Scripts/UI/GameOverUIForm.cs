using DG.Tweening;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[OptionUIGroup(UIGroupNameConstants.Window)]
public partial class GameOverUIForm : UIFormBase
{
    public const string P_IsWin = "IsWin";
    
    private bool isWin;
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        isWin = Params.Get<VarBoolean>(P_IsWin);
        varTitleTxt.text = isWin ? GameApp.Localization.GetString("Victory") : GameApp.Localization.GetString("Failed");
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if(btSelf == varBackBtn)
        {
            (GameApp.Procedure.CurrentProcedure as GameOverProcedure).BackHome();
        }
    }
}
