using UnityEngine;
using DG.Tweening;

public class GatlingTowerVisual : TowerVisual
{
    [SerializeField]
    private Sprite[] barrelSprites;
    [SerializeField]
    private SpriteRenderer barrel;
    [SerializeField]
    private float timeBetweenFrames;
    [SerializeField]
    private float slowDownTime;
    [SerializeField]
    private float slowDownDelay = 1;
    [SerializeField] 
    private float stopSoundDelay = 0.3f;

    private Sequence barrelSeq;
    private Sequence slowdownSeq;

    private float slowDownTimer = 0;
    private float stopSoundTimer = 0;

    public override void Shoot()
    {
        base.Shoot();
        
        if (slowdownSeq != null)
        {
            slowdownSeq.Kill();
            slowdownSeq = null;
        }

        slowDownTimer = slowDownDelay;
        StartAnimation();
        stopSoundTimer = stopSoundDelay;
        MusicManager.PlayGatlingMuzzleSound(this.transform);
    }

    private void Update()
    {
        if (slowDownTimer > 0)
        {
            slowDownTimer -= Time.deltaTime;
            if (slowDownTimer < 0)
            {
                ShowSlowdown();
            }
        }

        if (stopSoundTimer > 0)
        {
            stopSoundTimer -= Time.deltaTime;
            if (stopSoundTimer <= 0)
            {
                MusicManager.StopGatlingMuzzleSound(this.transform);
            }
        }
    }
    
    private void ShowSlowdown()
    {
        barrelSeq.Kill();
        barrelSeq = null;

        slowdownSeq = DOTween.Sequence();
        int startIndex = System.Array.IndexOf(barrelSprites, barrel.sprite);
        int step = 0;
        for (float i = 0; i < slowDownTime; i += (timeBetweenFrames * step))
        {
            int index = (startIndex + step) % barrelSprites.Length;
            barrelSeq.InsertCallback(i, () => SetBarrelSprite(barrelSprites[index]));
            step++;
        }
        slowdownSeq.OnComplete(() => slowdownSeq = null);
    }

    private void StartAnimation()
    {
        if (barrelSeq != null)
            return;

        barrelSeq = DOTween.Sequence();
        for (int i = 0; i < barrelSprites.Length; i++)
        {
            int index = i;
            barrelSeq.InsertCallback((i + 1) * timeBetweenFrames, () => SetBarrelSprite(barrelSprites[index]));
        }
        barrelSeq.SetLoops(-1);
    }

    private void SetBarrelSprite(Sprite sprite) => barrel.sprite = sprite;
}