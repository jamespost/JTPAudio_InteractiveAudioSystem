using UnityEngine;
using Combat;

namespace GAS
{
    [CreateAssetMenu(menuName = "GAS/Abilities/Melee Attack")]
    public class MeleeAttackAbility : GameplayAbility
    {
        [Header("Settings")]
        [Tooltip("Default damage if EnemyData is not found.")]
        public float DefaultDamage = 10f;
        
        [Header("Animation")]
        public bool UseAnimation = true;
        public string AnimationTriggerName = "Attack";

        [Header("Programmatic (No Animation)")]
        [Tooltip("Name of the hitbox to activate if not using animation events.")]
        public string HitboxName;
        public float HitboxStartDelay = 0f;
        public float HitboxActiveDuration = 0.5f;

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

            // 2. Setup Combat Manager
            var combatManager = asc.GetComponent<MeleeCombatManager>();
            if (combatManager != null)
            {
                combatManager.SetCurrentAttackPayload(DamageEffect, damage);
            }
            else
            {
                Debug.LogWarning($"MeleeAttackAbility: No MeleeCombatManager found on {asc.name}. Hitboxes will not work.");
            }

            // 3. Trigger Animation or Programmatic Routine
            var animator = asc.GetComponent<Animator>();
            if (UseAnimation && animator != null)
            {
                animator.SetTrigger(AnimationTriggerName);
                EndAbility(asc); // End immediately, let animation events handle the rest
            }
            else
            {
                // Programmatic Fallback
                if (combatManager != null)
                {
                    asc.StartCoroutine(ProgrammaticAttackRoutine(combatManager, asc));
                }
                else
                {
                    EndAbility(asc);
                }
            }
        }

        private System.Collections.IEnumerator ProgrammaticAttackRoutine(MeleeCombatManager combatManager, AbilitySystemComponent asc)
        {
            if (HitboxStartDelay > 0)
            {
                yield return new WaitForSeconds(HitboxStartDelay);
            }

            combatManager.ActivateHitbox(HitboxName);

            yield return new WaitForSeconds(HitboxActiveDuration);

            combatManager.DeactivateHitbox(HitboxName);
            
            EndAbility(asc);
        }
    }
}
