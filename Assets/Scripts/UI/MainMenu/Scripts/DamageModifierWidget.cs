using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DamageModifierWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<DamageModifierWidget, UxmlTraits>{}
        
        private VisualElement increaseDamage;
        private Label strongLabel;
        private VisualElement decreaseDamage;
        private Label weakLabel;
        private List<VisualElement> increaseDmgIcons;
        private int increaseDmgCounter;
        private List<VisualElement> decreaseDmgIcons;
        private int decreaseDmgCounter;

        public void Init()
        {
            increaseDamage = this.Q<VisualElement>("IncreaseDamage");
            decreaseDamage = this.Q<VisualElement>("DecreaseDamage");
            strongLabel = increaseDamage.Q<Label>("StrongLabel");
            weakLabel = decreaseDamage.Q<Label>("WeakLabel");
            increaseDmgIcons = increaseDamage.Query<VisualElement>("EnemyTypeIcon").ToList();
            decreaseDmgIcons = decreaseDamage.Query<VisualElement>("EnemyTypeIcon").ToList();
        }

        public void SetPart(WeaponPart part)
        {
            strongLabel.text = $"{LocalizationManager.GetTranslation("Menu/StrongAgainst")}:";
            weakLabel.text = $"{LocalizationManager.GetTranslation("Menu/WeakAgainst")}:";
            
            increaseDmgCounter = 0;
            decreaseDmgCounter = 0;
            for (int i = 0; i < increaseDmgIcons.Count; i++)
            {
                increaseDmgIcons[i].style.display = DisplayStyle.None;
                decreaseDmgIcons[i].style.display = DisplayStyle.None;
            }
            
            foreach (Tag tag in part.Bonuses)
            {
                if (tag is DamageModifiersStatTag dmTag)
                {
                    float bonus = dmTag.GetBonusForUI;
                    if (bonus > 0)
                    {
                        increaseDmgIcons[increaseDmgCounter].style.backgroundImage = new StyleBackground(dmTag.GetTypeIcon);
                        increaseDmgIcons[increaseDmgCounter].style.display = DisplayStyle.Flex;
                        increaseDmgCounter++;
                    }
                    else
                    {
                        decreaseDmgIcons[decreaseDmgCounter].style.backgroundImage = new StyleBackground(dmTag.GetTypeIcon);
                        decreaseDmgIcons[decreaseDmgCounter].style.display = DisplayStyle.Flex;
                        decreaseDmgCounter++;
                    }
                }
            }
        }
    }
}
