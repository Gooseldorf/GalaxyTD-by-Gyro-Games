using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TestProjectileShooter : MonoBehaviour
{
    private List<ProjectileVisual> Projectiles = new();
    private ParticleSystem Muzzle;
    #region sub
    public List<ProjectileVisual> SubLightProjectiles;
    public ParticleSystem SubLightMuzzle;
    public List<ProjectileVisual> SubHeavyProjectiles;
    public ParticleSystem SubHeavyMuzzle;
    public List<ProjectileVisual> SubGaussProjectiles;
    public ParticleSystem SubGaussMuzzle;
    public List<ProjectileVisual> SubPlasmaProjectiles;
    public ParticleSystem SubPlasmaMuzzle;
    public void SetLight() => SetNew(SubLightProjectiles, SubLightMuzzle, 0.2f, 100, 100, 3000);
    public void SetHeavy() => SetNew(SubHeavyProjectiles, SubHeavyMuzzle, 0.3f, 50, 1000, 6000);
    public void SetGauss() => SetNew(SubGaussProjectiles, SubGaussMuzzle, .9f, 10, 3000, 7000);
    public void SetPlasma() => SetNew(SubPlasmaProjectiles, SubPlasmaMuzzle, 0.1f, 150, 200, 3000);
    private void SetNew(List<ProjectileVisual> newP, ParticleSystem newM, float f1, int i2, int i3, int i4) 
    { Projectiles = newP; Muzzle = newM; Distance = f1; MuzzleDelay = i2; ShotsDelay = i3; HideDelay = i4; }
    #endregion
    public Transform AttackPoint;
    public float Distance;
    public int MuzzleDelay = 100;
    public int ShotsDelay = 200;
    public int HideDelay = 1000;

    public async void ImitateShoot()
    {
        foreach (var proj in Projectiles)
        {
            if (proj != null)
                Shoot(proj);
            await Task.Delay(ShotsDelay);
        }
    }

    private async void Shoot(ProjectileVisual proj)
    {
        if (!proj.gameObject.activeInHierarchy)
        {
            proj.transform.position = AttackPoint.position;
            proj.transform.right = Vector3.right;
            Muzzle.transform.position = AttackPoint.position;
            Muzzle.gameObject.SetActive(true);
            
            await Task.Delay(MuzzleDelay);
            if (proj != null) proj.gameObject.SetActive(true);
            //StartCoroutine(proj.DelayedActivation());
            
            await Task.Delay(HideDelay);
            if (proj!=null) proj.gameObject.SetActive(false);

        }
    }

    private Vector3 first = new(-1, 2, 0);
    private Vector3 second = new(-1, -2, 0);

    private void Update()
    {
        foreach (var proj in Projectiles)
        {
            if (proj != null && proj.gameObject.activeInHierarchy)
            { 
                proj.transform.localPosition += proj.transform.right * Distance;

                if (proj.transform.position.x > 21.34f && proj.transform.right == Vector3.right)
                {
                    proj.gameObject.transform.right = first;
                }
                else if (proj.transform.position.y > 13.67f)
                {
                    proj.gameObject.transform.right = second;
                }
            }
            else
                ImitateShoot();
        }
    }
}
