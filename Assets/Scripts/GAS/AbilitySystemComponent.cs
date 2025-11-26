using UnityEngine;
using System.Collections.Generic;

namespace GAS
{
    public class AbilitySystemComponent : MonoBehaviour
    {
        [SerializeField] private AttributeSet attributeSet;
        [SerializeField] private GameplayTagContainer tagContainer = new GameplayTagContainer();
        [SerializeField] private List<GameplayAbility> grantedAbilities = new List<GameplayAbility>();

        public AttributeSet AttributeSet => attributeSet;
        public GameplayTagContainer TagContainer => tagContainer;

        private void Awake()
        {
            if (attributeSet == null)
            {
                attributeSet = GetComponent<AttributeSet>();
            }
            
            if (attributeSet == null)
            {
                Debug.LogError($"AbilitySystemComponent on {gameObject.name} is missing an AttributeSet!");
            }
        }

        /// <summary>
        /// Applies a GameplayEffect to this ASC (Self).
        /// </summary>
        public void ApplyGameplayEffectToSelf(GameplayEffect effect, AbilitySystemComponent source)
        {
            if (effect == null) return;
            ApplyGameplayEffectSpec(effect, source, this);
        }

        /// <summary>
        /// Applies a GameplayEffect to a target ASC.
        /// </summary>
        public void ApplyGameplayEffectToTarget(GameplayEffect effect, AbilitySystemComponent target)
        {
            if (effect == null || target == null) return;
            target.ApplyGameplayEffectSpec(effect, this, target);
        }

        private void ApplyGameplayEffectSpec(GameplayEffect effect, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            // In a full system, we would check 'Application Requirements' (Tags) here.

            if (effect.DurationType == GameplayEffectDurationType.Instant)
            {
                ExecuteInstantEffect(effect);
            }
            else
            {
                // TODO: Implement ActiveGameplayEffects for Duration/Infinite policies.
                // This would involve creating a runtime wrapper class, adding it to a list, 
                // applying modifiers (if any), granting tags, and starting a timer/coroutine.
                Debug.LogWarning($"Duration/Infinite effects are not yet fully implemented. Effect '{effect.name}' was ignored.");
            }
        }

        private void ExecuteInstantEffect(GameplayEffect effect)
        {
            foreach (var mod in effect.Modifiers)
            {
                var attr = attributeSet.GetAttribute(mod.AttributeName);
                if (attr != null)
                {
                    float val = attr.CurrentValue;
                    switch (mod.Operation)
                    {
                        case AttributeModifierOp.Add:
                            val += mod.Value;
                            break;
                        case AttributeModifierOp.Multiply:
                            val *= mod.Value;
                            break;
                        case AttributeModifierOp.Override:
                            val = mod.Value;
                            break;
                    }
                    attr.CurrentValue = val;
                    // Debug.Log($"Attribute {mod.AttributeName} changed to {attr.CurrentValue}");
                }
            }
        }

        public float GetAttributeValue(string attributeName)
        {
            return attributeSet != null ? attributeSet.GetAttributeValue(attributeName) : 0f;
        }
        
        public bool HasTag(GameplayTag tag)
        {
            return tagContainer.HasTag(tag);
        }

        public void GrantAbility(GameplayAbility ability)
        {
            if (ability != null && !grantedAbilities.Contains(ability))
            {
                grantedAbilities.Add(ability);
            }
        }

        public void RevokeAbility(GameplayAbility ability)
        {
            if (grantedAbilities.Contains(ability))
            {
                grantedAbilities.Remove(ability);
            }
        }

        public bool TryActivateAbility(GameplayAbility ability)
        {
            if (grantedAbilities.Contains(ability))
            {
                if (ability.CanActivate(this))
                {
                    ability.Activate(this);
                    return true;
                }
            }
            return false;
        }

        public bool TryActivateAbilityByName(string abilityName)
        {
            var ability = grantedAbilities.Find(a => a.AbilityName == abilityName);
            if (ability != null)
            {
                return TryActivateAbility(ability);
            }
            return false;
        }
    }
}
