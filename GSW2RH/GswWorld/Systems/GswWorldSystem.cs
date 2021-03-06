﻿using System;
using System.Diagnostics;
using System.Drawing;
using GunshotWound2.Utils;
using Leopotam.Ecs;
using Rage;
using Rage.Native;

namespace GunshotWound2.GswWorld.Systems
{
    [EcsInject]
    public class GswWorldSystem : IEcsRunSystem
    {
        private EcsWorld _ecsWorld;

        private EcsFilter<GswWorldComponent> _world;
        private EcsFilter<GswPedComponent> _gswPeds;

        private EcsFilter<ForceCreateGswPedEvent> _forceCreateEvents;

        private readonly GswLogger _logger;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private static readonly Random Random = new Random();

        public GswWorldSystem()
        {
            _logger = new GswLogger(typeof(GswWorldSystem));
        }

        public void Run()
        {
            if (_world.EntitiesCount <= 0)
            {
                throw new Exception("World was not init!");
            }

            GswWorldComponent gswWorld = _world.Components1[0];
            bool detectingEnabled = gswWorld.HumanDetectingEnabled || gswWorld.AnimalDetectingEnabled;

            _stopwatch.Restart();
            if (gswWorld.NeedToCheckPeds.Count <= 0)
            {
                foreach (int i in _forceCreateEvents)
                {
                    Ped targetPed = _forceCreateEvents.Components1[i].TargetPed;
                    gswWorld.NeedToCheckPeds.Enqueue(targetPed);
                    gswWorld.ForceCreatePeds.Add(targetPed);
                    _ecsWorld.RemoveEntity(_forceCreateEvents.Entities[i]);
                }
                
                foreach (int i in _gswPeds)
                {
                    GswPedComponent gswPed = _gswPeds.Components1[i];

                    Ped ped = gswPed.ThisPed;
                    if (IsExistsAndAlive(ped)) continue;

                    int pedEntity = _gswPeds.Entities[i];
                    _ecsWorld.AddComponent<RemovedPedMarkComponent>(pedEntity);
                }

                if (detectingEnabled)
                {
                    foreach (Ped ped in World.GetAllPeds())
                    {
                        gswWorld.NeedToCheckPeds.Enqueue(ped);
                    }
                }
            }

            while (!TimeIsOver() && gswWorld.NeedToCheckPeds.Count > 0)
            {
                Ped ped = gswWorld.NeedToCheckPeds.Dequeue();
                if (IsNotExistsOrDead(ped)) continue;
                if (GswPedAlreadyExist(ped)) continue;

                bool forceCreatePed = gswWorld.ForceCreatePeds.Contains(ped);
                if (!forceCreatePed && IsNotDamaged(ped)) continue;

                int entity;
                if (ped.IsHuman && (forceCreatePed || gswWorld.HumanDetectingEnabled))
                {
                    entity = CreateHuman(gswWorld, ped);
                }
                else if (!ped.IsHuman && (forceCreatePed || gswWorld.AnimalDetectingEnabled))
                {
                    entity = CreateAnimal(gswWorld, ped);
                }
                else
                {
                    entity = -1;
                }

                if (!forceCreatePed) continue;
                
#if DEBUG
                _logger.MakeLog($"Ped {ped.Name(entity)} was force created");
#endif
                gswWorld.ForceCreatePeds.Remove(ped);
            }

#if DEBUG
            string pedCounter = "Peds: " + _gswPeds.EntitiesCount;
            pedCounter.ShowInGsw(0.165f, 0.94f, 0.25f, Color.White);

            string worldTime = "World Time: " + _stopwatch.ElapsedMilliseconds;
            worldTime.ShowInGsw(0.165f, 0.955f, 0.25f, Color.White);
#endif
            _stopwatch.Stop();
        }

        private bool TimeIsOver()
        {
            return _stopwatch.ElapsedMilliseconds > _world.Components1[0].MaxDetectTimeInMs;
        }

        private bool IsNotExistsOrDead(Ped ped)
        {
            return !ped.Exists() || ped.IsDead;
        }

        private bool IsExistsAndAlive(Ped ped)
        {
            return ped.Exists() && ped.IsAlive;
        }

        private bool GswPedAlreadyExist(Ped pedToCheck)
        {
            return _world.Components1[0].PedsToEntityDict.ContainsKey(pedToCheck);
        }

        private bool IsNotDamaged(Ped pedToCheck)
        {
            if (!_world.Components1[0].ScanOnlyDamaged) return false;

            bool damaged = NativeFunction.Natives.HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED<bool>(pedToCheck);
            return !damaged;
        }

        private int CreateHuman(GswWorldComponent gswWorld, Ped ped)
        {
            int entity = _ecsWorld.CreateEntityWith(out GswPedComponent gswPed, out NewPedMarkComponent _);
            gswPed.ThisPed = ped;

            if (!gswWorld.HumanAccuracy.IsDisabled())
            {
                ped.Accuracy = (int) Random.NextMinMax(gswWorld.HumanAccuracy);
            }

            if (!gswWorld.HumanShootRate.IsDisabled())
            {
                int rate = (int) Random.NextMinMax(gswWorld.HumanShootRate);
                NativeFunction.Natives.SET_PED_SHOOT_RATE(ped, rate);
            }

            gswWorld.PedsToEntityDict.Add(ped, entity);
            return entity;
        }

        private int CreateAnimal(GswWorldComponent gswWorld, Ped ped)
        {
            int entity = _ecsWorld.CreateEntityWith(
                out GswPedComponent gswPed, 
                out NewPedMarkComponent _,
                out AnimalMarkComponent _);
            gswPed.ThisPed = ped;

            gswWorld.PedsToEntityDict.Add(ped, entity);
            return entity;
        }
    }
}