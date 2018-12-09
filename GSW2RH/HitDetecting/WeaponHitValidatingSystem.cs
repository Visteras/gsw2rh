﻿using GunshotWound2.Weapons;
using Leopotam.Ecs;
using Rage;

namespace GunshotWound2.HitDetecting
{
    [EcsInject]
    public class WeaponHitValidatingSystem : IEcsRunSystem
    {
        private EcsWorld _ecsWorld;
        private EcsFilter<DamagedByWeaponComponent> _hits;
        
        public void Run()
        {
            foreach (int i in _hits)
            {
                int hitEntity = _hits.Entities[i];
                int weaponEntity = _hits.Components1[i].WeaponEntity;
                WeaponTypes type = _hits.Components1[i].WeaponType;
                
                if (!_ecsWorld.IsEntityExists(weaponEntity))
                {
                    Game.Console.Print("Weapon Entity with type " + type + " doesn't exist");
                    _ecsWorld.RemoveComponent<DamagedByWeaponComponent>(hitEntity);
                    continue;
                }

                if (_ecsWorld.GetComponent<BaseWeaponStatsComponent>(weaponEntity) == null)
                {
                    Game.Console.Print("Weapon Entity with type " + type +
                                       " doesn't have " + nameof(BaseWeaponStatsComponent));
                    _ecsWorld.RemoveComponent<DamagedByWeaponComponent>(hitEntity);
                    continue;
                }
            }
        }
    }
}