using System;
using GunshotWound2.GswWorld;
using GunshotWound2.Utils;
using Leopotam.Ecs;
using Rage;
using Rage.Native;

namespace GunshotWound2.WoundEffects.FacialAnimation.Systems
{
    [EcsInject]
    public class FacialAnimationSystem : BaseEffectSystem
    {
        private EcsFilter<GswPedComponent, PermanentFacialAnimationComponent> _permanentAnimPeds;

        private static readonly Random Random = new Random();

        public FacialAnimationSystem() : base(new GswLogger(typeof(FacialAnimationSystem)))
        {
        }

        protected override void PrepareRunActions()
        {
            foreach (int i in _permanentAnimPeds)
            {
                Ped ped = _permanentAnimPeds.Components1[i].ThisPed;
                if (!ped.Exists()) continue;

                PermanentFacialAnimationComponent animation = _permanentAnimPeds.Components2[i];
                NativeFunction.Natives.PLAY_FACIAL_ANIM(ped, animation.Name, animation.Dict);
            }
        }

        protected override void ResetEffect(Ped ped, int pedEntity)
        {
            var permanent = EcsWorld.GetComponent<PermanentFacialAnimationComponent>(pedEntity);
            if (permanent == null) return;

            EcsWorld.RemoveComponent<PermanentFacialAnimationComponent>(pedEntity);
        }

        protected override void ProcessWound(Ped ped, int pedEntity, int woundEntity)
        {
            var enable = EcsWorld.GetComponent<EnableFacialAnimationComponent>(woundEntity);
            if (enable != null)
            {
                string animationName = Random.NextFromList(enable.Animations);
                string dict = ped.IsMale
                    ? enable.MaleDict
                    : enable.FemaleDict;

                if (enable.Permanent)
                {
                    var permanent = EcsWorld.EnsureComponent<PermanentFacialAnimationComponent>(pedEntity, out _);
                    permanent.Name = animationName;
                    permanent.Dict = dict;
#if DEBUG
                    Logger.MakeLog($"{ped.Name(pedEntity)} got permanent facial animation {animationName}");
#endif
                }
                else
                {
                    NativeFunction.Natives.PLAY_FACIAL_ANIM(ped, animationName, dict);
#if DEBUG
                    Logger.MakeLog($"{ped.Name(pedEntity)} got short facial animation {animationName}");
#endif
                }
            }

            var disable = EcsWorld.GetComponent<DisableFacialAnimationComponent>(woundEntity);
            if (disable != null)
            {
                EcsWorld.RemoveComponent<PermanentFacialAnimationComponent>(pedEntity, true);
            }
        }
    }
}