// This shader takes the vertex colors painted on a 3D model and displays them.
// It is "Unlit," meaning it will not be affected by lights in the scene.
Shader "Custom/VertexColorUnlit"
{
    // Defines properties that can be adjusted in the Inspector, though we don't need any for this simple shader.
    Properties
    {
    }

    // The main part of the shader.
    SubShader
    {
        // Tags are used by the rendering engine to know how and when to render this shader.
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // Tells the GPU to not write to the depth buffer, useful for simple transparent effects if needed, but fine for opaque.
            ZWrite Off
            // Enables alpha blending. This allows for transparency based on vertex alpha.
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            // Declares the vertex and fragment shader functions.
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Input structure for the vertex shader.
            // It receives the vertex position and its color from the mesh data.
            struct appdata
            {
                float4 vertex : POSITION; // The vertex position in object space.
                fixed4 color : COLOR;    // The vertex color.
            };

            // Output structure for the vertex shader, which becomes the input for the fragment shader.
            struct v2f
            {
                float4 vertex : SV_POSITION; // The vertex position in clip space.
                fixed4 color : COLOR;        // The vertex color passed through.
            };

            // The Vertex Shader function.
            // This runs for every vertex of the model.
            v2f vert (appdata v)
            {
                v2f o;
                // Transform the vertex position from object space to clip space.
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Pass the vertex color directly to the fragment shader.
                o.color = v.color;
                return o;
            }

            // The Fragment Shader function.
            // This runs for every pixel on the model's surface.
            fixed4 frag (v2f i) : SV_Target
            {
                // The output color of the pixel is set to the color received from the vertex shader.
                return i.color;
            }
            ENDCG
        }
    }
    // Fallback for older hardware.
    FallBack "Unlit"
}