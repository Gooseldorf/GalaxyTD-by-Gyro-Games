using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RemoveEventSystem : ISystem
    {
        private EntityQuery cashQuery;
        private EntityQuery waveQuery;
        private EntityQuery spawnQuery;
        private EntityQuery cellQuery;
        private EntityQuery reloadQuery;
        private EntityQuery bubbleQuery;
        private EntityQuery warningQuery;

        private EntityQuery collisionQuery;
        private EntityQuery collisionObstaclesQuery;
        private EntityQuery aoeCollisionQuery;
        private EntityQuery wallKnockbackCollisionQuery;
        //private EntityQuery shootQuery;
        private EntityQuery aoeEffectQuery;
        private EntityQuery secondChanceQuery;
        private EntityQuery evolveEventQuery;
        private EntityQuery changePowersQuery;
        private EntityQuery dropZoneEventsQuery;

        public void OnCreate(ref SystemState state)
        {
            cashQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<CashUpdatedEvent>().Build(ref state);
            waveQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<NextWaveEvent>().Build(ref state);
            spawnQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<SpawnEvent>().Build(ref state);
            cellQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PowerCellEvent>().Build(ref state);
            reloadQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<ReloadEvent>().Build(ref state);
            bubbleQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<BubbleEvent>().Build(ref state);
            warningQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<ProximityWarningEvent>().Build(ref state);

            collisionObstaclesQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<CollisionObstacleEvent>().Build(ref state);
            collisionQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<GunCollisionEvent>().Build(ref state);
            aoeCollisionQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<AOECollisionEvent>().Build(ref state);
            //shootQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<ShootEvent>().Build(ref state);
            wallKnockbackCollisionQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<KnockBackWallDamageEvent>().Build(ref state);
            aoeEffectQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<TagEffectEvent>().Build(ref state);
            secondChanceQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<SecondChanceEvent>().Build(ref state);
            evolveEventQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<EvolveEvent>().Build(ref state);
            changePowersQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<ChangePowerEvent>().Build(ref state);
            dropZoneEventsQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<DropZoneEvent>().Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!cashQuery.IsEmpty)
                state.EntityManager.DestroyEntity(cashQuery);
            if (!waveQuery.IsEmpty)
                state.EntityManager.DestroyEntity(waveQuery);
            if (!spawnQuery.IsEmpty)
                state.EntityManager.DestroyEntity(spawnQuery);
            if (!cellQuery.IsEmpty)
                state.EntityManager.DestroyEntity(cellQuery);
            if (!reloadQuery.IsEmpty)
                state.EntityManager.DestroyEntity(reloadQuery);
            if (!bubbleQuery.IsEmpty)
                state.EntityManager.DestroyEntity(bubbleQuery);
            if (!warningQuery.IsEmpty)
                state.EntityManager.DestroyEntity(warningQuery);
            if (!collisionQuery.IsEmpty)
                state.EntityManager.DestroyEntity(collisionQuery);
            if (!aoeCollisionQuery.IsEmpty)
                state.EntityManager.DestroyEntity(aoeCollisionQuery);
            if (!collisionObstaclesQuery.IsEmpty)
                state.EntityManager.DestroyEntity(collisionObstaclesQuery);
            if (!wallKnockbackCollisionQuery.IsEmpty)
                state.EntityManager.DestroyEntity(wallKnockbackCollisionQuery);
            //if (!shootQuery.IsEmpty)
            //    state.EntityManager.DestroyEntity(shootQuery);
            if (!aoeEffectQuery.IsEmpty)
                state.EntityManager.DestroyEntity(aoeEffectQuery);
            if (!secondChanceQuery.IsEmpty)
                state.EntityManager.DestroyEntity(secondChanceQuery);
            if (!evolveEventQuery.IsEmpty)
                state.EntityManager.DestroyEntity(evolveEventQuery);
            if (!changePowersQuery.IsEmpty)
                state.EntityManager.DestroyEntity(changePowersQuery);
            if(!dropZoneEventsQuery.IsEmpty)
                state.EntityManager.DestroyEntity(dropZoneEventsQuery);
        }
    }
}