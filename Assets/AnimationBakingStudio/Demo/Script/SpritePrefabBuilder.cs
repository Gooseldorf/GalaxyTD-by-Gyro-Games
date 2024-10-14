using UnityEngine;

namespace ABS.Demo
{
    public class SpritePrefabBuilder : PrefabBuilder
    {
        public override BoxCollider2D GetBoxCollider2D(GameObject rootObject)
        {
            return rootObject.GetComponent<BoxCollider2D>();
        }
    }
}