using CardTD.Utilities;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using static MusicManager;

public class ReloadBar : MonoBehaviour
{
    private const float rotationMaxTime = 150;

    [SerializeField]
    private SpriteRenderer fg;
    [SerializeField]
    private GameObject bg;
    [SerializeField]
    private GameObject reloadIcon;
    [SerializeField]
    public SpriteRenderer noCashIcon;

    private Sequence reloadSeq;
    private Sequence noCashSeq;

    private Color transparencyColor = new Color(1, 1, 1, 0);

    private void Awake()
    {
        fg.color = Color.yellow;
        reloadIcon.SetActive(false);
        noCashIcon.gameObject.SetActive(false);
    }

    public void UpdateAmmoCount(float percent)
    {
        if (reloadSeq != null)
            return;
        fg.transform.localScale = new Vector3(120 * percent, fg.transform.localScale.y, fg.transform.localScale.z);
    }

    public void ShowReload(float reloadTime)
    {
        if (reloadSeq != null)
            reloadSeq.Kill();

        ShowNoCash(false);

        reloadIcon.SetActive(true);
        fg.color = Color.white;
        fg.transform.localScale = new Vector3(0, fg.transform.localScale.y, fg.transform.localScale.z);

        reloadSeq = DOTween.Sequence();
        reloadSeq.Append(fg.transform.DOScaleX(120, reloadTime).SetEase(Ease.Linear));
        reloadSeq.Insert(0, reloadIcon.transform.DOLocalRotate(new Vector3(0, 0, -36000), rotationMaxTime, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        reloadSeq.InsertCallback(reloadTime, () => HideReload());
    }

    public void ShowNoCash(bool show)
    {
        if (show && noCashSeq == null && reloadSeq == null)
        {
            noCashIcon.color = transparencyColor;
            //if(!noCashIcon.gameObject.activeInHierarchy)
                PlaySound2D(SoundKey.Lacking_ammo);

            noCashIcon.gameObject.SetActive(true);

            noCashSeq = DOTween.Sequence();
            noCashSeq.Append(noCashIcon.DOColor(Color.white, 1f).SetEase(Ease.OutExpo));
            noCashSeq.Append(noCashIcon.DOColor(transparencyColor, 1f).SetEase(Ease.InExpo));
            noCashSeq.SetLoops(-1);
            noCashSeq.SetUpdate(true);
        }
        if (!show && noCashSeq != null)
        {
            noCashSeq.Kill();
            noCashSeq = null;

            noCashIcon.gameObject.SetActive(false);
        }
    }

    public void HideReload()
    {
        //fg.SetActive(false);
        //bg.SetActive(false);

        if (reloadSeq != null)
        {
            reloadSeq.Kill();
            reloadSeq = null;
            reloadIcon.SetActive(false);
            fg.color = Color.yellow;
        }
    }

}
