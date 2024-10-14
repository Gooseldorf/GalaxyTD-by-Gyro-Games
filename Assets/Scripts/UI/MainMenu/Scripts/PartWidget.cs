using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PartWidget : SelectableElement
    {
        public new class UxmlFactory : UxmlFactory<PartWidget>{}

        private VisualElement partIcon;
        private VisualElement lockIcon;
        private Label partTitle;
        private Label partDesc;
        private VisualElement isNewNotification;
        private WeaponPart part;

        public WeaponPart Part => part;

        public override void Init()
        {
           base.Init();
           partIcon = this.Q<VisualElement>("PartIcon");
           partTitle = this.Q<Label>("PartTitle");
           lockIcon = this.Q<VisualElement>("LockIcon");
           partDesc = this.Q<Label>("PartDesc");
           isNewNotification = this.Q<VisualElement>("IsNewNotification");
        }

        public void SetPart(WeaponPart part)
        {
            this.part = part;
            partIcon.style.backgroundImage = new StyleBackground(part.Sprite);
        }

        public void SetIsNewNotification(bool isNew) => isNewNotification.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;

        public override void SetState(AllEnums.UIState state)
        {
            base.SetState(state);
            switch (state)
            {
                case AllEnums.UIState.Locked:
                    partIcon.style.opacity = new StyleFloat(0.5f);
                    partIcon.style.unityBackgroundImageTintColor = new StyleColor(UIHelper.Instance.LockedGrayTint);
                    if(lockIcon != null) lockIcon.style.display = DisplayStyle.Flex;
                    break;
                case AllEnums.UIState.Available:
                    partIcon.style.opacity = new StyleFloat(1);
                    partIcon.style.unityBackgroundImageTintColor = new StyleColor(UIHelper.Instance.LockedGrayTint);
                    if(lockIcon != null) lockIcon.style.display = DisplayStyle.None;
                    break;
                case AllEnums.UIState.Active:
                    partIcon.style.opacity = new StyleFloat(1);
                    partIcon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    if(lockIcon != null) lockIcon.style.display = DisplayStyle.None;
                    break;
            }
            UpdateLocalization();
        }

        public void UpdateLocalization()
        {
            if(partTitle!=null) partTitle.text = part.GetTitle();
            if (partDesc == null) return;
         
            switch (State)
            {
                case AllEnums.UIState.Locked:
                    partDesc.style.color = Color.gray;
                    partDesc.text = LocalizationManager.GetTranslation("WeaponParts/PartLocked");
                    break;
                case AllEnums.UIState.Available:
                    partDesc.style.color = Color.white;
                    partDesc.text = LocalizationManager.GetTranslation("WeaponParts/PartCanUnlock");
                    break;
                case AllEnums.UIState.Active:
                    partDesc.style.color = Color.white;
                    partDesc.text = LocalizationManager.GetTranslation("WeaponParts/PartCanInsert");
                    break;
            }
        }
    }
}