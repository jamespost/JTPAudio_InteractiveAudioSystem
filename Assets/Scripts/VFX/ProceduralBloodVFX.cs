using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JTPAudio.VFX
{
    /// <summary>
    /// Programmatically assembles a layered, fluid-like blood effect using multiple particle passes
    /// (core jet, sheet, droplets, mist, decals, drips) plus runtime LOD/scale controls. Attach this
    /// to an empty GameObject to create a blood VFX prefab driven entirely by code.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ProceduralBloodVFX : MonoBehaviour
    {
        private ParticleSystem ps; // Core jet
        private ParticleSystemRenderer psRenderer;
        private ParticleSystem sheetPs;
        private ParticleSystemRenderer sheetRenderer;
        private ParticleSystem dropletPs;
        private ParticleSystemRenderer dropletRenderer;
        private ParticleSystem mistPs;
        private ParticleSystemRenderer mistRenderer;
        private ParticleSystem decalPs;
        private ParticleSystemRenderer decalRenderer;
        private ParticleSystem dripPs;
        private ParticleSystemRenderer dripRenderer;
        private static Texture2D _cachedParticleTexture;
        private static Texture2D _cachedSplatTexture;
        private static Shader _cachedBloodShader;
        private static Material _cachedParticleMaterial;
        private static Material _cachedDecalMaterial;
        private static bool _didLogMissingShader;

        private readonly List<ParticleSystem> _allSystems = new List<ParticleSystem>();
        private readonly Dictionary<ParticleSystem, (short min, short max)> _burstCache = new Dictionary<ParticleSystem, (short, short)>();
        private readonly Dictionary<ParticleSystem, float> _layerWeights = new Dictionary<ParticleSystem, float>();
        private static readonly List<ParticleCollisionEvent> CollisionBuffer = new List<ParticleCollisionEvent>(64);

        [SerializeField]
        [Tooltip("Optional explicit shader reference to override automatic lookup.")]
        private Shader bloodShaderOverride;

        [Header("Fluid Behaviour")]
        [SerializeField]
        private AnimationCurve pressureOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [SerializeField]
        private Vector2 speedRange = new Vector2(7f, 18f);

        [SerializeField]
        private float jetGravityScale = 4f;

        [Header("Visual Toggles")]
        [SerializeField]
        private bool enableSheetConnector = false;

        [SerializeField]
        private bool enableRibbonTrails = false;

        [Header("LOD & Performance")]
        [SerializeField]
        private float highDetailDistance = 7f;

        [SerializeField]
        private float mediumDetailDistance = 14f;

        [SerializeField, Range(0.1f, 1f)]
        private float mediumIntensityMultiplier = 0.65f;

        [SerializeField, Range(0.05f, 0.8f)]
        private float lowIntensityMultiplier = 0.3f;

        private enum DetailTier { Full, Medium, Low }

        private DetailTier _currentTier = DetailTier.Full;
        private Camera _mainCamera;
        private float _tierIntensityMultiplier = 1f;
        private float _damageIntensityMultiplier = 1f;

        private const float DecalSurfaceOffset = 0.025f;
        private float _currentDamageScale = 1.0f;

        private void Awake()
        {
            _mainCamera = Camera.main;

            ps = GetComponent<ParticleSystem>();
            psRenderer = GetComponent<ParticleSystemRenderer>();
            RegisterSystem(ps);

            sheetPs = CreateChildSystem("SheetLayer", out sheetRenderer);
            dropletPs = CreateChildSystem("DropletLayer", out dropletRenderer);
            mistPs = CreateChildSystem("MistLayer", out mistRenderer);
            decalPs = CreateChildSystem("DecalLayer", out decalRenderer);
            dripPs = CreateChildSystem("DripLayer", out dripRenderer);

            ConfigureCoreJet();
            if (enableSheetConnector)
            {
                ConfigureSheetLayer();
            }
            else
            {
                DisableLayer(sheetPs, sheetRenderer);
            }
            ConfigureDropletLayer();
            ConfigureMistLayer();
            ConfigureDecalLayer();
            ConfigureDripLayer();
            ApplyLayerIntensities();
        }

        private void OnEnable()
        {
            if (_allSystems.Count == 0) return;
            ResetAndPlayAll();
            ScheduleDisable();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(DisableSelf));
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) return;

            float sqrDist = (transform.position - _mainCamera.transform.position).sqrMagnitude;
            DetailTier targetTier = DetailTier.Full;

            if (sqrDist > mediumDetailDistance * mediumDetailDistance)
            {
                targetTier = DetailTier.Low;
            }
            else if (sqrDist > highDetailDistance * highDetailDistance)
            {
                targetTier = DetailTier.Medium;
            }

            if (targetTier != _currentTier)
            {
                _currentTier = targetTier;
                _tierIntensityMultiplier = targetTier switch
                {
                    DetailTier.Medium => mediumIntensityMultiplier,
                    DetailTier.Low => lowIntensityMultiplier,
                    _ => 1f
                };
                ApplyLayerIntensities();
            }
        }

        private void DisableSelf()
        {
            gameObject.SetActive(false);
        }

        private static void ForceStopAndClear(ParticleSystem system)
        {
            if (system == null) return;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>
        /// Re-pressurizes the entire stack based on gameplay damage. Call this before enabling the VFX
        /// to remap burst counts, gravity, and start speeds so designers can tune brutality per hit.
        /// </summary>
        public void ScaleEffect(float damage)
        {
            if (ps == null) return;

            float normalized = Mathf.Clamp(damage / 30f, 0.4f, 2.5f);
            _damageIntensityMultiplier = normalized;

            _currentDamageScale = Mathf.Clamp(0.5f + damage / 50f, 0.6f, 1.5f);

            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x * normalized, speedRange.y * normalized * 1.15f);
            main.startSizeMultiplier = _currentDamageScale;
            main.gravityModifier = Mathf.Lerp(jetGravityScale * 0.6f, jetGravityScale * 1.35f, Mathf.Clamp01((normalized - 0.4f) / 2.1f));

            if (IsLayerActive(sheetPs))
            {
                var sheetMain = sheetPs.main;
                sheetMain.startSpeed = new ParticleSystem.MinMaxCurve(3f * normalized, 9f * normalized);
                sheetMain.startSizeMultiplier = Mathf.Lerp(0.85f, 1.35f, Mathf.Clamp01(normalized - 0.4f));
            }

            if (IsLayerActive(dropletPs))
            {
                var dropletMain = dropletPs.main;
                dropletMain.startSpeed = new ParticleSystem.MinMaxCurve(4f * normalized, 12f * normalized);
                dropletMain.gravityModifier = Mathf.Lerp(4f, 9f, Mathf.Clamp01(normalized * 0.5f));
            }

            ApplyLayerIntensities();
            RestartAll();
        }

        private void OnParticleCollision(GameObject other)
        {
            if (ps == null || decalPs == null) return;

            CollisionBuffer.Clear();
            int numCollisionEvents = ps.GetCollisionEvents(other, CollisionBuffer);
            if (numCollisionEvents == 0) return;

            int enemyLayer = LayerMask.NameToLayer("Enemy");

            for (int i = 0; i < numCollisionEvents; i++)
            {
                var collision = CollisionBuffer[i];

                // Ignore collisions with enemies to prevent floating decals
                bool isEnemy = false;
                
                if (enemyLayer != -1 && other.layer == enemyLayer) isEnemy = true;

                if (!isEnemy && (other.name.Contains("Enemy") || other.name.Contains("Target"))) isEnemy = true;

                if (!isEnemy)
                {
                    try { if (other.CompareTag("Enemy")) isEnemy = true; } catch { /* Tag doesn't exist */ }
                }

                if (isEnemy) continue;

                // Ballistics: Calculate impact angle
                // Dot product of velocity (normalized) and normal
                // If -1, it's head on (90 deg). If 0, it's grazing (0 deg).
                Vector3 velocityDir = collision.velocity.normalized;
                float impactDot = Vector3.Dot(velocityDir, collision.normal);
                
                // Determine shape based on angle
                // If grazing (impactDot close to 0), elongate
                float elongation = Mathf.Lerp(3f, 1f, Mathf.Abs(impactDot)); // 1 = circle, 3 = long streak
                
                // Randomize size and apply damage scale
                float size = Random.Range(0.025f, 0.075f) * _currentDamageScale;
                Vector3 size3D = new Vector3(size, size * elongation, 1f);

                // Orient to surface
                // We want the decal to lie flat on the surface (Z axis matches normal)
                // And Y axis (up) matches the velocity direction projected onto the plane
                Vector3 tangent = Vector3.ProjectOnPlane(velocityDir, collision.normal).normalized;
                if (tangent == Vector3.zero) 
                {
                    // If hitting perpendicular, pick any direction on the plane
                    if (Mathf.Abs(Vector3.Dot(Vector3.up, collision.normal)) > 0.99f)
                        tangent = Vector3.right;
                    else
                        tangent = Vector3.up;
                        
                    tangent = Vector3.ProjectOnPlane(tangent, collision.normal).normalized;
                }
                
                Quaternion rotation = Quaternion.LookRotation(collision.normal, tangent);

                // Emit decal
                var emitParams = new ParticleSystem.EmitParams();
                float pushOut = DecalSurfaceOffset * Random.Range(0.85f, 1.15f);
                emitParams.position = collision.intersection + (collision.normal * pushOut);
                emitParams.rotation3D = rotation.eulerAngles;
                emitParams.startSize3D = size3D;
                emitParams.startColor = Color.white; 
                
                decalPs.Emit(emitParams, 1);

                if (mistPs != null)
                {
                    var splashParams = new ParticleSystem.EmitParams
                    {
                        position = collision.intersection + (collision.normal * 0.05f)
                    };
                    Vector3 reflectDir = Vector3.Reflect(velocityDir, collision.normal);
                    splashParams.velocity = (reflectDir + Random.insideUnitSphere * 0.35f).normalized * Random.Range(1.5f, 4.5f);
                    mistPs.Emit(splashParams, Random.Range(4, 10));
                }

                if (dripPs != null)
                {
                    var dripParams = new ParticleSystem.EmitParams
                    {
                        position = collision.intersection + (collision.normal * DecalSurfaceOffset * 0.5f),
                        startSize = Random.Range(0.005f, 0.0125f) * _currentDamageScale,
                        velocity = Vector3.down * Random.Range(0.4f, 1.2f)
                    };
                    dripPs.Emit(dripParams, 1);
                }
            }

            CollisionBuffer.Clear();
        }

        private void ConfigureDecalLayer()
        {
            ForceStopAndClear(decalPs);

            var main = decalPs.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = 10f; // Decals stay longer
            main.startSpeed = 0f; // Static
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.playOnAwake = false;
            main.maxParticles = 100;
            main.startRotation3D = true;
            main.startSize3D = true;
            main.startColor = Color.white; // Texture has color
            
            // Color over lifetime for drying effect (White -> Dark Grey)
            // Since texture is already red, tinting it grey darkens it to brown/black
            var col = decalPs.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0.0f), // Fresh
                    new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 0.3f), // Drying
                    new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 1.0f)  // Dried
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            col.color = grad;

            var emission = decalPs.emission;
            emission.enabled = false; // Only emit via script

            var shape = decalPs.shape;
            shape.enabled = false;

            // Renderer - Needs to be Mesh (Quad) to orient properly
            decalRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            
            // Create a simple quad mesh programmatically to avoid asset dependencies
            if (decalRenderer.mesh == null)
            {
                Mesh quadMesh = new Mesh();
                quadMesh.name = "ProceduralQuad";
                quadMesh.vertices = new Vector3[] {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3(0.5f, -0.5f, 0),
                    new Vector3(-0.5f, 0.5f, 0),
                    new Vector3(0.5f, 0.5f, 0)
                };
                quadMesh.uv = new Vector2[] {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                };
                // Double-sided triangles to prevent backface culling issues
                quadMesh.triangles = new int[] { 
                    0, 2, 1, 2, 3, 1, // Front
                    1, 2, 0, 1, 3, 2  // Back
                };
                quadMesh.RecalculateNormals();
                decalRenderer.mesh = quadMesh;
            }

            decalRenderer.alignment = ParticleSystemRenderSpace.Local; // Respect per-particle rotation
            decalRenderer.sortingFudge = 2f; // Push above nearby geometry
            decalRenderer.enableGPUInstancing = true;
            decalRenderer.shadowCastingMode = ShadowCastingMode.Off;
            decalRenderer.receiveShadows = false;

            // Material for Decals
            var decalMaterial = GetDecalMaterial();
            if (decalMaterial != null)
            {
                decalRenderer.sharedMaterial = decalMaterial;
            }
        }

        private void ConfigureCoreJet()
        {
            ForceStopAndClear(ps);

            var main = ps.main;
            main.duration = 0.18f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.015f);
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = jetGravityScale;
            main.playOnAwake = false;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.7f, 0.05f, 0.05f), 0f),
                    new GradientColorKey(new Color(0.35f, 0f, 0f), 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            });

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, pressureOverLife);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.z = new ParticleSystem.MinMaxCurve(-1f, -0.5f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            var limitVel = ps.limitVelocityOverLifetime;
            limitVel.enabled = true;
            limitVel.limit = new ParticleSystem.MinMaxCurve(4f, 6f);
            limitVel.dampen = 0.4f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.strength = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.8f;
            noise.damping = true;

            var trails = ps.trails;
            trails.enabled = enableRibbonTrails;
            if (enableRibbonTrails)
            {
                trails.mode = ParticleSystemTrailMode.Ribbon;
                trails.ratio = 0.3f;
                trails.lifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
                trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
                trails.sizeAffectsLifetime = true;
            }

            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 0.75f;
            collision.bounce = 0.05f;
            collision.radiusScale = 0.6f;
            collision.lifetimeLoss = 1f;
            collision.sendCollisionMessages = true;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            collision.collidesWith = enemyLayer == -1 ? ~0 : ~(1 << enemyLayer);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short min = 112;
            short max = 260;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, min, max) });
            CacheBurst(ps, min, max);
            SetLayerWeight(ps, 1f);

            var particleMaterial = GetParticleMaterial();
            if (particleMaterial != null)
            {
                psRenderer.sharedMaterial = particleMaterial;
                psRenderer.trailMaterial = particleMaterial;
            }
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sortingFudge = 1f;
        }

        private void ConfigureSheetLayer()
        {
            ForceStopAndClear(sheetPs);

            var main = sheetPs.main;
            main.duration = 0.22f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.0075f, 0.02f);
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = jetGravityScale * 0.35f;
            main.startColor = Color.white;
            main.playOnAwake = false;

            var emission = sheetPs.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short min = 72;
            short max = 168;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, min, max) });
            CacheBurst(sheetPs, min, max);
            SetLayerWeight(sheetPs, 0.85f);

            var shape = sheetPs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.04f;

            var velocity = sheetPs.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            var textureSheet = sheetPs.textureSheetAnimation;
            textureSheet.enabled = false;

            sheetRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            sheetRenderer.alignment = ParticleSystemRenderSpace.View;
            sheetRenderer.minParticleSize = 0f;
            sheetRenderer.maxParticleSize = 0.25f;
            var mat = GetParticleMaterial();
            if (mat != null) sheetRenderer.sharedMaterial = mat;
        }

        private void ConfigureDropletLayer()
        {
            ForceStopAndClear(dropletPs);

            var main = dropletPs.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.00375f, 0.0125f);
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 5f;
            main.playOnAwake = false;

            var emission = dropletPs.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short min = 48;
            short max = 112;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, min, max) });
            CacheBurst(dropletPs, min, max);
            SetLayerWeight(dropletPs, 1.15f);

            var collision = dropletPs.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 0.65f;
            collision.bounce = 0.05f;
            collision.lifetimeLoss = 1f;

            dropletRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            dropletRenderer.minParticleSize = 0f;
            dropletRenderer.maxParticleSize = 0.15f;
            var mat = GetParticleMaterial();
            if (mat != null) dropletRenderer.sharedMaterial = mat;
        }

        private void ConfigureMistLayer()
        {
            ForceStopAndClear(mistPs);

            var main = mistPs.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.002f, 0.005f);
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emission = mistPs.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short min = 80;
            short max = 200;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.03f, min, max) });
            CacheBurst(mistPs, min, max);
            SetLayerWeight(mistPs, 0.5f);

            mistRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            mistRenderer.minParticleSize = 0f;
            mistRenderer.maxParticleSize = 0.05f;
            var mat = GetParticleMaterial();
            if (mat != null) mistRenderer.sharedMaterial = mat;
        }

        private void ConfigureDripLayer()
        {
            ForceStopAndClear(dripPs);

            var main = dripPs.main;
            main.duration = 2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.0125f);
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 9.81f;
            main.maxParticles = 120;
            main.playOnAwake = false;

            var emission = dripPs.emission;
            emission.enabled = false; // script-driven

            var trails = dripPs.trails;
            trails.enabled = true;
            trails.lifetime = 0.25f;
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;

            dripRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            dripRenderer.minParticleSize = 0f;
            dripRenderer.maxParticleSize = 0.15f;
            var mat = GetParticleMaterial();
            if (mat != null)
            {
                dripRenderer.sharedMaterial = mat;
                dripRenderer.trailMaterial = mat;
            }
        }

        private void RegisterSystem(ParticleSystem system)
        {
            if (system == null || _allSystems.Contains(system)) return;
            _allSystems.Add(system);
        }

        private void DisableLayer(ParticleSystem system, ParticleSystemRenderer renderer)
        {
            if (system == null) return;
            ForceStopAndClear(system);
            var emission = system.emission;
            emission.enabled = false;
            renderer.enabled = false;
            system.gameObject.SetActive(false);
            _allSystems.Remove(system);
            _burstCache.Remove(system);
            _layerWeights.Remove(system);
        }

        private ParticleSystem CreateChildSystem(string name, out ParticleSystemRenderer renderer)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            var system = obj.AddComponent<ParticleSystem>();
            renderer = obj.GetComponent<ParticleSystemRenderer>();
            RegisterSystem(system);
            return system;
        }

        private void ResetAndPlayAll()
        {
            foreach (var system in _allSystems)
            {
                ForceStopAndClear(system);
                system.Play();
            }
        }

        private void RestartAll()
        {
            foreach (var system in _allSystems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play();
            }
            ScheduleDisable();
        }

        private void ScheduleDisable()
        {
            float maxDuration = 0f;
            foreach (var system in _allSystems)
            {
                var main = system.main;
                float duration = main.duration + main.startLifetime.constantMax;
                if (duration > maxDuration) maxDuration = duration;
            }

            CancelInvoke(nameof(DisableSelf));
            Invoke(nameof(DisableSelf), maxDuration);
        }

        private void CacheBurst(ParticleSystem system, short min, short max)
        {
            _burstCache[system] = (min, max);
        }

        private void SetLayerWeight(ParticleSystem system, float weight)
        {
            _layerWeights[system] = Mathf.Max(0.05f, weight);
        }

        private bool IsLayerActive(ParticleSystem system)
        {
            return system != null && _layerWeights.ContainsKey(system);
        }

        private void ApplyLayerIntensities()
        {
            foreach (var kvp in _layerWeights)
            {
                ApplyBurstScale(kvp.Key, kvp.Value * _tierIntensityMultiplier * _damageIntensityMultiplier);
            }
        }

        private void ApplyBurstScale(ParticleSystem system, float multiplier)
        {
            if (!_burstCache.TryGetValue(system, out var burst)) return;
            short min = (short)Mathf.Clamp(burst.min * multiplier, 1, 1000);
            short max = (short)Mathf.Clamp(burst.max * multiplier, min, 1500);
            var emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, min, max) });
        }

        private Material GetParticleMaterial()
        {
            if (_cachedParticleMaterial != null) return _cachedParticleMaterial;

            Shader shader = ResolveBloodShader();
            if (shader == null) return null;

            _cachedParticleMaterial = new Material(shader);
            ConfigureMaterial(_cachedParticleMaterial, shader);

            if (_cachedParticleTexture == null)
            {
                _cachedParticleTexture = CreateCircleTexture();
            }
            _cachedParticleMaterial.mainTexture = _cachedParticleTexture;
            return _cachedParticleMaterial;
        }

        private Material GetDecalMaterial()
        {
            if (_cachedDecalMaterial != null) return _cachedDecalMaterial;

            Shader shader = ResolveBloodShader();
            if (shader == null) return null;

            _cachedDecalMaterial = new Material(shader);
            ConfigureMaterial(_cachedDecalMaterial, shader);

            if (_cachedSplatTexture == null)
            {
                _cachedSplatTexture = CreateSplatTexture();
            }
            _cachedDecalMaterial.mainTexture = _cachedSplatTexture;
            return _cachedDecalMaterial;
        }

        private Shader ResolveBloodShader()
        {
            if (bloodShaderOverride != null) return bloodShaderOverride;
            if (_cachedBloodShader != null) return _cachedBloodShader;

            string[] shaderNames = new string[]
            {
                "Custom/ProceduralBlood",
                "Particles/Standard Unlit",
                "Mobile/Particles/Alpha Blended",
                "Legacy Shaders/Particles/Alpha Blended",
                "Sprites/Default"
            };
            foreach (var name in shaderNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                {
                    _cachedBloodShader = shader;
                    return _cachedBloodShader;
                }
            }

            if (!_didLogMissingShader)
            {
                Debug.LogError("[ProceduralBloodVFX] Unable to locate a usable blood shader. Please ensure 'Custom/ProceduralBlood' exists or assign an override.");
                _didLogMissingShader = true;
            }

            return null;
        }

        private void ConfigureMaterial(Material mat, Shader shader)
        {
            // If using our custom shader, no extra config needed
            if (shader != null && shader.name == "Custom/ProceduralBlood") return;

            if (shader == null) return;

            // For Standard/URP particles ensure transparent blending
            if (shader.name.Contains("Standard Unlit") || shader.name.Contains("Universal Render Pipeline/Particles/Unlit"))
            {
                mat.SetFloat("_Mode", 2); // Fade/Transparent
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            }
        }

        private Texture2D CreateCircleTexture()
        {
            int resolution = 128;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] colors = new Color[resolution * resolution];
            float center = resolution / 2f;
            float radius = resolution / 2f;
            
            // Blood colors - Baked in for "wet" look with specular
            Color bloodDark = new Color(0.4f, 0.0f, 0.0f, 1f);
            Color bloodLight = new Color(0.7f, 0.05f, 0.05f, 1f);
            Color highlight = new Color(1f, 1f, 1f, 1f);

            Vector2 lightDir = new Vector2(-0.5f, 0.5f).normalized;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 relPos = pos - new Vector2(center, center);
                    float dist = relPos.magnitude;
                    float normDist = dist / radius;
                    
                    if (dist >= radius)
                    {
                        colors[y * resolution + x] = Color.clear;
                        continue;
                    }

                    // Fake spherical normal
                    float z = Mathf.Sqrt(Mathf.Max(0, 1f - normDist * normDist));
                    Vector3 normal = new Vector3(relPos.x / radius, relPos.y / radius, z).normalized;
                    Vector3 light = new Vector3(lightDir.x, lightDir.y, 0.5f).normalized;
                    
                    // Diffuse
                    float diffuse = Mathf.Clamp01(Vector3.Dot(normal, light));
                    Color baseCol = Color.Lerp(bloodDark, bloodLight, diffuse * 0.8f + 0.2f);

                    // Specular (Sharp wet highlight)
                    // Offset the reflection slightly
                    float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(normal, light)), 30f);
                    
                    Color finalCol = baseCol + (highlight * spec * 0.8f);
                    
                    // Soft edge alpha
                    float alpha = Mathf.SmoothStep(1f, 0.85f, (dist - (radius * 0.85f)) / (radius * 0.15f));
                    finalCol.a = alpha;

                    colors[y * resolution + x] = finalCol;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private Texture2D CreateSplatTexture()
        {
            int resolution = 256;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] colors = new Color[resolution * resolution];
            float center = resolution / 2f;
            float maxRadius = resolution / 2f;

            Color bloodDark = new Color(0.35f, 0.0f, 0.0f, 1f);
            Color bloodLight = new Color(0.6f, 0.0f, 0.0f, 1f);
            Color highlight = new Color(1f, 1f, 1f, 0.8f);
            Vector2 lightDir = new Vector2(-0.5f, 0.5f).normalized;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 relPos = pos - new Vector2(center, center);
                    Vector2 dir = relPos.normalized;
                    float dist = relPos.magnitude;
                    
                    // Multi-layered noise for jagged liquid edges
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    float noise1 = Mathf.PerlinNoise(angle * 3f, dist * 0.02f);
                    float noise2 = Mathf.PerlinNoise(angle * 10f + 50f, dist * 0.05f);
                    float combinedNoise = (noise1 * 0.7f) + (noise2 * 0.3f);
                    
                    float currentRadius = maxRadius * (0.4f + combinedNoise * 0.5f);

                    if (dist > currentRadius)
                    {
                        colors[y * resolution + x] = Color.clear;
                        continue;
                    }

                    // Fake volume/lighting
                    float normDist = dist / currentRadius;
                    float z = Mathf.Sqrt(Mathf.Max(0, 1f - normDist * normDist));
                    Vector3 normal = new Vector3(relPos.x / maxRadius, relPos.y / maxRadius, z).normalized;
                    Vector3 light = new Vector3(lightDir.x, lightDir.y, 0.5f).normalized;

                    float diffuse = Mathf.Clamp01(Vector3.Dot(normal, light));
                    Color baseCol = Color.Lerp(bloodDark, bloodLight, diffuse);

                    // Wet highlight
                    float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(normal, light)), 20f);
                    Color finalCol = baseCol + (highlight * spec * 0.6f);

                    // Alpha fade at very edge
                    float alpha = Mathf.SmoothStep(1f, 0.85f, (dist - (currentRadius * 0.92f)) / (currentRadius * 0.08f));
                    finalCol.a = alpha;

                    colors[y * resolution + x] = finalCol;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
