using I2.Loc;
using UnityEngine.UIElements;

namespace UI
{
    public class SlotWidget: SelectableElement
    {
        public new class UxmlFactory: UxmlFactory<SlotWidget>{}

        private VisualElement partIcon;
        private Label slotTitle;
        private VisualElement isNewNotification;

        private ISlot slot;

        public ISlot Slot => slot;

        public override void Init()
        {
            base.Init();
            partIcon = this.Q<VisualElement>("PartIcon");
            slotTitle = this.Q<Label>("SlotTitle");
            isNewNotification = this.Q<VisualElement>("IsNewNotification");
        }

        public void SetSlot(ISlot slot)
        {
            this.slot = slot;
            SetWeaponPart(slot.WeaponPart);
            UpdateLocalization();
        }

        public void SetWeaponPart(WeaponPart part)
        {
            if(part!=null)
                partIcon.style.backgroundImage = new StyleBackground(part.Sprite);
        }
        
        public void SetIsNewNotification(bool isNew) => isNewNotification.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;

        public void UpdateLocalization()
        {
            if(slot == null || slotTitle == null) return;
            switch (slot.PartType)
            {
                case AllEnums.PartType.Barrel:
                    slotTitle.text = LocalizationManager.GetTranslation("WeaponParts/Barrel");
                    break;
                case AllEnums.PartType.Magazine:
                    slotTitle.text = LocalizationManager.GetTranslation("WeaponParts/Magazine");
                    break;
                case AllEnums.PartType.RecoilSystem:
                    slotTitle.text = LocalizationManager.GetTranslation("WeaponParts/RecoilSystem");
                    break;
            }
        }

        public override void PlaySoundOnClick(ClickEvent clk) { }
    }
}