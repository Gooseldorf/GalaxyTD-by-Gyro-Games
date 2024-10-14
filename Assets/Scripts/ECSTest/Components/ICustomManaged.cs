using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICustomManaged<T> : IDisposable
{
    public T Clone();
    public void Load(T from);
}
