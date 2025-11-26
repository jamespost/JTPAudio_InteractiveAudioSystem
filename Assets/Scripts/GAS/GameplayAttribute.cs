using System;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayAttribute
    {
        [SerializeField] private float baseValue;
        [SerializeField] private float currentValue;

        public float BaseValue => baseValue;
        public float CurrentValue 
        {
            get => currentValue;
            set 
            {
                if (Mathf.Abs(currentValue - value) > Mathf.Epsilon)
                {
                    currentValue = value;
                    OnAttributeChanged?.Invoke(this);
                }
            }
        }

        public event Action<GameplayAttribute> OnAttributeChanged;

        public GameplayAttribute(float value)
        {
            baseValue = value;
            currentValue = value;
        }
        
        public void SetBaseValue(float value)
        {
            baseValue = value;
            // In a full system, we'd re-aggregate modifiers here.
            // For now, we'll just reset current to base if no modifiers exist (simplified).
            currentValue = value; 
        }
    }
}
