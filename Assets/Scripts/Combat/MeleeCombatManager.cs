using UnityEngine;
using System.Collections.Generic;
using GAS;

namespace Combat
{
    /// <summary>
    /// Manages melee hitboxes and handles hit registration.
    /// Listens for Animation Events to activate/deactivate hitboxes.
    /// Integrates with GAS to apply effects on hit.
    /// </summary>
    [RequireComponent(typeof(AbilitySystemComponent))]
    public class MeleeCombatManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("List of hitboxes managed by this component.")]
        public List<HitboxComponent> hitboxes = new List<HitboxComponent>();

        [Header("Debug")]
        public bool debugMode = false;

        private AbilitySystemComponent _asc;
        private GameplayEffect _currentDamageEffect;
        private float _currentDamageAmount = 0f;

        private void Awake()
        {
            _asc = GetComponent<AbilitySystemComponent>();
            
            // Auto-register hitboxes if they are children and not assigned
            if (hitboxes.Count == 0)
            {
                GetComponentsInChildren(true, hitboxes);
            }

            // Subscribe to hitbox events
            foreach (var hitbox in hitboxes)
            {
                hitbox.OnHit.AddListener(HandleHit);
            }
        }

        private void OnDestroy()
        {
            foreach (var hitbox in hitboxes)
            {
                if (hitbox != null) hitbox.OnHit.RemoveListener(HandleHit);
            }
        }

        /// <summary>
        /// Editor Helper: Creates a child GameObject with a HitboxComponent and BoxCollider.
        /// Use this to create independent hitboxes you can move/rotate/scale.
        /// </summary>
        [ContextMenu("Create Hitbox Child")]
        public void CreateHitboxChild()
        {
            GameObject go = new GameObject("NewHitbox");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 1, 1); // Default slightly forward/up
            
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            // Default to a smaller size (0.5m) to avoid overwhelming small models
            col.size = new Vector3(0.5f, 0.5f, 0.5f);
            
            var hitbox = go.AddComponent<HitboxComponent>();
            
            // Register immediately if in Editor
            if (!hitboxes.Contains(hitbox))
            {
                hitboxes.Add(hitbox);
            }
            
            Debug.Log($"[MeleeCombatManager] Created new Hitbox child on {name}. Select 'NewHitbox' to position it.");
        }

        /// <summary>
        /// Called by the Ability before the attack starts to define what happens on hit.
        /// </summary>
        public void SetCurrentAttackPayload(GameplayEffect effect, float damageAmount)
        {
            _currentDamageEffect = effect;
            _currentDamageAmount = damageAmount;
        }

        /// <summary>
        /// Animation Event: Activates a named hitbox.
        /// </summary>
        public void ActivateHitbox(string hitboxName)
        {
            var hitbox = hitboxes.Find(h => h.name == hitboxName || h.gameObject.name == hitboxName);
            if (hitbox != null)
            {
                hitbox.Activate();
                if (debugMode) Debug.Log($"[MeleeCombatManager] Activated hitbox: {hitboxName}");
            }
            else
            {
                if (debugMode) Debug.LogWarning($"[MeleeCombatManager] Could not find hitbox: {hitboxName}");
            }
        }

        /// <summary>
        /// Animation Event: Deactivates a named hitbox.
        /// </summary>
        public void DeactivateHitbox(string hitboxName)
        {
            var hitbox = hitboxes.Find(h => h.name == hitboxName || h.gameObject.name == hitboxName);
            if (hitbox != null)
            {
                hitbox.Deactivate();
                if (debugMode) Debug.Log($"[MeleeCombatManager] Deactivated hitbox: {hitboxName}");
            }
        }

        /// <summary>
        /// Animation Event: Deactivates all hitboxes (safety cleanup).
        /// </summary>
        public void DeactivateAllHitboxes()
        {
            foreach (var hitbox in hitboxes)
            {
                hitbox.Deactivate();
            }
        }

        private void HandleHit(Collider other)
        {
            // Check if target has GAS
            var targetASC = other.GetComponent<AbilitySystemComponent>();
            if (targetASC != null)
            {
                // Priority: Direct Attribute Modification (to support dynamic damage from EnemyData)
                // This is a temporary solution until SetByCaller magnitudes are implemented in the GAS system.
                var healthAttr = targetASC.AttributeSet.GetAttribute("Health");
                if (healthAttr != null && _currentDamageAmount > 0)
                {
                    // Don't damage if already dead
                    if (healthAttr.CurrentValue <= 0) return;

                    healthAttr.CurrentValue -= _currentDamageAmount;
                    if (debugMode) Debug.Log($"[MeleeCombatManager] Applied {_currentDamageAmount} direct damage to {other.name}");
                }
                else if (_currentDamageEffect != null)
                {
                    // Fallback to fixed Effect
                    _asc.ApplyGameplayEffectToTarget(_currentDamageEffect, targetASC);
                    if (debugMode) Debug.Log($"[MeleeCombatManager] Applied {_currentDamageEffect.name} to {other.name}");
                }
            }
            else
            {
                // Fallback for non-GAS targets (e.g. simple Health component)
                var health = other.GetComponent<Health>();
                if (health != null)
                {
                    float damage = _currentDamageAmount > 0 ? _currentDamageAmount : 10f;
                    health.TakeDamage(damage);
                    
                    if (debugMode) Debug.Log($"[MeleeCombatManager] Applied {damage} damage to {other.name} (Legacy Health)");
                }
            }
        }
    }
}
