using UnityEngine;

namespace JTPAudio.VFX
{
    /// <summary>
    /// Programmatically configures a ParticleSystem to look like blood splatter.
    /// Attach this to an empty GameObject to create a blood VFX prefab.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ProceduralBloodVFX : MonoBehaviour
    {
        private ParticleSystem ps;
        private ParticleSystemRenderer psRenderer;
        private ParticleSystem decalPs; // Secondary system for decals
        private ParticleSystemRenderer decalRenderer;
        private static Texture2D _cachedParticleTexture;
        private static Texture2D _cachedSplatTexture;
        private float _currentDamageScale = 1.0f;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            psRenderer = GetComponent<ParticleSystemRenderer>();
            
            // Create Decal Particle System
            GameObject decalObj = new GameObject("DecalSubEmitter");
            decalObj.transform.SetParent(transform);
            decalObj.transform.localPosition = Vector3.zero;
            decalObj.transform.localRotation = Quaternion.identity;
            
            decalPs = decalObj.AddComponent<ParticleSystem>();
            decalRenderer = decalObj.AddComponent<ParticleSystemRenderer>();

            ConfigureParticleSystem();
            ConfigureDecalSystem();
        }

        private void OnEnable()
        {
            if (ps != null)
            {
                // Re-apply configuration to ensure material is correct even after pooling
                ConfigureParticleSystem();

                ps.Clear();
                ps.Play();
                
                if (decalPs != null)
                {
                    decalPs.Clear();
                    decalPs.Play();
                }

                // Disable after the longest possible lifetime to return to pool
                float maxLifetime = ps.main.startLifetime.constantMax;
                float duration = ps.main.duration;
                Invoke(nameof(DisableSelf), duration + maxLifetime);
            }
        }

        private void DisableSelf()
        {
            gameObject.SetActive(false);
        }

        public void ScaleEffect(float damage)
        {
            if (ps == null) ps = GetComponent<ParticleSystem>();

            // Scale burst count based on damage
            // Assuming base damage is around 10-20
            short minBurst = (short)Mathf.Clamp(damage * 2f, 10, 100);
            short maxBurst = (short)Mathf.Clamp(damage * 5f, 20, 200);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { 
                new ParticleSystem.Burst(0f, minBurst, maxBurst) 
            });

            // Scale size slightly based on damage
            var main = ps.main;
            // Reduced max multiplier to prevent giant pixelated blobs
            // Base scale around 1.0 for average damage (15-20)
            _currentDamageScale = Mathf.Clamp(0.7f + (damage / 50f), 0.8f, 1.3f);
            main.startSizeMultiplier = _currentDamageScale;

