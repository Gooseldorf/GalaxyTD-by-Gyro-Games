using Unity.Mathematics;
using UnityEngine;

namespace MyNamespace
{
    public class SquareTest : MonoBehaviour, ISquareObstacle
    {
        public GameObject a, b, c, d;
        private float2 a1 => new(a.transform.position.x, a.transform.position.y);
        private float2 b1 => new(b.transform.position.x, b.transform.position.y);
        private float2 c1 => new(c.transform.position.x, c.transform.position.y);
        private float2 d1 => new(d.transform.position.x, d.transform.position.y);


        public AllEnums.ObstacleType ObstacleType => AllEnums.ObstacleType.OnlyRicochet;

        float2[] ISquareObstacle.Points => new float2[4] { a1, b1, c1, d1 };
    }
}
