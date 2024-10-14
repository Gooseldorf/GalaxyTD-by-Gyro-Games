using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DirectiveWidget : CommonButton
    {
        public new class UxmlFactory : UxmlFactory<DirectiveWidget, UxmlTraits>{}
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Sprite> emptyDirectiveSprite 
                = new UxmlAssetAttributeDescription<Sprite>(){ name = "EmptyDirectiveSprite", defaultValue = null };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
 
                if (emptyDirectiveSprite.TryGetValueFromBag(bag, cc, out Sprite value))
                {
                    ((DirectiveWidget)ve).EmptyDirectiveSprite = value;
                }
            }
        }
        public Sprite EmptyDirectiveSprite { get; set; }

        private WeaponPart directive;
        private ISlot slot;
        private VisualElement lockIcon;
        private VisualElement directiveIcon;
        private VisualElement directiveSelection;
        private Label directiveTitle;
        private Label countLabel;
        private VisualElement isNewNotification;
        
        public WeaponPart Directive => directive;
        public ISlot Slot => slot;

        public override void Init()
        {
            base.Init();
            lockIcon = this.Q<VisualElement>("LockIcon");
            directiveIcon = this.Q<VisualElement>("DirectiveIcon");
            directiveTitle = this.Q<Label>("PartTitle");
            countLabel = this.Q<Label>("CountLabel");
            directiveSelection = this.Q<VisualElement>("DirectiveSelection");
            isNewNotification = this.Q<VisualElement>("IsNewNotification");
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            directiveSelection.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
            Color color = directiveSelection.style.unityBackgroundImageTintColor.value;
            directiveSelection.style.unityBackgroundImageTintColor = State == AllEnums.UIState.Locked ? 
                new StyleColor(new Color(color.r, color.g, color.b, 0.2f)) : new StyleColor(new Color(color.r, color.g, color.b, 1));
        }

        public void SetDirective(WeaponPart directive)
        {
            this.directive = directive;
            directiveIcon.style.backgroundImage = new StyleBackground(directive.Sprite);
            directiveIcon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
            directiveSelection.style.unityBackgroundImageTintColor = new StyleColor(UIHelper.Instance.DirectivesColorData.GetDirectiveColor(directive));
            if(directiveTitle != null) directiveTitle.text = $"DR_{DataManager.Instance.Get<UnlockManager>().GetDirectiveId(directive)}";
        }

        public void SetCount(int count)
        {
            countLabel.style.display = count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            countLabel.text = count.ToString();
        }

        public void SetIsNewNotification(bool isNew) => isNewNotification.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;
        
        public void SetSlot(ISlot slot)
        {
            this.slot = slot;
            directive = slot.WeaponPart;
            if (slot.WeaponPart == null)
                return;
            
            SetDirective(slot.WeaponPart);
        }

        public void SetEmptySlot()
        {
            slot = null;
            directive = null;
            directiveIcon.style.backgroundImage = new StyleBackground(EmptyDirectiveSprite);
            directiveIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(1,1,1, 0.3f));
            directiveSelection.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.5f,0.5f,0.5f,0.3f));
        }

        public void RemoveDirective()
        {
            directiveIcon.style.backgroundImage = new StyleBackground(EmptyDirectiveSprite);
            directiveIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(1,1,1, 0.3f));
            directiveSelection.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.5f,0.5f,0.5f,0.3f));
            if(directiveTitle != null) directiveTitle.text = "DR_0";
            
            directive = null;
        }
        
        public override void SetState(AllEnums.UIState state)
        {
            switch (state)
            {
                case AllEnums.UIState.Locked:
                    directiveIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(1, 1, 1, 0.2f));
                    lockIcon.style.display = DisplayStyle.Flex;
                    style.opacity = 0.7f;
                    State = AllEnums.UIState.Locked;
                    break;
                case AllEnums.UIState.Available:
                    directiveIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(1, 1, 1, 1));
                    lockIcon.style.display = DisplayStyle.None;
                    style.opacity = 1;
                    State = AllEnums.UIState.Available;
                    break;
                case AllEnums.UIState.Active:
                    directiveIcon.style.unityBackgroundImageTintColor = new StyleColor(new Color(1, 1, 1, 1));
                    lockIcon.style.display = DisplayStyle.None;
                    style.opacity = 1;
                    State = AllEnums.UIState.Active;
                    break;
            }
        }
    }
}