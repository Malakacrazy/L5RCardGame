using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace L5RGame.Client.Scripts.Core
{
    public class DynamicEffect : StaticEffect
    {
        private Dictionary<string, object> values;
        private Func<object, AbilityContext, object> calculate;

        public DynamicEffect(string type, Func<object, AbilityContext, object> calculate) 
            : base(type)
        {
            this.values = new Dictionary<string, object>();
            this.calculate = calculate;
        }

        public override void Apply(object target)
        {
            base.Apply(target);
            Recalculate(target);
        }

        public bool Recalculate(object target)
        {
            var card = target as BaseCard;
            if (card == null) return false;

            var oldValue = GetValue(target);
            var newValue = SetValue(target, calculate(target, Context));

            if (oldValue is Func<object, object> oldFunc && newValue is Func<object, object> newFunc)
            {
                return oldFunc.ToString() != newFunc.ToString();
            }

            if (oldValue is Array oldArray && newValue is Array newArray)
            {
                return JsonConvert.SerializeObject(oldArray) != JsonConvert.SerializeObject(newArray);
            }

            return !Equals(oldValue, newValue);
        }

        public object GetValue(object target)
        {
            var card = target as BaseCard;
            if (card != null && values.ContainsKey(card.Uuid))
            {
                return values[card.Uuid];
            }
            return null;
        }

        public object SetValue(object target, object value)
        {
            var card = target as BaseCard;
            if (card != null)
            {
                values[card.Uuid] = value;
            }
            return value;
        }
    }
}
