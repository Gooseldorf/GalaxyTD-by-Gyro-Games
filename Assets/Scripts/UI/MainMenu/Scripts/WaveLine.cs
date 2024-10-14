using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace UI
{
    public class WaveLine : VisualElement
    {
        #region UxmlStaff

        public new class UxmlFactory: UxmlFactory<WaveLine, UxmlTraits>{}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<SpriteAtlas> wavesAtlas = new(){ name = "waves_atlas", defaultValue = null };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (wavesAtlas.TryGetValueFromBag(bag, cc, out SpriteAtlas value1))
                    ((WaveLine)ve).WavesAtlas = value1;
            }
        }
    
        public SpriteAtlas WavesAtlas { get; set; }
        #endregion

        private Label countLabel;
        private VisualElement icon;
        private VisualElement armorType;
        private VisualElement fleshType;

        private int creepCount;

        public void Init()
        {
            countLabel = this.Q<Label>("Count");
            icon = this.Q<VisualElement>("Icon");
            armorType = this.Q<VisualElement>("ArmorType");
            fleshType = this.Q<VisualElement>("FleshType");
        }

        public void SetWave(CreepStats stats, int count)
        {
            creepCount = count;
            this.countLabel.text = count.ToString();
            icon.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetWaveIcon($"{stats.CreepType.ToString()}Icon"));
            armorType.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetWaveIcon($"{stats.ArmorType.ToString()}Icon"));
            fleshType.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetWaveIcon($"{stats.FleshType.ToString()}FleshIcon"));
        }

        public void Show() => style.display = DisplayStyle.Flex;
        public void Hide() => style.display = DisplayStyle.None;

        public void UpdateUnitsCount(int count) => countLabel.text = count >= 0 ? count.ToString() : "0";
        
    }
}

