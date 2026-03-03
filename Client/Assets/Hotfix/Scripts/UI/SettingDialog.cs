using DG.Tweening;
using GameFrameX.Event.Runtime;
using GameFrameX.Localization.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Config.Tables.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[OptionUIGroup(UIGroupNameConstants.Window)]
public partial class SettingDialog : UIFormBase
{
    int m_ClickCount;
    float m_LastClickTime;
    readonly float clickInterval = 0.4f;
    float m_ToggleHandleX;
    public override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_ToggleHandleX = Mathf.Abs(varVibrateHandle.localPosition.x);

        varToggleVibrate.onValueChanged.AddListener(isOn =>
        {
            OnToggleChanged(varToggleVibrate);
        });

        varMusicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        varSoundFxSlider.onValueChanged.AddListener(OnSoundFxSliderChanged);
    }
    public override void InitLocalization()
    {
        base.InitLocalization();
        //varVersionTxt.text = Utility.Text.Format("{0}v{1}", AppSettings.Instance.DebugMode ? "Debug " : string.Empty, GameApp.Base.EditorResourceMode ? Application.version : Utility.Text.Format("{0}({1})", Application.version, GameApp.Asset.InternalResourceVersion));
        varVersionTxt.text = Utility.Text.Format("{0}v{1}", AppSettings.Instance.DebugMode ? "Debug " : string.Empty, Application.version);
        var handleText = varToggleVibrate.GetComponentInChildren<TextMeshProUGUI>();
        handleText.text = varToggleVibrate.isOn ? GameApp.Localization.GetString("ON") : GameApp.Localization.GetString("OFF");
    }
    private void OnSoundFxSliderChanged(float arg0)
    {
        GameApp.Setting.SetMediaVolume(Const.SoundGroup.Sound, arg0);
        GameApp.Setting.SetMediaMute(Const.SoundGroup.Sound, arg0 == 0);
    }

    private void OnMusicSliderChanged(float arg0)
    {
        GameApp.Setting.SetMediaVolume(Const.SoundGroup.Music, arg0);
        GameApp.Setting.SetMediaMute(Const.SoundGroup.Music, arg0 == 0);
    }

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GameApp.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        m_ClickCount = 0;
        m_LastClickTime = Time.time;
        InitSettings();
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        GameApp.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);

        base.OnClose(isShutdown, userData);
    }
    private void InitSettings()
    {
        varMusicSlider.value = GameApp.Setting.GetMediaMute(Const.SoundGroup.Music) ? 0 : GameApp.Setting.GetMediaVolume(Const.SoundGroup.Music);
        varSoundFxSlider.value = GameApp.Setting.GetMediaMute(Const.SoundGroup.Sound) ? 0 : GameApp.Setting.GetMediaVolume(Const.SoundGroup.Sound);

        varToggleVibrate.SetIsOnWithoutNotify(!GameApp.Setting.GetMediaMute(Const.SoundGroup.Vibrate));
        OnToggleChanged(varToggleVibrate);
        RefreshLanguage();
    }

    private void OnToggleChanged(Toggle tg)
    {
        var handleText = varVibrateHandle.GetComponentInChildren<TextMeshProUGUI>();
        float targetX = tg.isOn ? m_ToggleHandleX : -m_ToggleHandleX;
        float duration = (Mathf.Abs(targetX - varVibrateHandle.anchoredPosition.x) / m_ToggleHandleX) * 0.2f;
        varVibrateHandle.DOAnchorPosX(targetX, duration).onComplete = () =>
        {
            handleText.text = tg.isOn ? GameApp.Localization.GetString("ON") : GameApp.Localization.GetString("OFF");
        };

        GameApp.Setting.SetMediaMute(Const.SoundGroup.Vibrate, !varToggleVibrate.isOn);
    }

    private void RefreshLanguage()
    {
        var curLang = GameApp.Setting.GetLanguage();
        var langTb = GameApp.Config.GetConfig<TbLanguagesTable>();
        var langRow = langTb.Find(row => row.LanguageKey == curLang.ToString());
        varIconFlag.SetSprite(langRow.LanguageIcon);
        varLanguageName.text = langRow.LanguageDisplay;
    }
    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varBtnLanguage)
        {
            var uiParms = UIParams.Create();
            VarAction action = ReferencePool.Acquire<VarAction>();
            action.Value = OnLanguageChanged;
            uiParms.Set<VarAction>(LanguagesDialog.P_LangChangedCb, action);
            GameApp.UI.OpenUIForm(UIViews.LanguagesDialog, uiParms);
        }
        else if (btSelf == varBtnHelp)
        {
            GameApp.UI.ShowToast(GameApp.Localization.GetString("Nothing"));
        }
        else if (btSelf == varBtnPrivacy)
        {
            GameApp.UI.ShowToast(GameApp.Localization.GetString("Nothing"));
        }
        else if (btSelf == varBtnTermsOfService)
        {
            GameApp.UI.ShowToast(GameApp.Localization.GetString("Nothing"));
        }
        else if (btSelf == varBtnRating)
        {
            GameApp.UI.OpenUIForm(UIViews.RatingDialog);
        }
    }
    void OnLanguageChanged()
    {
        RefreshLanguage();
        GameApp.UI.CloseUIForms(UIViews.LanguagesDialog);
        ReloadLanguage();
    }
    private void ReloadLanguage()
    {
        //GameApp.Localization.RemoveAllRawStrings();
        //GameApp.Localization.LoadLanguage(GameApp.Localization.Language.ToString(), this);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        GameApp.UI.UpdateLocalizationTexts();
    }
    public void OnClickVersionText()
    {
        if (Time.time - m_LastClickTime <= clickInterval)
        {
            m_ClickCount++;
            if (m_ClickCount > 5)
            {
                //GameApp.Debugger.ActiveWindow = !GameApp.Debugger.ActiveWindow;
                m_ClickCount = 0;
            }
        }
        else
        {
            m_ClickCount = 0;
        }
        m_LastClickTime = Time.time;
    }

    private void Back2Home()
    {
        var curProcedure = GameApp.Procedure.CurrentProcedure;
        if (curProcedure is GameProcedure)
        {
            var gameProcedure = curProcedure as GameProcedure;
            gameProcedure.BackHome();
        }
    }

}
