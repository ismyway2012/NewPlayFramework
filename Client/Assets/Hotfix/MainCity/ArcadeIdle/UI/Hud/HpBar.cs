using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : HudBase
{
    public Slider bar;
    public TMP_Text hpText;

    public void SetHp(Transform target, float currentHp, float maxHp)
    {
        this.Target = target;
        gameObject.SetActive(true);
        bar.maxValue = maxHp;
        bar.minValue = 0;
        bar.value = currentHp;
        hpText.text = $"{(int)currentHp} / {(int)maxHp}";
        CancelInvoke(nameof(HideHpBar));
        Invoke(nameof(HideHpBar), 1.5f);
    }

    public void HideHpBar()
    {
        gameObject.SetActive(false);
    }
}
