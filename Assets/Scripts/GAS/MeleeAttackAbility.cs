using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(menuName = "GAS/Abilities/Melee Attack")]
    public class MeleeAttackAbility : GameplayAbility
    {
        [Header("Settings")]
        [Tooltip("Default damage if EnemyData is not found.")]
        public float DefaultDamage = 10f;
        [Tooltip("Range to check for the player if target is not explicitly passed (fallback).")]
        public float Range = 3.0f;
        
        [Header("Effects")]
        public GameplayEffect DamageEffect;

        protected override void OnActivate(AbilitySystemComponent asc)
        {
            // 1. Determine Damage
            float damage = DefaultDamage;
            
            // Try to get damage from EnemyTeefAI (specific)
            var teefAI = asc.GetComponent<EnemyTeefAI>();
            if (teefAI != null && teefAI.enemyData != null)
            {
                damage = teefAI.enemyData.attackDamage;
            }
            // Try to get damage from generic EnemyAI (generic)
            else
            {
                var enemyAI = asc.GetComponent<EnemyAI>();
                if (enemyAI != null && enemyAI.enemyData != null)
                {
                    damage = enemyAI.enemyData.attackDamage;
                }
            }

            // 2. Find Target
            // Since we don't have an EventData system yet to pass the collision target,
            // we will look for the player within range.
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            
            if (target != null)
            {
                float distance = Vector3.Distance(asc.transform.position, target.transform.position);
                
                // We use a slightly generous range to account for the collision that just happened
                if (distance <= Range)
                {
                    ApplyDamage(asc, target, damage);
                }
            }

            EndAbility(asc);
        }

        private void ApplyDamage(AbilitySystemComponent sourceASC, GameObject target, float damageAmount)
        {
            // Priority 1: GAS Attribute
            var targetASC = target.GetComponent<AbilitySystemComponent>();
            if (targetASC != null && DamageEffect != null)
            {
                // Note: We need a way to set the magnitude of the effect dynamically if we want to use 'damageAmount'.
                // For now, if DamageEffect is a fixed "Damage" effect, it might have its own value.
                // If we want to use the EnemyData's damage, we might need to modify the effect spec or use a SetByCaller magnitude (advanced GAS).
                // For this simple implementation, we will assume the DamageEffect is configured with a base value, 
                // OR we manually modify the attribute if the effect doesn't support dynamic values yet.
                
                // SIMPLE APPROACH for Phase 4.2:
                // If we have a DamageEffect, apply it. 
                // Ideally, we would create a runtime spec and set the value.
                // Since our current ApplyGameplayEffectToTarget is simple, let's just apply it.
                // BUT, if we want to use the exact damage from EnemyData, we might need to bypass the Effect or update it.
                
                // Let's stick to the pattern established in FireWeaponAbility:
                // If we want to be precise with data-driven values from EnemyData, we might have to cheat a bit 
                // until we implement SetByCaller magnitudes.
                
                // "Cheat" / Direct Attribute Modification for now to respect EnemyData:
                var healthAttr = targetASC.AttributeSet.GetAttribute("Health");
                if (healthAttr != null)
                {
                    healthAttr.CurrentValue -= damageAmount;
                }
                else
                {
                    // Fallback to applying the effect if it exists (maybe it has a fixed value)
                    sourceASC.ApplyGameplayEffectToTarget(DamageEffect, targetASC);
                }
            }
            // Priority 2: Legacy Health Component
            else
            {
                var health = target.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damageAmount);
                }
            }
        }
    }
}
