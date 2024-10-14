using UnityEngine.UIElements;

namespace UI
{
    public class SelectableElement : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<SelectableElement, UxmlTraits>{}

        protected VisualElement frame;

        protected bool selected;

        public bool Selected => selected;

        public override void Init()
        {
            base.Init();
            frame = this.Q<VisualElement>("Frame");
            //SetSelected(false);
        }

        public virtual void SetSelected(bool selected)
        {
            this.selected = selected;
            if(frame == null)
                return;
            frame.style.display = this.selected ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}