using I2.Loc;
using UnityEngine.UIElements;

namespace UI
{
    public class FireModeWidget : VisualElement
    {
        public new class UxmlFactory: UxmlFactory<FireModeWidget>{}
        
        private SelectableStateElement single;
        private SelectableStateElement burst;
        private SelectableStateElement auto;

        public void Init()
        {
            single = this.Q<SelectableStateElement>("Single");
            single.Init();
            burst = this.Q<SelectableStateElement>("Burst");
            burst.Init();
            auto = this.Q<SelectableStateElement>("Auto");
            auto.Init();
        }

        public void SetPart(WeaponPart part)
        {
            foreach (Tag tag in part.Bonuses)
            {
                if (tag is ReplaceStatsTag replaceStatsTag)
                {
                    if (replaceStatsTag.TryGetAttackPatterns(out AllEnums.AttackPattern patterns))
                    {
                        single.SetSelected(patterns.HasFlag(AllEnums.AttackPattern.Single));
                        burst.SetSelected(patterns.HasFlag(AllEnums.AttackPattern.Burst));
                        auto.SetSelected(patterns.HasFlag(AllEnums.AttackPattern.Auto));
                    }
                }
            }
        }

        public void UpdateLocalization()
        {
            single.Q<Label>().text = LocalizationManager.GetTranslation("TowerStats/Single");
            burst.Q<Label>().text = LocalizationManager.GetTranslation("TowerStats/Burst");
            auto.Q<Label>().text = LocalizationManager.GetTranslation("TowerStats/Auto");
            single.Q<VisualElement>("AvailableVisual").Q<Label>().text = LocalizationManager.GetTranslation("TowerStats/Single");
            burst.Q<VisualElement>("AvailableVisual").Q<Label>().text = LocalizationManager.GetTranslation("TowerStats/Burst");
            auto.Q<VisualElement>("AvailableVisual").Q<Label>().text = LocalizationManager.GetTranslation("TowerStats/Auto");
        }
    }
}