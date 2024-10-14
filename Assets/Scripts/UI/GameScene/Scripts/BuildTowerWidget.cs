using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class BuildTowerWidget : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<BuildTowerWidget> {}

        private Tower tower;

        private Label buildPriceLabel;
        private VisualElement towerIcon;
        
        public Tower Tower => tower;
        public bool IsAvailable;
        private bool isActive;
        
        
        public void Init(Tower tower)
        {
            base.Init();
            this.tower = tower;
            this.name = $"BuildTowerWidget_{tower.TowerId.ToString()}";
            
            buildPriceLabel = this.Q<Label>("BuildPrice");
            buildPriceLabel.text = tower.BuildCost.ToString();

            towerIcon = this.Q<VisualElement>("TowerIcon");
            towerIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetTowerSprite(tower.TowerId.ToString()));
        }

        public void SetAvailability(bool isAvailable)
        {
            IsAvailable = isAvailable;
            
            this.Q<VisualElement>("ActiveWidget").style.backgroundImage = new StyleBackground(isAvailable ? UIHelper.Instance.AvailableTowerBuildWidget : UIHelper.Instance.LockedTowerBuildWidget);
            towerIcon.style.unityBackgroundImageTintColor = new StyleColor(isAvailable ? Color.white: Color.gray);
            buildPriceLabel.style.color = new StyleColor(isAvailable ? Color.white : UIHelper.Instance.Red);
        }
    }
}