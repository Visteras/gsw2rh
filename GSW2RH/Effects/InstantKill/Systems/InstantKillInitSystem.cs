using System.Xml.Linq;
using GunshotWound2.Utils;

namespace GunshotWound2.Effects.InstantKill.Systems
{
    public class InstantKillInitSystem : BaseEffectInitSystem
    {
        public InstantKillInitSystem() : base(new GswLogger(typeof(InstantKillInitSystem)))
        {
        }

        protected override void CheckPart(XElement partRoot, int partEntity)
        {
            XElement element = partRoot.Element("InstantKill");
            if (element == null) return;

            EcsWorld
                .AddComponent<InstantKillComponent>(partEntity)
                .ReasonKey = element.GetAttributeValue("ReasonKey");
        }
    }
}