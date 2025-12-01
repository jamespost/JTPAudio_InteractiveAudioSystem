using System.Collections.Generic;
using UnityEngine;

namespace JTPAudio.VFX
{
    /// <summary>
    /// Handles realistic ballistic impact effects including dust jets, chunky debris, and physics interactions.
    /// Scales effect intensity based on projectile damage.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BallisticImpactVFX : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float baseLifetime = 2.0f;
        [SerializeField] private bool usePhysicsForDebris = true;
        
        [Header("Scaling")]
        [SerializeField] private float minDamageRef = 10f;
        [SerializeField] private float maxDamageRef = 100f;
        [SerializeField] private float minScaleMultiplier = 0.5f;
        [SerializeField] private float maxScaleMultiplier = 2.0f;

        [Header("Materials (Optional)")]
        [Tooltip("Material for the chunky debris particles.")]
        [SerializeField] private Material debrisMaterial;
        [Tooltip("Material for the dust/smoke particles.")]
        [SerializeField] private Material dustMaterial;
        [Tooltip("Material for the spark particles.")]
        [SerializeField] private Material sparkMaterial;
        [Tooltip("Material for the muzzle flash/impact flash.")]
        [SerializeField] private Material flashMaterial;

        // Child systems
        private ParticleSystem mainDustPS;
        private ParticleSystem dustJetPS;
        private ParticleSystem debrisPS;
        private ParticleSystem sparksPS;
        private ParticleSystem flashPS;

        private List<ParticleSystem> allSystems = new List<ParticleSystem>();
        private float currentDamageScale = 1.0f;

        private class SystemState
        {
            public float startSizeMultiplier;
            public float startSpeedMultiplier;
            public ParticleSystem.Burst[] bursts;
        }

        private Dictionary<ParticleSystem, SystemState> initialStates = new Dictionary<ParticleSystem, SystemState>();

        private void Awake()
        {
            InitializeSystems();
            CaptureInitialState();
        }

        private void CaptureInitialState()
        {
            foreach (var ps in allSystems)
            {
                if (ps == null) continue;

                var state = new SystemState();
                var main = ps.main;
                state.startSizeMultiplier = main.startSizeMultiplier;
                state.startSpeedMultiplier = main.startSpeedMultiplier;

                var emission = ps.emission;
                state.bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(state.bursts);

                initialStates[ps] = state;
            }
        }

        private void InitializeSystems()
        {
            // Main system is the general dust cloud
            mainDustPS = GetComponent<ParticleSystem>();
            if (dustMaterial != null) mainDustPS.GetComponent<ParticleSystemRenderer>().material = dustMaterial;
            allSystems.Add(mainDustPS);

            // Create or find child systems
            dustJetPS = GetOrCreateChildSystem("DustJet", SetupDustJetSystem);
            debrisPS = GetOrCreateChildSystem("Debris", SetupDebrisSystem);
            sparksPS = GetOrCreateChildSystem("Sparks", SetupSparksSystem);
            flashPS = GetOrCreateChildSystem("Flash", SetupFlashSystem);
        }

        private ParticleSystem GetOrCreateChildSystem(string name, System.Action<ParticleSystem> setupAction)
        {
            Transform child = transform.Find(name);
            ParticleSystem ps;
            if (child == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                ps = go.AddComponent<ParticleSystem>();
                
                // Ensure system is stopped before configuration to avoid "Setting the duration while system is still playing" error
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.playOnAwake = false;

                setupAction?.Invoke(ps);
            }
            else
            {
                ps = child.GetComponent<ParticleSystem>();
                if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();
                
                // If we found an existing one, we might still want to apply materials if they are set on this script
                ApplyMaterial(ps, name);
            }
            
            allSystems.Add(ps);
            return ps;
        }

