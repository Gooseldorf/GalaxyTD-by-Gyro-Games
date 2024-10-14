using DarkTonic.MasterAudio;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace CardTD.TestScripts
{
    [ExecuteAlways]
    public sealed class DynamicSoundVoicesChanger : MonoBehaviour
    {
#if UNITY_EDITOR
        [ShowInInspector]
        private List<SoundVariation> groups;

        private void OnEnable()
        {
            DynamicSoundGroup[] soundGroups = GetComponentsInChildren<DynamicSoundGroup>();
            groups = new List<SoundVariation>(soundGroups.Length);

            foreach (DynamicSoundGroup soundGroup in soundGroups)
            {
                foreach (DynamicGroupVariation variation in soundGroup.groupVariations)
                    groups.Add(new SoundVariation(soundGroup, variation));
            }
        }

        [Button]
        public void SetForAll([PropertyRange(0, 100)] int voices)
        {
            foreach (SoundVariation variation in groups)
                variation.Voices = voices;
        }
        
        [HideReferenceObjectPicker]
        public sealed class SoundVariation
        {
            private readonly DynamicGroupVariation dynamicSoundVariation;
            
            [HideLabel, ShowInInspector]
            public string Label { get; }

            [ShowInInspector, PropertyRange(0, 100)]
            public int Voices
            {
                get => dynamicSoundVariation.weight;
                set
                {
                    if(value == Voices)
                        return;
                    
                    dynamicSoundVariation.weight = value;
                    UnityEditor.EditorUtility.SetDirty(dynamicSoundVariation);
                }
            }
            
            public SoundVariation(DynamicSoundGroup soundGroup, DynamicGroupVariation variation)
            {
                dynamicSoundVariation = variation;
                Label = $"{soundGroup.name} - {variation.name}";
            }
        }
#endif
    }
}