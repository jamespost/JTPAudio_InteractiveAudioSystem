# 🎨 Shader Brief: Simulated Subsurface Scattering for Deep-Sea Creature Skin

**Target Engine:** Unity 2022.3.x  
**Render Pipeline:** Built-in (Forward Rendering)  
**Shader Type:** Custom Surface Shader (written in ShaderLab with HLSL lighting function override)  
**Performance Priority:** High — must be optimized for use on many entities at once  
**Visual Goal:** Translucent, fleshy, bioluminescent skin with soft backlight and moist sheen, like a deep-sea creature

## 📌 Core Shader Features

### Backlight/Transmission Term
- Simulates subsurface scattering by boosting light from behind the surface.
- Based on angle between surface normal and light/view direction.
- Tinted color property (`_SubsurfaceColor`) and strength (`_SSSStrength`).

### Specular Highlights (Moist Look)
- Basic Blinn-Phong or Cook-Torrance model.
- Tunable specular intensity and smoothness.

### Albedo + Normal Map Support
- `_MainTex` for base color.
- `_BumpMap` for surface detail.

### Optional Emission/Glow Map
- `_EmissionMap` + `_EmissionColor` for bioluminescent features.

## 🎛️ Material Properties

| Property Name   | Type       | Description                     |
|-----------------|------------|---------------------------------|
| `_MainTex`      | Texture2D  | Albedo texture                 |
| `_BumpMap`      | Texture2D  | Normal map                     |
| `_SubsurfaceColor` | Color   | Tint for SSS effect            |
| `_SSSStrength`  | Float      | Intensity of backlight effect  |
| `_SpecColor`    | Color      | Specular highlight color       |
| `_Glossiness`   | Float      | Shininess/smoothness of specular |
| `_EmissionMap`  | Texture2D  | Optional glowing texture       |
| `_EmissionColor`| Color      | Color/tint of glow             |

## 📐 Technical Notes
- Shader should be written as a Surface Shader with a Lighting function override to inject the custom SSS/backlight logic.
- Use half Lambert or a wrapped lighting term for diffuse softness.
- Ensure the shader falls back gracefully on lower-end hardware (e.g., skip emission if not set).
- Support instancing and GPU batching where possible.
