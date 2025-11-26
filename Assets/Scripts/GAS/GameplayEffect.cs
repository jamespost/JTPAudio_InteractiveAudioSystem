using UnityEngine;
using System.Collections.Generic;

namespace GAS
{
    public enum GameplayEffectDurationType
    {
        Instant,
        Infinite,
        HasDuration
    }

    public enum AttributeModifierOp
    {
        Add,
        Multiply,
        Override
    }

    [System.Serializable]
    public struct GameplayEffectModifier
    {
        public string AttributeName;
        public AttributeModifierOp Operation;
        public float Value;
    }

    [CreateAssetMenu(menuName = "GAS/Gameplay Effect")]
    public class GameplayEffect : ScriptableObject
    {
        [Header("Duration")]
        public GameplayEffectDurationType DurationType;
        public float Duration; // Only used if HasDuration

        [Header("Modifiers")]
        public List<GameplayEffectModifier> Modifiers = new List<GameplayEffectModifier>();

        [Header("Tags")]
        public List<GameplayTag> GrantedTags = new List<GameplayTag>();
    }
}
