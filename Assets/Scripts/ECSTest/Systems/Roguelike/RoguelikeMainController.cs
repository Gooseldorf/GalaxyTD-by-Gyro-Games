using CardTD.Utilities;
using ECSTest.Components;
using System.Collections.Generic;
using System.Linq;
using UI;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Systems.Roguelike
{
    public class RoguelikeMainController : MonoBehaviour
    {
        private GameData gameData;

        private const int wavePerAddTower = 1;
        private int lastAddedTower = 0;
        private List<Entity> towers = new();

        private readonly Dictionary<Entity, TowerFactory> towersDictionary = new();

        public List<WeaponPart> Directives = new();

        public Tower GetTower(Entity towerEntity) => towersDictionary.TryGetValue(towerEntity, out TowerFactory value) ? value.GetAssembledTower() : null;

        public static RoguelikeMainController Link;

        public void Init(GameData data)
        {
            Link = this;
            gameData = data;
            Clear();
        }

        private void Awake()
        {
            Messenger<Entity>.AddListener(GameEvents.BuildTower, BuildTower);
            Messenger<Entity>.AddListener(GameEvents.TowerSell, SellTower);
            Messenger.AddListener(GameEvents.Restart, RestartGame);
            Messenger<int>.AddListener(GameEvents.NextWave, OnWaveStarted);
        }

        private void OnDestroy()
        {
            Messenger.RemoveListener(GameEvents.Restart, RestartGame);
            Messenger<int>.RemoveListener(GameEvents.NextWave, OnWaveStarted);
            Messenger<Entity>.RemoveListener(GameEvents.BuildTower, BuildTower);
            Messenger<Entity>.RemoveListener(GameEvents.TowerSell, SellTower);
            Link = null;
        }

        private void Clear()
        {
            lastAddedTower = 0;
            towers.Clear();
            towersDictionary.Clear();
            Directives.Clear();
        }

        private void BuildTower(Entity towerEntity)
        {
            towers.Add(towerEntity);

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var attaker = manager.GetComponentData<AttackerComponent>(towerEntity);

            foreach (TowerFactory factory in gameData.Factories)
            {
                if (attaker.TowerType == factory.TowerId)
                {
                    towersDictionary.Add(towerEntity, factory.Clone());
                    break;
                }
            }
        }

        private void SellTower(Entity tower)
        {
            towers.Remove(tower);
            towersDictionary.Remove(tower);
        }

        private void OnWaveStarted(int waveNum)
        {
            // AddTower();
            AddDirectives();
        }

        private bool CanAddNewDirectives(Entity towerEntity, out int emptySlotIndex)
        {
            emptySlotIndex = -1;
            if (!towersDictionary.ContainsKey(towerEntity))
                return false;

            TowerFactory factory = towersDictionary[towerEntity];

            for (int i = 0; i < factory.Directives.Count; i++)
            {
                if (factory.Directives[i].WeaponPart != null) continue;
                emptySlotIndex = i;
                return true;
            }

            return false;
        }

        private void AddDirectives()
        {
            List<WeaponPart> directives = DataManager.Instance.Get<PartsHolder>().Directives;

            List<WeaponPart> parts = new();

            for (int i = 0; i < 3; i++)
            {
                WeaponPart findPart = directives.GetRandomValue();

                while (parts.Find((part) => part.name == findPart.name))
                    findPart = directives.GetRandomValue();

                parts.Add(findPart);
            }

            foreach (var part in parts)
            {
                Debug.Log($"find {part.name}");
            }

            Directives.Add(parts.GetRandomValue());
        }

        public void AddDirectiveToTower(Entity towerEntity, WeaponPart part)
        {
            if (!CanAddNewDirectives(towerEntity, out int index)) return;

            Directives.Remove(part);

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var attacker = manager.GetComponentData<AttackerComponent>(towerEntity);
            TowerFactory factory = towersDictionary[towerEntity];
            int level = attacker.Level;
            attacker.Level = 1;
            manager.SetComponentData(towerEntity, attacker);

            factory.AddDirective(part, index);
            GameServices.Instance.UpdateTowerLevelData(towerEntity, level, factory.GetAssembledTower());
            Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, towerEntity, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void AddTower()
        {
            int countTowers = GameServices.Instance.Towers.Count;

            if (gameData.TowerFactories.Count > countTowers)
            {
                GameServices.Instance.AddTower(gameData.TowerFactories[countTowers].GetAssembledTower());
            }
        }

        private void RestartGame()
        {
            Clear();
            GameServices.Instance.SetTowers(gameData.GetStartTowerForRoguelike());
            Debug.Log("RestartGame");
        }
    }
}