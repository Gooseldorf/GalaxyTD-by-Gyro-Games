using Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data.Managers
{
    public class AdsTester : MonoBehaviour
    {

        [Button]
        private void LoadReward()
        {
            AdsManager.LoadReward(AdsRewardType.Test);
        }

        [Button]
        private void ShowAds()
        {
            AdsManager.TryShowReward(() => { Debug.Log("Start show");}, () => { Debug.Log("Add reward");});
        }
    }
}