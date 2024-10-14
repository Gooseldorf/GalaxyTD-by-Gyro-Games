using CardTD.Utilities;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class SelectionVisual : MonoBehaviour
{
    [SerializeField]
    protected GameObject rotationRoot;
    [SerializeField]
    protected SpriteRenderer outher;
    [SerializeField]
    protected SpriteRenderer inner1;
    [SerializeField]
    protected SpriteRenderer inner2;
    [SerializeField]
    protected SpriteRenderer line;
    [SerializeField]
    public TMP_Text Label;
    [SerializeField]
    protected float rotationTime = 3;

    public Color Color
    {
        get => outher.color;
        set => outher.color = inner1.color = inner2.color = Label.color = line.color = value;
    }

    protected Sequence rotationSeq;

    [Button]
    public void Show(Color clr)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        Restart(clr);
    }

    public virtual void Restart(Color clr)
    {
        Stop();

        rotationSeq = DOTween.Sequence();
        transform.DORotate(new Vector3(0, 0, 360), rotationTime);
        rotationSeq.Append(DOTween.To(() => inner1.transform.rotation.eulerAngles,
                        x => inner1.transform.rotation = Quaternion.Euler(x),
                        new Vector3(0f, 0f, 360f), rotationTime)
                    .SetEase(Ease.Linear));
        rotationSeq.Insert(0, DOTween.To(() => inner2.transform.rotation.eulerAngles,
                        x => inner2.transform.rotation = Quaternion.Euler(x),
                        new Vector3(0f, 0f, -360f), rotationTime)
                    .SetEase(Ease.Linear));
        rotationSeq.SetLoops(-1);
        rotationSeq.SetUpdate(true);

        line.gameObject.SetActive(true);

    }

    protected virtual void Stop()
    {
        if (rotationSeq != null)
        {
            rotationSeq.Kill();
            rotationSeq = null;
            inner1.transform.localRotation = inner2.transform.localRotation = Quaternion.identity;
            Label.text = "";
            line.gameObject.SetActive(false);
        }
    }

    [Button]
    public void Hide()
    {
        Stop();
        gameObject.SetActive(false);
    }

    public void SetPosition(float2 position) => transform.position = position.ToFloat3();

    public void SetPosition(Vector3 position) => transform.position = position;
}
