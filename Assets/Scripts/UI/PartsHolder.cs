using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UI
{
    public class PartsHolder : SerializedScriptableObject
    {
        public List<WeaponPart> Items;
        public List<WeaponPart> Directives;

#if UNITY_EDITOR
        [Button]
        public void RefillWeaponParts()
        {
            Items.Clear();
            Directives.Clear();

            string[] itemsDirectories = Directory.GetDirectories("Assets/LevelsScriptableObjects/WeaponParts", "*",
                SearchOption.AllDirectories).Where(path => !path.Contains("Directives")).ToArray();

            string[] directiveDirectories = Directory.GetDirectories("Assets/LevelsScriptableObjects/WeaponParts/Directives", "*",
                SearchOption.AllDirectories).ToArray();

            LoadAssets(Items, itemsDirectories);
            LoadAssets(Directives, directiveDirectories);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private static void LoadAssets(ICollection<WeaponPart> list, string[] directories)
        {
            foreach (string directory in directories)
            {
                string[] assets = AssetDatabase.FindAssets("t:WeaponPart", new[] { directory });
                foreach (string asset in assets)
                {
                    string path = AssetDatabase.GUIDToAssetPath(asset);
                    WeaponPart weaponPart = AssetDatabase.LoadAssetAtPath<WeaponPart>(path);
                    if (weaponPart != null)
                    {
                        list.Add(weaponPart);
                    }
                }
            }
        }
        [SerializeField, TextArea] private string parseArea;
        [OdinSerialize, NonSerialized, ShowInInspector] private Dictionary<WeaponPart, int> parsedCosts;

        [Button]
        private void ParseWeaponPartCosts()
        {
            //IReadOnlyDictionary<WeaponPart, int> unlockDict = DataManager.Instance.Get<UnlockManager>().WeaponPartUnlockDictionary;

            //string[] data = parseArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            //parsedCosts.Clear();

            //for (int i = 0; i < data.Length; i++)
            //{
            //    WeaponPart part = unlockDict.FirstOrDefault(x => x.Value == i + 14).Key;
            //    if (part != null)
            //    {
            //        if (int.TryParse(data[i], out int cost))
            //        {
            //            parsedCosts.Add(part, cost);
            //        }
            //    }
            //}

            Debug.LogError("this button doesn't work, Ivan broke it");
        }

        [Button]
        private void SetWeaponCosts()
        {
            foreach (var pair in parsedCosts)
            {
                EditorUtility.SetDirty(pair.Key);
                pair.Key.ScrapCost = pair.Value;
            }
            AssetDatabase.SaveAssets();
        }

        [Button]
        private void SetDirectivesCosts()
        {
            UnlockManager unlockManager = DataManager.Instance.Get<UnlockManager>();
            foreach (var directive in Directives)
            {
                EditorUtility.SetDirty(directive);
                directive.HardCost = unlockManager.GetPartUnlockMission(directive) switch
                {
                    <= 30 => 30,
                    <= 60 => 60,
                    _ => 100
                };
            }
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
