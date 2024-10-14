using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using ECSTest.Systems;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[ShowOdinSerializedPropertiesInInspector]
public class EnergyCoreVisual : EnvironmentVisual, IPowerableVisual, ISerializationCallbackReceiver
{
    [SerializeField] private int2 energyCoreOffset;
    [ShowInInspector] public int Id { get; set; }

    private Entity coreEntity;
    [ShowInInspector] public bool IsPowered { get; set; }

    private readonly List<EnvironmentVisual> powerCellVisuals = new();

    [Required] public int PowerCellCount;

    [Required] public float DeactivationTime = 0;
    private bool isMenuScene = false;

    [ShowInInspector] public EnergyCore EnergyCore { get; private set; }
    [OdinSerialize, NonSerialized] public List<IPowerableVisual> ConnectedPowerables = new();

    [SerializeField]
    private SpriteRenderer warningIcon;
    private Sequence alertSeq;

    [SerializeField, FoldoutGroup("ProgressBar")]
    private GameObject progressBar;
    [SerializeField, FoldoutGroup("ProgressBar")]
    private Transform fg;
    [SerializeField, FoldoutGroup("ProgressBar")]
    private Transform point;

    #region AnimationFields

    [FoldoutGroup("Core Animation")]
    [SerializeField]
    private SpriteRenderer coreMiddle;

    [FoldoutGroup("Core Animation")]
    [SerializeField]
    private SpriteRenderer coreTop;

    [FoldoutGroup("Core Animation")]
    [SerializeField]
    private float middleRotationSpeed;

    [FoldoutGroup("Core Animation")]
    [SerializeField]
    private float topRotationSpeed;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private Transform powerCellsParent;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private Transform innerCircleTransform;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private float innerCircleCellsRotationSpeed = 1;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private Transform outerCircleTransform;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private float outerCircleCellsRotationSpeed = 1;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private float innerCircleRadius = 0.5f;

    [FoldoutGroup("PowerCells Animation")]
    [SerializeField]
    private float outerCircleRadius = 0.7f;

    private int cellsInEachLayer;

    #endregion

    private void Awake()
    {
        isMenuScene = SceneManager.GetActiveScene().buildIndex == 0;
    }

    public void InitVisual(PositionComponent position, EnergyCoreComponent component, Entity coreEntity)
    {
        this.coreEntity = coreEntity;
        transform.position = position.Position.ToFloat3();
        PowerCellCount = component.PowerCellCount;
        DeactivationTime = component.DeactivationTime;

        if (component.DeactivationTime != 0)
        {
            progressBar.SetActive(true);
            fg.localScale = new Vector3(0, 1, 1);
            point.localPosition = new Vector3(-.94f, 0, 0);
        }
        else
            progressBar.SetActive(false);


        CreatePowerCells();
        Messenger<PowerCellEvent>.AddListener(GameEvents.CellDetached, DetachCell);
        Messenger<PowerCellEvent>.AddListener(GameEvents.CellDestroyedAll, DetachCells);
        Messenger<PowerCellEvent>.AddListener(GameEvents.CellAttachedNew, AttachCells);
        Messenger<PowerCellEvent>.AddListener(GameEvents.CellAttached, AttachCell);
        Messenger<Entity, bool>.AddListener(GameEvents.UpdateVisualWarning, OnWarningUpdate);
        warningIcon.color = new Color(1, 1, 1, 0);
    }

