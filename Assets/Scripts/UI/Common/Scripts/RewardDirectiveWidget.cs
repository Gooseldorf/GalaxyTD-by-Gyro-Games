using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class RewardDirectiveWidget : SelectableElement
    {
        public new class UxmlFactory : UxmlFactory<RewardDirectiveWidget> { }

        private VisualElement selection;
        private VisualElement icon;
        private Label directiveId;

        public void Init(WeaponPart directive)
        {
            base.Init();
            icon = this.Q<VisualElement>("Icon");
            icon.style.backgroundImage = new StyleBackground(directive.Sprite);

            selection = this.Q<VisualElement>("DirectiveSelection");
            selection.style.unityBackgroundImageTintColor = new StyleColor(UIHelper.Instance.DirectivesColorData.GetDirectiveColor(directive));

            directiveId = this.Q<Label>("DirectiveId");
            directiveId.text = $"DR_{DataManager.Instance.Get<UnlockManager>().GetDirectiveId(directive)}";
            
            SetSelected(false);
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            selection.style.display = this.selected ? DisplayStyle.Flex : DisplayStyle.None;
            icon.style.unityBackgroundImageTintColor = new StyleColor(Color.gray);
        }
    }
}
