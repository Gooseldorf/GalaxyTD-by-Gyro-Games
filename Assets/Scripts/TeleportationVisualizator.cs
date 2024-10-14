using CardTD.Utilities;
using DefaultNamespace.Systems.Interfaces;
using Unity.Mathematics;
using UnityEngine;
using Visual;

public class TeleportationVisualizator : ScriptableObject, IInitializable
{
    [SerializeField] private Color inColor;
    [SerializeField] private Color outColor;
    private SimpleEffectManager effectManager;
    
    public void Init()
    {
        effectManager = GameServices.Instance.Get<SimpleEffectManager>();
        Messenger<float3, float3>.AddListener(GameEvents.TeleportEvent, ShowTeleportationEffect);
    }

    public void DeInit()
    {
        Messenger<float3, float3>.RemoveListener(GameEvents.TeleportEvent, ShowTeleportationEffect);
    }

    public void Clear()
    {
        
    }

    private void ShowTeleportationEffect(float3 inPos, float3 outPos)
    {
        TeleportationEffectVisual inVisual = InitTeleportationEffect(inPos, inColor);
        TeleportationEffectVisual outVisual = InitTeleportationEffect(outPos, outColor);

        MusicManager.PlayTeleportSound(inVisual.transform, outVisual.transform);
    }

    public TeleportationEffectVisual InitTeleportationEffect(float3 position, Color color)
    {
        var pool = effectManager.TeleportationEffectPool;
        var go = pool.Get().GetComponent<TeleportationEffectVisual>();
        go.Play(pool, position, color);

        return go;
    }
}
