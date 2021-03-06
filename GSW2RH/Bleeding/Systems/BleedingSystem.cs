using GunshotWound2.GswWorld;
using GunshotWound2.Health;
using GunshotWound2.Utils;
using Leopotam.Ecs;
using Rage;

namespace GunshotWound2.Bleeding.Systems
{
    [EcsInject]
    public class BleedingSystem : IEcsRunSystem
    {
        private const float HEAL_RATE_SLOWER = 100f;
        
        private EcsWorld _ecsWorld;
        private EcsFilter<GswPedComponent, HealthComponent, BleedingInfoComponent> _entities;

        private readonly GswLogger _logger;

        public BleedingSystem()
        {
            _logger = new GswLogger(typeof(BleedingSystem));
        }

        public void Run()
        {
            foreach (int i in _entities)
            {
                Ped ped = _entities.Components1[i].ThisPed;
                if (!ped.Exists()) continue;

                HealthComponent health = _entities.Components2[i];
                if (health.Health <= 0) continue;

                BleedingInfoComponent info = _entities.Components3[i];
                int pedEntity = _entities.Entities[i];
                float bleedingDamage = 0;

                for (int bleedingIndex = info.BleedingEntities.Count - 1; bleedingIndex >= 0; bleedingIndex--)
                {
                    int bleedingEntity = info.BleedingEntities[bleedingIndex];
                    if (!_ecsWorld.IsEntityExists(bleedingEntity))
                    {
                        info.BleedingEntities.RemoveAt(bleedingIndex);
                        continue;
                    }

                    var bleeding = _ecsWorld.GetComponent<BleedingComponent>(bleedingEntity);
                    if (bleeding == null)
                    {
                        info.BleedingEntities.RemoveAt(bleedingIndex);
                        continue;
                    }

                    float delta = GswExtensions.GetDeltaTime();
                    if (delta <= 0) continue;

                    bleedingDamage += bleeding.Severity * delta;
                    bleeding.Severity -= info.BleedingHealRate / HEAL_RATE_SLOWER * delta;
                    if (bleeding.Severity > 0) continue;

#if DEBUG
                    _logger.MakeLog($"Bleeding {bleedingEntity} on Entity ({pedEntity}) was healed");
#endif
                    _ecsWorld.RemoveComponent<BleedingComponent>(bleedingEntity);
                    info.BleedingEntities.RemoveAt(bleedingIndex);
                }

                if (bleedingDamage <= 0) continue;
                
                health.Health -= bleedingDamage;
                ped.SetHealth(health.Health);
            }
        }
    }
}