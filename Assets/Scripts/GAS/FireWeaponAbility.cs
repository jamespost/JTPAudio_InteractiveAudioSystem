using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(menuName = "GAS/Abilities/Fire Weapon")]
    public class FireWeaponAbility : GameplayAbility
    {
        [Header("Weapon Config")]
        [Tooltip("If null, will try to use WeaponData from the WeaponController on the actor.")]
        public WeaponData OverrideWeaponData;

        [Header("Effects")]
        public GameplayEffect DamageEffect;

        protected override void OnActivate(AbilitySystemComponent asc)
        {
            // 1. Get Context
            var weaponController = asc.GetComponent<WeaponController>();
            if (weaponController == null)
            {
                Debug.LogError("FireWeaponAbility requires a WeaponController on the ASC owner.");
                EndAbility(asc);
                return;
            }

            // 2. Resolve Data
            WeaponData data = OverrideWeaponData != null ? OverrideWeaponData : weaponController.weaponData;
            if (data == null)
            {
                Debug.LogError("No WeaponData found for FireWeaponAbility.");
                EndAbility(asc);
                return;
            }

            // 3. Check Ammo & Consume
            // We use the controller's method to handle ammo, recoil, and bloom state.
            if (!weaponController.TryConsumeAmmo(1))
            {
                // Failed to consume ammo (empty), so we don't fire.
                EndAbility(asc);
                return;
            }

            // 4. Perform Fire Logic
            PerformFire(asc, weaponController, data);

            EndAbility(asc);
        }

        private void PerformFire(AbilitySystemComponent asc, WeaponController controller, WeaponData data)
        {
            Transform firePoint = controller.firePoint;
            if (firePoint == null) return;

            // Calculate Spread (Simplified)
            Vector3 spreadDirection = firePoint.forward;
            
            // Raycast
            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, spreadDirection, out hit, data.range, controller.hitLayers))
            {
                // Apply Damage
                // Priority 1: Legacy Health Component
                Health health = hit.collider.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(data.damage);
                }
                
                // Priority 2: GAS Attribute (Future proofing)
                var targetASC = hit.collider.GetComponent<AbilitySystemComponent>();
                if (targetASC != null)
                {
                    // In the future, we would apply a GameplayEffect here.
                    // For now, we can manually decrement health if we wanted to support pure GAS entities.
                    // var healthAttr = targetASC.AttributeSet.GetAttribute("Health");
                    // if (healthAttr != null) healthAttr.CurrentValue -= data.damage;
                }

                // Visuals & Audio
                if (data.fireSound != null)
                {
                    AudioManager.Instance.PostEvent(data.fireSound.eventID, controller.gameObject);
                }
                
                if (data.impactSound != null)
                {
                    AudioManager.Instance.PostEvent(data.impactSound.eventID, hit.collider.gameObject);
                }
            }
        }
    }
}
