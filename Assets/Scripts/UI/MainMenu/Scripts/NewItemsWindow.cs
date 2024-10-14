using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UI
{
    public class NewItemsWindow : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<NewItemsWindow>{}

        private VisualElement background;
        private CommonButton confirmButton;
        private Label windowTitle;

        private VisualElement content;
        private VisualElement towerContainer;
        private VisualElement towerIcon;
        private VisualElement partContainer;
        private VisualElement partIcon;
        private VisualElement partTowerIcon;
        private VisualElement directiveIcon;
        private List<VisualElement> purchasedDirectives;
        
        private List<TowerFactory> newFactories = new ();
        private List<WeaponPart> newItems = new ();
        private int newItemsTotalCount;
        private UIHelper uiHelper;
        public bool IsShowing;
        
        public void Init()
        {
            background = this.parent;
            
            windowTitle = this.Q<Label>("WindowTitle");
            content = this.Q<VisualElement>("ContentContainer");
            towerContainer = this.Q<VisualElement>("TowerContainer");
            towerIcon = towerContainer.Q<VisualElement>("TowerIcon");
            partContainer = this.Q<VisualElement>("PartContainer");
            partIcon = this.Q<VisualElement>("PartIcon");
            partTowerIcon = partContainer.Q<VisualElement>("PartTowerIcon");
            directiveIcon = this.Q<VisualElement>("MainDirectiveIcon");
            purchasedDirectives = this.Query<VisualElement>("DirectiveIcon").ToList();
            
            confirmButton = this.Q<TemplateContainer>("ConfirmButton").Q<CommonButton>();
            confirmButton.Init();
            confirmButton.SetText(LocalizationManager.GetTranslation("OK"));
            confirmButton.RegisterCallback<ClickEvent>(OnConfirmClick);
            
            style.display = DisplayStyle.None;
            background.style.display = DisplayStyle.None;

            uiHelper = UIHelper.Instance;
        }

        public void ShowNewItems()
        {
            IsShowing = true;
            GameData gameData = DataManager.Instance.GameData;
            
            if (gameData.NewUnlockedFactories.Count > 0)
            {
                List<TowerFactory> factories = new ((IEnumerable<TowerFactory>)DataManager.Instance.GameData.TowerFactories);
                foreach (int towerId in gameData.NewUnlockedFactories)
                {
                    newFactories.Add(factories.Find(x=>x.TowerId == (AllEnums.TowerId)towerId));
                }
            }
            newItems = new(gameData.NewUnlockedItems);
            newItems.RemoveAll(item => item.PartType == AllEnums.PartType.Ammo);
            
            newItemsTotalCount = newFactories.Count + newItems.Count;
      
            Show();
            ShowNextItem();
        }

        public void ShowPurchasedDirectives(List<WeaponPart> directives)
        {
            this.Q<VisualElement>("DirectivesContainer").style.display = DisplayStyle.Flex;
            directiveIcon.style.display = DisplayStyle.None;
            towerContainer.style.display = DisplayStyle.None;
            partContainer.style.display = DisplayStyle.None;
            //this.Q<VisualElement>("DirectivesContainer").style.display = DisplayStyle.None;
            for (int i = 0; i < purchasedDirectives.Count; i++)
            {
                if (i < directives.Count)
                {
                    purchasedDirectives[i].style.backgroundImage = new StyleBackground(directives[i].Sprite);
                    purchasedDirectives[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    purchasedDirectives[i].style.display = DisplayStyle.None;
                }
            }

            Show();
            DOVirtual.DelayedCall(0.3f, () =>
            {
                uiHelper.PlayTypewriter(windowTitle, LocalizationManager.GetTranslation("Menu/NewDirectivesAvailable"));
            });
        }

        private void ShowNextItem()
        {
            WeaponPart item = newItems.Find(x => x.PartType == AllEnums.PartType.Directive);
            if (item != null)
            {
                ShowDirective(item, LocalizationManager.GetTranslation("ConfirmWindow/NewDirectiveUnlocked"));
                newItems.Remove(item);
            }
            else if (newFactories.Count > 0)
            {
                ShowFactory(newFactories[0]);
                newFactories.RemoveAt(0);
            }
            else if (newItems.Count > 0)
            {
                ShowPart(newItems[0]);
                newItems.RemoveAt(0);
            }

            newItemsTotalCount--;
        }
        
        public void ShowDirective(WeaponPart directive, string title)
        {
            this.Q<VisualElement>("DirectivesContainer").style.display = DisplayStyle.Flex;
            foreach (var purchasedDirective in purchasedDirectives)
            {
                purchasedDirective.style.display = DisplayStyle.None;
            }

            directiveIcon.style.backgroundImage = new StyleBackground(directive.Sprite);
            this.Q<VisualElement>("DirectivesContainer").style.display = DisplayStyle.None;
            DOTween.Kill(windowTitle);
            uiHelper.PlayTypewriter(windowTitle, title);
        }

        private bool newTower = false;
        private AllEnums.TowerId towerId;
        private void ShowFactory(TowerFactory factory)
        {
            towerId = factory.TowerId;
            newTower = true;
            
            directiveIcon.style.display = DisplayStyle.None;
            this.Q<VisualElement>("DirectivesContainer").style.display = DisplayStyle.None;
            towerContainer.style.display = DisplayStyle.Flex;
            
            towerIcon.style.backgroundImage = new StyleBackground(factory.TowerPrototype.Sprite);
            DOTween.Kill(windowTitle);
            uiHelper.PlayTypewriter(windowTitle, LocalizationManager.GetTranslation("ConfirmWindow/NewTowerUnlocked"));
        }

        private void ShowPart(WeaponPart part)
        {
            directiveIcon.style.display = DisplayStyle.None;
            towerContainer.style.display = DisplayStyle.None;
            this.Q<VisualElement>("DirectivesContainer").style.display = DisplayStyle.None;
            partContainer.style.display = DisplayStyle.Flex;

            partIcon.style.backgroundImage = new StyleBackground(part.Sprite);
            partTowerIcon.style.backgroundImage = new StyleBackground(uiHelper.GetTowerSprite(part.TowerId.ToString()));
            DOTween.Kill(windowTitle);
            uiHelper.PlayTypewriter(windowTitle, LocalizationManager.GetTranslation("ConfirmWindow/NewPartUnlocked"));
        }

        public void Show()
        {
            style.display = DisplayStyle.Flex;
            background.style.display = DisplayStyle.Flex;

            Tween animation = UIHelper.Instance.GetShowWindowTween(this, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void Hide()
        {
            DataManager.Instance.GameData.ClearNewUnlockedItems();
            
            Tween animation = UIHelper.Instance.GetHideWindowTween(this, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
            IsShowing = false;
        }
        
        private void OnConfirmClick(ClickEvent clk)
        {
            if(newTower)
                TutorialManager.Instance.ShowIsolatedTutorial($"{TutorialKeys.NewTower}_{towerId}");
            newTower = false;
            
            if (newItemsTotalCount == 0) 
                Hide();
            else
                ShowTransition();
        }

        private void ShowTransition()
        {
            Sequence transitionSeq = DOTween.Sequence();

            transitionSeq.Append(uiHelper.GetMenuPanelFadeTween(content, false, true));
            transitionSeq.Append(DOVirtual.DelayedCall(0, ShowNextItem));
            transitionSeq.Append(uiHelper.GetMenuPanelFadeTween(content, true, true));
        }
    }
}