using DarkTonic.MasterAudio;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CardTD.TestScripts
{
    public sealed class DynamicSoundVariationReName : MonoBehaviour
    {
        [Button]
        private void RenameAll()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                DynamicGroupVariation[] variations = child.GetComponentsInChildren<DynamicGroupVariation>();

#if UNITY_EDITOR && ADDRESSABLES_ENABLED
                
                foreach (DynamicGroupVariation variation in variations)
                    variation.gameObject.name = variation.audioClipAddressable.editorAsset.name;

#endif
            }
        }
    }
}