    public void OnDestroy()
    {
        Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDetached, DetachCell);
        Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellAttached, AttachCell);
        Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDestroyedAll, DetachCells);
        Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellAttachedNew, AttachCells);
        Messenger<Entity, bool>.RemoveListener(GameEvents.UpdateVisualWarning, OnWarningUpdate);
    }

    private void CreatePowerCells()
    {
        for (int i = 0; i < PowerCellCount; i++)
        {
            powerCellVisuals.Add(GameServices.Instance.Get<SimpleEffectManager>().PowerCellsPool.Get().GetComponent<EnvironmentVisual>());
        }

        AdjustPowerCellVisuals();
    }

    private void Update()
    {
        if (PowerCellCount > 0 && !isMenuScene)
        {
            AnimatePowerCells();
            AnimateCore();
        }

        if (progressBar.activeSelf && coreEntity != Entity.Null)
        {
            EnergyCoreComponent coreComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<EnergyCoreComponent>(coreEntity);
            if (coreComponent.DeactivationTime > 0)
            {
                fg.localScale = new Vector3(math.remap(DeactivationTime, 0, 47, 0, coreComponent.DeactivationTime), 1, 1);
                point.localPosition = new Vector3(math.remap(DeactivationTime, 0, .94f, -.94f, coreComponent.DeactivationTime), 0, 0);
            }
            else
                progressBar.SetActive(false);
        }
    }

    private void AttachCell(PowerCellEvent powerCellEvent)
    {
        if (powerCellEvent.Core != coreEntity) return;

        AttachCell();
        AdjustPowerCellVisuals();
    }

    private void AttachCell()
    {
        powerCellVisuals.Add(GameServices.Instance.Get<SimpleEffectManager>().PowerCellsPool.Get().GetComponent<EnvironmentVisual>());
        PowerCellCount++;
    }

    private void AttachCells(PowerCellEvent powerCellEvent)
    {
        if (powerCellEvent.Core != coreEntity) return;

        for (int i = 0; i < powerCellEvent.Value; i++)
            AttachCell();

        AdjustPowerCellVisuals();
    }

    private void DetachCell(PowerCellEvent powerCellEvent)
    {
        if (powerCellEvent.Core != coreEntity || powerCellVisuals.Count == 0) return;
        DetachCell();
        AdjustPowerCellVisuals();
    }

    private void DetachCell()
    {
        GameServices.Instance.Get<SimpleEffectManager>().PowerCellsPool.Release(powerCellVisuals[^1].gameObject);
        powerCellVisuals.Remove(powerCellVisuals[^1]);
        PowerCellCount--;
    }

    private void DetachCells(PowerCellEvent powerCellEvent)
    {
        if (powerCellEvent.Core != coreEntity)
            return;

        for (int i = 0; i < powerCellEvent.Value; i++)
        {
            if (powerCellVisuals.Count > 0)
                DetachCell();
        }

        AdjustPowerCellVisuals();
    }

    private void AdjustPowerCellVisuals()
    {
        cellsInEachLayer = PowerCellCount / 2;

        if (cellsInEachLayer == 0)
            cellsInEachLayer = 1;

        for (int i = 0; i < powerCellVisuals.Count; i++)
        {
            int layer = i / cellsInEachLayer;
            float layerRadius = layer == 0 ? outerCircleRadius : innerCircleRadius;

            float angleDelta = math.radians(360.0f / cellsInEachLayer);

            int positionInLayer = i % cellsInEachLayer;

            float angle = positionInLayer * angleDelta;

            powerCellVisuals[i].transform.SetPositionAndRotation(new Vector3(
                transform.position.x + layerRadius * math.cos(angle),
                transform.position.y + layerRadius * math.sin(angle),
                0), Utilities.Direction2DToQuaternion(1));

            if (layer == 0)
            {
                powerCellVisuals[i].Icon.sortingOrder = coreMiddle.sortingOrder + 1;
                powerCellVisuals[i].transform.SetParent(outerCircleTransform);
            }
            else
            {
                powerCellVisuals[i].Icon.sortingOrder = coreTop.sortingOrder + 1;
                powerCellVisuals[i].transform.SetParent(innerCircleTransform);
            }
        }
    }

    private void AnimatePowerCells()
    {
        innerCircleTransform.Rotate(0, 0, innerCircleCellsRotationSpeed * Time.deltaTime);
        outerCircleTransform.Rotate(0, 0, outerCircleCellsRotationSpeed * Time.deltaTime);
    }

    private void AnimateCore()
    {
        coreMiddle.transform.Rotate(0, 0, middleRotationSpeed * Time.deltaTime);
        coreTop.transform.Rotate(0, 0, topRotationSpeed * Time.deltaTime);
    }

    public void TogglePower() => SetPowered(!IsPowered);

    public void SetPowered(bool isPowered)
    {
        IsPowered = isPowered;
    }

    private void OnWarningUpdate(Entity entity, bool needToShow)
    {
        if (entity != coreEntity) return;

        if (PowerCellCount <= 0)
        {
            HideAlert();
            return;
        }

        if (needToShow && alertSeq == null)
            ShowAlert();
        if (!needToShow && alertSeq != null)
            HideAlert();

    }
    private void ShowAlert()
    {
        if (alertSeq != null)
            alertSeq.Kill();

        alertSeq = DOTween.Sequence();
        warningIcon.color = new Color(1, 1, 1, 0);
        alertSeq.Append(warningIcon.DOColor(new Color(1, 1, 1, 1), 1f));
        alertSeq.Append(warningIcon.DOColor(new Color(1, 1, 1, 0), .5f));
        alertSeq.SetLoops(-1);
    }

    private void HideAlert()
    {
        if (alertSeq != null)
        {
            alertSeq.Kill();
            alertSeq = null;
            warningIcon.color = new Color(1, 1, 1, 0);
        }
    }
