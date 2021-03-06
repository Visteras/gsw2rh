using System;
using System.Xml.Linq;
using GunshotWound2.Configs;
using GunshotWound2.GswWorld;
using GunshotWound2.Player;
using GunshotWound2.Utils;
using Leopotam.Ecs;
using Rage;

namespace GunshotWound2.Health.Systems
{
    [EcsInject]
    public class HealthInitSystem : IEcsPreInitSystem, IEcsInitSystem, IEcsRunSystem
    {
        private EcsWorld _ecsWorld;

        private EcsFilter<LoadedConfigComponent> _loadedConfigs;
        private EcsFilter<LoadedItemConfigComponent> _initParts;

        private EcsFilter<PedHealthStatsComponent> _healthStats;
        private EcsFilter<GswPedComponent, NewPedMarkComponent>.Exclude<AnimalMarkComponent> _newHumans;
        private EcsFilter<GswPedComponent, NewPedMarkComponent, AnimalMarkComponent> _newAnimals;
        private static readonly Random Random = new Random();

        private readonly GswLogger _logger;

        public HealthInitSystem()
        {
            _logger = new GswLogger(typeof(HealthInitSystem));
        }

        public void PreInitialize()
        {
            _logger.MakeLog("HealthSystem is loading!");

            var stats = _ecsWorld.AddComponent<HealthStatsComponent>(GunshotWound2Script.StatsContainerEntity);
            stats.DamageMultiplier = 1f;
            stats.DamageDeviation = 0.2f;

            var pedStats = _ecsWorld.AddComponent<PedHealthStatsComponent>(GunshotWound2Script.StatsContainerEntity);
            pedStats.PlayerHealth = 100f;
            pedStats.PedHealth = new MinMax
            {
                Min = 50,
                Max = 100
            };

            foreach (int i in _loadedConfigs)
            {
                LoadedConfigComponent config = _loadedConfigs.Components1[i];
                XElement xmlRoot = config.ElementRoot;

                XElement multElement = xmlRoot.Element("OverallDamageMultiplier");
                if (multElement != null)
                {
                    stats.DamageMultiplier = multElement.GetFloat();
                }

                XElement devElement = xmlRoot.Element("OverallDamageDeviation");
                if (devElement != null)
                {
                    stats.DamageDeviation = devElement.GetFloat();
                }

                XElement pedElement = xmlRoot.Element("PedHealth");
                if (pedElement != null)
                {
                    var health = pedElement.GetMinMax();
                    if (!health.IsDisabled())
                    {
                        pedStats.PedHealth = health;
                    }
                }

                XElement playerElement = xmlRoot.Element("PlayerHealth");
                if (playerElement != null)
                {
                    pedStats.PlayerHealth = playerElement.GetFloat();
                }
            }

            _logger.MakeLog(stats.ToString());
            _logger.MakeLog(pedStats.ToString());
            _logger.MakeLog("HealthSystem loaded!");
        }

        public void Initialize()
        {
            foreach (int i in _initParts)
            {
                XElement partRoot = _initParts.Components1[i].ElementRoot;
                int partEntity = _initParts.Entities[i];
                
                XElement multElement = partRoot.Element("DamageMult");
                if (multElement != null)
                {
                    var mult = _ecsWorld.AddComponent<DamageMultComponent>(partEntity);
                    mult.Multiplier = multElement.GetFloat();
                }

                XElement baseDamageElement = partRoot.Element("BaseDamage");
                if (baseDamageElement != null)
                {
                    var damage = _ecsWorld.AddComponent<BaseDamageComponent>(partEntity);
                    damage.BaseDamage = baseDamageElement.GetFloat();
                }
            }
        }

        public void Run()
        {
            if (_healthStats.EntitiesCount <= 0)
            {
                throw new Exception("HealthSystem was not init!");
            }
            PedHealthStatsComponent stats = _healthStats.Components1[0];

            foreach (int i in _newHumans)
            {
                Ped ped = _newHumans.Components1[i].ThisPed;
                int humanEntity = _newHumans.Entities[i];

                bool isPlayer = _ecsWorld.GetComponent<PlayerMarkComponent>(humanEntity) != null;

                float healthAmount = isPlayer
                    ? stats.PlayerHealth
                    : Random.NextMinMax(stats.PedHealth);
                
                var health = _ecsWorld.AddComponent<HealthComponent>(humanEntity);
                health.MinHealth = 0;
                health.Health = health.MinHealth + healthAmount;
                health.MaxHealth = (float) Math.Floor(health.Health);

                ped.SetMaxHealth(health.MaxHealth);
                ped.SetHealth(health.Health);
            }

            foreach (int i in _newAnimals)
            {
                Ped ped = _newAnimals.Components1[i].ThisPed;
                int animalEntity = _newAnimals.Entities[i];

                var health = _ecsWorld.AddComponent<HealthComponent>(animalEntity);
                health.MinHealth = 0;
                health.Health = health.MinHealth + ped.GetHealth();
                health.MaxHealth = (float) Math.Floor(health.Health);
                
                ped.SetMaxHealth(health.MaxHealth);
                ped.SetHealth(health.Health);
            }
        }

        public void PreDestroy()
        {
        }

        public void Destroy()
        {
        }
    }
}