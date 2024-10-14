using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using CardTD.Utilities;

public class TargetingVisual : SelectionVisual
{
    [SerializeField]
    private float aimingTime = 3;
    [SerializeField]
    private float textChangeTime = .2f;

    private string[] text = new string[] { "2ŸØF", "0-ðÐ", "Æc0x", "ƒ<<Œ", "¢ø=Ñ" };

    private Sequence aimingSeq;

    public override void Restart(Color clr)
    {
        base.Restart(clr);
       
        aimingSeq = DOTween.Sequence();
        aimingSeq.Append(DOTween.To(() => Color, x => Color = x, clr, aimingTime).SetEase(Ease.Linear));
        aimingSeq.Insert(0, rotationRoot.transform.DOScale(.7f, aimingTime).SetEase(Ease.Linear));
        for (int i = 0; i < aimingTime / textChangeTime; i++)
        {
            aimingSeq.InsertCallback(textChangeTime * i, () => Label.text = text.GetRandomValue());
        }
        aimingSeq.SetUpdate(true);
        aimingSeq.OnComplete(() =>
        {
            aimingSeq = null;
            Label.text = "";
            line.gameObject.SetActive(false);
        });
    }

    protected override void Stop()
    {
        base.Stop();

        if (aimingSeq != null)
        {
            aimingSeq.Kill();
            aimingSeq = null;
        }

        Color = Color.white;
        rotationRoot.transform.localScale = Vector3.one;
    }
}
