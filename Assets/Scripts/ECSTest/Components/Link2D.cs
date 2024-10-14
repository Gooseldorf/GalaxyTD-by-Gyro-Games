using System;
using Unity.Entities;
using UnityEngine;

public class Link2D : IComponentData, ICloneable, IDisposable
{
    public GameObject Companion;

    public Func<GameObject> Get;
    public Action<Link2D> Release;

    public bool IsHide = false;

    public object Clone()
    {
        return new Link2D()
        {
            Companion = this.Get.Invoke(),
            Get = this.Get,
            Release = this.Release,
            IsHide = false,
        };
    }


    public void Hide()
    {
        if(IsHide)
            return;
        IsHide = true;
        Companion.SetActive(false);
    }
    
    public void Dispose()
    {
        Release(this);
    }
}
