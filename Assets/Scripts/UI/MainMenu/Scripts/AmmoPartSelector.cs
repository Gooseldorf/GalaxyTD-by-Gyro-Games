using CardTD.Utilities;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class AmmoPartSelector : PartSelector
    {
        public new class UxmlFactory : UxmlFactory<AmmoPartSelector, UxmlTraits>{}

        private TowerFactory factory;
        private CostDescriptionWidget costDescriptionWidget;
        private DamageModifierWidget damageModifierWidget;

        public void SetFactory(TowerFactory factory) => this.factory = factory;

        public override void Init(VisualTreeAsset partWidgetPrefab, ShopPanel shopPanel, ConfirmWindow confirmWindow)
        {
            base.Init(partWidgetPrefab, shopPanel, confirmWindow);
            costDescriptionWidget = this.Q<TemplateContainer>("CostDescriptionWidget").Q<CostDescriptionWidget>();
            costDescriptionWidget.Init();
            damageModifierWidget = this.Q<TemplateContainer>("DamageModifierWidget").Q<DamageModifierWidget>();
            damageModifierWidget.Init();
        }

        public override void Show(AllEnums.TowerId towerId, WeaponPart selectedPart)
        {
            base.Show(towerId, selectedPart);
            if(TutorialManager.Instance.HasCurrentTutorial(TutorialKeys.NewAmmo)&& !TutorialManager.Instance.IsShowingTutorial) 
                Messenger<AllEnums.TowerId>.Broadcast(TutorialKeys.NewAmmo, towerId, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        protected override void SetPart(PartWidget partWidget)
        {
            base.SetPart(partWidget);
            damageModifierWidget.SetPart(partWidget.Part);
            PlaySound2D(SoundKey.Menu_workshop_ammoSet);
        }

        protected override List<WeaponPart> FindWeaponParts(AllEnums.TowerId towerId, AllEnums.PartType partType)
        {
            //(x.TowerId & towerId) != 0 is a bit mask to find that at least one flag in x.TowerId matches towerId
            List<WeaponPart> result = DataManager.Instance.GameData.Inventory.UnusedAmmoParts.FindAll(x => (x.TowerId & towerId) != 0 && x.PartType == partType);
            if (factory.Ammo.WeaponPart != null)
                result.Add(factory.Ammo.WeaponPart);
            
            return result;
        }

        protected override void AdjustUiState(List<PartWidget> partWidgets)
        {
            for (int i = 0; i < partWidgets.Count; i++)
            {
                partWidgets[i].SetPart(currentParts[i]);
                partWidgets[i].name = $"AmmoWidget_{currentParts[i].SerializedID}";
                partWidgets[i].SetIsNewNotification(DataManager.Instance.GameData.NewItems.Contains(currentParts[i]));
                partWidgets[i].SetState(AllEnums.UIState.Active);
                partWidgets[i].style.display = DisplayStyle.Flex;
            }
        }
    }
}