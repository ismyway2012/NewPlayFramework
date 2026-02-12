using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.UI.Runtime;

//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameProcedure : ProcedureBase
{
    private GameUIForm m_GameUI;
    private LevelEntity m_Level;
    private IFsm<IProcedureManager> procedure;

    protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;

        if (GameApp.Base.IsGamePaused)
        {
            GameApp.Base.ResumeGame();
        }

        GameApp.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GameApp.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);
        GameApp.Event.Subscribe(GameplayEventArgs.EventId, OnGameplayEvent);
        m_Level = procedureOwner.GetData<VarUnityObject>("LevelEntity").Value as LevelEntity;
        procedureOwner.RemoveData("LevelEntity");

        m_GameUI = await GameApp.UI.OpenUIFormAwait(UIViews.GameUIForm) as GameUIForm;
        m_Level.StartGame();
    }


    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

    }
    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (GameApp.Base.IsGamePaused)
        {
            GameApp.Base.ResumeGame();
        }
        GameApp.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GameApp.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIForm);
        GameApp.Event.Unsubscribe(GameplayEventArgs.EventId, OnGameplayEvent);
        base.OnLeave(procedureOwner, isShutdown);
    }


    public void Restart()
    {
        ChangeState<MenuProcedure>(procedure);
    }
    public void BackHome()
    {
        ChangeState<MenuProcedure>(procedure);
    }

    private void OnGameplayEvent(object sender, GameEventArgs e)
    {
        var args = e as GameplayEventArgs;
        if(args.EventType == GameplayEventType.GameOver)
        {
            OnGameOver(args.Params.Get<VarBoolean>("IsWin"));
        }
    }
    private void OnGameOver(bool isWin)
    {
        Log.Info("Game Over, isWin:{0}", isWin);
        procedure.SetData<VarBoolean>("IsWin", isWin);
        ChangeState<GameOverProcedure>(procedure);
    }
    private void CheckGamePause()
    {
        if (m_GameUI == null) return;

        if (GameApp.UI.GetTopUIFormId() != m_GameUI.SerialId)
        {
            if (!GameApp.Base.IsGamePaused)
            {
                GameApp.Base.PauseGame();
            }
        }
        else
        {
            if (GameApp.Base.IsGamePaused)
            {
                GameApp.Base.ResumeGame();
            }
        }
    }
    private void OnCloseUIForm(object sender, GameEventArgs e)
    {
        CheckGamePause();
    }

    private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        var args = e as OpenUIFormSuccessEventArgs;
        CheckGamePause();
    }
}
