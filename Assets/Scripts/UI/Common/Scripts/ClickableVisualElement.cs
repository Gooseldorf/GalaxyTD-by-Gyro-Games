using System;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class ClickableVisualElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ClickableVisualElement, UxmlTraits> {}

        public AllEnums.UIState State = AllEnums.UIState.Available;

        public string SoundName = SoundKey.Interface_button;
        private Action onClickAction;
        private Action onEndTransitionAction;

        public virtual void Init()
        {
            RegisterCallback<ClickEvent>(PlaySoundOnClick);
            
            RegisterCallback<ClickEvent>(PlayOnClickAnimation);
            onClickAction = () =>
            {
                this.RemoveFromClassList("inactive");
                this.AddToClassList("active");
            };
            
            this.RegisterCallback<TransitionEndEvent>(OnEndTransition);
            onEndTransitionAction = () =>
            {
                this.RemoveFromClassList("active");
                this.AddToClassList("inactive");
            };
        }

        public virtual void Dispose()
        {
            UnregisterCallback<ClickEvent>(PlaySoundOnClick);
            this.UnregisterCallback<TransitionEndEvent>(OnEndTransition);
            UnregisterCallback<ClickEvent>(PlayOnClickAnimation);
        }

        public virtual void SetState(AllEnums.UIState state)
        {
            State = state;
        }

        public virtual void PlaySoundOnClick(ClickEvent clk)
        {
            if(State != AllEnums.UIState.Locked && State != AllEnums.UIState.Unavailable)
                PlaySound2D(SoundName);
        }

        public void PlayOnClickAnimation(ClickEvent clk)
        {
            if (this.ClassListContains("noAnimation"))
                return;
           
            onClickAction?.Invoke();
        }

        private void OnEndTransition(TransitionEndEvent end)
        {
            onEndTransitionAction?.Invoke();
        }
    }
}