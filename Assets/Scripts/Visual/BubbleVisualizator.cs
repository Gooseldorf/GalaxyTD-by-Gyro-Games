using CardTD.Utilities;
using ECSTest.Components;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class BubbleVisualizator : MonoBehaviour
{
    private Dictionary<Entity, BubbleVisual> bubbles = new ();

    private void Awake()
    {
        Messenger<Entity, bool>.AddListener(GameEvents.BubbleEvent, OnBubbleEvent);
        Messenger.AddListener(GameEvents.Restart, Clear);
    }

    private void OnDestroy()
    {
        Messenger<Entity, bool>.RemoveListener(GameEvents.BubbleEvent, OnBubbleEvent);
        Messenger.RemoveListener(GameEvents.Restart, Clear);
        Clear();
    }

    private void OnBubbleEvent(Entity powerCell, bool isShow)
    {
        if (isShow)
        {
            if (bubbles.ContainsKey(powerCell))
            {
                //throw new System.Exception("Attempt to add the same Power Cell to Bubbles dictionary");
                Debug.LogError("Attempt to add the same Power Cell to Bubbles dictionary");
                return;
            }
            else
            {
                BubbleVisual bubble = GameServices.Instance.Get<SimpleEffectManager>().BubblesPool.Get().GetComponent<BubbleVisual>();
                bubble.OnBubbleClick += OnBubbleClick;
                PositionComponent positionComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PositionComponent>(powerCell);
                bubble.transform.position = positionComponent.Position.ToFloat3();
                bubbles.Add(powerCell, bubble);
            }
        }
        else
        {
            if (!bubbles.ContainsKey(powerCell))
            {
                //throw new System.Exception("Attempt to remove Power Cell from Bubbles dictionary, which doesn't present");
                //Debug.LogError("Attempt to remove Power Cell from Bubbles dictionary, which doesn't present");
                return;
            }
            else
            {
                ReleaseBubble(bubbles[powerCell], powerCell);
            }
        }
    }

    private void OnBubbleClick(BubbleVisual bubbleVisual)
    {
        Entity powerCell = bubbles.GetKeyForValue(bubbleVisual);
        GameServices.Instance.PowerCellClicked(powerCell);
        ReleaseBubble(bubbleVisual, powerCell);
    }

    private void ReleaseBubble(BubbleVisual bubbleVisual, Entity powerCell)
    {
        if (!bubbles.ContainsKey(powerCell))
            return;

        bubbleVisual.OnBubbleClick -= OnBubbleClick;
        GameServices.Instance.Get<SimpleEffectManager>().BubblesPool.Release(bubbleVisual.gameObject);
        bubbles.Remove(powerCell);
    }

    private void Clear()
    {
        SimpleEffectManager sem = GameServices.Instance.Get<SimpleEffectManager>();
        foreach (var bubble in bubbles)
        {
            bubble.Value.OnBubbleClick -= OnBubbleClick;
            sem.BubblesPool.Release(bubble.Value.gameObject);
        }
        bubbles.Clear();
    }
}
