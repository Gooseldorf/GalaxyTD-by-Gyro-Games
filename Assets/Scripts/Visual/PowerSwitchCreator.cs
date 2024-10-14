using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerSwitchCreator : SerializedMonoBehaviour
{
    public int Id;
    [OdinSerialize, NonSerialized] public List<IPowerableVisual> ConnectedPowerables = new();
    public bool IsTurnedOn = true;
    public float Timer;
    public float TimeBetweenToggles = 1f;
    public int Activations = 1;

    public class PowerSwitchData :IIdentifiable
    {

        [SerializeField] public List<int> Powerables;
        public bool IsTurnedOn = true;
        public float Timer;
        public float TimeBetweenToggles = 1f;
        public int Activations;
        [SerializeField]
        public int Id { get; set; }

        public object Clone()
        {
            PowerSwitchData clone = this.MemberwiseClone() as PowerSwitchData;
            clone.Powerables = new List<int>(this.Powerables);
            return clone;
        }
    }

    public PowerSwitchData GetData()
    {
        return new PowerSwitchData
        {
            Id = Id,
            Powerables = ConnectedPowerables.ConvertAll(x => x.Id),
            IsTurnedOn = IsTurnedOn,
            Timer = Timer,
            TimeBetweenToggles = TimeBetweenToggles,
            Activations = Activations
        };

    }

    internal void Init(PowerSwitchData data)
    {
        Id = data.Id;
        IsTurnedOn = data.IsTurnedOn;
        Timer = data.Timer;
        TimeBetweenToggles = data.TimeBetweenToggles;
        Activations = data.Activations;

        UpdatePowerables();


        void UpdatePowerables()
        {
            ConnectedPowerables = new List<IPowerableVisual>();

            List<MonoBehaviour> powerables = new (FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            powerables.RemoveAll(x => x is not IPowerableVisual);

            foreach (var powerable in powerables)
            {
                IPowerableVisual powerableVisual = powerable as IPowerableVisual;
                if (data.Powerables.Contains(powerableVisual.Id))
                {
                    ConnectedPowerables.Add(powerableVisual);
                }
            }

            if (ConnectedPowerables.Count != data.Powerables.Count)
            {
                Debug.LogError($"{nameof(EnergyCore)} {data.Id} connected powerables are missing! ");
            }
        }
    }
}
