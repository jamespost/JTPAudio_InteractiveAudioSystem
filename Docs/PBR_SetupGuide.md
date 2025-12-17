# PBR Material Setup Guide

This guide documents the workflow for importing PBR assets from Blender/Substance Painter into Unity, specifically addressing the conversion from **Roughness** (standard in external tools) to **Smoothness** (standard in Unity).

## Overview

Unity's Standard Shader (and URP Lit) uses a **Metallic/Smoothness** workflow, where:
- **Metallic** is stored in the RGB channels.
- **Smoothness** is stored in the Alpha channel of the Metallic map.

Most external tools (Substance, Blender) export **Roughness** maps. 
- **Roughness**: 0 = Smooth, 1 = Rough
- **Smoothness**: 0 = Rough, 1 = Smooth

We use a custom **Texture Packer** tool to automatically invert the Roughness map and pack it into the Alpha channel of the Metallic map.

---

## Workflow Steps

### 1. Export from Blender / Substance
Continue using your standard export settings. Ensure you have:
- **Albedo/Base Color**
- **Normal Map** (OpenGL format preferred for Unity, but Unity can fix this)
- **Roughness Map** (Grayscale)
- **Metallic Map** (Optional, if the object is metallic)

### 2. Import & Extract Materials
When importing a `.fbx` model that has materials embedded:

1. Select the model in the **Project** window.
2. Go to the **Inspector** > **Materials** tab.
3. Click **Extract Materials...**.
4. Save the material to `Assets/Materials/`.

### 3. Pack Textures (Roughness -> Smoothness)
We use a custom tool to convert the textures.

1. In Unity, go to the top menu: **Tools > Texture Packer (Roughness to Smoothness)**.
2. **Roughness Map**: Drag and drop your exported Roughness texture here (Required).
3. **Metallic Map**: Drag and drop your Metallic texture here (Optional).
   - *If you don't have a metallic map, the tool will create a black metallic map with the smoothness in the alpha.*
4. Click **Pack Textures**.

**Result:** A new texture will be created in the same folder named `[OriginalName]_MetallicSmoothness.png`.

### 4. Material Configuration
1. Select your extracted Material in Unity.
2. **Albedo**: Assign your Base Color texture.
3. **Metallic**: Assign the **new `_MetallicSmoothness` texture**.
4. **Smoothness Source**: Ensure the dropdown next to the slider is set to **Metallic Alpha**.
5. **Normal Map**: Assign your Normal map.
   - *Note: If the normal map looks purple/wrong in the inspector, click "Fix Now" to mark it as a Normal Map.*

### 5. Troubleshooting
- **Object looks too shiny?** Check the Smoothness slider (it acts as a multiplier). Usually, keep it at 1.
- **Object looks too dark?** Ensure the Metallic map isn't pure white if the object isn't metal.
- **Normal map looks inverted?** In the texture import settings for the Normal map, try checking "Create from Grayscale" (rarely needed) or ensure it was exported as OpenGL (Y+) not DirectX (Y-).
