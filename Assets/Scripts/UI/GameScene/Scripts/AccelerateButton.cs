using UnityEngine.UIElements;

namespace UI
{
    public class AccelerateButton : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<AccelerateButton> { }

        private VisualElement normalSpeed;
        private VisualElement x2;
        private VisualElement x4;
        
        public override void Init()
        {
            base.Init();
            normalSpeed = this.Q<VisualElement>("Normal");
            x2 = this.Q<VisualElement>("x2");
            x4 = this.Q<VisualElement>("x4");
            
            normalSpeed.style.display = DisplayStyle.Flex;
            x2.style.display = DisplayStyle.None;
            x4.style.display = DisplayStyle.None;
        }

        public void SetSpeed(SpeedState speedState)
        {
            normalSpeed.style.display = DisplayStyle.None;
            x2.style.display = DisplayStyle.None;
            x4.style.display = DisplayStyle.None;

            switch (speedState)
            {
                case SpeedState.Normal:
                    normalSpeed.style.display = DisplayStyle.Flex;
                    break;
                case SpeedState.X2:
                    x2.style.display = DisplayStyle.Flex;
                    break;
                case SpeedState.X4:
                    x4.style.display = DisplayStyle.Flex;
                    break;
            }
        }
    }
    
    public enum SpeedState
    {
        Normal = 1,
        X2 = 2,
        X4 = 4
    }
}
