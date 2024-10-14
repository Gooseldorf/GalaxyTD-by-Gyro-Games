using UnityEngine;

public class TowerPreview : ReferencedVisual, IPreviewObject
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material suitMaterial;
    [SerializeField] private Material noSuitMaterial;
    [SerializeField] private SpriteRenderer rangeSprite;

    private bool isSuit = false;

    public void SetRange(float range)
    {
        rangeSprite.transform.localScale = new Vector3(.78125f * range, .78125f * range, 0);
        //rangeSprite.gameObject.SetActive(false);
    }


    public void SetSuit(bool isSuit)
    {
        if (this.isSuit == isSuit)
            return;

        this.isSuit = isSuit;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = isSuit ? suitMaterial : noSuitMaterial;
        }

        if (isSuit && !rangeSprite.gameObject.activeSelf)
            rangeSprite.gameObject.SetActive(true);

        if (!isSuit && rangeSprite.gameObject.activeSelf)
            rangeSprite.gameObject.SetActive(false);
        //rangeSprite.color = isSuit ? Color.green : Color.yellow;
    }

    public void SetPostion(Vector3 position)
    {
        transform.position = position;
    }

#if UNITY_EDITOR
    [Sirenix.OdinInspector.Button]
    public void SetSuitExternal(bool state) => SetSuit(state);
#endif
}