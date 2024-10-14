using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using System.Collections.Generic;
using Tags;
using UI;
using UnityEditor;
using UnityEngine;

public class StringReferenceResolver : ScriptableObjSingleton<StringReferenceResolver>, IExternalStringReferenceResolver
{
    public IExternalStringReferenceResolver NextResolver { get; set; }

    public List<TowerPrototype> TowerPrototypes;
    public List<MenuUpgrade> MenuUpgrades;
    public List<WeaponPart> WeaponParts;

    private Dictionary<string, ICustomSerialized> allObjects;

    public bool CanReference(object value, out string id)
    {
        InitAllObjectsList();
        if (value is ICustomSerialized customSerialized && allObjects.ContainsValue(customSerialized))
        {
            id = customSerialized.SerializedID;
            return true;
        }

        id = string.Empty;
        return false;
    }



    private void InitAllObjectsList()
    {
        if (allObjects == null || allObjects.Count == 0)
        {
            allObjects = new Dictionary<string, ICustomSerialized>();
            AddListToDictionary(TowerPrototypes);
            AddListToDictionary(MenuUpgrades);
            AddListToDictionary(WeaponParts);
        }
    }

    private void AddListToDictionary(IEnumerable<ICustomSerialized> customSerializedList)
    {
        foreach (var serializedObj in customSerializedList)
        {
            if (!allObjects.ContainsKey(serializedObj.SerializedID))
                allObjects.Add(serializedObj.SerializedID, serializedObj);
            else Debug.LogError($"Duplicate id {serializedObj.SerializedID}");
        }
    }

#if UNITY_EDITOR
    [Button]
    private void AutoInitAllObjects()
    {
        TowerPrototypes = new List<TowerPrototype>();
        MenuUpgrades = new List<MenuUpgrade>();
        WeaponParts = new List<WeaponPart>();
        AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/LevelsScriptableObjects" }).ForEach(guid =>
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj is TowerPrototype towerPrototype) TowerPrototypes.Add(towerPrototype);
            else if (obj is MenuUpgrade menuUpgrade) MenuUpgrades.Add(menuUpgrade);
            else if (obj is WeaponPart weaponPart) WeaponParts.Add(weaponPart);
        });

        InitAllObjectsList();
        AssetDatabase.SaveAssets();
    }
    
    [Button]
    private void FillWeaponPartsFromPartsHolder()
    {
        PartsHolder holder = DataManager.Instance.Get<PartsHolder>();
    
        WeaponParts.Clear();
        WeaponParts = new List<WeaponPart>();
        WeaponParts.AddRange(holder.Items);
        WeaponParts.AddRange(holder.Directives);
        
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif



    //[Button]
    //private void UpdateAllObjectsList()
    //{
    //    foreach (var reference in allObjects)
    //        if (reference == null) allObjects.Remove(reference);

    //    foreach (var towerPrototype in TowerPrototypes)
    //        if (!allObjects.Contains(towerPrototype)) allObjects.Add(towerPrototype);

    //    foreach (var weaponPart in WeaponParts)
    //        if (!allObjects.Contains(weaponPart)) allObjects.Add(weaponPart);

    //    foreach (var weaponPart in WeaponParts)
    //        if (!allObjects.Contains(weaponPart)) allObjects.Add(weaponPart);
    //}

    public bool TryResolveReference(string id, out object value)
    {
        InitAllObjectsList();
        if (allObjects.TryGetValue(id, out ICustomSerialized customSerialized))
        {
            value = customSerialized;
            return true;
        }
        value = null; return false;
    }

    //[Button]
    //private void FillMenuUpgradesFromUpgradeProvider()
    //{
    //    UpgradeProvider upgradeProvider = DataManager.Instance.Get<UpgradeProvider>();

    //    foreach (var towerUpgrades in upgradeProvider.TowerUpgradesList)
    //    {
    //        if (!MenuUpgrades.Contains(towerUpgrades.InfinityUpgrade))
    //        {
    //            MenuUpgrades.Add(towerUpgrades.InfinityUpgrade);
    //        }

    //        foreach (var menuUpgrade in towerUpgrades.Upgrades)
    //        {
    //            if (!MenuUpgrades.Contains(menuUpgrade))
    //            {
    //                MenuUpgrades.Add(menuUpgrade);
    //            }
    //        }
    //    }
    //}
}
