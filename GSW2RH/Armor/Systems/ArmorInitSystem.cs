using System.Xml.Linq;
using GunshotWound2.Bodies;
using GunshotWound2.Configs;
using GunshotWound2.GswWorld;
using GunshotWound2.Utils;
using GunshotWound2.Weapons;
using Leopotam.Ecs;
using Rage;

namespace GunshotWound2.Armor.Systems
{
    [EcsInject]
    public class ArmorInitSystem : IEcsInitSystem,  IEcsRunSystem
    {
        private EcsWorld _ecsWorld;

        private EcsFilter<LoadedItemConfigComponent, WeaponComponent> _initWeapons;
        private EcsFilter<LoadedItemConfigComponent, BodyPartComponent> _initParts;

        private EcsFilter<GswPedComponent, NewPedMarkComponent>.Exclude<AnimalMarkComponent> _newPeds;

        private readonly GswLogger _logger;

        public ArmorInitSystem()
        {
            _logger = new GswLogger(typeof(ArmorInitSystem));
        }

        public void Initialize()
        {
            foreach (int i in _initWeapons)
            {
                XElement weaponRoot = _initWeapons.Components1[i].ElementRoot;
                XElement statsElement = weaponRoot.GetElement("WeaponArmorStats");
                int weaponEntity = _initWeapons.Entities[i];

                var stats = _ecsWorld.AddComponent<ArmorWeaponStatsComponent>(weaponEntity);
                stats.CanPenetrateArmor = statsElement.GetBool("CanPenetrateArmor");
                stats.ChanceToPenetrateHelmet = statsElement.GetFloat("ChanceToPenetrateHelmet");
                stats.ArmorDamage = statsElement.GetInt("ArmorDamage");
                stats.MinArmorPercentForPenetration = statsElement.GetFloat("MinArmorPercentForPenetration");
            }
            
            foreach (int i in _initParts)
            {
                int entity = _initParts.Entities[i];
                XElement partRoot = _initParts.Components1[i].ElementRoot;
                XElement protection = partRoot.Element("Protection");

                var bodyArmor = _ecsWorld.AddComponent<BodyPartArmorComponent>(entity);
                bodyArmor.ProtectedByHelmet = protection.GetBool("ByHelmet");
                bodyArmor.ProtectedByBodyArmor = protection.GetBool("ByArmor");
            }
        }
        
        public void Run()
        {
            foreach (int i in _newPeds)
            {
                Ped ped = _newPeds.Components1[i].ThisPed;
                int pedEntity = _newPeds.Entities[i];

                var armor = _ecsWorld.AddComponent<ArmorComponent>(pedEntity);
                armor.Armor = ped.Armor;
            }
        }

        public void Destroy()
        {
        }
    }
}