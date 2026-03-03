using GameFrameX.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[OptionUIGroup(UIGroupNameConstants.Window)]
public class CommonDialog : UIFormBase
{
    [SerializeField] Text title;
    [SerializeField] TextMeshProUGUI content;
    [SerializeField] Button closeBt;
    [SerializeField] Button[] buttons;
    [SerializeField] System.Action positiveAction;
    [SerializeField] System.Action negativeAction;
    public override void OnInit(object userData)
    {
        base.OnInit(userData);
        for (int i = 0; i < buttons.Length; i++)
        {
            int btTag = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => { ClickButton(btTag); });
        }
    }
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        var positiveData = Params.Get<VarObject>("PositiveAction");
        positiveAction = positiveData != null ? positiveData.Value as System.Action : null;

        var negativeData = Params.Get<VarObject>("NegativeAction");
        negativeAction = negativeData != null ? negativeData.Value as System.Action : null;

        bool showClose = Params.Get<VarBoolean>("ShowClose", true);

        closeBt.interactable = showClose;
        title.text = Params.Get<VarString>("Title");
        content.text = Params.Get<VarString>("Content");
        //buttons[1].gameObject.SetActive(positiveAction != null);
        buttons[0].gameObject.SetActive(negativeAction != null);
    }
    private void ClickButton(int btTag)
    {
        switch (btTag)
        {
            case 0:
                {
                    negativeAction?.Invoke();
                    OnClickClose();
                }
                break;
            case 1:
                {
                    positiveAction?.Invoke();
                    OnClickClose();
                }
                break;
        }
    }
}
