using Sirenix.OdinInspector;
using UnityEngine;

public class MapPreviewController : MonoBehaviour
{
    public Camera PreviewCamera;


    private void Awake()
    {
        PreviewCamera.enabled = false;
    }

    [Button]
    public void CenterCameraToTilemap(Mission mission)
    {
        Vector3Int size = new Vector3Int(mission.LevelMatrix.Bounds.x, mission.LevelMatrix.Bounds.y );

        Vector3 center = size / 2;

        PreviewCamera.transform.position = new Vector3(center.x, center.y, -10);
        
        PreviewCamera.orthographicSize = Mathf.Max(size.x, size.y) / 2.0f;
        PreviewCamera.enabled = true;
    }
}
