using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using static MusicManager;

public class SecondChanceWaveVisual : MonoBehaviour
{
    public void PlayWave(float2 position, float range)
    {
        transform.position = new Vector3(position.x, position.y, 0);
        transform.localScale = Vector3.zero;
        transform.DOScale(range * 3.35f, .3f).SetUpdate(true).OnComplete(() => Destroy(gameObject));
        PlaySound2D(SoundKey.Second_chance);
    }
}
