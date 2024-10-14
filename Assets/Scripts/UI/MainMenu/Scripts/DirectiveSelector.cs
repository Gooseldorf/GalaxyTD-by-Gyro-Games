using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class DirectiveSelector : Selector
    {
        public new class UxmlFactory : UxmlFactory<DirectiveSelector>{}

        private VisualTreeAsset directiveWidgetPrefab;
        private Label titleLabel;
        private Label descLabel;
        private ScrollView descriptionScrollView;
        private ScrollView directivesScroll;
        private VisualElement directivesContainer;
        private DirectiveWidget directiveShopButton;
        private DirectiveWidget removeDirectiveButton;
        private CostDescriptionWidget costDescriptionWidget;
        private List<DirectiveWidget> directiveWidgets = new ();

        private AllEnums.TowerId towerId;
        private WeaponPart currentDirective;
        private List<WeaponPart> totalDirectivesList = new();
        public VisualElement DirectiveShopButton => directiveShopButton;
        
        public event Action<WeaponPart> OnApplyDirective;
        public event Action OnRemoveDirective;

        public void Init(VisualTreeAsset directiveWidgetPrefab)
        {
            this.directiveWidgetPrefab = directiveWidgetPrefab;
            
            titleLabel = this.Q<Label>("TitleLabel");
            descLabel = this.Q<Label>("Desc");
            descriptionScrollView = this.Q<ScrollView>("DescriptionScroll");
            directivesScroll = this.Q<ScrollView>("DirectivesScroll");
            directivesContainer = this.Q<VisualElement>("DirectivesContainer");

            directiveShopButton = this.Q<DirectiveWidget>("DirectivesShopButton");
            directiveShopButton.Init();

            removeDirectiveButton = this.Q<DirectiveWidget>("EmptyDirectiveWidget");
            removeDirectiveButton.Init();
            removeDirectiveButton.RegisterCallback<ClickEvent>(OnRemoveDirectiveClick);
            
            costDescriptionWidget = this.Q<TemplateContainer>("CostDescriptionWidget").Q<CostDescriptionWidget>();
            costDescriptionWidget.Init();
        }

        public void Dispose()
        {
            directiveShopButton.Dispose();
            
            removeDirectiveButton.UnregisterCallback<ClickEvent>(OnRemoveDirectiveClick);
            
            foreach (DirectiveWidget directiveWidget in directiveWidgets)
            {
                directiveWidget.Dispose();
                directiveWidget.UnregisterCallback<ClickEvent>(OnDirectiveClick);
            }
        }

        public void Show(AllEnums.TowerId towerId, WeaponPart selectedDirective)
        {
            this.towerId = towerId;
            SetDirective(selectedDirective);
            UpdateDirectives(selectedDirective);
            style.display = DisplayStyle.Flex;
            
            if (TutorialManager.Instance.HasTutorialToShow(TutorialKeys.Directives) && !TutorialManager.Instance.IsShowingTutorial) 
                Messenger.Broadcast(TutorialKeys.Directives, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void SetDirective(WeaponPart directive)
        {
            currentDirective = directive;
            if (directive != null)
            {
                costDescriptionWidget.SetPart(directive);
                this.Q<VisualElement>("BackgroundIcon").style.backgroundImage = new StyleBackground(directive.Sprite);
            }
            else
            {
                costDescriptionWidget.SetEmpty();
                this.Q<VisualElement>("BackgroundIcon").style.backgroundImage = new StyleBackground(UIHelper.Instance.EmptyDirective);
            }

            AnimateText();
            PlaySound2D(SoundKey.Menu_workshop_directiveSet);
        }

        public void Hide() => style.display = DisplayStyle.None;

        private void UpdateDirectives(WeaponPart directiveInSlot)
        {
            IDictionary<WeaponPart, int> directivesInventory = DataManager.Instance.GameData.Inventory.UnusedDirectives;

            totalDirectivesList = new List<WeaponPart>();
            
            foreach (KeyValuePair<WeaponPart, int> directivePair in directivesInventory)
            {
                if(directivePair.Key.TowerId.HasFlag(towerId))
                    totalDirectivesList.Add(directivePair.Key);
            }
            
            if (directiveInSlot != null && !directivesInventory.ContainsKey(directiveInSlot))
            {
                totalDirectivesList.Add(directiveInSlot);
            }
            
            PopulateWidgets(totalDirectivesList.Count);
            
            totalDirectivesList.Sort(DataManager.Instance.Get<UnlockManager>().WeaponPartComparer);

            for (int i = 0; i < totalDirectivesList.Count; i++)
            {
                directiveWidgets[i].SetDirective(totalDirectivesList[i]);
                directiveWidgets[i].name = $"Selector_{totalDirectivesList[i].SerializedID}";
                if (directivesInventory.ContainsKey(totalDirectivesList[i]))
                {
                    directiveWidgets[i].SetCount(directivesInventory[totalDirectivesList[i]]);
                }
                else
                {
                    directiveWidgets[i].SetCount(0);
                }
                directiveWidgets[i].SetIsNewNotification(DataManager.Instance.GameData.NewItems.Contains(totalDirectivesList[i]));
                directiveWidgets[i].style.display = DisplayStyle.Flex;
            }

            Select(directiveInSlot != null ? directiveWidgets.Find(x => x.Directive == directiveInSlot) : removeDirectiveButton);

            elementsToResolve.Clear();
            foreach (var widget in directiveWidgets)
            {
                if (widget.style.display == DisplayStyle.Flex && widget.layout.y == 0)
                {
                    elementsToResolve.Add(widget);
                    widget.RegisterCallback<GeometryChangedEvent>(OnWidgetsResolve);
                }
            }
            if(elementsToResolve.Count == 0) MoveScrollToLastSelectedWidget();
        }

        private void PopulateWidgets(int directivesCount)
        {
            if (directivesCount > directiveWidgets.Count)
            {
                int missingWidgetsCount = directivesCount - directiveWidgets.Count;
                for (int i = 0; i < missingWidgetsCount; i++)
                {
                    DirectiveWidget newDirectiveWidget = directiveWidgetPrefab.Instantiate().Q<DirectiveWidget>("DirectiveWidget");
                    newDirectiveWidget.Init();
                    newDirectiveWidget.RegisterCallback<ClickEvent>(OnDirectiveClick);
                    directiveWidgets.Add(newDirectiveWidget);
                    directivesContainer.Insert(1,newDirectiveWidget);
                }
            }
            else
            {
                for (int i = directiveWidgets.Count - 1; i >= directivesCount; i--)
                {
                    directiveWidgets[i].style.display = DisplayStyle.None;
                }
            }
        }

        private readonly List<VisualElement> elementsToResolve = new();
        
        private void OnWidgetsResolve(GeometryChangedEvent geom)
        {
            elementsToResolve.Remove((VisualElement)geom.currentTarget);
            if (elementsToResolve.Count == 0)
            {
                MoveScrollToLastSelectedWidget();
            }
        }
        
        private void MoveScrollToLastSelectedWidget()
        {
            DirectiveWidget widget = (DirectiveWidget)LastSelected;
            
            float offset = widget.layout.y;
            MoveScrollTo(new Vector2(0, offset), 0.5f).SetUpdate(true).Play();
        }
        
        private Tweener MoveScrollTo(Vector2 targetOffset, float duration)
        {
            Vector2 currentScrollOffset = directivesScroll.scrollOffset;
            return DOTween.To(() => currentScrollOffset,x => currentScrollOffset = x, targetOffset, duration)
                .OnUpdate(() => directivesScroll.scrollOffset = currentScrollOffset);
        }

        private void OnDirectiveClick(ClickEvent clk)
        {
            SelectableElement target = (SelectableElement)clk.currentTarget;
            Select(target);
            SetDirective(((DirectiveWidget)LastSelected).Directive);
            OnApplyDirective?.Invoke(((DirectiveWidget)LastSelected).Directive);
            DataManager.Instance.GameData.RemoveFromNewItems(((DirectiveWidget)LastSelected).Directive);
            ((DirectiveWidget)LastSelected).SetIsNewNotification(false);
            UpdateDirectives(((DirectiveWidget)LastSelected).Directive);
        }

        private void OnRemoveDirectiveClick(ClickEvent clk)
        {
            OnRemoveDirective?.Invoke();
            Select(removeDirectiveButton);
            costDescriptionWidget.SetEmpty();
            currentDirective = null;
            AnimateText();
        }

        private void AnimateText()
        {
            AnimateTitle();
            AnimateDescription();
        }

        private void AnimateTitle()
        {
            DOTween.Kill(titleLabel);
            string title = currentDirective == null ? LocalizationManager.GetTranslation("WeaponParts/DirectiveSelection") : currentDirective.GetTitle();
            UIHelper.Instance.PlayTypewriter(titleLabel, title);
        }

        private void AnimateDescription()
        {
            DOTween.Kill(descLabel);
            string description = currentDirective == null? LocalizationManager.GetTranslation($"WeaponParts/DirectiveSelection_desc") : currentDirective.GetDescription();
            UIHelper.Instance.PlayTypewriter(descLabel, description, true, descriptionScrollView, 1.5f, true);
        }
    }
}