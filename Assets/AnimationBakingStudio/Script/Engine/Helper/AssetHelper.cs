#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ABS
{
	public static class AssetHelper
    {
        public static T FindAsset<T>(string folderName, string assetName) where T : class
        {
#if UNITY_EDITOR
            string[] assetGuids = AssetDatabase.FindAssets(assetName);
            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(EngineConstants.PROJECT_PATH_NAME + "/" + folderName + "/"))
                {
                    if (AssetDatabase.LoadAssetAtPath(path, typeof(T)) is T foundAsset)
                        return foundAsset;
                }
            }
#endif

            return default;
        }
    }
}
