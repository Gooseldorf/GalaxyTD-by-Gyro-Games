using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[ShowOdinSerializedPropertiesInInspector]
public class SpawnGroupVisual : ReferencedVisual, ISerializationCallbackReceiver
{
    [SerializeField] private int2 spawnZonePartialOffset;
    
    public int Id;
    
    public SpawnZonePartialVisual[] Zones;

    [OdinSerialize, NonSerialized] public List<Wave> Waves = new ();

    public void Init(SpawnGroup spawnGroup, SimpleEffectManager effectVisualManager)
    {
        Zones = new SpawnZonePartialVisual[spawnGroup.SpawnPositions.Length];

        Waves = spawnGroup.Waves;

        for (int i = 0; i < spawnGroup.SpawnPositions.Length; ++i)
        {
            ReferencedVisual referencedVisual = effectVisualManager.GetSimpleVisual<SpawnZonePartialVisual>();
            if (referencedVisual != null)
            {
                SpawnZonePartialVisual zoneVisual = referencedVisual as SpawnZonePartialVisual;
                zoneVisual.transform.SetParent(transform);
                zoneVisual.InitPosition(spawnGroup.SpawnPositions[i]);
                if (spawnGroup.CombinedZones.Contains(spawnGroup.SpawnPositions[i]))
                {
                    zoneVisual.SetCombinedZoneSprite();
                }
                Zones[i] = zoneVisual;
            }
        }
    }

    public SpawnGroup GetSpawnGroup(int2 gridPosOffset)
    {
        SpawnGroup spawnGroup = new SpawnGroup();
        spawnGroup.SpawnPositions = new GridPosition[Zones.Length];

        for (int i = 0; i < Zones.Length; i++)
        {
            spawnGroup.SpawnPositions[i] = new GridPosition(Zones[i].GridPosition + gridPosOffset + spawnZonePartialOffset, Zones[i].GridSize);
            
            if (Zones[i].IsCombinedZone)
            {
                spawnGroup.CombinedZones.Add(spawnGroup.SpawnPositions[i]);
            }
        }

        //Probably doesn't have to do any cloning here
        spawnGroup.Waves = Waves;
        return spawnGroup;
    }


    #region Serialization you don't have to care about

    [SerializeField, HideInInspector] private SerializationData serializationData;

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        UnitySerializationUtility.DeserializeUnityObject(this, ref this.serializationData);
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        UnitySerializationUtility.SerializeUnityObject(this, ref this.serializationData);
    }

    #endregion
}