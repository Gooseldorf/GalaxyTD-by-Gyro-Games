using System;
using UnityEngine;

namespace TestingAgent.Editor.Utils
{
    public static class MissionFactory
    {
        public static Mission Clone(Mission mission)
        {
            Mission clone = ScriptableObject.CreateInstance<Mission>();
            clone.name = $"{mission.name}_Clone";
            clone.CashPerWaveStart = mission.CashPerWaveStart;
            clone.MissionIndex = mission.MissionIndex;
            clone.HpModifier = mission.HpModifier;
            clone.LevelMatrix = mission.LevelMatrix;
            clone.Reward = mission.Reward;
            Array.Copy(mission.SpawnData, clone.SpawnData = new SpawnGroup[mission.SpawnData.Length], mission.SpawnData.Length);
            Array.Copy(mission.DropZones, clone.DropZones = new DropZone[mission.DropZones.Length], mission.DropZones.Length);
            Array.Copy(mission.EnergyCores, clone.EnergyCores = new EnergyCore[mission.EnergyCores.Length], mission.EnergyCores.Length);
            Array.Copy(mission.Obstacles, clone.Obstacles = new IObstacle[mission.Obstacles.Length], mission.Obstacles.Length);
            Array.Copy(mission.ExitPoints, clone.ExitPoints = new ExitPoint[mission.ExitPoints.Length], mission.ExitPoints.Length);
            Array.Copy(mission.Portals, clone.Portals = new Portal[mission.Portals.Length], mission.Portals.Length);
            Array.Copy(mission.Bridges, clone.Bridges = new Bridge[mission.Bridges.Length], mission.Bridges.Length);
            Array.Copy(mission.Gates, clone.Gates = new Gate[mission.Gates.Length], mission.Gates.Length);
            Array.Copy(mission.CritterSpawnPoints, clone.CritterSpawnPoints = new CritterSpawnPoint[mission.CritterSpawnPoints.Length], mission.CritterSpawnPoints.Length);
            return clone;
        }
    }
}