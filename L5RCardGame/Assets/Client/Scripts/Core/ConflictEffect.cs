using System;
using UnityEngine;

namespace L5RGame.Client.Scripts.Core
{
    public class ConflictEffect : Effect
    {
        public ConflictEffect(Game game, BaseCard source, EffectProperties properties, IEffect effect) 
            : base(game, source, properties, effect)
        {
            // Override any erroneous match passed through properties
            properties.Match = (conflict, context) => true;
        }

        public override object[] GetTargets()
        {
            return Game.CurrentConflict != null ? new object[] { Game.CurrentConflict } : new object[0];
        }
    }
}
