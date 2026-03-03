using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[OptionUIGroup(UIGroupNameConstants.Window)]
public partial class GameUIForm : UIFormBase
{
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RefreshCoinsText();

        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, false);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        this.OpenSubUIForm(UIViews.UITopbar, 1, uiparms);
    }
    private void RefreshCoinsText()
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        coinNumText.text = playerDm.Coins.ToString();
    }
}
