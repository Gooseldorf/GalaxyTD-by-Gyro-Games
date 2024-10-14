using DG.Tweening;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TestProjectileShooter1 : MonoBehaviour
{
    public List<ProjectileVisual> Projectiles = new();
    public List<ParticleSystem> Muzzles = new();
    public List<ParticleSystem> Impacts = new();
    public int Index = 0;


    public Transform AttackPoint;
    public float Distance = 5;
    public int MuzzleDelay = 100;
    public int ShotsDelay = 200;
    public int HideDelay = 1000;

    public void ImitateShoot()
    {
        foreach (var item in Projectiles)
        {
            ShootRocket(item);
            //Index = Index < Projectiles.Count-1 ? Index +1: 0;
        }
        Muzzles[0].gameObject.SetActive(true);
        //await Task.Delay(ShotsDelay);
    }
    private async void ShootRocket(ProjectileVisual proj )
    {
        if (!proj.gameObject.activeInHierarchy)
        {
            await Task.Delay(MuzzleDelay);
            //if (proj != null) (proj as MissileProjectileVisual).AA();
            //proj.transform.position = AttackPoint.position + proj.transform.right * Distance *  500;

            await Task.Delay((int)(HideDelay ));

            //f (proj != null) proj.gameObject.SetActive(false);
            //impact.transform.position = proj.transform.position;
            //impact.gameObject.SetActive(true);

            await Task.Delay(1000);

        }
    }
    private async void Muzz(ParticleSystem muzzle)
    {
        muzzle.transform.position = AttackPoint.position;
        muzzle.gameObject.SetActive(true);

        await Task.Delay(MuzzleDelay);
        await Task.Delay((int)(HideDelay));
        
        await Task.Delay(1000);
        muzzle.Stop(true);
    }

    private async void Shoot(ProjectileVisual proj, ParticleSystem muzzle, ParticleSystem impact)
    {
        if (!proj.gameObject.activeInHierarchy)
        {
            float rand = Random.Range(1.2f,2f);

            //proj.transform.position = AttackPoint.position;
            //proj.transform.right = Vector3.right;
            muzzle.transform.position = AttackPoint.position;
            muzzle.gameObject.SetActive(true);
            
            await Task.Delay(MuzzleDelay);
            if (proj != null) proj.gameObject.SetActive(true);
            //proj.transform.DOLocalMoveY(1f  /* * Random.Range(-1, 2)*/, rand /2).SetLoops(2, LoopType.Yoyo); ;
            //StartCoroutine(proj.DelayedActivation());
            //proj.ScaleInAir(1f * rand, 1);
            proj.transform.position = AttackPoint.position + proj.transform.right * Distance * rand * 500;
            await Task.Delay((int)(HideDelay * rand));
            if (proj!=null) proj.gameObject.SetActive(false);
            impact.transform.position = proj.transform.position;
            impact.gameObject.SetActive(true);
            await Task.Delay(1000);
            muzzle.Stop(true);

        }
    }

    //private void Update()
    //{
    //    foreach (var proj in Projectiles)
    //    {
    //        if (proj != null && proj.gameObject.activeInHierarchy)
    //        {
    //            proj.transform.localPosition += proj.transform.right * Distance;
    //        }
    //    }
    //}
}
