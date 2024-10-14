using ECSTest.Components;
using ECSTest.Structs;
using I2.Loc;

namespace Systems.Attakers
{
    public class MortarStats : AttackStats
    {
        public float AOE;
        public float ArrivalTime;
        public float ScatterDistance;

        //public override AttackStatsStruct GetStats()
        //{
        //    var stat = base.GetStats();
        //    stat.AOE = AOE;
        //    return stat;
        //}

        public void SumAttackStats(ref MortarStatsComponent mortarStats)
        {
            mortarStats.AOE += AOE;
            mortarStats.ScatterDistance += ScatterDistance;
            mortarStats.ArrivalTime += ArrivalTime;
        }

        public void SubtractAttackStats(ref MortarStatsComponent mortarStats)
        {
            mortarStats.AOE -= AOE;
            mortarStats.ScatterDistance -= ScatterDistance;
            mortarStats.ArrivalTime -= ArrivalTime;
        }

        public void MultiplyAttackStats(ref MortarStatsComponent mortarStats)
        {
            mortarStats.AOE += AOE * mortarStats.AOE;
            mortarStats.ScatterDistance += ScatterDistance * mortarStats.ScatterDistance;
            mortarStats.ArrivalTime += ArrivalTime * mortarStats.ArrivalTime;
        }

        public void DivideAttackStats(ref MortarStatsComponent mortarStats)
        {
            mortarStats.AOE -= AOE * mortarStats.AOE;
            mortarStats.ScatterDistance -= ScatterDistance * mortarStats.ScatterDistance;
            mortarStats.ArrivalTime -= ArrivalTime * mortarStats.ArrivalTime;
        }

        public override void SumAttackStats(AttackStats add)
        {
            base.SumAttackStats(add);
            if (add is not MortarStats stats) return;
            AOE += stats.AOE;
            ArrivalTime += stats.ArrivalTime;
            ScatterDistance += stats.ScatterDistance;
        }

        public override void SubtractAttackStats(AttackStats subtract)
        {
            base.SubtractAttackStats(subtract);

            if (subtract is not MortarStats stats) return;

            AOE -= stats.AOE;
            ArrivalTime -= stats.ArrivalTime;
            ScatterDistance -= stats.ScatterDistance;
        }

        public override void MultiplyAttackStats(AttackStats mult)
        {
            base.MultiplyAttackStats(mult);

            if (mult is not MortarStats stats) return;

            AOE += AOE * stats.AOE;
            ArrivalTime += ArrivalTime * stats.ArrivalTime;
            ScatterDistance += ScatterDistance * stats.ScatterDistance;
        }

        public override void DivideAttackStats(AttackStats divide)
        {
            base.DivideAttackStats(divide);

            if (divide is not MortarStats stats) return;

            AOE -= AOE * stats.AOE;
            ArrivalTime -= ArrivalTime * stats.ArrivalTime;
            ScatterDistance -= ScatterDistance * stats.ScatterDistance;
        }

        public override string GetDescription(bool isPercent, bool showMagazineSizeMult = true, bool popUpUpgradeText = false)
        {
            string result = base.GetDescription(isPercent, showMagazineSizeMult, popUpUpgradeText);

            if (AOE != 0)
            {
                CheckLine(ref result);
                result += InsertValue(AOE, isPercent) + LocalizationManager.GetTranslation("TowerStats/AOE");
            }

            if (ScatterDistance != 0)
            {
                CheckLine(ref result);
                result += InsertValue(ScatterDistance, isPercent) + LocalizationManager.GetTranslation("TowerStats/ScatterDistance");
            }

            if (ArrivalTime != 0)
            {
                CheckLine(ref result);
                result += InsertValue(ArrivalTime, isPercent) + LocalizationManager.GetTranslation("TowerStats/ArrivalTime");
            }

            return result;
        }
    }
}