using Unity.Entities;
using ECSTest.Components;
using Unity.Mathematics;

public partial struct CashForReloadSystem : ISystem
{
    private const int timeBuffer = 60;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CashComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        int cashForReloadTemp = 0;

        foreach (AttackerComponent attacker in SystemAPI.Query<AttackerComponent>())
        {
            float time = attacker.AttackStats.ReloadStats.RawMagazineSize * attacker.AttackStats.ShootingStats.ShotDelay + attacker.AttackStats.ReloadStats.ReloadTime;
            cashForReloadTemp += (int)math.ceil(timeBuffer / time * attacker.AttackStats.ReloadStats.ReloadCost);
        }

        state.EntityManager.CompleteDependencyBeforeRW<CashComponent>();
        RefRW<CashComponent> cashComponent = SystemAPI.GetSingletonRW<CashComponent>();
        cashComponent.ValueRW.SetCashsToSafeReload(cashForReloadTemp);
    }
}