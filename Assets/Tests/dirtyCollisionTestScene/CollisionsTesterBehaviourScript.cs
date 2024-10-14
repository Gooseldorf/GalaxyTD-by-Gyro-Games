using CardTD.Utilities;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MyNamespace
{
    public class CollisionsTesterBehaviourScript : MonoBehaviour
    {
        public List<IObstacle> list = new();

        public GameObject start, end, collision;
        public List<SquareTest> cubes = new();
        public List<GameObject> spheres = new();

        public void AddSquareObstacle()
        {
            foreach (var item in cubes)
            {
                list.Add(item);
            }
        }

        public void AddRoundObstacle()
        {
            foreach (var item in spheres)
            {
                RoundObstacle round = new(item);
                list.Add(round);
            }
        }

        [Button]
        public void Check()
        {
            AddSquareObstacle();
            AddRoundObstacle();

            if (CheckCollision(start.transform.position, end.transform.position, out CollisionDumb cs))
                collision.transform.position = cs.Point;
            else
                collision.transform.position = new(-1000, -1000, -1000);
            list.Clear();
        }
        public bool CheckCollision(float3 startPoint, float3 endPoint, /*GunProjectile projectile,*/
            out CollisionDumb collision)
        {
            List<CollisionDumb> allCollisions = new();


            collision = new CollisionDumb();
            //найти все коллизии
            foreach (var item in list)
            {
                if (HasCollision(item, startPoint, endPoint, out float3 tempCollisionPoint, out float3 tempNormal))
                    allCollisions.Add(new(item, /*projectile,*/ tempCollisionPoint, tempNormal));
            }

            //найти ближайшую коллизию к startPoint
            if (allCollisions.Count != 0)
            {
                float closestDistance = math.distance(startPoint, endPoint);

                for (int i = 0; i < allCollisions.Count; i++)
                {
                    float tempDist = math.distance(startPoint, allCollisions[i].Point);
                    if (tempDist <= closestDistance)
                    {
                        collision = allCollisions[i];
                        closestDistance = tempDist;
                    }
                }
                return true;
            }

            return false;
        }
        #region CS
        private bool HasCollision(IObstacle obstacle, float3 startPoint, float3 endPoint, out float3 CollisionPoint, out float3 tempNormal)
        {
            bool result = false;
            float2 CP2 = new(); float2 normal2 = new();

            float2 start2 = new(startPoint.x, startPoint.y);
            float2 end2 = new(endPoint.x, endPoint.y);

            switch (obstacle)
            {
                case ISquareObstacle square: result = HasCollision(square, start2, end2, out CP2, out normal2); break;
                case IRoundObstacle round: result = HasCollision(round, start2, end2, out CP2, out normal2); break;
            }
            CollisionPoint = new(CP2, 0);
            tempNormal = new(normal2, 0);

            return result;
        }


        private bool HasCollision(IRoundObstacle round, float2 A, float2 B, out float2 CollisionPoint, out float2 tempNormal)
        {
            CollisionPoint = tempNormal = new();

            float2 C = new(round.Center.x, round.Center.y);
            float r = round.Range;

            float2 AB = B - A;
            float2 CA = A - C;

            // a(x*x)+bx+c =0 , where x is distance from A to CollisionPoint
            float a = math.dot(AB, AB);
            float b = 2 * math.dot(CA, AB);
            float c = math.dot(CA, CA) - r * r;

            float discriminant = b * b - 4 * a * c;
            if (discriminant >= 0)
            {
                float x = (-b - math.sqrt(discriminant)) / (2 * a); //closest distance, couse we get -sqrt(discriminant)
                                                                    //float x2 = (-b + math.sqrt(discriminant)) / (2 * a); //if we need 2nd intersection => check (x2 > 0 && x2 <= 1)

                if (x > 0 && x <= 1)//if x==0, so CollisionPoint at A
                {
                    CollisionPoint = A + x * AB;
                    return true;
                }

            }
            // no intersection
            return false;
        }

        private bool HasCollision(ISquareObstacle square, float2 startPoint, float2 endPoint, out float2 CollisionPoint, out float2 tempNormal)
        {
            CollisionPoint = tempNormal = new();

            if (IsPointInside(square, startPoint))//!!note: проверить - предполагаю что если square.Points заданы по порядку и точка лежит по одну и ту же сторону относительно каждой стороны => она внутри
                return false;

            List<float2> intersectionPoints = new();
            List<float2> tempNormals = new();

            //найти все пересечения //!!note: рядом стоящие square.Points образуют стороны (не диагонали)
            FindAllIntersections(square, startPoint, endPoint, intersectionPoints, tempNormals);

            //найти ближайшее пересечение к startPoint
            if (intersectionPoints.Count != 0)
            {
                float2 start = new(startPoint.x, startPoint.y);
                float closestDistance = math.distance(start, new(endPoint.x, endPoint.y));

                for (int i = 0; i < intersectionPoints.Count; i++)
                {
                    float tempDist = math.distance(start, intersectionPoints[i]);
                    if (math.distance(start, intersectionPoints[i]) <= closestDistance)
                    {
                        closestDistance = tempDist;
                        CollisionPoint = new(intersectionPoints[i].x, intersectionPoints[i].y);
                        tempNormal = new(tempNormals[i].x, tempNormals[i].y);
                    }
                }
                return true;
            }

            return false;

            bool IsPointInside(ISquareObstacle square, float2 PTest)
            {
                float a = (square.Points[0].x - PTest.x) * (square.Points[1].y - square.Points[0].y) - (square.Points[1].x - square.Points[0].x) * (square.Points[0].y - PTest.y);
                float b = (square.Points[1].x - PTest.x) * (square.Points[2].y - square.Points[1].y) - (square.Points[2].x - square.Points[1].x) * (square.Points[1].y - PTest.y);
                float c = (square.Points[2].x - PTest.x) * (square.Points[3].y - square.Points[2].y) - (square.Points[3].x - square.Points[2].x) * (square.Points[2].y - PTest.y);
                float d = (square.Points[3].x - PTest.x) * (square.Points[0].y - square.Points[3].y) - (square.Points[0].x - square.Points[3].x) * (square.Points[3].y - PTest.y);


                if ((a >= 0 && b >= 0 && c >= 0 && d >= 0) || (a <= 0 && b <= 0 && c <= 0 && d <= 0))
                    return true;
                else
                    return false;
            }

            void FindAllIntersections(ISquareObstacle square, float2 startPoint, float2 endPoint, List<float2> intersectionPoints, List<float2> tempNormals)
            {
                for (int i = 0; i < square.Points.Length; i++)
                {
                    int nextPoint = i + 1 == square.Points.Length ? 0 : i + 1;
                    if (FindIntersection(startPoint, endPoint, square.Points[i], square.Points[nextPoint], out float2 intersection, out float2 normal))
                    {
                        intersectionPoints.Add(intersection);
                        tempNormals.Add(normal);
                    }
                }

                bool FindIntersection(float2 A, float2 B, float2 C, float2 D, out float2 CollisionPoint, out float2 normal)
                {
                    CollisionPoint = new();
                    float2 CD = math.normalize(D - C);
                    normal = new(CD.y, -1 * CD.x);
                    //check that normal is turned to A
                    if (math.dot(normal, math.normalize(B - A)) > 0)
                        normal *= -1;

                    //      A + t R = C + u S       => CollisionPoint //p,q,  r,s - Vector2
                    float2 R = new(B.x - A.x, B.y - A.y);
                    float2 S = new(D.x - C.x, D.y - C.y);

                    //1     t = (C - A) × s / (r × s)
                    //2     u = (C - A) × r / (r × s)

                    //check if (r × s) == 0
                    float crossRS = Utilities.CrossProduct(R, S);
                    float crossQpR = Utilities.CrossProduct(C - A, R);
                    if (crossRS == 0)
                    {
                        //two lines are collinear
                        if (crossQpR == 0)
                        {
                            //express C => A + tR, C+S => A + tR so
                            float2 c2 = (C - A) / R;
                            float2 d2 = c2 + S / R;

                            //if at least one point of segment at AB
                            if ((c2.x > A.x == c2.x < (A + R).x) || (c2.y > A.y == c2.y < (A + R).y) ||
                                (d2.x > A.x == d2.x < (A + R).x) || (d2.y > A.y == d2.y < (A + R).y))
                            {
                                CollisionPoint = math.distance(A, c2) < math.distance(A, d2) ? c2 : d2;
                                return true;
                            }
                            //no points of segment at AB
                            return false;
                        }
                        else //two lines are parallel and non-intersecting
                            return false;
                    }
                    else
                    {
                        //CollisionPoint at A + t R  (same: C + u S)
                        float t = Utilities.CrossProduct((C - A), S) / crossRS;
                        float u = Utilities.CrossProduct((C - A), R) / crossRS;

                        CollisionPoint = A + t * R;
                        return t <= 1 && t >= 0 && u <= 1 && u >= 0;// so point at AB and at CD
                    }
                }
            }
        }

        #endregion

        public class RoundObstacle : IRoundObstacle
        {
            public GameObject round;

            public RoundObstacle(GameObject go) => round = go;

            public float Range => round.transform.localScale.x / 2;

            public float2 Center => new float2(round.transform.position.x,round.transform.position.y);

            public AllEnums.ObstacleType ObstacleType => AllEnums.ObstacleType.OnlyRicochet;
        }

        public struct CollisionDumb
        {
            public IObstacle Target { get; private set; }
            public float3 Point { get; private set; }
            public float3 Normal { get; private set; }

            public CollisionDumb(IObstacle target, float3 point, float3 normal)
            {
                Target = target;
                Point = point;
                Normal = normal;
            }
        }
    }
}