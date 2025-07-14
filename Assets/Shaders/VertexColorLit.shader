// This Surface Shader colors an object using its vertex colors
// and allows it to be affected by lights and shadows in the scene.
Shader "Custom/VertexColorLit"
{
    // Properties that can be adjusted in the Material's Inspector.
    Properties
    {
        // _Color allows you to apply a tint on top of the vertex colors.
        _Color ("Color Tint", Color) = (1,1,1,1)

        // _Glossiness controls how shiny the surface is.
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        // _Metallic controls how 'metal-like' the material is.
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        // Tags tell Unity this is an opaque object that fits into the standard rendering pipeline.
        Tags { "RenderType"="Opaque" }
        LOD 200

        // The CGPROGRAM block contains the actual shader code.
        CGPROGRAM
        // This line declares it's a surface shader, using the 'surf' function
        // and the 'Standard' lighting model (physically based).
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 for better compatibility and features.
        #pragma target 3.0

        // Input structure for the surface shader.
        // We MUST include 'float4 color' to get access to the mesh's vertex colors.
        struct Input
        {
            float4 color : COLOR;
        };

        // Declaring the properties we defined earlier so we can use them in the code.
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // The surface function 'surf' is called for every pixel being rendered.
        // It determines the final look of the object's surface.
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // o: The output structure containing surface properties like Albedo, Metallic, etc.
            // IN: The input structure with our vertex color.

            // The final color (Albedo) is the vertex color from the model (IN.color)
            // multiplied by the _Color property we can change in the inspector.
            fixed4 c = IN.color * _Color;
            o.Albedo = c.rgb;

            // Set the Metallic and Smoothness properties from our slider values.
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}