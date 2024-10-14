// using System;
// using System.Threading.Tasks;
using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.ResourceManagement.AsyncOperations;

public class ScriptableObjSingleton<T> : ScriptableObject where T : ScriptableObject
{
    private static T instance;

    // public static event Action<T> ObjectLoaded;

    public static bool IsLoaded => instance != null;

    public static T Instance
    {
        get
        {
            if (instance)
                return instance;
            instance = Resources.Load<T>(typeof(T).Name);
            return instance;
        }
    }

    

    // public static Task<ScriptableObject> LoadAsset()
    // {
    //     return LoadAsset(typeof(T).Name);
    // }

    // public static Task<ScriptableObject> LoadAsset(object key)
    // {
    //     AsyncOperationHandle<ScriptableObject> handle = Addressables.LoadAssetAsync<ScriptableObject>(key);
    //     handle.Completed += OnObjectLoadDone;
    //
    //     return handle.Task;
    // }

    // public static void Release()
    // {
    //     Addressables.Release(instance);
    //     instance = null;
    // }

    // public static void OnObjectLoadDone(AsyncOperationHandle<ScriptableObject> taskResult)
    // {
    //     if (taskResult.Status != AsyncOperationStatus.Succeeded)
    //         throw new Exception($"Addressable singleton of {typeof(T)} failed to load");
    //
    //     if (instance != null)
    //     {
    //         ObjectLoaded?.Invoke(instance);
    //         return;
    //     }
    //
    //     if (taskResult.Result is T t)
    //         instance = t;
    //
    //     ObjectLoaded?.Invoke(instance);
    // }
}