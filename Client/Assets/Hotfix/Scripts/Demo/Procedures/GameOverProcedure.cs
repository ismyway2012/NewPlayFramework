using DG.Tweening;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameOverProcedure : ProcedureBase
{
    IFsm<IProcedureManager> procedure;
    private bool isWin;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;
        isWin = this.procedure.GetData<VarBoolean>("IsWin");

        ShowGameOverUIForm(2);
    }
    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (!isShutdown)
        {
            GameApp.UI.CloseAllLoadingUIForms();
            GameApp.UI.CloseAllLoadedUIForms();
            GameApp.Entity.HideAllLoadingEntities();
            GameApp.Entity.HideAllLoadedEntities();
        }
        base.OnLeave(procedureOwner, isShutdown);
    }

    private void ShowGameOverUIForm(float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            var gameoverParms = UIParams.Create();
            gameoverParms.Set<VarBoolean>(GameOverUIForm.P_IsWin, isWin);
            GameApp.UI.OpenUIForm(UIViews.GameOverUIForm, gameoverParms);
        });
    }

    internal void BackHome()
    {
        ChangeState<MenuProcedure>(procedure);
    }
}
