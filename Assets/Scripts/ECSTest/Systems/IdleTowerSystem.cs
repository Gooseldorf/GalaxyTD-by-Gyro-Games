using CardTD.Utilities;
using ECSTest.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(MovingSystem))]
    [UpdateAfter(typeof(TargetingSystemBase))]
    public partial struct IdleTower : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new IdleTowerJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();
        }

        [BurstCompile(CompileSynchronously = true)]
        public partial struct IdleTowerJob : IJobEntity
        {
            public const float IdleStartTime = 3f;

            public float DeltaTime;

            private void Execute(ref AttackerComponent attackerComponent, ref PositionComponent towerPosition, in PowerableComponent power,in DestroyComponent destroyComponent)
            {
                if(destroyComponent.IsNeedToDestroy)
                    return;
                
                if (!power.IsTurnedOn)
                {
                    attackerComponent.IdleTimer = 0;
                    return;
                }

                bool hasTarget = attackerComponent.Target != Entity.Null;
                bool isIdleReady = attackerComponent.ReloadTimer == 0 
                    //&& attackerComponent.WindUpTimer == 0
                    && (attackerComponent.ShootTimer == 0 || attackerComponent.Bullets <= 0)
                    && attackerComponent.IdleTimer > (IdleStartTime * attackerComponent.IdleRandomizer);

                if ((!hasTarget || attackerComponent.Bullets <= 0) && isIdleReady)
                {
                    var targetPatrolPosition = towerPosition.Position + towerPosition.Direction.GetNormal();
                    float rotationSpeed = CalculateRandomSpeed(ref attackerComponent);
                    if (rotationSpeed != 0)
                        TargetingSystemBase.RotateToTarget(targetPatrolPosition, DeltaTime, ref towerPosition, rotationSpeed);
                }

                float CalculateRandomSpeed(ref AttackerComponent attackerComponent)
                {
                    float randomizer = attackerComponent.IdleRandomizer;
                    // -1 0 1 0
                    int randSpeedDir = (((int)(attackerComponent.IdleTimer / randomizer + 2*randomizer) % 4) - 2) % 2;
                    // .5 .25
                    float randSpeedMultiplayer = ((int)attackerComponent.IdleTimer % 2 + 1) / 4.0f;

                    return attackerComponent.AttackStats.AimingStats.RotationSpeed * randSpeedMultiplayer * randSpeedDir;
                }
            }
        }
    }
}