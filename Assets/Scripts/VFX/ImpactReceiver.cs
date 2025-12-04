using UnityEngine;

namespace JTPAudio.VFX
{
    [RequireComponent(typeof(Renderer))]
    public class ImpactReceiver : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Speed at which the ripple wave travels.")]
        public float waveSpeed = 5.0f;
        
        [Tooltip("Frequency of the ripple wave.")]
        public float waveFrequency = 10.0f;
        
        [Tooltip("Amplitude (height) of the ripple wave.")]
        public float waveAmplitude = 0.1f;
        
        [Tooltip("Time in seconds for the ripple to fade out completely.")]
        public float impactDuration = 1.0f;
        
        [Tooltip("Maximum distance the wave affects.")]
        public float maxDistance = 2.0f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Triggers an impact ripple effect at the specified point.
        /// </summary>
        /// <param name="point">World space position of the impact.</param>
        /// <param name="force">Force of the impact (scales amplitude).</param>
        public void AddImpact(Vector3 point, float force)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetVector("_ImpactPoint", point);
            
            // Subtract a small buffer to ensure the effect starts immediately on the GPU
            // and has propagated slightly to hit nearby vertices (helpful for low poly)
            _propBlock.SetFloat("_ImpactTime", Time.time - 0.02f);
            
            // Scale amplitude by force (clamped to avoid extreme distortion)
            // Assuming force is roughly damage or impact force. 
            // Adjust the divisor (10f) based on your typical damage values.
            float scaledAmplitude = waveAmplitude * Mathf.Clamp(force / 10f, 0.5f, 2.0f);
            _propBlock.SetFloat("_WaveAmplitude", scaledAmplitude);
            
            // Set other properties
            _propBlock.SetFloat("_WaveSpeed", waveSpeed);
            _propBlock.SetFloat("_WaveFrequency", waveFrequency);
            
            // Calculate decay rate so that it fades out by impactDuration
            // exp(-rate * duration) = 0.01 (1% visible) -> rate = 4.6 / duration
            float calculatedDecay = 4.6f / Mathf.Max(impactDuration, 0.01f);
            _propBlock.SetFloat("_DecayRate", calculatedDecay);
            
            _propBlock.SetFloat("_MaxDistance", maxDistance);

            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
