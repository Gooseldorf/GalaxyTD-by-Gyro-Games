using System.Collections.Generic;
using UnityEngine;

namespace CardTD.UIAndVisual.Visualization
{
    public class HpBar : MonoBehaviour
    {
        [SerializeField]
        private GameObject fgRoot;
        [SerializeField]
        private SpriteRenderer bg;
        [SerializeField]
        private SpriteRenderer splitterSample;
        [SerializeField]
        private Transform splitterRoot;
        [SerializeField]
        private Transform statusRoot; 
        [SerializeField]
        private Transform pointerRoot;

        private const float oneBarSplitterLength = 25;
        private const int maxSplitterCount = 11;
        private const int maxActiveStatuses = 4;

        private Vector3 forward, up, localScale;
        private List<GameObject> createdSplittersList = new List<GameObject>();

        // private List<CreepStatusTagVisual> statusesQueue = new();
        // private List<CreepStatusTagVisual> statuses = new();

        private Camera cachedCamera;
        private Transform cameraTransform;
        
        private void Start()
        {
            cachedCamera = Camera.main;
            cameraTransform = cachedCamera.transform;
            localScale = bg.transform.localScale;
        }

        public void ShowHp(float percent)
        {
            if (float.IsNaN(percent)) return;
            localScale.x = (bg.transform.localScale.x * percent);
            fgRoot.transform.localScale = localScale;
        }

        public void ShowArrow() => pointerRoot.gameObject.SetActive(true);
        
        public void HideArrow() => pointerRoot.gameObject.SetActive(false);

        private void Update()
        {
            //forward = transform.position - cameraTransform.position;
            //forward.Normalize();
            //up = Vector3.Cross(forward, cameraTransform.right);
            //transform.rotation = Quaternion.LookRotation(forward, up);
            transform.rotation = Quaternion.identity;
        }

        public void SetHpBarSplitters(float creepMaxHp)
        {
            int i;
            int splittersCount = (int)((creepMaxHp) / oneBarSplitterLength);
            if (splittersCount > 0)
            {
                float hpBlockLength;

                if (splittersCount > maxSplitterCount)
                {
                    splittersCount = maxSplitterCount;
                    hpBlockLength = (1.0f / maxSplitterCount);
                }
                else
                    hpBlockLength = oneBarSplitterLength / creepMaxHp;

                for (i = 1; i <= splittersCount; i++)
                {
                    if ((i * hpBlockLength) < 1)
                    {
                        Vector3 localPosition = new Vector3(i * hpBlockLength, 0, 0);
                        if (i > createdSplittersList.Count)
                        {
                            GameObject duplicate = Instantiate(splitterSample.gameObject, splitterRoot);
                            createdSplittersList.Add(duplicate);
                            SetPosition(localPosition, duplicate);
                        }
                        else
                            SetPosition(localPosition, createdSplittersList[i - 1]);
                    }
                }
            }

            // statuses.Clear();
        }

        private void SetPosition(Vector3 localPosition, GameObject splitterClone)
        {
            splitterClone.transform.localPosition = localPosition;
            splitterClone.SetActive(true);
        }

        public void HideSplitters()
        {
            foreach (GameObject splitter in createdSplittersList)
                splitter.SetActive(false);
        }

        // public void AddStatus(CreepStatusTagVisual status)
        // {
        //     if(!PlayerSettings.ShowGainStatusInfo)
        //         status.Hide();
        //     
        //     status.SetTransformParent(statusRoot);
        //     status.transform.localRotation = Quaternion.identity;
        //     statusesQueue.Add(status);
        //     UpdateStatuses();
        // }
        //
        // public void RemoveStatus(CreepStatusTagVisual status)
        // {
        //     if(!PlayerSettings.ShowGainStatusInfo)
        //         status.Show();
        //     
        //     if (statusesQueue.Contains(status))
        //         statusesQueue.Remove(status);
        //     else
        //     {
        //         statuses.Remove(status);
        //         UpdateStatuses();
        //     }
        //
        // }

        // private void UpdateStatuses()
        // {
        //     if (statuses.Count >= maxActiveStatuses)
        //         return;
        //     int i;
        //
        //     for (i = statuses.Count - 1; i < maxActiveStatuses; i++)
        //     {
        //         if (statusesQueue.Count > 0)
        //         {
        //             statuses.Add(statusesQueue[0]);
        //             statusesQueue.Remove(statuses[^1]);
        //             statuses[^1].Enable();
        //         }
        //     }
        //
        //     for (i = 0; i < statuses.Count; i++)
        //     {
        //         statuses[i].transform.localPosition = new Vector3((-.233f * (statuses.Count - 1)) + (i * .466f), 0, 0);
        //     }
        //
        //
        //     //int Comparer(CreepStatusTagVisual value1, CreepStatusTagVisual value2)
        //     //{
        //     //    if (value1 == null)
        //     //        return 1;
        //     //    if (value2 == null)
        //     //        return -1;
        //     //    return 0;
        //     //}
        // }
    }
}
