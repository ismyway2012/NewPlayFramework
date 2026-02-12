//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
using GameFrameX.UI.Runtime;
using Hotfix.Config.Tables.Core;

[OptionUIGroup(UIGroupNameConstants.Window)]
public partial class LanguagesDialog : UIFormBase
{
    public const string P_LangChangedCb = "LangChangedCb";
    VarAction m_VarAction;

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_VarAction = Params.Get<VarAction>(P_LangChangedCb);
        RefreshList();
    }
    public override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        //UnspawnAll();
    }
    void RefreshList()
    {
        var langTb = GameApp.Config.GetConfig<TbLanguagesTable>();
        langTb.ForEach(lang =>
        {
            var item = this.SpawnItem<UIItemObject>(varLanguageToggle, varToggleGroup.transform);
            (item.itemLogic as LanguageItem).SetData(lang, varToggleGroup, m_VarAction);
        });
    }
}
