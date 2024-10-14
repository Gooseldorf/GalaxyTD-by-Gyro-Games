using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Data.Managers
{
    public class MobileAdsEventExecutor : MonoBehaviour
    {
        public static MobileAdsEventExecutor Link;

        private Action currentAction;

        public static bool IsActive = false;
        
        public static void Initialize()
        {
            if (Link != null)
                return;
            GameObject obj = new("MobileAdsMainThreadExecuter") {hideFlags = HideFlags.HideAndDontSave};
            Link = obj.AddComponent<MobileAdsEventExecutor>();
            DontDestroyOnLoad(obj);
        }

        public static void SetAction(Action action)
        {
            Link.currentAction = action;
        }

        public void Update()
        {
            if(!IsActive)
                return;
            
            if (currentAction != null)
                currentAction.Invoke();

            currentAction = null;
            IsActive = false;
        }
        
    }
}