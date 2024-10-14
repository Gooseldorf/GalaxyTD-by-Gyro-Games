
public class BubbleVisual : ReferencedVisual
{
    public event System.Action<BubbleVisual> OnBubbleClick;
    public void OnClick() => OnBubbleClick?.Invoke(this);
}