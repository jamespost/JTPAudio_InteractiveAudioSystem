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

        private void Awake()
        {
            InitializeSystems();
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.gravityModifier = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f; // Narrow angle for "jet"
            shape.radius = 0.05f;

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
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.gravityModifier = 1.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 5, 10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;

            if (usePhysicsForDebris)
            {
                var collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.dampen = 0.5f;
                collision.bounce = 0.3f;
                collision.quality = ParticleSystemCollisionQuality.Medium;
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            // We would ideally assign a mesh and material here. 
            // For now, we'll rely on the default or what's set in the inspector if the user modifies the prefab.
            // If purely procedural, we'd need to load resources.
            // renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); // This often fails in runtime if not careful
            // Instead, let's default to billboard if no mesh is set, or assume the user will set it.
            // But for "chunky particles", we really want meshes.
            // Let's try to create a primitive cube mesh if we can, or just leave it as billboard if no mesh is assigned.
            
            if (debrisMaterial != null) renderer.material = debrisMaterial;
            else renderer.material = new Material(Shader.Find("Standard")); 
        }

        private void SetupSparksSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 5, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.05f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 5f;
            
            if (sparkMaterial != null) renderer.material = sparkMaterial;
            else 
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.4f), new Color(1f, 0.5f, 0.1f));
            }
        }

        private void SetupFlashSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.05f;
            main.loop = false;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new Color(1f, 0.9f, 0.7f, 0.5f);

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

            // Apply to all systems
            foreach (var ps in allSystems)
            {
                if (ps == null) continue;

                var main = ps.main;
                
                // Scale size
                // Note: This is a simplification. Ideally we'd scale the curve values.
                main.startSizeMultiplier *= currentDamageScale;
                
                // Scale speed (more damage = more kinetic energy)
                main.startSpeedMultiplier *= Mathf.Lerp(1f, 1.5f, t);

                // Scale emission count
                var emission = ps.emission;
                ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(bursts);
                for (int i = 0; i < bursts.Length; i++)
                {
                    bursts[i].count = new ParticleSystem.MinMaxCurve(
                        bursts[i].count.constantMin * currentDamageScale,
                        bursts[i].count.constantMax * currentDamageScale
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
