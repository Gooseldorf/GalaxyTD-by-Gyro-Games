#if ADDRESSABLES_ENABLED
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

public class AddressablePool<T> : List<T> where T : MonoBehaviour
{
    public Func<T, bool> IsUsed;
    public AssetReference AssetReference;
    public Action<T, bool> Activate;
    public Action<T> AfterCreateAction;

    public AddressablePool(Func<T, bool> isUsed, Action<T, bool> activate, AssetReference assetReference,
        Action<T> afterCreateAction = null)
    {
        IsUsed = isUsed;
        Activate = activate;
        AssetReference = assetReference;
        AfterCreateAction = afterCreateAction;
    }

    public async Task<T> TryGetFree()
    {
        T result = this.Find(x => !IsUsed(x));

        if (result == null)
        {
//#if UNITY_EDITOR
//            if (!Application.isPlaying) {

//                Debug.LogError("Editor Create");
//                string path = AssetDatabase.GUIDToAssetPath(AssetReference.RuntimeKey.ToString());
//                result = AssetDatabase.LoadAssetAtPath<T>(path);
//                AfterCreateAction?.Invoke(result);
//            }
//#endif
            try
            {
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(AssetReference);
                if (handle.Status != AsyncOperationStatus.Succeeded)
                    await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    result = handle.Result.GetComponent<T>();

                    if (result == null)
                    {
                        Debug.LogError(
                            $"[{nameof(AddressablePool<T>)}] {nameof(TryGetFree)}: Component type of <b>{typeof(T).Name}</b> is null for {handle.Result.name}!");
                        return null;
                    }

                    AfterCreateAction?.Invoke(result);
                }
                else
                    return null;

                this.Add(result);
            }
            catch (Exception e)
            {
                Debug.Log($"ex: {e}");
            }
        }

        Activate?.Invoke(result, true);
        return result;
    }

    public void Release(T item)
    {
        if (item != null)
            Addressables.ReleaseInstance(item.gameObject);
    }

    public void ReleaseAll()
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            Release(this[i]);
        }

        this.Clear();
    }
}
#endif