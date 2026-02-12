using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[OptionUIGroup(UIGroupNameConstants.Window)]
public partial class UITopbar : UIFormBase
{
    public const string P_EnableBG = "EnableBG";
    public const string P_EnableSettingBtn = "EnableSettingBtn";
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GameApp.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);

        varBg.enabled = Params.Get<VarBoolean>(P_EnableBG, true);
        varBtnMenu.gameObject.SetActive(Params.Get<VarBoolean>(P_EnableSettingBtn, true));


        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        varTxtCoin.text = playerDm.Coins.ToString();
        varTxtEnergy.text = playerDm.GetData(PlayerDataType.Energy).ToString();
        varTxtGem.text = playerDm.GetData(PlayerDataType.Diamond).ToString();
    }
    public override void OnClose(bool isShutdown, object userData)
    {
        GameApp.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnPlayerDataChanged);
        base.OnClose(isShutdown, userData);
    }
    private void OnPlayerDataChanged(object sender, GameEventArgs e)
    {
        var args = e as PlayerDataChangedEventArgs;
        switch (args.DataType)
        {
            case PlayerDataType.Coins:
                varTxtCoin.text = args.Value.ToString();
                break;
            case PlayerDataType.Diamond:
                varTxtGem.text = args.Value.ToString();
                break;
            case PlayerDataType.Energy:
                varTxtEnergy.text = args.Value.ToString();
                break;
        }
    }

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varBtnMenu)
        {
            GameApp.UI.OpenUIForm(UIViews.SettingDialog);
        }
        else if (btSelf == varBtnCoin)
        {
            GameApp.UI.ShowToast("加金币");
        }
        else if (btSelf == varBtnGem)
        {
            GameApp.UI.ShowToast("加钻石");
        }
        else if (btSelf == varBtnEnergy)
        {
            GameApp.UI.ShowToast("加能量");
        }
    }
}