#if UNITY_EDITOR

    public EnergyCore GetEnergyCoreData(int2 gridPosOffset)
    {
        List<int> connectedPowerableIds = new();

        for (int i = 0; i < ConnectedPowerables.Count; i++)
            connectedPowerableIds.Add(ConnectedPowerables[i].Id);

        EnergyCore = new EnergyCore(GridPosition + gridPosOffset + energyCoreOffset, GridSize, connectedPowerableIds, PowerCellCount, Id, DeactivationTime);
        return EnergyCore;
    }

    public override void InitVisual(object data)
    {
        EnergyCore energyCore = data as EnergyCore;

        EnergyCore = energyCore;
        Id = EnergyCore.Id;
        PowerCellCount = EnergyCore.PowerCellCount;
        DeactivationTime = EnergyCore.DeactivationTime;
        InitPosition(energyCore);


        UpdatePowerables();

        void UpdatePowerables()
        {
            ConnectedPowerables.Clear();

            PortalVisual[] portals = FindObjectsByType<PortalVisual>(FindObjectsSortMode.None);

            foreach (PortalVisual portal in portals)
            {
                if (this.EnergyCore.Powerables.Contains(portal.Id))
                    ConnectedPowerables.Add(portal);
            }

            DropZoneVisual[] dropZones = FindObjectsByType<DropZoneVisual>(FindObjectsSortMode.None);

            foreach (DropZoneVisual dropZoneVisual in dropZones)
            {
                if (this.EnergyCore.Powerables.Contains(dropZoneVisual.Id))
                    ConnectedPowerables.Add(dropZoneVisual);
            }

            BridgeVisual[] bridges = FindObjectsByType<BridgeVisual>(FindObjectsSortMode.None);

            foreach (BridgeVisual bridgeVisual in bridges)
            {
                if (this.EnergyCore.Powerables.Contains(bridgeVisual.Id))
                    ConnectedPowerables.Add(bridgeVisual);
            }

            GateVisual[] gates = FindObjectsByType<GateVisual>(FindObjectsSortMode.None);

            foreach (GateVisual gateVisual in gates)
            {
                if (this.EnergyCore.Powerables.Contains(gateVisual.Id))
                    ConnectedPowerables.Add(gateVisual);
            }

            ConveyorBeltVisual[] conveyors = FindObjectsByType<ConveyorBeltVisual>(FindObjectsSortMode.None);
            foreach (ConveyorBeltVisual conveyor in conveyors)
            {
                if (this.EnergyCore.Powerables.Contains(conveyor.Id))
                    ConnectedPowerables.Add(conveyor);
            }

            if (ConnectedPowerables.Count != energyCore.Powerables.Count)
            {
                Debug.LogError($"{nameof(EnergyCore)} {energyCore.Id} connected powerables are missing! ");
            }
        }
    }

    [Button]
    private void ConnectSelectedPowerables()
    {
        GameObject[] temp = Selection.gameObjects;
        foreach (var go in temp)
        {
            if (go.TryGetComponent(out EnvironmentVisual visual) && visual is IPowerableVisual powerable && !ConnectedPowerables.Contains(powerable))
            {
                ConnectedPowerables.Add(powerable);
            }
        }
    }

    [Button]
    private void SelectConnectedPowerables()
    {
        List<GameObject> gameObjectsToSelect = new();
        foreach (var powerable in ConnectedPowerables)
        {
            gameObjectsToSelect.Add(((EnvironmentVisual)powerable).gameObject);
        }

        Selection.objects = gameObjectsToSelect.ToArray();
    }
#endif

    #region Serialization you don't have to care about

    [SerializeField, HideInInspector] private SerializationData serializationData;

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        UnitySerializationUtility.DeserializeUnityObject(this, ref this.serializationData);
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        UnitySerializationUtility.SerializeUnityObject(this, ref this.serializationData);
    }
}

#endregion

public interface IPowerableVisual
{
    public int Id { get; set; }
    public bool IsPowered { get; set; }
    void TogglePower();
    void SetPowered(bool isPowered);
}