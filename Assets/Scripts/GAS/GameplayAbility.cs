using UnityEngine;
using System.Collections.Generic;

namespace GAS
{
    public abstract class GameplayAbility : ScriptableObject
    {
        [Header("Config")]
        public string AbilityName;
        [TextArea] public string Description;

        [Header("Costs & Cooldowns")]
        public GameplayEffect Cost;
        public GameplayEffect Cooldown;

        [Header("Tags")]
        public GameplayTagContainer ActivationRequiredTags;
        public GameplayTagContainer ActivationBlockedTags;

        /// <summary>
        /// Checks if the ability can be activated (Costs, Cooldowns, Tags).
        /// </summary>
        public virtual bool CanActivate(AbilitySystemComponent asc)
        {
            if (asc == null) return false;

            // 1. Check Tags
            if (ActivationRequiredTags != null && ActivationRequiredTags.Tags.Count > 0)
            {
                if (!asc.TagContainer.HasAll(ActivationRequiredTags.Tags)) return false;
            }

            if (ActivationBlockedTags != null && ActivationBlockedTags.Tags.Count > 0)
            {
                if (asc.TagContainer.HasAny(ActivationBlockedTags.Tags)) return false;
            }

            // 2. Check Cooldowns (TODO: Requires Duration Effect tracking on ASC)
            
            // 3. Check Costs (Simplified: We assume we can always pay for now, or implement specific checks later)
            
            return true;
        }

        /// <summary>
        /// The entry point for activating the ability.
        /// </summary>
        public void Activate(AbilitySystemComponent asc)
        {
            if (!CanActivate(asc)) return;

            CommitAbility(asc);
            OnActivate(asc);
        }

        /// <summary>
        /// Applies Cost and Cooldown effects.
        /// </summary>
        protected virtual void CommitAbility(AbilitySystemComponent asc)
        {
            if (Cost != null)
            {
                asc.ApplyGameplayEffectToSelf(Cost, asc);
            }

            if (Cooldown != null)
            {
                asc.ApplyGameplayEffectToSelf(Cooldown, asc);
            }
        }

        /// <summary>
        /// The actual logic of the ability. Override this.
        /// </summary>
        protected abstract void OnActivate(AbilitySystemComponent asc);

        /// <summary>
        /// Call this when the ability finishes.
        /// </summary>
        protected virtual void EndAbility(AbilitySystemComponent asc)
        {
            // Cleanup logic if needed
        }
    }
}
