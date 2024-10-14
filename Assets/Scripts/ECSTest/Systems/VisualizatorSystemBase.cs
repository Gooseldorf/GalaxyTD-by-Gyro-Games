using CardTD.Utilities;
using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Systems
{
    public partial struct VisualizatorSystemBase : ISystem, ISystemStartStop
    {
        private EntityQuery noVisualSpawnQuery;
        private EntityQuery noVisualExitQuery;
        private EntityQuery updateTowersQuery;

        private DefaultNamespace.TextAnimationData textAnimationData;

        public void OnCreate(ref SystemState state)
        {
            noVisualSpawnQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpawnZoneComponent, PositionComponent>()
                .WithAbsent<EnvironmentVisualComponent>()
                .Build(ref state);

            noVisualExitQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ExitPointComponent, PositionComponent>()
                .WithAbsent<EnvironmentVisualComponent>()
                .Build(ref state);

            updateTowersQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TowerUpdateEvent>()
                .Build(ref state);
        }

        public void OnStartRunning(ref SystemState state)
        {
            textAnimationData = GameServices.Instance.RenderDataHolder.TextAnimationData.GetTextAnimationData;
        }

        public void OnStopRunning(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            SimpleEffectManager effectManager = GameServices.Instance.Get<SimpleEffectManager>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            CheckIfNeedAddVisual(effectManager, ecb, ref state);
            CheckPowerState(ref state);
            CheckDropZoneState(ref state, ecb);

            if (!updateTowersQuery.IsEmpty)
            {
                NativeArray<TowerUpdateEvent> towerUpdateEvents = updateTowersQuery.ToComponentDataArray<TowerUpdateEvent>(Allocator.Temp);

                NativeList<Entity> updatedTowers = new(Allocator.Temp);

                foreach (TowerUpdateEvent towerUpdateEvent in towerUpdateEvents)
                {
                    if (towerUpdateEvent.TowerEntity == Entity.Null)
                        continue;

                    if (updatedTowers.Contains(towerUpdateEvent.TowerEntity))
                        continue;

                    AttackerComponent attackComponent = state.EntityManager.GetComponentData<AttackerComponent>(towerUpdateEvent.TowerEntity);

                    if (!towerUpdateEvent.IsTurnedOn)
                        attackComponent.AttackPattern = AllEnums.AttackPattern.Off;
                    else
                    {
                        AllEnums.AttackPattern patern = attackComponent.AttackStats.ShootingStats.GetNextAvailableAttackPattern(attackComponent.AttackPattern);
                        attackComponent.AttackPattern = patern;
                    }

                    state.EntityManager.SetComponentData(towerUpdateEvent.TowerEntity, attackComponent);

                    Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, towerUpdateEvent.TowerEntity, MessengerMode.DONT_REQUIRE_LISTENER);
                    updatedTowers.Add(towerUpdateEvent.TowerEntity);
                }

                updatedTowers.Dispose();
                towerUpdateEvents.Dispose();
                state.EntityManager.DestroyEntity(updateTowersQuery);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void CheckDropZoneState(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach ((DropZoneEvent dropZoneEvent, Entity entity) in SystemAPI.Query<DropZoneEvent>().WithEntityAccess())
            {
                if (state.EntityManager.HasComponent(dropZoneEvent.Entity, typeof(EnvironmentVisualComponent)))
                {
                    var evc = state.EntityManager.GetComponentData<EnvironmentVisualComponent>(dropZoneEvent.Entity);
                    if (evc.EnvironmentVisual is DropZoneVisual dropZone)
                    {
                        DropZoneComponent dropZoneComponent = state.EntityManager.GetComponentData<DropZoneComponent>(dropZoneEvent.Entity);
                        PowerableComponent powerableComponent = state.EntityManager.GetComponentData<PowerableComponent>(dropZoneEvent.Entity);

                        if (!dropZoneComponent.IsPossibleToBuild || dropZoneComponent is {IsOccupied: true, TimeToReactivate: <= 0})
                        {
                            dropZone.Show(false);
                        }
                        else
                        {
                            dropZone.Show(true);


                            if (!dropZoneComponent.IsOccupied && powerableComponent.IsTurnedOn != dropZone.IsPowered)
                            {
                                dropZone.SetPowered(powerableComponent.IsTurnedOn);
                            }
                            else if (dropZoneComponent.IsOccupied)
                            {
                                if (dropZoneComponent.TimeToReactivate > 0)
                                {
                                    if (dropZone.IsPowered)
                                        dropZone.SetPowered(false);
                                    //dropZone.UpdateTimer(Mathf.RoundToInt(dropZoneComponent.TimeToReactivate));
                                    //dropZone.StartTimer();//here to brg
                                    Entity dropZoneTextEntity = ecb.CreateEntity();
                                    ecb.SetName(dropZoneTextEntity, "AnimatedText");
                                    ecb.AddComponent(dropZoneTextEntity,
                                        new AnimatedTextComponent()
                                        {
                                            NonCashValue = (int)dropZoneComponent.TimeToReactivate,
                                            Position = new float2(dropZone.transform.position.x + textAnimationData.DropZoneXStartOffset,
                                                dropZone.transform.position.y + textAnimationData.DropZoneYStartOffset),
                                            Timer = 0,
                                            Color = textAnimationData.DropZoneTextColor,
                                            TextType = AllEnums.TextType.DropZone,
                                            Scale = textAnimationData.DropZoneScale
                                        });
                                }
                            }
                        }
                    }
                }

                state.EntityManager.SetComponentEnabled<DropZoneEvent>(entity, false);
            }
        }

        private void CheckPowerState(ref SystemState state)
        {
            foreach ((ChangePowerEvent changePowerEvent, Entity entity) in SystemAPI.Query<ChangePowerEvent>().WithEntityAccess())
            {
                if (state.EntityManager.HasComponent(changePowerEvent.Entity, typeof(EnvironmentVisualComponent)))
                {
                    var evc = state.EntityManager.GetComponentData<EnvironmentVisualComponent>(changePowerEvent.Entity);
                    if (evc.EnvironmentVisual is IPowerableVisual powerableVisual)
                        if (changePowerEvent.IsTurnedOn != powerableVisual.IsPowered)
                            powerableVisual.SetPowered(changePowerEvent.IsTurnedOn);
                }

                state.EntityManager.SetComponentEnabled<ChangePowerEvent>(entity, false);
            }
        }

        #region Check and add visual if not exist methods

        private void CheckIfNeedAddVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            CheckAddSpawnExitZoneVisual(effectManager, ecb);
            CheckAddDropZoneVisual(effectManager, ecb, ref state);
            CheckAddEnergyCoreVisual(effectManager, ecb, ref state);
            CheckAddGateVisual(effectManager, ecb, ref state);
            CheckAddBridgeVisual(effectManager, ecb, ref state);
            CheckAddPortalVisual(effectManager, ecb, ref state);
            CheckAddConveyorVisual(effectManager, ecb, ref state);
        }

        private void CheckAddConveyorVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            foreach ((ConveyorComponent conveyorComponent, PowerableComponent powerableComponent, GridPositionComponent gridPositionComponent, Entity entity) in SystemAPI
                         .Query<ConveyorComponent, PowerableComponent, GridPositionComponent>()
                         .WithAbsent<EnvironmentVisualComponent>()
                         .WithEntityAccess())
            {
                ConveyorBeltVisual visual = effectManager.GetSimpleVisual<ConveyorBeltVisual>();
                visual.InitVisual(conveyorComponent, gridPositionComponent, powerableComponent);
                AddVisual(ecb, entity, visual);
            }
        }

        private void CheckAddPortalVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            foreach ((PortalComponent portalComponent, PowerableComponent powerableComponent, Entity entity) in SystemAPI
                         .Query<PortalComponent, PowerableComponent>()
                         .WithAbsent<EnvironmentVisualComponent>()
                         .WithEntityAccess())
            {
                PortalVisual visual = effectManager.GetSimpleVisual<PortalVisual>();
                visual.InitVisual(portalComponent, powerableComponent);
                AddVisual(ecb, entity, visual);
            }
        }

        private void CheckAddBridgeVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            foreach ((GridPositionComponent gridPositionComponent, PowerableComponent powerableComponent, Entity entity) in SystemAPI
                         .Query<GridPositionComponent, PowerableComponent>()
                         .WithAbsent<EnvironmentVisualComponent>()
                         .WithAll<BridgeComponent>()
                         .WithEntityAccess())
            {
                BridgeVisual visual = effectManager.GetSimpleVisual<BridgeVisual>();
                visual.InitPosition(gridPositionComponent.Value, powerableComponent.IsTurnedOn);
                AddVisual(ecb, entity, visual);
            }
        }

        private void CheckAddGateVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            foreach ((GridPositionComponent gridPositionComponent, PowerableComponent powerableComponent, Entity entity) in SystemAPI
                         .Query<GridPositionComponent, PowerableComponent>()
                         .WithAbsent<EnvironmentVisualComponent>()
                         .WithAll<GateComponent>()
                         .WithEntityAccess())
            {
                var visual = effectManager.GetSimpleVisual<GateVisual>();
                visual.InitPosition(gridPositionComponent.Value, powerableComponent.IsTurnedOn);
                AddVisual(ecb, entity, visual);
            }
        }

        private void CheckAddEnergyCoreVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            foreach ((PositionComponent positionComponent, EnergyCoreComponent energyCoreComponent, Entity entity) in SystemAPI
                         .Query<PositionComponent, EnergyCoreComponent>()
                         .WithAbsent<EnvironmentVisualComponent>()
                         .WithEntityAccess())
            {
                var visual = effectManager.GetSimpleVisual<EnergyCoreVisual>();
                visual.InitVisual(positionComponent, energyCoreComponent, entity);
                AddVisual(ecb, entity, visual);
            }
        }

        private void CheckAddDropZoneVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb, ref SystemState state)
        {
            foreach ((PositionComponent positionComponent, Entity dropZoneEntity) in SystemAPI
                         .Query<PositionComponent>()
                         .WithAbsent<EnvironmentVisualComponent>()
                         .WithAll<DropZoneComponent>()
                         .WithEntityAccess())
            {
                DropZoneVisual visual = effectManager.GetSimpleVisual<DropZoneVisual>();
                visual.transform.position = positionComponent.Position.ToFloat3();
                AddVisual(ecb, dropZoneEntity, visual);
            }
        }

        private void AddVisual(EntityCommandBuffer ecb, Entity entity, EnvironmentVisual visual)
        {
            var component = new EnvironmentVisualComponent();
            component.AddVisual(visual);
            ecb.AddComponent(entity, component);
        }

        private void CheckAddSpawnExitZoneVisual(SimpleEffectManager effectManager, EntityCommandBuffer ecb)
        {
            int noVisualSpawnQueryCount = noVisualSpawnQuery.CalculateEntityCount();
            int noVisualExitQueryCount = noVisualExitQuery.CalculateEntityCount();

            if (noVisualSpawnQueryCount == 0 && noVisualExitQueryCount == 0)
                return;

            NativeArray<Entity> noVisualSpawnEntities = noVisualSpawnQuery.ToEntityArray(Allocator.Temp);
            NativeArray<PositionComponent> noVisualSpawnPositions = noVisualSpawnQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
            NativeArray<Entity> noVisualExitEntities = noVisualExitQuery.ToEntityArray(Allocator.Temp);
            NativeArray<PositionComponent> noVisualExitPositions = noVisualExitQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);

            for (int i = 0; i < noVisualSpawnEntities.Length; i++)
            {
                bool isCombined = IsCombined(noVisualSpawnPositions[i].Position, ref noVisualExitPositions);
                var visual = effectManager.GetSimpleVisual<SpawnZonePartialVisual>();
                visual.InitSpawnZoneVisual(noVisualSpawnPositions[i].Position, isCombined);
                AddVisual(ecb, noVisualSpawnEntities[i], visual);
            }

            for (int i = 0; i < noVisualExitEntities.Length; i++)
            {
                bool isCombined = IsCombined(noVisualExitPositions[i].Position, ref noVisualSpawnPositions);
                var visual = effectManager.GetSimpleVisual<ExitPointVisual>();
                visual.InitExitPointVisual(noVisualExitPositions[i].Position, isCombined);
                AddVisual(ecb, noVisualExitEntities[i], visual);
            }

            noVisualSpawnEntities.Dispose();
            noVisualSpawnPositions.Dispose();
            noVisualExitEntities.Dispose();
            noVisualExitPositions.Dispose();

            bool IsCombined(float2 positionToCheck, ref NativeArray<PositionComponent> positions)
            {
                foreach (var posComponent in positions)
                {
                    if (posComponent.Position.Equals(positionToCheck))
                        return true;
                }

                return false;
            }
        }

        #endregion
    }
}