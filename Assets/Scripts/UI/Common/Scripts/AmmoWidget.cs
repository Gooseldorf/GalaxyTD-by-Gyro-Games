using DG.Tweening;
using I2.Loc;
using Sounds.Attributes;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class AmmoWidget : SelectableElement
    {
        public new class UxmlFactory : UxmlFactory<AmmoWidget, UxmlTraits>
        {
        }

        private VisualElement ammoType;
        private VisualElement ammoTypeContainer;
        private VisualElement isNewNotification;
        private VisualElement reloadActiveButton;
        private VisualElement reloadActiveIcon;
        private VisualElement reloadInactiveButton;
        private VisualElement ammoProgressBar;
        private Label fireModeText;
        private VisualElement fireModeIcon;
        private Label partTitle;
        private Label ammoCountLabel;
        private Label magazineSizeLabel;
        private int currentAmmoCount = 0;
        private ISlot slot;
        private WeaponPart ammo;
        private PriceButton reloadButton;

        private Tweener rotationTweener;
        private bool isReloading = false;
        private bool isOff = false;
        private int magazineSize;
        public ISlot Slot => slot;
        public WeaponPart Ammo => ammo;

        public override void Init()
        {
            base.Init();
            partTitle = this.Q<Label>("PartTitle");
            ammoType = this.Q<VisualElement>("AmmoType");
            ammoTypeContainer = this.Q<VisualElement>("AmmoTypeContainer");
            ammoProgressBar = this.Q<VisualElement>("AmmoProgressBar");
            fireModeText = this.Q<Label>("FireModeText");
            fireModeIcon = this.Q<VisualElement>("FireModeIcon");
            ammoCountLabel = this.Q<Label>("AmmoCount");
            magazineSizeLabel = this.Q<Label>("MagazineSize");
            isNewNotification = this.Q<VisualElement>("IsNewNotification");

            reloadActiveButton = this.Q<VisualElement>("ReloadActive");
            reloadActiveIcon = this.Q<VisualElement>("ReloadActiveIcon");

            reloadButton = this.Q<PriceButton>("ReloadButton");
            if (reloadButton != null) reloadButton.Init();

            reloadInactiveButton = this.Q<VisualElement>("ReloadInactive");
        }

        public void SetReloadButtonState(AllEnums.UIState state)
        {
            reloadButton.style.display = DisplayStyle.None;
            reloadInactiveButton.style.display = DisplayStyle.None;
            reloadActiveButton.style.display = DisplayStyle.None;

            switch (state)
            {
                case AllEnums.UIState.Locked:
                    reloadInactiveButton.style.display = DisplayStyle.Flex;
                    reloadButton.SoundName = SoundKey.Lacking_supplies;
                    break;
                case AllEnums.UIState.Available:
                    reloadButton.style.display = DisplayStyle.Flex;
                    reloadButton.SoundName = SoundConstants.EmptyKey;
                    break;
                case AllEnums.UIState.Active:
                    reloadActiveButton.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        public void SetFireModeButtonState(AllEnums.AttackPattern ammoMode)
        {
            switch (ammoMode)
            {
                case AllEnums.AttackPattern.Off:
                    fireModeIcon.style.backgroundImage =
                        new StyleBackground(UIHelper.Instance.GetFireModeSprite("ModeOffIcon"));
                    fireModeText.text = LocalizationManager.GetTranslation("TowerStats/Off");
                    break;
                case AllEnums.AttackPattern.Auto:
                    fireModeIcon.style.backgroundImage =
                        new StyleBackground(UIHelper.Instance.GetFireModeSprite("ModeAutoIcon"));
                    fireModeText.text = LocalizationManager.GetTranslation("TowerStats/Auto");
                    break;
                case AllEnums.AttackPattern.Burst:
                    fireModeIcon.style.backgroundImage =
                        new StyleBackground(UIHelper.Instance.GetFireModeSprite("ModeBurstIcon"));
                    fireModeText.text = LocalizationManager.GetTranslation("TowerStats/Burst");
                    break;
                case AllEnums.AttackPattern.Single:
                    fireModeIcon.style.backgroundImage =
                        new StyleBackground(UIHelper.Instance.GetFireModeSprite("ModeSingleIcon"));
                    fireModeText.text = LocalizationManager.GetTranslation("TowerStats/Single");
                    break;
            }
        }

        public void PlayOnOffSound(bool isOffMode)
        {
            if (isOff && !isOffMode)
            {
                PlaySound2D(SoundKey.Tower_on);
                isOff = false;
            }
            else
            {
                PlaySound2D(SoundKey.Tower_off);
                isOff = true;
            }
        }

        public void SetSlot(ISlot slot) => this.slot = slot;

        public void SetIsNewNotification(bool isNew) => isNewNotification.style.display = isNew ? DisplayStyle.Flex : DisplayStyle.None;

        public void SetAmmo(WeaponPart part, int magazineSize)
        {
            if (part == null)
            {
                Debug.LogWarning("ammo part is null");
                return;
            }
            ammo = part;
            ammoType.style.backgroundImage = new StyleBackground(ammo.Sprite);
            ammoTypeContainer.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetAmmoBg(ammo));
            if (slot != null)
            {
                UIHelper.Instance.ChangeNumberInLabelTween(ammoCountLabel, currentAmmoCount, magazineSize, 0.5f);
                currentAmmoCount = magazineSize;
                ammoCountLabel.text = $"{magazineSize}";
            }
            else
            {
                ammoCountLabel.text = $"{magazineSize}";
                magazineSizeLabel.text = $"/{magazineSize}";
                ammoCountLabel.parent.schedule.Execute(() =>
                {
                    Vector2 size = ammoCountLabel.MeasureTextSize($"{magazineSize}/{magazineSize}", 0, MeasureMode.Undefined, ammoCountLabel.resolvedStyle.height, MeasureMode.Exactly);
                    ammoCountLabel.parent.style.width = size.x + ammoCountLabel.parent.style.paddingLeft.value.value * 2;
                }).StartingIn(20);
            }
        }

        public void SetAmmoCount(int ammo, int magazineSize)
        {
            ammoProgressBar.style.width = Length.Percent((float)ammo / magazineSize * 100);
            this.magazineSize = magazineSize;
            ammoCountLabel.text = $"{ammo.ToString()}";
            magazineSizeLabel.text = $"/{magazineSize}";
        }

        public void ShowReload(float currentReloadTime, float reloadDuration)
        {
            if (currentReloadTime <= 0)
            {
                DOTween.Kill(this);
                return;
            }
            
            isReloading = true;
            DOTween.Kill(this, true);

            if (reloadActiveButton == null || reloadActiveIcon == null)
                return;
            
            SetReloadButtonState(AllEnums.UIState.Active);
            
            DOTween.To(() => worldTransform.rotation.eulerAngles, x => reloadActiveIcon.transform.rotation = Quaternion.Euler(x), new Vector3(0f, 0f, 360f), 1)
                .SetEase(Ease.Linear).SetLoops(-1).SetTarget(this);

            float startWidthPercent = (reloadDuration - currentReloadTime) / reloadDuration * 100;
            UIHelper.Instance.ChangeWidthByPercent(ammoProgressBar, startWidthPercent, 100, reloadDuration).SetTarget(this).OnComplete(() =>
            {
                DOTween.Kill(this, true);
                isReloading = false;
            });
            UIHelper.Instance.ChangeNumberInLabelTween(ammoCountLabel, 0, magazineSize, reloadDuration).SetTarget(this);
        }

        public void UpdateLocalization()
        {
            if (partTitle != null)
            {
                partTitle.text = LocalizationManager.GetTranslation("WeaponParts/Ammo");
            }

            if (reloadButton != null)
            {
                reloadButton.Q<Label>().text = LocalizationManager.GetTranslation("GameScene/Reload");
            }
        }
        public override void PlaySoundOnClick(ClickEvent clk) { }
    }
}