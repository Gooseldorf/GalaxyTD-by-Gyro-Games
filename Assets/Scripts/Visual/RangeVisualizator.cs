using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Entities;
using ECSTest.Systems;
using Unity.Mathematics;
using NativeTrees;
using CardTD.Utilities;
using ECSTest.Components;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

[BurstCompile]
public class RangeVisualizator : MonoBehaviour
{
    [SerializeField]
    private MeshFilter rangeMeshFilter;
    [SerializeField]
    private MeshFilter deviationMeshFilter;
    [SerializeField, SuffixLabel("� degrees")]
    private int step = 3;
    [SerializeField]
    private ParticleSystem selection;
    [SerializeField]
    private TargetingVisual targeting;

    private Mesh rangeMesh;
    private Mesh deviationMesh;

    private NativeArray<float3> rangeVerts;

    private bool needToShowDeviation;

    private Entity tower;
    private Entity target;
    private int selectTowerLevel;

    private void Awake()
    {
        rangeMesh = new Mesh();
        deviationMesh = new Mesh();
        Messenger<Entity>.AddListener(UIEvents.ObjectSelected, OnObjectSelected);
        Messenger.AddListener(GameEvents.Restart, Clear);
        targeting.Hide();
    }

    private void OnDestroy()
    {
        Messenger<Entity>.RemoveListener(UIEvents.ObjectSelected, OnObjectSelected);
        Messenger.RemoveListener(GameEvents.Restart, Clear);
        if (rangeVerts.IsCreated)
            rangeVerts.Dispose();
    }

