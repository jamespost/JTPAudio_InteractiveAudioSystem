using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using GAS; // Added for smart detection

namespace Combat
{
    /// <summary>
    /// A component that manages a collider for melee hit detection.
    /// It should be attached to the weapon or limb object.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitboxComponent : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, the hitbox will be active by default (useful for testing).")]
        public bool alwaysActive = false;
        
        [Tooltip("If true, tries to find the root entity (Rigidbody/Health/ASC) to prevent double-hits on multi-collider objects.")]
        public bool smartHitDetection = true;

        [Tooltip("Color of the hitbox when inactive.")]
        public Color inactiveColor = new Color(0, 1, 0, 0.3f);
        
        [Tooltip("Color of the hitbox when active.")]
        public Color activeColor = new Color(1, 0, 0, 0.5f);

        [Header("Events")]
        public UnityEvent<Collider> OnHit;

        private Collider _collider;
        private bool _isActive = false;
        
        // We track objects (GameObjects) instead of Colliders if smart detection is on
        private HashSet<GameObject> _hitObjects = new HashSet<GameObject>();

        public bool IsActive => _isActive;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            if (_collider == null)
            {
                Debug.LogError($"[HitboxComponent] No Collider found on {gameObject.name}!");
                return;
            }

            // Safety check: If this is on the same object as a Rigidbody, it might break physics movement
            if (GetComponent<Rigidbody>() != null && !GetComponent<Rigidbody>().isKinematic)
            {
                Debug.LogWarning($"[HitboxComponent] Attached to {gameObject.name} which also has a Rigidbody. " +
                                 "Setting this collider to Trigger might cause the object to fall through the ground. " +
                                 "Consider moving the HitboxComponent to a child GameObject.");
            }

            _collider.isTrigger = true; // Ensure it's a trigger
            _collider.enabled = false; // Start disabled unless alwaysActive is true
            
            if (alwaysActive)
            {
                Activate();
            }
        }

        private void Reset()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null) _collider.isTrigger = true;
        }

        public void Activate()
        {
            _isActive = true;
            _collider.enabled = true;
            _hitObjects.Clear();
        }

        public void Deactivate()
        {
            _isActive = false;
            _collider.enabled = false;
            _hitObjects.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            GameObject hitEntity = ResolveHitEntity(other);

            // Check if we've already hit this entity in this swing
            if (_hitObjects.Contains(hitEntity)) return;

            _hitObjects.Add(hitEntity);
            OnHit?.Invoke(other);
            
            // Debug visual
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[Hitbox] {_collider.name} hit {other.name} (Entity: {hitEntity.name})");
            }
        }

        private GameObject ResolveHitEntity(Collider col)
        {
            if (!smartHitDetection) return col.gameObject;

            // 1. Try Rigidbody (Standard Unity Physics Root)
            if (col.attachedRigidbody != null) return col.attachedRigidbody.gameObject;

            // 2. Try GAS AbilitySystemComponent (Project Specific)
            var asc = col.GetComponentInParent<AbilitySystemComponent>();
            if (asc != null) return asc.gameObject;

            // 3. Try Health Component (Legacy/Simple)
            var health = col.GetComponentInParent<Health>();
            if (health != null) return health.gameObject;

            // 4. Fallback to the object itself
            return col.gameObject;
        }

        private void OnDrawGizmos()
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_collider == null) return;
            
            Gizmos.color = _isActive || alwaysActive ? activeColor : inactiveColor;
            DrawGizmoShape(true);
        }

        private void OnDrawGizmosSelected()
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_collider == null) return;

            // Draw a solid wireframe when selected so it's very easy to see
            Gizmos.color = _isActive || alwaysActive ? Color.red : Color.green;
            DrawGizmoShape(false);
        }

        private void DrawGizmoShape(bool solid)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (_collider is BoxCollider box)
            {
                if (solid) Gizmos.DrawCube(box.center, box.size);
                else Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_collider is SphereCollider sphere)
            {
                if (solid) Gizmos.DrawSphere(sphere.center, sphere.radius);
                else Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (_collider is CapsuleCollider capsule)
            {
                if (solid) Gizmos.DrawWireSphere(capsule.center, capsule.radius); // Capsule solid not supported easily
                else Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
            else if (_collider is MeshCollider mesh)
            {
                if (mesh.sharedMesh != null)
                {
                    if (solid) Gizmos.DrawMesh(mesh.sharedMesh);
                    else Gizmos.DrawWireMesh(mesh.sharedMesh);
                }
            }
        }
    }
}
