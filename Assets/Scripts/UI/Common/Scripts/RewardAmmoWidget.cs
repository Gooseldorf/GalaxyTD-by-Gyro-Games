using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class RewardAmmoWidget : SelectableElement
    {
        public new class UxmlFactory : UxmlFactory<RewardAmmoWidget> { }

        private VisualElement icon;
        private VisualElement ammoBg;

        private Color inactiveColor = new Color32(0x00, 0xb2, 0xe2, 0xff);

        public void Init(WeaponPart ammo)
        {
            base.Init();
            icon = this.Q<VisualElement>("Icon");
            ammoBg = this.Q<VisualElement>("IconBg");
            icon.style.backgroundImage = new StyleBackground(ammo.Sprite);
            ammoBg.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetAmmoBg(ammo));
            SetSelected(false);
        }

        public override void SetSelected(bool selected)
        {
            this.selected = selected;
            foreach (VisualElement corner in frame.Children())
            {
                corner.style.borderBottomColor =
                    corner.style.borderLeftColor =
                    corner.style.borderRightColor =
                    corner.style.borderTopColor = selected ? Color.yellow : inactiveColor;
            }
            icon.style.unityBackgroundImageTintColor = new StyleColor(selected ? Color.white : Color.gray);
            ammoBg.style.unityBackgroundImageTintColor = new StyleColor(selected ? Color.white : Color.gray);
        }

    }
}
