using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Systems.Attakers;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class TowerStatsWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TowerStatsWidget>
        { }

        private Label damageLabel;
        private Label projectilesCountLabel;
        private Label damage;
        private int currentDamage = 0;
        private LineStats fireRate;
        private LineStats knockBack;
        private LineStats penetration;
        private LineStats ricochet;
        private LineStats AOE;
        private LineStats scatter;
        private LineStats reloadTime;
        private LineStats reloadCost;

        private float nextFireRate = 0f;
        private float nextKnockBack = 0f;
        private float nextPenetration = 0f;
        private float nextRicochet = 0f;
        private float nextAoe = 0f;
        private float nextScatter = 0f;
        private float nextReloadTime = 0f;
        private float nextReloadCost = 0f;

        private UIHelper uiHelper;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            damageLabel = this.Q<Label>("DamageLabel");
            projectilesCountLabel = this.Q<Label>("ProjectilesCountLabel");
            damage = this.Q<Label>("Damage");
            fireRate = this.Q<TemplateContainer>("FireRate").Q<LineStats>("LineStat");
            knockBack = this.Q<TemplateContainer>("KnockBack").Q<LineStats>("LineStat");
            penetration = this.Q<TemplateContainer>("Penetration").Q<LineStats>("LineStat");
            ricochet = this.Q<TemplateContainer>("Ricochet").Q<LineStats>("LineStat");
            AOE = this.Q<TemplateContainer>("AOE").Q<LineStats>("LineStat");
            scatter = this.Q<TemplateContainer>("Scatter").Q<LineStats>("LineStat");
            reloadTime = this.Q<TemplateContainer>("ReloadTime").Q<LineStats>("LineStat");
            reloadCost = this.Q<TemplateContainer>("ReloadCost").Q<LineStats>("LineStat");

            StatsWidgetData statsData = uiHelper.StatsWidgetData;
            //TODO: Init with correct values
            fireRate.Init(uiHelper.GetTowerStatSprite("FireRateIcon"), statsData.FirerateRange.x, statsData.FirerateRange.y);
            knockBack.Init(uiHelper.GetTowerStatSprite("KnockBackIcon"), statsData.ForceRange.x, statsData.ForceRange.y);
            penetration.Init(uiHelper.GetTowerStatSprite("PenetrationIcon"), statsData.PenetrationRange.x, statsData.PenetrationRange.y);
            ricochet.Init(uiHelper.GetTowerStatSprite("RicochetIcon"), statsData.RicochetRange.x, statsData.RicochetRange.y);
            AOE.Init(uiHelper.GetTowerStatSprite("AOEIcon"), statsData.AOERange.x, statsData.AOERange.y);
            scatter.Init(uiHelper.GetTowerStatSprite("ScatterIcon"), statsData.AccuracyRange.x, statsData.AccuracyRange.y);
            reloadTime.Init(uiHelper.GetTowerStatSprite("ReloadTimeIcon"), statsData.ReloadSpeedRange.x, statsData.ReloadSpeedRange.y);
            reloadCost.Init(uiHelper.GetTowerStatSprite("ReloadCostIcon"), statsData.ReloadCostRange.x, statsData.ReloadCostRange.y);
            
            reloadCost.style.visibility = DataManager.Instance.GameData.UseBulletCost ? Visibility.Visible : Visibility.Hidden;
        }

        public void UpdateStats(AttackerComponent attackerComponent, IComponentData additionalStats)
        {
            if (attackerComponent.TowerType is AllEnums.TowerId.Shotgun or AllEnums.TowerId.TwinGun)
            {
                projectilesCountLabel.style.display = DisplayStyle.Flex;
                projectilesCountLabel.text = $"{attackerComponent.AttackStats.ShootingStats.ProjectilesPerShot}x";
            }
            else
            {
                projectilesCountLabel.style.display = DisplayStyle.None;
            }

            damage.text = Mathf.RoundToInt(attackerComponent.AttackStats.DamagePerBullet).ToStringBigValue();

            fireRate.UpdateGameStats(attackerComponent.AttackStats.ShootingStats.ShotDelay != 0 ? 1 / attackerComponent.AttackStats.ShootingStats.ShotDelay : 0);
            knockBack.UpdateGameStats(attackerComponent.AttackStats.KnockBackPerBullet);
            reloadTime.UpdateGameStats(attackerComponent.AttackStats.ReloadStats.ReloadTime);
            reloadTime.SetAsSeconds();
            reloadCost.UpdateGameStats(attackerComponent.AttackStats.ReloadStats.ReloadCost, true);

            switch (additionalStats)
            {
                case GunStatsComponent gunStats:
                    penetration.UpdateGameStats(gunStats.PenetrationCount, true);
                    ricochet.UpdateGameStats(gunStats.RicochetCount, true);
                    ToggleGunRocketStats(true);
                    break;
                case RocketStatsComponent rocketStats:
                    AOE.UpdateGameStats(rocketStats.AOE);
                    scatter.UpdateGameStats(rocketStats.ScatterDistance);
                    ToggleGunRocketStats(false);
                    break;
                case MortarStatsComponent mortarStats:
                    AOE.UpdateGameStats(mortarStats.AOE);
                    scatter.UpdateGameStats(mortarStats.ScatterDistance);
                    ToggleGunRocketStats(false);
                    break;
            }

            if (GameServices.Instance.CanTowerUpgrade(attackerComponent, out CompoundUpgrade nextGameUpgrade))
                UpdateNextValues(nextGameUpgrade, attackerComponent, additionalStats);
            else
                HideNextValues();
        }

        private void UpdateNextValues(CompoundUpgrade nextGameUpgrade, AttackerComponent attackerComponent, IComponentData additionalStats)
        {
            foreach (SimpleUpgrade nextUpgrade in nextGameUpgrade.Upgrades)
            {
                nextFireRate += nextUpgrade.Bonus.FirerateChangePercent / 100f;
                nextKnockBack += nextUpgrade.Bonus.KnockBackPerBullet;
                nextReloadTime += nextUpgrade.Bonus.ReloadStats.ReloadTime;
                nextReloadCost = NextReloadCost(nextUpgrade);

                switch (additionalStats)
                {
                    case GunStatsComponent:
                        nextPenetration += nextUpgrade.Bonus is GunStats penetrationStat ? penetrationStat.RicochetStats.PenetrationCount : 0;
                        nextRicochet += nextUpgrade.Bonus is GunStats ricochetStat ? ricochetStat.RicochetStats.RicochetCount : 0;
                        break;
                    case MortarStatsComponent:
                        nextAoe += nextUpgrade.Bonus is MortarStats aoeStat ? aoeStat.AOE : 0;
                        nextScatter += nextUpgrade.Bonus is MortarStats scatterStat ? scatterStat.ScatterDistance : 0;
                        break;
                    case RocketStatsComponent:
                        nextAoe += nextUpgrade.Bonus is RocketStats aoeRocketStat ? aoeRocketStat.AOE : 0;
                        nextScatter += nextUpgrade.Bonus is RocketStats scatterRocketStat ? scatterRocketStat.ScatterDistance : 0;
                        break;
                }
            }

            fireRate.UpdateNextStats(attackerComponent.AttackStats.ShootingStats.ShotDelay != 0 ? 1 / attackerComponent.AttackStats.ShootingStats.ShotDelay : 0, nextFireRate);
            knockBack.UpdateNextStats(attackerComponent.AttackStats.KnockBackPerBullet, nextKnockBack);
            reloadTime.UpdateNextStats(attackerComponent.AttackStats.ReloadStats.ReloadTime, nextReloadTime);
            
            reloadCost.UpdateNextStats(1, nextReloadCost, true);

            switch (additionalStats)
            {
                case GunStatsComponent:
                    penetration.UpdateNextStats(1, nextPenetration, true);
                    ricochet.UpdateNextStats(1, nextRicochet, true);
                    break;
                case MortarStatsComponent mortarStats:
                    AOE.UpdateNextStats(mortarStats.AOE, nextAoe);
                    scatter.UpdateNextStats(mortarStats.ScatterDistance, nextScatter);
                    break;
                case RocketStatsComponent rocketStats:
                    AOE.UpdateNextStats(rocketStats.AOE, nextAoe);
                    scatter.UpdateNextStats(rocketStats.ScatterDistance, nextScatter);
                    break;
            }

            nextFireRate = nextKnockBack = nextReloadCost = nextReloadTime = nextAoe = nextPenetration = nextRicochet = nextScatter = 0f;

            int NextReloadCost(SimpleUpgrade nextUpgrade)
            {
                float nextBulletCost = attackerComponent.AttackStats.ReloadStats.BulletCost * nextGameUpgrade.AmmoCostMult;
                int nextCost = (int)math.max(1, math.round(nextBulletCost * (attackerComponent.AttackStats.ReloadStats.MagazineSize * nextUpgrade.Bonus.ReloadStats.RawMagazineSize)));
                return (nextCost - attackerComponent.AttackStats.ReloadStats.ReloadCost) == 0 ? 0 : (nextCost - attackerComponent.AttackStats.ReloadStats.ReloadCost);
            }
        }

        private void HideNextValues()
        {
            fireRate.HideBonus();
            knockBack.HideBonus();
            reloadTime.HideBonus();
            reloadCost.HideBonus();
            penetration.HideBonus();
            ricochet.HideBonus();
            AOE.HideBonus();
            scatter.HideBonus();
        }

        public void UpdateStats(Tower fullTower, Tower baseTower)
        {
            if (fullTower.TowerId is AllEnums.TowerId.Shotgun or AllEnums.TowerId.TwinGun)
            {
                projectilesCountLabel.style.display = DisplayStyle.Flex;
                projectilesCountLabel.text = $"{fullTower.AttackStats.ShootingStats.ProjectilesPerShot}x";
            }
            else
            {
                projectilesCountLabel.style.display = DisplayStyle.None;
            }

            int nextDamage = Mathf.RoundToInt(fullTower.AttackStats.DamagePerBullet);
            UIHelper.Instance.ChangeBigNumberInLabelTween(damage, currentDamage, nextDamage, 0.7f);
            currentDamage = nextDamage;

            fireRate.UpdateMenuStats(1 / baseTower.AttackStats.ShootingStats.ShotDelay, 1 / fullTower.AttackStats.ShootingStats.ShotDelay - 1 / baseTower.AttackStats.ShootingStats.ShotDelay);
            knockBack.UpdateMenuStats(Mathf.Abs(baseTower.AttackStats.KnockBackPerBullet), fullTower.AttackStats.KnockBackPerBullet - baseTower.AttackStats.KnockBackPerBullet);
            reloadTime.UpdateMenuStats(baseTower.AttackStats.ReloadStats.ReloadTime, fullTower.AttackStats.ReloadStats.ReloadTime - baseTower.AttackStats.ReloadStats.ReloadTime);
            reloadTime.SetAsSeconds();
            reloadCost.UpdateMenuStats(baseTower.AttackStats.ReloadStats.ReloadCost, fullTower.AttackStats.ReloadStats.ReloadCost - baseTower.AttackStats.ReloadStats.ReloadCost, true);

            switch (fullTower.AttackStats)
            {
                case GunStats gunStats:
                    GunStats baseGunStats = (GunStats)baseTower.AttackStats;
                    penetration.UpdateMenuStats(baseGunStats.RicochetStats.PenetrationCount, gunStats.RicochetStats.PenetrationCount - baseGunStats.RicochetStats.PenetrationCount, true);
                    ricochet.UpdateMenuStats(baseGunStats.RicochetStats.RicochetCount, gunStats.RicochetStats.RicochetCount - baseGunStats.RicochetStats.RicochetCount, true);
                    ToggleGunRocketStats(true);
                    break;
                case RocketStats rocketStats:
                    RocketStats baseRocketStats = (RocketStats)baseTower.AttackStats;
                    AOE.UpdateMenuStats(baseRocketStats.AOE, rocketStats.AOE - baseRocketStats.AOE);
                    scatter.UpdateMenuStats(baseRocketStats.ScatterDistance, rocketStats.ScatterDistance - baseRocketStats.ScatterDistance);
                    ToggleGunRocketStats(false);
                    break;
                case MortarStats mortarStats:
                    MortarStats baseMortairStats = (MortarStats)baseTower.AttackStats;
                    AOE.UpdateMenuStats(baseMortairStats.AOE, mortarStats.AOE - baseMortairStats.AOE);
                    scatter.UpdateMenuStats(baseMortairStats.ScatterDistance, mortarStats.ScatterDistance - baseMortairStats.ScatterDistance);
                    ToggleGunRocketStats(false);
                    break;
            }
        }

        private void ToggleGunRocketStats(bool isGunStats)
        {
            penetration.style.display = isGunStats ? DisplayStyle.Flex : DisplayStyle.None;
            ricochet.style.display = isGunStats ? DisplayStyle.Flex : DisplayStyle.None;
            AOE.style.display = isGunStats ? DisplayStyle.None : DisplayStyle.Flex;
            scatter.style.display = isGunStats ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void UpdateLocalization()
        {
            damageLabel.text = LocalizationManager.GetTranslation("TowerStats/Damage").ToUpper() + ":";
            uiHelper.SetLocalizationFont(damageLabel);
            uiHelper.SetLocalizationFont(projectilesCountLabel);
            uiHelper.SetLocalizationFont(damage);
            fireRate.SetStatName(LocalizationManager.GetTranslation("TowerStats/Firerate").ToUpper());
            uiHelper.SetLocalizationFont(fireRate);
            knockBack.SetStatName(LocalizationManager.GetTranslation("TowerStats/KnockBack").ToUpper());
            uiHelper.SetLocalizationFont(knockBack);
            penetration.SetStatName(LocalizationManager.GetTranslation("TowerStats/Penetration").ToUpper());
            uiHelper.SetLocalizationFont(penetration);
            ricochet.SetStatName(LocalizationManager.GetTranslation("TowerStats/Ricochet").ToUpper());
            uiHelper.SetLocalizationFont(ricochet);
            AOE.SetStatName(LocalizationManager.GetTranslation("TowerStats/AOE").ToUpper());
            uiHelper.SetLocalizationFont(AOE);
            scatter.SetStatName(LocalizationManager.GetTranslation("TowerStats/ScatterDistance").ToUpper());
            uiHelper.SetLocalizationFont(scatter);
            reloadTime.SetStatName(LocalizationManager.GetTranslation("TowerStats/ReloadSpeed").ToUpper());
            uiHelper.SetLocalizationFont(reloadTime);
            reloadCost.SetStatName(LocalizationManager.GetTranslation("TowerStats/ReloadCost").ToUpper());
            uiHelper.SetLocalizationFont(reloadCost);
        }
    }
}