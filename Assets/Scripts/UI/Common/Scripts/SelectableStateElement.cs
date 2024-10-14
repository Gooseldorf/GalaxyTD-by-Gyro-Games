using UnityEngine.UIElements;

namespace UI
{
    public class SelectableStateElement : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<SelectableStateElement, UxmlTraits>{}
        
        private VisualElement availableVisual;
        private VisualElement selectedVisual;
        
        public override void Init()
        {
            base.Init();
            
            availableVisual = this.Q<VisualElement>("AvailableVisual");
            selectedVisual = this.Q<VisualElement>("SelectedVisual");
            
            SetSelected(false);
        }
        
        public void SetSelected(bool selected)
        {
            if (availableVisual == null || selectedVisual == null)
                return;

            availableVisual.style.display = selected? DisplayStyle.None : DisplayStyle.Flex;
            selectedVisual.style.display = selected? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
