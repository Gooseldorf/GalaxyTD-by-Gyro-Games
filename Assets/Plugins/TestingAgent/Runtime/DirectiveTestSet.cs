using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace TestingAgent
{
    [CreateAssetMenu(menuName = "DIR", fileName = "DIR")]
    public sealed class DirectiveTestSet : SerializedScriptableObject
    {
        [FormerlySerializedAs("directives")] [SerializeField] 
        public List<WeaponPart> Directives;

        [SerializeField, ReadOnly] 
        private List<string> dirSequence;
        
        public List<string> DirSequence => dirSequence;
        
        [Button]
        public bool CheckSequence()
        {
            List<string> dirNames = Directives.Select(x => x.name).ToList();

            if (dirNames.Count != dirSequence.Count)
                return false;

            for (int i = 0; i < dirNames.Count; i++)
            {
                if (dirNames[i] != dirSequence[i])
                    return false;
            }

            return true;
        }
        
        [Button]
        public void CreateSequence()
        {
            dirSequence = Directives.Select(x => x.name).ToList();
        }

#if UNITY_EDITOR
        [Button]
        private void GetDirectives()
        {
            string[] guids = AssetDatabase.FindAssets("t:PartsHolder");
            PartsHolder partsHolder = AssetDatabase.LoadAssetAtPath<PartsHolder>(AssetDatabase.GUIDToAssetPath(guids[0]));

            List<WeaponPart> result = new(partsHolder.Directives);
            Directives = result
                .Where(x => x is not CompoundWeaponPart && x.PartType == AllEnums.PartType.Directive)
                .ToList();

            CreateSequence();
        }
#endif
    }
}