            // Restart to apply changes immediately
            ps.Clear();
            ps.Play();
        }

        private void ConfigureParticleSystem()
        {
            // 1. Main Module
            var main = ps.main;
            main.duration = 0.1f; // Short burst
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); // Longer persistence
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f); // Faster speed for "gushing"
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.1f); // Reduced max size for finer droplets
            
            // Randomize color between dark red and brighter blood red
            // Reduced variance for more consistent look
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.5f, 0.0f, 0.0f, 1f), // Deep red
                new Color(0.7f, 0.05f, 0.05f, 1f)  // Slightly brighter red
            );
            
            main.gravityModifier = 3f; // Heavier gravity
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // 2. Emission
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            // Burst of particles
            emission.SetBursts(new ParticleSystem.Burst[] { 
                new ParticleSystem.Burst(0f, (short)20, (short)50) 
            });

            // 3. Shape
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f; // Tighter cone for more directional force
            shape.radius = 0.05f; 
            shape.radiusThickness = 1f; 

            // Add Noise for turbulence/liquid feel
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            noise.frequency = 0.5f;
            noise.scrollSpeed = 1f;
            noise.damping = true;

            // 4. Collision
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 0.5f; // Less sticky, allows sliding
            collision.bounce = 0.2f; // Slight bounce
            collision.radiusScale = 0.5f; // Reduce collision radius to avoid snagging on enemy mesh
            collision.lifetimeLoss = 1f; // Kill particle on collision so we can spawn a decal instead
            collision.minKillSpeed = 0f;
            
            // Exclude Enemy layer from collision if possible to prevent initial bounce on enemy
            // Assuming "Enemy" layer exists, otherwise it defaults to Everything
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                collision.collidesWith = ~(1 << enemyLayer);
            }
            else
            {
                collision.collidesWith = -1; 
            }
            
            collision.sendCollisionMessages = true; // Enable OnParticleCollision

            // 5. Renderer
            // Force a material that supports vertex colors and is unlit
            // We check if we need to create a material. We do this if it's null or the default one.
            if (psRenderer.sharedMaterial == null || psRenderer.sharedMaterial.name.StartsWith("Default-Particle"))
            {
                // Try to find a reliable shader that supports vertex colors
                // Legacy shaders are often more reliable for simple programmatic vertex color support
                Shader particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                if (particleShader == null) particleShader = Shader.Find("Mobile/Particles/Alpha Blended");
                if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
                
                if (particleShader != null)
                {
                    Material particleMat = new Material(particleShader);
                    
                    // For Standard Unlit, we need to ensure it's set to transparent and uses vertex colors
                    if (particleShader.name.Contains("Standard Unlit"))
                    {
                        particleMat.SetFloat("_Mode", 2); // Fade/Transparent
                        particleMat.EnableKeyword("_ALPHABLEND_ON");
                        particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        particleMat.SetInt("_ZWrite", 0);
                        particleMat.DisableKeyword("_ALPHATEST_ON");
                        particleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        particleMat.renderQueue = 3000;
                    }

                    // Generate and assign a texture so it's not a square
                    if (_cachedParticleTexture == null)
                    {
                        _cachedParticleTexture = CreateCircleTexture();
                    }
                    particleMat.mainTexture = _cachedParticleTexture;

                    psRenderer.material = particleMat;
                }
                else
                {
                    Debug.LogWarning("[ProceduralBloodVFX] Could not find a suitable particle shader.");
                }
            }
            
            // Set render mode to Billboard for more realistic droplets
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.minParticleSize = 0f;
            psRenderer.maxParticleSize = 0.5f;
        }

        private void OnParticleCollision(GameObject other)
        {
            if (decalPs == null) return;

            int numCollisionEvents = ps.GetCollisionEvents(other, new System.Collections.Generic.List<ParticleCollisionEvent>());
            var collisionEvents = new System.Collections.Generic.List<ParticleCollisionEvent>(numCollisionEvents);
            ps.GetCollisionEvents(other, collisionEvents);

            foreach (var collision in collisionEvents)
            {
                // Ignore collisions with enemies to prevent floating decals
                // We check layer, name, and tag (safely)
                bool isEnemy = false;
                
                // Check Layer
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1 && other.layer == enemyLayer) isEnemy = true;

                // Check Name
                if (!isEnemy && (other.name.Contains("Enemy") || other.name.Contains("Target"))) isEnemy = true;

                // Check Tag (safely)
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
                float size = Random.Range(0.1f, 0.3f) * _currentDamageScale;
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
                emitParams.position = collision.intersection + (collision.normal * 0.01f); // Slight offset to prevent z-fighting
                emitParams.rotation3D = rotation.eulerAngles;
                emitParams.startSize3D = size3D;
                emitParams.startColor = new Color(0.6f, 0f, 0f, 1f); // Start dark red
                
                decalPs.Emit(emitParams, 1);
            }
        }

        private void ConfigureDecalSystem()
        {
            var main = decalPs.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = 10f; // Decals stay longer
            main.startSpeed = 0f; // Static
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 100;
            
            // Color over lifetime for drying effect (Red -> Brown)
            var col = decalPs.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.6f, 0.0f, 0.0f), 0.0f), // Fresh Red
                    new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 0.3f), // Drying Brown-Red
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.0f), 1.0f)  // Dried Dark Brown
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
                quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
                quadMesh.RecalculateNormals();
                decalRenderer.mesh = quadMesh;
            }

            decalRenderer.alignment = ParticleSystemRenderSpace.World; // Important for custom rotation

            // Material for Decals
            // We check if we need to create a material. We do this if it's null or the default one.
            if (decalRenderer.sharedMaterial == null || decalRenderer.sharedMaterial.name.StartsWith("Default-Particle"))
            {
                // Reuse the particle shader but with a splat texture
                Shader particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended"); // Reliable
                if (particleShader == null) particleShader = Shader.Find("Mobile/Particles/Alpha Blended");
                if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
                if (particleShader == null) particleShader = Shader.Find("Sprites/Default"); // Ultimate fallback

                if (particleShader != null)
                {
                    Material decalMat = new Material(particleShader);
                    
                    // For Standard Unlit, we need to ensure it's set to transparent and uses vertex colors
                    if (particleShader.name.Contains("Standard Unlit"))
                    {
                        decalMat.SetFloat("_Mode", 2); // Fade/Transparent
                        decalMat.EnableKeyword("_ALPHABLEND_ON");
                        decalMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        decalMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        decalMat.SetInt("_ZWrite", 0);
                        decalMat.DisableKeyword("_ALPHATEST_ON");
                        decalMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        decalMat.renderQueue = 3000;
                    }

                    if (_cachedSplatTexture == null) _cachedSplatTexture = CreateSplatTexture();
                    decalMat.mainTexture = _cachedSplatTexture;
                    decalRenderer.material = decalMat;
                }
                else
                {
                    Debug.LogError("[ProceduralBloodVFX] Could not find ANY suitable shader for decals!");
                }
            }
        }

        private Texture2D CreateCircleTexture()
        {
            int resolution = 256; // Increased resolution for sharper edges
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] colors = new Color[resolution * resolution];
            float center = resolution / 2f;
            float radius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    
                    // Create a sharper, more solid circle
                    float alpha = 0f;
                    if (dist < radius * 0.8f)
                    {
                        alpha = 1f; // Solid core
                    }
                    else if (dist < radius)
                    {
                        // Quick fade out at edge
                        alpha = 1f - ((dist - (radius * 0.8f)) / (radius * 0.2f));
                    }
                    
                    colors[y * resolution + x] = new Color(1, 1, 1, alpha);
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private Texture2D CreateSplatTexture()
        {
            int resolution = 128;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] colors = new Color[resolution * resolution];
            float center = resolution / 2f;
            float maxRadius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 dir = (pos - new Vector2(center, center)).normalized;
                    float dist = Vector2.Distance(pos, new Vector2(center, center));
                    
                    // Noise for irregular edges
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    float noise = Mathf.PerlinNoise(angle * 2f, dist * 0.05f);
                    float radius = maxRadius * (0.5f + noise * 0.4f);

                    float alpha = Mathf.Clamp01(1f - (dist / radius));
                    alpha = Mathf.Pow(alpha, 2); // Sharpen
                    
                    colors[y * resolution + x] = new Color(1, 1, 1, alpha);
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