        private void ApplyMaterial(ParticleSystem ps, string systemName)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (systemName == "Debris" && debrisMaterial != null) renderer.material = debrisMaterial;
            else if (systemName == "DustJet" && dustMaterial != null) renderer.material = dustMaterial;
            else if (systemName == "Sparks" && sparkMaterial != null) renderer.material = sparkMaterial;
            else if (systemName == "Flash" && flashMaterial != null) renderer.material = flashMaterial;
        }

        private void SetupDustJetSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f); // Faster initial burst
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.gravityModifier = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15, 25) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.05f;

            // Add drag to make it "puff" out and stop
            var limitVelocity = ps.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.dampen = 0.15f;
            limitVelocity.limit = 2f;

            // Fade out and grow
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.5f);
            curve.AddKey(1.0f, 1.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (dustMaterial != null) renderer.material = dustMaterial;
            else renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        }

        private void SetupDebrisSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.gravityModifier = 1.5f; // Heavier feel
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 4, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f; // Wider spread
            shape.radius = 0.1f;

            // Add tumbling rotation
            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            if (usePhysicsForDebris)
            {
                var collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.dampen = 0.6f;
                collision.bounce = 0.4f;
                collision.quality = ParticleSystemCollisionQuality.Medium;
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            
            // Try to create a primitive cube mesh if we can, or just leave it as billboard if no mesh is assigned.
            // Since we can't easily load default resources without knowing the path or GUID, we'll rely on the user assigning a mesh in the inspector
            // OR we can fallback to billboard if no mesh is assigned, but set it to a small square.
            // Ideally, we'd use GameObject.CreatePrimitive to get a mesh, but that creates a GO in the scene.
            // For now, let's stick to billboard if no material/mesh is provided, but make them look like small chunks.
            
            if (debrisMaterial != null) 
            {
                renderer.material = debrisMaterial;
            }
            else 
            {
                renderer.material = new Material(Shader.Find("Standard")); 
                renderer.renderMode = ParticleSystemRenderMode.Billboard; // Fallback
            }
        }

        private void SetupSparksSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); // Shorter life
            main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f); // Faster
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.05f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 10f; // Longer streaks
            renderer.velocityScale = 0.1f;
            
            if (sparkMaterial != null) renderer.material = sparkMaterial;
            else 
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.5f, 0.1f));
            }
        }

        private void SetupFlashSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.05f;
            main.loop = false;
            main.startLifetime = 0.05f; // Very short
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new Color(1f, 0.95f, 0.8f, 0.8f); // Brighter

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            
            if (flashMaterial != null) renderer.material = flashMaterial;
            else renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        }

        public void ScaleEffect(float damage)
        {
            // Calculate scale factor based on damage
            float t = Mathf.InverseLerp(minDamageRef, maxDamageRef, damage);
            currentDamageScale = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
            float speedScale = Mathf.Lerp(1f, 1.5f, t);

            // Apply to all systems
            foreach (var ps in allSystems)
            {
                if (ps == null) continue;
                
                // Ensure we have initial state
                if (!initialStates.ContainsKey(ps)) continue;
                var state = initialStates[ps];

                var main = ps.main;
                
                // Reset to initial then scale
                main.startSizeMultiplier = state.startSizeMultiplier * currentDamageScale;
                main.startSpeedMultiplier = state.startSpeedMultiplier * speedScale;

                // Scale emission count
                var emission = ps.emission;
                ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[state.bursts.Length];
                
                for (int i = 0; i < state.bursts.Length; i++)
                {
                    bursts[i] = state.bursts[i]; // Copy original burst settings
                    
                    // Scale the counts
                    float min = state.bursts[i].count.constantMin;
                    float max = state.bursts[i].count.constantMax;
                    
                    bursts[i].count = new ParticleSystem.MinMaxCurve(
                        min * currentDamageScale,
                        max * currentDamageScale
                    );
                }
                emission.SetBursts(bursts);
            }
        }

        private void OnEnable()
        {
            // Reset and play
            foreach (var ps in allSystems)
            {
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }
            }
        }
    }
}
