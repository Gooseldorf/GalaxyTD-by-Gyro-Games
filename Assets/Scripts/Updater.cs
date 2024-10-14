using UnityEngine;

public class Updater : MonoBehaviour
{
    private void Update()
    {
        GameServices.Instance.UpdateTime(Time.deltaTime);
    }
}