using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class EnvironmentVisual : ReferencedVisual
{
    [SerializeField, Required] protected SpriteRenderer icon;
    public SpriteRenderer Icon => icon;
    //public int Id { get; set; }
    
    public virtual int2 GridSize => new (Mathf.RoundToInt(icon.size.x), Mathf.RoundToInt(icon.size.y));
    public int2 GridPosition => new((int)transform.localPosition.x, (int)transform.localPosition.y);

    public virtual void InitPosition(IGridPosition gridPosition)
    {
        icon.size = new Vector2(gridPosition.GridSize.x, gridPosition.GridSize.y);
        transform.position = gridPosition.Position;    
        gameObject.SetActive(true);
    }

    public virtual void InitVisual(object data){}
    
    public override void Disable()
    {
        gameObject.SetActive(false);
    }
}

