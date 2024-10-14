using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

namespace Sounds.Attributes
{
#if UNITY_EDITOR
    public class SoundKeyAttributeDrawer: OdinAttributeDrawer<SoundKeyAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            List<string> namesList = new ();
            namesList.Add(SoundConstants.EmptyKey);
            
            AddSoundKeys(namesList,nameof(MusicManager));
            AddSoundKeys(namesList,"DynamicSoundGroupCreatorMenu");    
            AddSoundKeys(namesList,"DynamicSoundGroupCreatorGame");

            string[] names = namesList.ToArray();
            if (names.Length <= 0)
            {
                return;
            }

            GUIHelper.PushLabelWidth(GUIHelper.BetterLabelWidth);
            
            string name = this.ValueEntry.SmartValue;
            if (string.IsNullOrEmpty(name))
            {
                name = names[0];
            }

            int currentIndex = 0;
            if (Array.Exists(names, it => it == name))
            {
                currentIndex = Array.IndexOf(names, name);
            }
            
            currentIndex = EditorGUILayout.Popup(label, currentIndex, names);
            this.ValueEntry.SmartValue = names[currentIndex];

            GUIHelper.PopLabelWidth();
        }

        private void AddSoundKeys(List<string> namesList,string resourceName)
        {
            GameObject go = Resources.Load<GameObject>(resourceName);

            if (go == null)
            {
                Debug.LogError($"resourceName {resourceName} go is null");
                return;
            }
            
            for (int i = 0; i < go.transform.childCount; i++)
            {
                namesList.Add(go.transform.GetChild(i).name);
            }
        }
    }
#endif
    
    public class SoundConstants
    {
        public const string EmptyKey = "Empty";
    }
    
}

