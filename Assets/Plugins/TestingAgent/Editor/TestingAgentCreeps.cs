using ECSTest.Components;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace TestingAgent.Editor
{
    public sealed partial class TestingAgent
    {
        private void ReduceCreepCount()
        {
            if (currentTestIteration == 0 || currentTestIteration > maxTestIteration) 
                return;
            
            int newCreepsCount = waves.Entries[currentTestIteration].Count;
            
            using EntityQuery creepQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabled<CreepComponent>()
                .Build(EntityManager);
                
            using NativeArray<Entity> creepEntities = creepQuery.ToEntityArray(Allocator.Temp);
            List<int> indexesToStay = Enumerable.Range(0, newCreepsCount).ToList();

            foreach (int arrayIndex in Enumerable.Range(0, creepEntities.Length).Except(indexesToStay))
                EntityManager.DestroyEntity(creepEntities[arrayIndex]);
        }
        
        private void ModifyCreepHp()
        {
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabled<CreepComponent>()
                .Build(EntityManager);

            using NativeArray<CreepComponent> creepComponents = query.ToComponentDataArray<CreepComponent>(Allocator.Temp);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            float creepHp = waves.Entries[currentTestIteration].OverrideCreepHp
                ? waves.Entries[currentTestIteration].CreepHp * currentHpModifier
                : waves.Stats.MaxHP * currentHpModifier;

            for (int i = 0; i < creepComponents.Length; i++)
            {
                CreepComponent component = creepComponents[i];
                component.MaxHp = component.Hp = creepHp;
                EntityManager.SetComponentData(entities[i], component);
            }
        }
    }
}