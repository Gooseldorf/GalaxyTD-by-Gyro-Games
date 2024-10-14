#if UNITY_EDITOR

using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AllEnums;

public class ArmorTypeHpModifier : SerializedScriptableObject
{
    [SerializeField] private List<Mission> missionsToChange;

    [SerializeField] public bool IsSingleType;
    [OdinSerialize, HideIf("@IsSingleType")]
    private Dictionary<ArmorType, float> hpModifiersDict = new Dictionary<ArmorType, float>();

    [OdinSerialize, ShowIf("@IsSingleType")]
    public Dictionary<Pair<FleshType, ArmorType>, float> HpModifiersSingleTypeDictianary = new Dictionary<Pair<FleshType, ArmorType>, float>(){
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Bio, Value2 = ArmorType.Unarmored}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Bio, Value2 = ArmorType.Light}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Bio, Value2 = ArmorType.Heavy}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Mech, Value2 = ArmorType.Unarmored}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Mech, Value2 = ArmorType.Light}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Mech, Value2 = ArmorType.Heavy}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Energy, Value2 = ArmorType.Unarmored}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Energy, Value2 = ArmorType.Light}, 1 },
        { new Pair<FleshType, ArmorType>(){Value1 = FleshType.Energy, Value2 = ArmorType.Heavy}, 1 } };

    [Button]
    public void UpdatePrototypesAndUpgradeCosts()
    {

        foreach (var mission in missionsToChange)
        {
            EditorUtility.SetDirty(mission);

            foreach (var spawnGroup in mission.SpawnData)
            {
                foreach (Wave wave in spawnGroup.Waves)
                {
                    if (IsSingleType)
                    {
                        float hpModifier = 0;
                        foreach (Pair<FleshType, ArmorType> key in HpModifiersSingleTypeDictianary.Keys)
                        {
                            if (key.Value1 == mission.CreepStatsPerWave[wave.WaveNum].FleshType && key.Value2 == mission.CreepStatsPerWave[wave.WaveNum].ArmorType)
                                HpModifiersSingleTypeDictianary.TryGetValue(key, out hpModifier);
                        }

                        if (hpModifier != 0)
                            wave.CreepHp = (int)(wave.CreepHp * hpModifier);
                        else
                        {
                            Debug.LogError($"mission {mission.MissionIndex + 1} creeps hp modified error");
                            return;
                        }
                    }
                    else
                    {
                        hpModifiersDict.TryGetValue(mission.CreepStatsPerWave[wave.WaveNum].ArmorType, out float hpModifier);
                        wave.CreepHp = (int)(wave.CreepHp * hpModifier);
                    }
                }
            }
            Debug.Log($"mission {mission.MissionIndex + 1} creeps hp modified correctly");
        }
        AssetDatabase.SaveAssets();
    }

    [System.Serializable]
    public class Pair<T1, T2>
    {
        [HorizontalGroup, HideLabel]
        public T1 Value1;
        [HorizontalGroup, HideLabel]
        public T2 Value2;
    }

    [InfoBox("value count as multiplier - if you want add 20% you should put 1.2")]
    [Button]
    private void ChangeLastWaveHp(float modifier)
    {
        int maxWaveNum;
        foreach (var mission in missionsToChange)
        {
            EditorUtility.SetDirty(mission);
            maxWaveNum = mission.WavesCount - 1;
            foreach (var spawnGroup in mission.SpawnData)
            {
                Wave wave = spawnGroup.Waves.Find(x => x.WaveNum == maxWaveNum);
                if(wave != null)
                {
                    wave.CreepHp = (int)(wave.CreepHp * modifier);
                }
            }
            Debug.Log($"mission {mission.MissionIndex + 1} last wave hp modified correctly");
        }
        AssetDatabase.SaveAssets();
    }
}
#endif