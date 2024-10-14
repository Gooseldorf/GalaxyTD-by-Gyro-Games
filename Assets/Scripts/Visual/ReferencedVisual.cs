using Sounds.Attributes;
using Unity.Mathematics;
using UnityEngine;
#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;
#endif

public class ReferencedVisual : MonoBehaviour
{
#if ADDRESSABLES_ENABLED
    [ValidateInput("@AssetReference.RuntimeKeyIsValid()", "Click on button to set reference")]
    public AssetReference AssetReference;
#endif

    [SoundKey, SerializeField] protected string sound = string.Empty;

    public virtual void InitPosition(float3 position)
    {
        transform.position = position;
    }

    /// <summary>Activate visual</summary>
    public virtual void Enable()
    {
    }

    /// <summary>Stop playing visual</summary>
    public virtual void Disable()
    {
    }

    protected void TryPlaySound()
    {
        TryPlaySound(transform);
    }

    protected void TryPlaySound(Transform location)
    {
        MusicManager.Play3DSoundOnTransform(sound, location);
    }

#if UNITY_EDITOR
#if ADDRESSABLES_ENABLED
    [Button]
    private void SetReference()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);
        //EditorLogger.Log(path);
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
        //EditorLogger.Log(guid);
        AssetReference = new AssetReference(guid);
    }
    #endif
#endif
}
