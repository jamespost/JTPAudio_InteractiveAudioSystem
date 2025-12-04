Shader "Custom/ImpactRipple"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        
        _EmissionColor ("Emission Color", Color) = (0,0,0)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        
        // Impact Properties
        _ImpactPoint ("Impact Point", Vector) = (0,0,0,0)
        _ImpactTime ("Impact Time", Float) = -100
        _WaveSpeed ("Wave Speed", Float) = 5.0
        _WaveFrequency ("Wave Frequency", Float) = 10.0
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
        _DecayRate ("Decay Rate", Float) = 2.0
        _MaxDistance ("Max Distance", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        // addshadow is needed for vertex displacement shadows
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_EmissionMap;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _EmissionColor;

        float4 _ImpactPoint;
        float _ImpactTime;
        float _WaveSpeed;
        float _WaveFrequency;
        float _WaveAmplitude;
        float _DecayRate;
        float _MaxDistance;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v) {
            // Calculate world position of vertex
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            
            // Distance from impact point
            float dist = distance(worldPos, _ImpactPoint.xyz);
            
            // Time since impact
            float timeSinceImpact = _Time.y - _ImpactTime;
            
            // Removed strict > 0 check to avoid 1-frame delays due to float precision or sync
            if (dist < _MaxDistance) {
                // Calculate wave
                // Wave moves outward: dist - speed * time
                float wavePhase = dist - _WaveSpeed * timeSinceImpact;
                
                // Decay based on time and distance
                // Ensure decay doesn't go negative or blow up for negative times (from pre-warm)
                float safeTime = max(0, timeSinceImpact);
                float decay = exp(-_DecayRate * safeTime) * (1 - saturate(dist / _MaxDistance));
                
                // Use Cosine for immediate displacement at the center (peak instead of zero crossing)
                // This ensures the impact point "pops" immediately.
                float wave = cos(wavePhase * _WaveFrequency) * _WaveAmplitude * decay;
                
                // Apply displacement along normal
                v.vertex.xyz += v.normal * wave;
            }
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            
            // Normal Map
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex)); // Assuming shared UVs
            
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            
            // Emission
            fixed4 e = tex2D(_EmissionMap, IN.uv_MainTex) * _EmissionColor;
            o.Emission = e.rgb;
            
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
