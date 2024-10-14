using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class CostDescriptionWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<CostDescriptionWidget, UxmlTraits> { }

        private VisualElement buildCostContainer;
        private Label buildCostLabel;
        private Label buildCostPlusLabel;
        private VisualElement ammoCostContainer;
        private Label ammoCostLabel;
        private Label ammoCostPlusLabel;

        private int currentBuildCost = 0;
        private int currentAmmoCost = 0;
        private UIHelper uiHelper;

        public void Init()
        {
            buildCostContainer = this.Q<VisualElement>("BuildCost");
            buildCostLabel = buildCostContainer.Q<Label>("BuildCostLabel");
            buildCostPlusLabel = buildCostContainer.Q<Label>("PlusLabel");

            ammoCostContainer = this.Q<VisualElement>("AmmoCost");
            ammoCostLabel = ammoCostContainer.Q<Label>("AmmoCostLabel");
            ammoCostPlusLabel = ammoCostContainer.Q<Label>("PlusLabel");

            uiHelper = UIHelper.Instance;
        }

        public void SetPart(WeaponPart part)
        {
            switch (part.TowerCostIncrease)
            {
                case > 0:
                    buildCostContainer.style.backgroundColor = uiHelper.CostWidgetRed;
                    buildCostPlusLabel.style.visibility = Visibility.Visible;
                    break;
                case 0:
                    buildCostContainer.style.backgroundColor = uiHelper.CostWidgetBlue;
                    break;
                default:
                    buildCostContainer.style.backgroundColor = uiHelper.CostWidgetGreen;
                    buildCostPlusLabel.style.visibility = Visibility.Hidden;
                    break;
            }
            SetBuildCost(Mathf.RoundToInt(part.TowerCostIncrease * 100));

            switch (part.BulletCostIncrease)
            {
                case > 0:
                    ammoCostContainer.style.backgroundColor = uiHelper.CostWidgetRed;
                    ammoCostPlusLabel.style.visibility = Visibility.Visible;
                    break;
                case 0:
                    ammoCostContainer.style.backgroundColor = uiHelper.CostWidgetBlue;
                    break;
                default:
                    ammoCostContainer.style.backgroundColor = uiHelper.CostWidgetGreen;
                    ammoCostPlusLabel.style.visibility = Visibility.Hidden;
                    break;
            }
            SetAmmoCost(Mathf.RoundToInt(part.BulletCostIncrease * 100));
        }

        public void SetEmpty()
        {
            SetBuildCost(0);
            buildCostContainer.style.backgroundColor = uiHelper.CostWidgetBlue;
            SetAmmoCost(0);
            ammoCostContainer.style.backgroundColor = uiHelper.CostWidgetBlue;
        }

        private void SetAmmoCost(int ammoCost)
        {
            uiHelper.ChangeNumberInLabelTween(ammoCostLabel, currentAmmoCost, ammoCost, 0.5f);
            currentAmmoCost = ammoCost;
        }

        private void SetBuildCost(int buildCost)
        {
            uiHelper.ChangeNumberInLabelTween(buildCostLabel, currentBuildCost, buildCost, 0.5f);
            currentBuildCost = buildCost;
        }
    }
}
