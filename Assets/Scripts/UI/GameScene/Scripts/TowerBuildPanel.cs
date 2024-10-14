using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class TowerBuildPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TowerBuildPanel> { }

        private List<BuildTowerWidget> buildTowerWidgets = new();
        public Entity dropZone;
        private VisualTreeAsset buildTowerPrefab;
        

        private DisplayStyle panelDisplay
        {
            get => style.display.value;
            set
            {
                DisplayStyle tempStyle = value;
                style.display = tempStyle;
            }
        }

        public void Init(VisualTreeAsset buildTowerWidgetPrefab)
        {
            buildTowerPrefab = buildTowerWidgetPrefab;
            Hide();
            Messenger<int, bool>.AddListener(GameEvents.CashUpdated, UpdateTowersAvailability);

            UpdateTowers();
            Reset();
        }

        private void UpdateTowers()
        {
            List<Tower> sortedTowers = new(GameServices.Instance.Towers);
            sortedTowers.Sort(DataManager.Instance.Get<UnlockManager>().TowerComparer);
            sortedTowers.Reverse();

            /*if (buildTowerWidgets.Count > sortedTowers.Count)
            {
                for (int i=sortedTowers.Count;i<buildTowerWidgets.Count;i++)
                {
                    buildTowerWidgets[i].Hide();
                }
            }*/

            for (int i = 0; i < sortedTowers.Count; i++)
            {
                if (buildTowerWidgets.Count<=i)
                {
                    TemplateContainer towerWidgetPrefab = buildTowerPrefab.Instantiate();
                    this.Q().Add(towerWidgetPrefab);
                    BuildTowerWidget buildTowerWidget = towerWidgetPrefab.Q<BuildTowerWidget>("BuildTowerWidget");
                    buildTowerWidget.RegisterCallback<ClickEvent>(OnTowerWidgetClick);
                    buildTowerWidgets.Add(buildTowerWidget);
                }
                buildTowerWidgets[i].Init(sortedTowers[i]);
            }
        }

        public void Reset()
        {
            CashComponent cashComponent = GameServices.Instance.GetCashComponent();
            UpdateTowersAvailability(cashComponent.Cash, false);
        }

        public void Dispose()
        {
            foreach (var buildTowerWidget in buildTowerWidgets)
            {
                buildTowerWidget.Dispose();
                buildTowerWidget.UnregisterCallback<ClickEvent>(OnTowerWidgetClick);
            }

            Messenger<int, bool>.RemoveListener(GameEvents.CashUpdated, UpdateTowersAvailability);
            buildTowerWidgets = null;
        }

        public void Show(Entity dropZone)
        {
            MusicManager.PlaySound2D(SoundKey.Interface_start);
            UpdateTowers();
            Reset();
            this.dropZone = dropZone;
            panelDisplay = DisplayStyle.Flex;
            AddToClassList("Show");
            RemoveFromClassList("Hide");
        }

        public void Hide()
        {
            panelDisplay = DisplayStyle.None;
            dropZone = Entity.Null;
            AddToClassList("Hide");
            RemoveFromClassList("Show");
        }

        private void OnTowerWidgetClick(ClickEvent clk)
        {
            BuildTowerWidget selectedWidget = (BuildTowerWidget)clk.currentTarget;
            if (!selectedWidget.IsAvailable)
            {
                PlaySound2D(SoundKey.Lacking_supplies);
                Messenger<string, float2>.Broadcast(UIEvents.ShowNotification, LocalizationManager.GetTranslation("TowerStats/NoAmmo"), new float2(Screen.width - 650,this.resolvedStyle.height + 50));
                return;
            }

            GameServices.Instance.BuildTower(selectedWidget.Tower, dropZone);
            PlaySound2D(SoundKey.Tower_build);
        }

        private void UpdateTowersAvailability(int cash, bool cashForWave)
        {
            foreach (BuildTowerWidget buildTowerWidget in buildTowerWidgets)
            {
                buildTowerWidget.SetAvailability(buildTowerWidget.Tower.BuildCost <= cash);
            }
        }
    }
}