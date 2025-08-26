using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base effect class without networking for mobile-only version
    /// Alternative to NetworkBehaviour for mobile builds
    /// </summary>
    [System.Serializable]
    public class Effect : MonoBehaviour
    {
        [Header("Effect Configuration")]
        public string effectName;
        public bool isActive = false;
        public float duration = 0f;
        public string targetType = "";
        
        // Replace SyncVar with regular properties for mobile
        private string syncedEffectName;
        private bool syncedIsActive;
        
        public virtual string EffectName 
        { 
            get => effectName; 
            set => effectName = value; 
        }
        
        public virtual bool IsActive 
        { 
            get => isActive; 
            set => isActive = value; 
        }
        
        /// <summary>
        /// Initialize effect (replaces network spawn)
        /// </summary>
        public virtual void Initialize(string name, BaseCard card, Game game)
        {
            effectName = name;
            // Initialize effect without networking
        }
        
        /// <summary>
        /// Apply effect to target
        /// </summary>
        public virtual bool CanAffect(object target)
        {
            return target != null;
        }
        
        /// <summary>
        /// Execute effect logic
        /// </summary>
        public virtual void Apply(object target)
        {
            isActive = true;
            // Effect application logic
        }
        
        /// <summary>
        /// Remove effect
        /// </summary>
        public virtual void Remove()
        {
            isActive = false;
            // Effect removal logic
        }
        
        /// <summary>
        /// Update effect state (called each frame if needed)
        /// </summary>
        protected virtual void UpdateEffect()
        {
            if (isActive && duration > 0)
            {
                duration -= Time.deltaTime;
                if (duration <= 0)
                {
                    Remove();
                }
            }
        }
        
        void Update()
        {
            if (isActive)
            {
                UpdateEffect();
            }
        }
    }
}