using UnityEngine.UIElements;

namespace UI
{
    public class Selector : VisualElement
    {
        public SelectableElement LastSelected;

        protected virtual void Select(SelectableElement selectable)
        {
            LastSelected?.SetSelected(false);
            selectable?.SetSelected(true);
            LastSelected = selectable;
        }
    }
}