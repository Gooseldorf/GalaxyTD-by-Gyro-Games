using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

public class PopUpTextVisualizator : MonoBehaviour
{
    [SerializeField] private PopUpText textPrefab;
    [SerializeField] private int poolSize;
    [SerializeField] private float2 towerRankTextOffset;
    private UpgradeProvider upgradeProvider;
    private PopUpTextAnimationData animData;

    public ObjectPool<PopUpText> TextObjPool { get; private set; }
    
    private void Awake()
    {
        TextObjPool = new ObjectPool<PopUpText>(CreateText,GetText,ReleaseText,
            text => Destroy(text.gameObject),true, poolSize);

        upgradeProvider = DataManager.Instance.Get<UpgradeProvider>();
        animData = UIHelper.Instance.PopUpTextAnimationData;
        
        Messenger<Entity, int>.AddListener(GameEvents.TowerUpgraded, OnTowerUpgraded);
    }

    private void OnDestroy()
    {
        Messenger<Entity, int>.RemoveListener(GameEvents.TowerUpgraded, OnTowerUpgraded);
    }

    private PopUpText CreateText()
    {
        PopUpText text = Instantiate(textPrefab, transform);
        text.gameObject.SetActive(false);
        return text;
    }

    private void GetText(PopUpText text)
    {
        text.gameObject.SetActive(true);
    }

    private void ReleaseText(PopUpText text)
    {
        DOTween.Kill(text);
        text.gameObject.SetActive(false);
    }
    
    private void OnTowerUpgraded(Entity tower, int level)
    {
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        float2 position =  manager.GetComponentData<PositionComponent>(tower).Position;
        AllEnums.TowerId towerId = manager.GetComponentData<AttackerComponent>(tower).TowerType;
        
        if (level >= upgradeProvider.GameUpgradeLevelCap)
        {
            ShowPopUpText(LocalizationManager.GetTranslation("GameScene/MaxLevel_var"), position);
            return;
        }
        upgradeProvider.TryGetNextGameUpgrade(towerId, level - 1, out CompoundUpgrade upgrade);
        
        string text = "";

        for (int i = upgrade.Upgrades.Count - 1 ; i >= 0 ; i--)
        {
            text += upgrade.Upgrades[i].Bonus.GetDescription(true, showMagazineSizeMult: false, popUpUpgradeText: true);
            text += "\n";
        }

        if (text.Length == 0) return;
        
        text = text.Replace("<color=#1fb2de>></color> ", "");
        if (towerId == AllEnums.TowerId.Shotgun && level == 9)
        {
            text = MergeSimilarLines(text);
        }
        ShowPopUpText(text, position + towerRankTextOffset);
        
    }
    
    private string MergeSimilarLines(string text)
    {
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var dict = new Dictionary<string, int>();

        foreach (string line in lines)
        {
            var match = Regex.Match(line, @"([-\+]\d+)(<color=#1fb2de>%</color>)(.*$)");
            if (match.Success)
            {
                int value = int.Parse(match.Groups[1].Value);
                string descriptor = match.Groups[3].Value;

                if (dict.ContainsKey(descriptor))
                    dict[descriptor] += value;
                else
                    dict[descriptor] = value;
            }
            else
            {
                if (!dict.ContainsKey(line))
                    dict[line] = 0;
            }
        }

        var result = dict.Select(kvp => kvp.Value != 0 ? 
            $"{(kvp.Value > 0 ? "+" : "")}{kvp.Value}<color=#1fb2de>%</color>{kvp.Key}" : 
            $"{kvp.Key}"
        ).ToList();

        return string.Join("\n", result.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
    
    [Button]
    private void ShowPopUpText(string text, float2 position)
    {
        string[] lines = text.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        float delay = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            PopUpText popUpText = TextObjPool.Get();
            popUpText.TMP.text = lines[i];
            popUpText.transform.position = new Vector3(position.x, position.y);
        
            Sequence seq = DOTween.Sequence();
            seq.Append(popUpText.transform.DOScale(1, animData.ScaleTime).From(0)); 
            seq.Join(DOTween.To(() => popUpText.TMP.color, x => popUpText.TMP.color = x, new Color(popUpText.TMP.color.r, popUpText.TMP.color.g, popUpText.TMP.color.b, 1f), animData.FadeInTime));
            seq.Join(popUpText.transform.DOMoveY(popUpText.transform.position.y + animData.MoveYDistance /*+ (0.6f * (lines.Length - 1 - i))*/, animData.MoveYTime));
        
            seq.AppendInterval(animData.IdleTime); 
        
            seq.Append(DOTween.To(() => popUpText.TMP.color, x => popUpText.TMP.color = x, new Color(popUpText.TMP.color.r, popUpText.TMP.color.g, popUpText.TMP.color.b, 0f), animData.FadeOutTime));

            seq.SetUpdate(true);
            seq.SetTarget(popUpText);
            seq.SetDelay(delay);
            seq.OnComplete(() => TextObjPool.Release(popUpText));
            delay += 0.3f;
        }
    }
}
