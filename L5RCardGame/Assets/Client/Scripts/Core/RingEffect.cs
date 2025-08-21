using System.Linq;
using UnityEngine;

namespace L5RGame.Client.Scripts.Core
{
    public class RingEffect : Effect
    {
        public RingEffect(Game game, BaseCard source, EffectProperties properties, IEffect effect) 
            : base(game, source, properties, effect)
        {
        }

        public override object[] GetTargets()
        {
            return Game.Rings
                .Where(ring => Match(ring, Context))
                .Cast<object>()
                .ToArray();
        }
    }
}
