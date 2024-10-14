using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class WorkshopPanel : VisualElement, IMenuPanel
    {
        public new class UxmlFactory : UxmlFactory<WorkshopPanel> { }

        private TowerCustomizationPanel towerCustomizationPanel;
        private Label titleLabel;
        private VisualElement factoryWidgetContainer;
        private List<WorkshopFactoryWidget> factoryWidgets;

        private UnlockManager unlockManager;
        private List<ITowerFactory> sortedTowerFactories;

        private ScrollView factoriesScroll;
        private Vector2 cachedScrollOffset = Vector2.zero;

        public event Action OnFactoryWidgetClick;

        public void Init(TowerCustomizationPanel towerCustomizationPanel, VisualTreeAsset factoryWidgetPrefab)
        {
            this.towerCustomizationPanel = towerCustomizationPanel;

            titleLabel = this.Q<Label>("TitleLabel");

            factoryWidgetContainer = this.Q<VisualElement>("FactoryWidgetsContainer");
            factoriesScroll = this.Q<ScrollView>();
            factoryWidgets = new();
            unlockManager = DataManager.Instance.Get<UnlockManager>();

            sortedTowerFactories = new(DataManager.Instance.GameData.TowerFactories);
            sortedTowerFactories.Sort(DataManager.Instance.Get<UnlockManager>().TowerFactoryComparer);

            for (int i = 0; i < sortedTowerFactories.Count; i++)
            {
                WorkshopFactoryWidget newFactoryWidget = factoryWidgetPrefab.Instantiate().Q<WorkshopFactoryWidget>("WorkshopFactoryWidget");
                newFactoryWidget.Init();
                newFactoryWidget.name = $"WorkshopFactoryWidget_{sortedTowerFactories[i].TowerId}";

                if (!unlockManager.IsTowerUnlocked(sortedTowerFactories[i].TowerId) && i != 0)
                {
                    newFactoryWidget.SetLocked();
                }
                else
                {
                    newFactoryWidget.SetTower((TowerFactory)sortedTowerFactories[i]);
                    newFactoryWidget.UpdateIsNewNotifications();
                    newFactoryWidget.RegisterCallback<ClickEvent>(OnFactoryClick);
                }

                factoryWidgets.Add(newFactoryWidget);
                factoryWidgetContainer.Add(newFactoryWidget);
            }

            style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            foreach (WorkshopFactoryWidget factoryWidget in factoryWidgets)
            {
                factoryWidget.Dispose();
                factoryWidget.UnregisterCallback<ClickEvent>(OnFactoryClick);
            }
        }

        public void UpdateFactoryWidgets()
        {
            for (int i = 0; i < sortedTowerFactories.Count; i++)
            {
                factoryWidgets[i].SetTower((TowerFactory)sortedTowerFactories[i]);
                factoryWidgets[i].UpdateIsNewNotifications();
            }
        }

        public void RestoreScrollOffset() => this.RegisterCallback<GeometryChangedEvent>(RestoreOffsetOnResolve);
        private void RestoreOffsetOnResolve(GeometryChangedEvent geom)
        {
            factoriesScroll.scrollOffset = cachedScrollOffset;
            this.UnregisterCallback<GeometryChangedEvent>(RestoreOffsetOnResolve);
        }

        public void SaveScrollOffset() => cachedScrollOffset = factoriesScroll.scrollOffset; 

        private void OnFactoryClick(ClickEvent clk)
        {
            WorkshopFactoryWidget target = (WorkshopFactoryWidget)clk.currentTarget;
            towerCustomizationPanel.SetFactory(target.Factory);
            DataManager.Instance.GameData.RemoveFromNewItems(target.Factory.TowerId);
            OnFactoryWidgetClick?.Invoke();
        }

        public void SetFactoryWidgetOnScrollStart(AllEnums.TowerId towerId)
        {
            int offset = sortedTowerFactories.FindIndex(x => x.TowerId == towerId);
            cachedScrollOffset = new Vector2(offset * 506, 0); //506 is WorkshopFactoryWidget width. It's quite difficult to get resolved width for tutorial purposes, so magic number
            RestoreScrollOffset();
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("Menu/WorkshopButton");
            foreach (WorkshopFactoryWidget factoryWidget in factoryWidgets)
            {
                factoryWidget.UpdateLocalization();
                UIHelper.Instance.SetLocalizationFont(factoryWidget);
            }
            towerCustomizationPanel.UpdateLocalization();
        }
    }
}