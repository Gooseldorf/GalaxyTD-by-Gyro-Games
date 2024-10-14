using CardTD.Utilities;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

[SelectionBase]
public class DropZoneVisual : EnvironmentVisual, IPowerableVisual
{
    [SerializeField] private int2 dropZoneOffset;
    [SerializeField] private bool isCanInfluenceToFlowField;
    [ShowInInspector] public int Id { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; } = true;

    [SerializeField] private SpriteRenderer lamps;
    [SerializeField] private SpriteRenderer rotationPart;
    [SerializeField] private SpriteRenderer towerIcon;

    [SerializeField] private float animationLoopTime = 2;

    private Sequence animationSeq;

    [ShowInInspector, ReadOnly]
    private bool isAnimated => animationSeq != null;

    private bool isMenuScene = false;
    
    public override int2 GridSize => new int2(2, 2);

    public DropZone GetDropZoneData(int2 gridPositionOffset)
    {
        return new DropZone(GridPosition + gridPositionOffset + dropZoneOffset, GridSize, Id, IsPowered, isCanInfluenceToFlowField);
    }

    public override void InitPosition(IGridPosition gridPosition)
    {
        transform.position = gridPosition.Position;
        gameObject.SetActive(true);
    }

    private void Awake()
    {
        isMenuScene = SceneManager.GetActiveScene().buildIndex == 0;
    }

    private void Start()
    {
        //Animator animator = gameObject.GetComponent<Animator>();
        //animator.Play("DropZoneAnimation", 0, UnityEngine.Random.Range(0f, 1f));
        
        if (!isMenuScene)
        {
            StartAnimation();
            animationSeq?.Goto(UnityEngine.Random.Range(0f, animationLoopTime), true);
        }
    }

    public void Show(bool isShow)
    {
        if (isShow && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            StartAnimation();
        }

        if (!isShow && gameObject.activeSelf)
        {
            StopAnimation();
            gameObject.SetActive(false);

        }
    }

    private void StartAnimation()
    {
        if(isMenuScene) return;
        
        ResetAnimation();
        Vector3 endRotation = new Vector3(0, 0, 43);
        animationSeq = DOTween.Sequence();
        animationSeq.Append(rotationPart.transform.DOLocalRotate(endRotation, animationLoopTime).SetEase(Ease.Linear));
        animationSeq.Insert(0, lamps.DOColor(GetLampsColor(1), animationLoopTime / 2).SetEase(Ease.Linear));
        animationSeq.Insert(animationLoopTime / 2, lamps.DOColor(GetLampsColor(0), animationLoopTime / 2).SetEase(Ease.Linear));
        animationSeq.InsertCallback(animationLoopTime, () => ResetAnimation());
        animationSeq.SetUpdate(true);
        animationSeq.SetLoops(-1);
        animationSeq.Play();
    }

    private void StopAnimation()
    {
        if (animationSeq != null)
        {
            animationSeq.Kill();
            animationSeq = null;
        }
    }

    private void OnDestroy()
    {
        StopAnimation();
    }

    public override void InitVisual(object data)
    {
        DropZone dropZone = data as DropZone;
        Id = dropZone.Id;
        isCanInfluenceToFlowField = dropZone.IsCanInfluenceToFlowField;
        InitPosition(dropZone);
    }

    [Button]
    public void TogglePower() => SetPowered(!IsPowered);

    public void SetPowered(bool isPowered)
    {
        StopAnimation();
        IsPowered = isPowered;
        lamps.color = IsPowered ? new Color(0, 1, 0.7016382f) : Color.red;
        towerIcon.color = IsPowered ? new Color(0, 1, .7016382f) : new Color(.09803922f, .1960784f, .254902f, .5f);
        StartAnimation();
    }

    private Color GetLampsColor(float alpha) => new Color(lamps.color.r, lamps.color.g, lamps.color.b, alpha);

    private void ResetAnimation()
    {
        lamps.color = GetLampsColor(0);
        rotationPart.transform.localRotation = Quaternion.identity;
    }
}