using UI;
using UnityEngine;

public class BackgroundParalax : MonoBehaviour
{
    private Vector2 baseWidthHeight = new Vector2(2400f, 1080f);
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer foregroundStars1Renderers;
    [SerializeField] private SpriteRenderer foregroundStars2Renderers;

    [SerializeField] private float speedBG = 10;
    [SerializeField] private float speedStars1 = -20;
    [SerializeField] private float speedStars2 = -40;

    [SerializeField] private Transform cameraTransform; 
    private Vector3 lastCameraPosition;

    private void Start()
    {
        InitSprites();

        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;

        //Vector3 backgroundTransformLocalScale = backgroundTransform.localScale;

        if (Screen.currentResolution.width > baseWidthHeight.x || Screen.currentResolution.height > baseWidthHeight.y)
            ResizeSprites(Screen.currentResolution.width / baseWidthHeight.x, Screen.currentResolution.height / baseWidthHeight.y);
    }

    private void ResizeSprites(float xModifier, float yModifier)
    {
        backgroundRenderer.transform.localScale = new Vector3(
            xModifier > 1? backgroundRenderer.transform.localScale.x * xModifier: backgroundRenderer.transform.localScale.x,
            yModifier > 1 ? backgroundRenderer.transform.localScale.y * yModifier : backgroundRenderer.transform.localScale.y, 1);

        foregroundStars1Renderers.size = new Vector2(
            xModifier > 1 ? foregroundStars1Renderers.size.x * xModifier: foregroundStars1Renderers.size.x,
            yModifier > 1 ? foregroundStars1Renderers.size.y * yModifier : foregroundStars1Renderers.size.y);

        foregroundStars2Renderers.size = new Vector2(
            xModifier > 1 ? foregroundStars2Renderers.size.x * xModifier: foregroundStars2Renderers.size.x,
            yModifier > 1 ? foregroundStars2Renderers.size.y * yModifier : foregroundStars2Renderers.size.y);

    }

    private void InitSprites()
    {
        backgroundRenderer.sprite = UIHelper.Instance.BackgroundSprites[Random.Range(0, UIHelper.Instance.BackgroundSprites.Count)];
        foregroundStars1Renderers.sprite = UIHelper.Instance.ForegroundStarsLayerOneSprites[Random.Range(0, UIHelper.Instance.ForegroundStarsLayerOneSprites.Count)];
        foregroundStars2Renderers.sprite = UIHelper.Instance.ForegroundStarsLayerTwoSprites[Random.Range(0, UIHelper.Instance.ForegroundStarsLayerTwoSprites.Count)];
    }

    private void LateUpdate()
    {
        if (lastCameraPosition != cameraTransform.position)
            UpdateBG();
    }

    private void UpdateBG()
    {
        Vector3 delta = cameraTransform.position - lastCameraPosition;
        //backgroundRenderer.transform.DOLocalMove(new Vector3(speedBG * delta.x, speedBG * delta.y , 1), 0);
        //foregroundStars1Renderers.transform.DOLocalMove(new Vector3(speedStars1 * delta.x, speedStars1 * delta.y, 1), 0);
        //foregroundStars2Renderers.transform.DOLocalMove(new Vector3(speedStars2 * delta.x, speedStars2 * delta.y, 1), 0);

        backgroundRenderer.transform.localPosition += new Vector3(speedBG * delta.x, speedBG * delta.y, 0);// DOLocalMove(new Vector3(speedBG * delta.x, speedBG * delta.y, 1), 0);
        foregroundStars1Renderers.transform.localPosition += new Vector3(speedStars1 * delta.x, speedStars1 * delta.y, 0);//DOLocalMove(new Vector3(speedStars1 * delta.x, speedStars1 * delta.y, 1), 0);
        foregroundStars2Renderers.transform.localPosition += new Vector3(speedStars2 * delta.x, speedStars2 * delta.y, 0);//DOLocalMove(new Vector3(speedStars2 * delta.x, speedStars2 * delta.y, 1), 0);
        lastCameraPosition = cameraTransform.position;
    }
}