    private void Update()
    {
        if (tower != Entity.Null)
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if(!manager.Exists(tower))
                return;
            
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            if(attackerComponent.TowerType != AllEnums.TowerId.Rocket && attackerComponent.TowerType != AllEnums.TowerId.Mortar)
                ShowTargeting(manager, attackerComponent);
            ShowDeviation(manager, attackerComponent);

            if (attackerComponent.Level == selectTowerLevel)
                return;
            var positionComponent = manager.GetComponentData<PositionComponent>(tower);
            UpdateRange(manager, attackerComponent,positionComponent);
        }
    }

    private void UpdateRange(EntityManager manager,AttackerComponent attackerComponent,PositionComponent positionComponent)
    {
        selectTowerLevel = attackerComponent.Level;
        if (manager.HasComponent<RocketStatsComponent>(tower))
            ShowRocketRange(attackerComponent, positionComponent);
        else
            ShowRange(attackerComponent, positionComponent, manager, !manager.HasComponent<MortarStatsComponent>(tower));
    }

    private void ShowTargeting(EntityManager manager, AttackerComponent attackerComponent)
    {
        if (attackerComponent.Target == Entity.Null || !manager.HasComponent<PositionComponent>(attackerComponent.Target))
        {
            HideTargeting();
        }
        else
        {
            PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(attackerComponent.Target);
            if (target != attackerComponent.Target)
            {
                target = attackerComponent.Target;
                targeting.Show(Color.red);
            }
            targeting.SetPosition(positionComponent.Position);
        }
    }

    private void Clear()
    {
        tower = Entity.Null;
        HideRange();
        HideSelection();
        HideTargeting();
    }

    private void OnObjectSelected(Entity selected)
    {
        if (selected == Entity.Null)
        {
            Clear();
            return;
        }

        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(selected);
        ShowSelection(positionComponent.Position);

        if (manager.HasComponent<DropZoneComponent>(selected))
        {
            tower = Entity.Null;
            HideTargeting();
            HideRange();
            return;
        }

        if (manager.HasComponent<AttackerComponent>(selected))
        {
            tower = selected;

            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(selected);
            needToShowDeviation = manager.HasComponent<GunStatsComponent>(selected);
            UpdateRange(manager, attackerComponent,positionComponent);
        }
    }

    private void ShowSelection(float2 pos)
    {
        selection.transform.position = pos.ToFloat3();
        if (!selection.isPlaying)
            selection.Play();
    }

    private void HideSelection()
    {
        if (!selection.isStopped)
            selection.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void HideRange()
    {
        rangeMesh.Clear();
        deviationMesh.Clear();
    }

    private void HideTargeting()
    {
        target = Entity.Null;
        targeting.Hide();
    }

    public void ShowDeviation(EntityManager manager, AttackerComponent attackerComponent)
    {
        if (!needToShowDeviation)
        {
            if (deviationMesh.vertexCount > 0)
                deviationMesh.Clear();
            return;
        }

        float2 startPos = rangeVerts[0].xy;
        PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(tower);
        float startAngle = Utilities.SignedAngleBetween(new float2(1, 0), positionComponent.Direction) * math.TODEGREES;

        if (startAngle < 0) startAngle = 360 + startAngle;
        float endAngle = startAngle;

        startAngle -= attackerComponent.CurrentDeviation / 2;
        if (startAngle < 0) startAngle = 360 + startAngle;
        endAngle += attackerComponent.CurrentDeviation / 2;
        if (endAngle > 360)
            endAngle -= 360;

        //Debug.Log(startAngle + " " + endAngle);

        int startIndex = 1 + (int)startAngle / step;
        int endIndex = (int)(endAngle / step) + 2;

        bool needExtraStart = false;
        bool needExtraEnd = false;

        if (startAngle % step != 0)
        {
            needExtraStart = true;
            startIndex++;
        }

        if (endAngle % step != 0)
        {
            needExtraEnd = true;
        }
        //Debug.Log("start index = " + startIndex + " end index = " + endIndex);
        int dotsCount = (GetEndIndex() - startIndex) + (needExtraEnd ? 1 : 0) + (needExtraStart ? 1 : 0);
        //Debug.Log("dots count  = " + dotsCount);
        NativeArray<float3> devVerts = new NativeArray<float3>(dotsCount + 1, Allocator.Temp);
        NativeArray<int> devTris = new NativeArray<int>((dotsCount * 3), Allocator.Temp);
        NativeArray<float2> devUv = new NativeArray<float2>(dotsCount + 1, Allocator.Temp);

        devVerts[0] = new float3(startPos, -.1f);
        devUv[0] = startPos;

        int index = 1;

        //Debug.Log(rangeVerts.Length + " " + rangeVerts[1] + " " + rangeVerts[^1]);

        if (needExtraStart)
        {
            //Debug.Log("Extra start point betwwen: " + rangeVerts[GetIndex(startIndex - 1)] + " and " + rangeVerts[GetIndex(startIndex)] + " (" + GetIndex(startIndex - 1) + " " + GetIndex(startIndex) + ")");
            devVerts[index] = GetPointBetween(rangeVerts[GetIndex(startIndex - 1)], rangeVerts[GetIndex(startIndex)], startAngle % step);
            //Debug.Log(rangeVerts[startIndex - 1] + " " + rangeVerts[startIndex] + " " + devVerts[index] + " " + startAngle % step);
            devUv[index] = devVerts[index].xy;
            index++;
        }
        //Debug.Log(startIndex + " " + endIndex + " " + dotsCount);
        int innerIndex;
        for (int i = startIndex; i < GetEndIndex(); i++)
        {
            if (i < rangeVerts.Length - 1)
                innerIndex = i;
            else
                innerIndex = (i % (rangeVerts.Length - 1)) + 1;
            //Debug.Log(innerIndex + " " + rangeVerts[innerIndex]);

            devVerts[index] = rangeVerts[innerIndex];
            devUv[index] = devVerts[index].xy;
            index++;
        }

        if (needExtraEnd)
        {
            //Debug.Log("Extra end point betwwen: " + rangeVerts[GetIndex(endIndex - 1)] + " and " + rangeVerts[GetIndex(endIndex)] + " (" + GetIndex(endIndex - 1) + " " + GetIndex(endIndex) + ")");
            devVerts[index] = GetPointBetween(rangeVerts[GetIndex(endIndex - 1)], rangeVerts[GetIndex(endIndex)], endAngle % step);
            devUv[index] = devVerts[index].xy;
        }

        for (int i = 0; i < devVerts.Length - 2; i++)
        {
            devTris[i * 3] = 0;
            devTris[i * 3 + 1] = 1 + i + 1;
            devTris[i * 3 + 2] = 1 + i;
        }

        deviationMesh.Clear();
        deviationMesh.SetVertices(devVerts);
        deviationMesh.SetTriangles(devTris.ToArray(), 0);
        deviationMesh.SetUVs(0, devUv);
        deviationMeshFilter.mesh = deviationMesh;

        devVerts.Dispose();
        devTris.Dispose();
        devUv.Dispose();

        float3 GetPointBetween(float3 a, float3 b, float deltaAngle)
        {
            return new float3(a.x + ((b.x - a.x) * (deltaAngle / step)), a.y + ((b.y - a.y) * (deltaAngle / step)), a.z + ((b.z - a.z) * (deltaAngle / step)));
        }

        int GetEndIndex()
        {
            if (startIndex > endIndex)
                return (startIndex + (rangeVerts.Length - 2 - startIndex) + endIndex);
            return endIndex;
        }

        int GetIndex(int ind)
        {
            if (ind == 0)
                return rangeVerts.Length - 2;
            return ind;
        }
    }
    private void ShowRange(AttackerComponent attackerComponent, PositionComponent positionComponent, EntityManager manager, bool checkObstacles)
    {
        float2 startPos = positionComponent.Position;
        float range = attackerComponent.AttackStats.AimingStats.Range;

        EntityQuery locatorQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(ObstaclesLocator) });
        ObstaclesLocator obstaclesLocator = locatorQuery.GetSingleton<ObstaclesLocator>();

        if (step == 0)
            step = 1;
        
        int rangeDotsCount = (360 + step) / step;

        if (!rangeVerts.IsCreated)
            rangeVerts = new NativeArray<float3>(rangeDotsCount + 1, Allocator.Persistent);

        NativeArray<int> rangeTris = new NativeArray<int>((rangeDotsCount * 3), Allocator.TempJob);
        NativeArray<float2> rangeUv = new NativeArray<float2>(rangeDotsCount + 1, Allocator.TempJob);

        rangeVerts[0] = new float3(startPos, -.1f);
        rangeUv[0] = startPos;

        FillCircleMesh fillMeshData = new FillCircleMesh()
        {
            StartAngle = 0,
            Direction = new float2(range, 0),
            Range = range,
            Step = step,
            StartPosition = startPos,
            CheckObstacles = checkObstacles,
            ObstaclesLocator = obstaclesLocator,
            Verts = rangeVerts,
            Tris = rangeTris,
            UV = rangeUv
        };

        JobHandle jobHandle = fillMeshData.Schedule(rangeDotsCount, 32);
        jobHandle.Complete();

        rangeMesh.Clear();
        rangeMesh.SetVertices(rangeVerts);
        rangeMesh.SetTriangles(rangeTris.ToArray(), 0);
        rangeMesh.SetUVs(0, rangeUv);
        rangeMeshFilter.mesh = rangeMesh;

        //rangeVerts.Dispose();
        rangeTris.Dispose();
        rangeUv.Dispose();
    }

    private void ShowRocketRange(AttackerComponent attackerComponent, PositionComponent positionComponent)
    {
        float range = attackerComponent.AttackStats.AimingStats.Range;

        if (step == 0)
            step = 1;
        
        int rangeDotsCount = ((360 + step) / step);

        NativeArray<float3> rangeVerts1 = new NativeArray<float3>(rangeDotsCount * 2, Allocator.TempJob);
        NativeArray<int> rangeTris = new NativeArray<int>((rangeDotsCount * 3 * 2), Allocator.TempJob);
        NativeArray<float2> rangeUv = new NativeArray<float2>(rangeDotsCount * 2, Allocator.TempJob);

        FillTorMesh fillTorMesh = new FillTorMesh()
        {
            Direction = new float2(range, 0),
            MinDirection = new float2(RocketTargetingSystem.MinRocketRange(range), 0),
            Height = -.1f * ((range - RocketTargetingSystem.MinRocketRange(range)) / range),
            Range = attackerComponent.AttackStats.AimingStats.Range,
            StartAngle = 0,
            StartPosition = positionComponent.Position,
            Step = step,
            Tris = rangeTris,
            UV = rangeUv,
            Verts = rangeVerts1
        };

        JobHandle jobHandle = fillTorMesh.Schedule(rangeDotsCount, 32);
        jobHandle.Complete();

        rangeTris[^6] = (rangeDotsCount * 2) - 2;
        rangeTris[^5] = 1;
        rangeTris[^4] = (rangeDotsCount * 2) - 1;
        rangeTris[^3] = (rangeDotsCount * 2) - 2;
        rangeTris[^2] = 0;
        rangeTris[^1] = 1;

        rangeMesh.Clear();
        rangeMesh.SetVertices(rangeVerts1);
        rangeMesh.SetTriangles(rangeTris.ToArray(), 0);
        rangeMesh.SetUVs(0, rangeUv);
        rangeMeshFilter.mesh = rangeMesh;

        rangeVerts1.Dispose();
        rangeTris.Dispose();
        rangeUv.Dispose();
    }

    [BurstCompile]
    private struct FillTorMesh : IJobParallelFor
    {
        [Unity.Collections.ReadOnly] public float2 MinDirection;
        [Unity.Collections.ReadOnly] public float2 Direction;
        [Unity.Collections.ReadOnly] public float2 StartPosition;
        [Unity.Collections.ReadOnly] public int Step;
        [Unity.Collections.ReadOnly] public float Range;
        [Unity.Collections.ReadOnly] public float Height;

        [Unity.Collections.ReadOnly] public float StartAngle;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> Verts;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<int> Tris;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float2> UV;

        public void Execute(int i)
        {
            float angle = (StartAngle + (i * Step)) * math.TORADIANS;
            float2 endPos = Utilities.GetRotated(MinDirection, angle) + StartPosition;
            Verts[(i * 2) + 0] = new float3(endPos, Height);
            UV[(i * 2) + 0] = endPos;

            endPos = Utilities.GetRotated(Direction, angle) + StartPosition;
            Verts[(i * 2) + 1] = endPos.ToFloat3();
            UV[(i * 2) + 1] = endPos;

            if ((i * 2) < Verts.Length - 2)
            {
                Tris[i * 6] = (i * 2);
                Tris[i * 6 + 1] = (i * 2) + 3;
                Tris[i * 6 + 2] = (i * 2) + 1;
                Tris[i * 6 + 3] = (i * 2);
                Tris[i * 6 + 4] = (i * 2) + 2;
                Tris[i * 6 + 5] = (i * 2) + 3;
            }
        }
    }

    [BurstCompile]
    private struct FillCircleMesh : IJobParallelFor
    {
        [Unity.Collections.ReadOnly] public float2 Direction;
        [Unity.Collections.ReadOnly] public float2 StartPosition;
        [Unity.Collections.ReadOnly] public int Step;
        [Unity.Collections.ReadOnly] public float Range;
        [Unity.Collections.ReadOnly] public bool CheckObstacles;

        [Unity.Collections.ReadOnly] public float StartAngle;

        [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction]
        public ObstaclesLocator ObstaclesLocator;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> Verts;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<int> Tris;
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<float2> UV;

        public void Execute(int i)
        {
            float angle = (StartAngle + (i * Step)) * math.TORADIANS;
            float2 endPos = Utilities.GetRotated(Direction, angle) + StartPosition;

            if (CheckObstacles && ObstaclesLocator.RaycastObstacle(StartPosition, endPos, out QuadtreeRaycastHit<ObstacleInfo> obstacleHit, default))
            {
                float length = math.length(StartPosition - obstacleHit.point);
                float height = -.1f * ((Range - length) / Range);
                Verts[1 + i] = new float3(obstacleHit.point, height);
                UV[1 + i] = obstacleHit.point;
            }
            else
            {
                Verts[1 + i] = endPos.ToFloat3();
                UV[1 + i] = endPos;
            }

            if (i < Verts.Length - 2)
            {
                Tris[i * 3] = 0;
                Tris[i * 3 + 1] = 1 + i + 1;
                Tris[i * 3 + 2] = 1 + i;
            }
        }
    }
}
