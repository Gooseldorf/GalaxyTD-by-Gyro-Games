using UnityEngine;
using Random = UnityEngine.Random;

public class PowerCellVisual : EnvironmentVisual
{
    [SerializeField] private Sprite[] powerCellAnimationSprites;
    [SerializeField] private float frameRate = 0.05f;

    private float timer;
    private int currentFrame;

    private void Awake() => currentFrame = Random.Range(0, powerCellAnimationSprites.Length);

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > frameRate)
        {
            timer -= frameRate;
            currentFrame = (currentFrame + 1) % powerCellAnimationSprites.Length;
            icon.sprite = powerCellAnimationSprites[currentFrame];
        }
    }
}
