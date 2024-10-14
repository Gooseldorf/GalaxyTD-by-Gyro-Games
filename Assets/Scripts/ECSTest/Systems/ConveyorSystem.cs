using ECSTest.Aspects;
using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(MovingSystem))]
    public partial struct ConveyorSystem: ISystem
    {
        private EntityQuery conveyorQuery;
        
        public void OnCreate(ref SystemState state)
        {
            conveyorQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<ConveyorComponent>().WithAll<GridPositionComponent>().WithAll<PowerableComponent>().Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeArray<ConveyorComponent> conveyorComponents = conveyorQuery.ToComponentDataArray<ConveyorComponent>(Allocator.TempJob);
            NativeArray<PowerableComponent> powerableComponents = conveyorQuery.ToComponentDataArray<PowerableComponent>(Allocator.TempJob);
            NativeArray<GridPositionComponent> conveyorPositions = conveyorQuery.ToComponentDataArray<GridPositionComponent>(Allocator.TempJob);
            
            new ConveyorJob()
            {
                ConveyorComponents = conveyorComponents,
                PowerableComponents = powerableComponents,
                ConveyorPositions = conveyorPositions,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    public partial struct ConveyorJob: IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<ConveyorComponent> ConveyorComponents;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<PowerableComponent> PowerableComponents;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<GridPositionComponent> ConveyorPositions;
        public float DeltaTime;
        
        public void Execute(MoveAspect moveAspect)
        {
            float2 creepPosition = moveAspect.PositionComponent.ValueRO.Position;
            int2 creepGreedPosition = new ((int)creepPosition.x, (int)creepPosition.y);

            for (int i = 0; i < ConveyorComponents.Length; i++)
            {
                if(!PowerableComponents[i].IsPowered) continue;
                
                if (creepGreedPosition.x >= ConveyorPositions[i].Value.GridPos.x &&
                    creepGreedPosition.y >= ConveyorPositions[i].Value.GridPos.y &&
                    creepGreedPosition.x < ConveyorPositions[i].Value.GridPos.x + ConveyorPositions[i].Value.GridSize.x &&
                    creepGreedPosition.y < ConveyorPositions[i].Value.GridPos.y + ConveyorPositions[i].Value.GridSize.y)
                {
                    float2 displacement = (float2)ConveyorComponents[i].Direction * ConveyorComponents[i].Speed * DeltaTime;
                    creepPosition += displacement;
                    moveAspect.PositionComponent.ValueRW.Position = creepPosition;
                }
            }
        }
    }
}