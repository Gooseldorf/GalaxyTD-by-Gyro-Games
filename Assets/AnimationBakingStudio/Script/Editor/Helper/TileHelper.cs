using UnityEngine;

namespace ABS
{
    public static class TileHelper
    {
        public static void UpdateTileToModel(Model model, Studio studio)
        {
            if (model == null || studio.view.tileObj == null)
                return;

            Transform squareT = studio.view.tileObj.transform.Find("Square");
            Transform hexagonT = studio.view.tileObj.transform.Find("Hexagon");
            if (squareT == null || hexagonT == null)
                return;

            GameObject squareTileObj = squareT.gameObject, hexagonTileObj = hexagonT.gameObject;

            if (!model.IsTileAvailable() || !studio.view.isTileVisible)
            {
                squareTileObj.SetActive(false);
                hexagonTileObj.SetActive(false);
                return;
            }

            if (studio.view.tileType == TileType.Square)
            {
                UpdateTile(model, squareTileObj, studio.view.tileType, studio.view.baseTurnAngle);
                hexagonTileObj.SetActive(false);
            }
            else if (studio.view.tileType == TileType.Hexagon)
            {
                UpdateTile(model, hexagonTileObj, studio.view.tileType, studio.view.baseTurnAngle);
                squareTileObj.SetActive(false);
            }
        }

        private static void UpdateTile(Model model, GameObject tileObj, TileType gridType, float baseAngle = 0)
        {
            if (model == null)
                return;

            Renderer tileRenderer = tileObj.GetComponent<Renderer>();
            if (tileRenderer == null)
                return;

            Transform tileT = tileObj.transform;

            tileT.localScale = Vector3.one;
            float scaleRatio;
            {
                Vector3 modelSize = model.GetDynamicSize();
                float animMaxLength = Mathf.Max(modelSize.x, modelSize.z);
                float tileLength = 0.0f;
                if (gridType == TileType.Square)
                    tileLength = (tileRenderer.bounds.size.x / 2.0f) * (1.0f / Mathf.Sqrt(2.0f)) * 2.0f;
                else if (gridType == TileType.Hexagon)
                    tileLength = (tileRenderer.bounds.size.x / 2.0f) * (Mathf.Sqrt(3.0f) / 2.0f) * 1.5f;
                scaleRatio = animMaxLength / tileLength;
            }
            tileT.localScale = new Vector3(scaleRatio, 1.0f, scaleRatio);

            tileT.rotation = Quaternion.identity;
            tileT.RotateAround(tileT.position, Vector3.up, baseAngle);

            tileObj.SetActive(true);
        }

        public static void HideAllTiles()
        {
            GameObject tilesObj = GameObject.Find(EditorConstants.HELPER_TILES_NAME);
            if (tilesObj == null)
                return;

            Transform squareT = tilesObj.transform.Find("Square");
            Transform hexagonT = tilesObj.transform.Find("Hexagon");
            if (squareT != null && hexagonT != null)
            {
                squareT.gameObject.SetActive(false);
                hexagonT.gameObject.SetActive(false);
            }
        }
    }
}
