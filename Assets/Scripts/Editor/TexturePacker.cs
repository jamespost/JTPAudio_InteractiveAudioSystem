using UnityEngine;
using UnityEditor;
using System.IO;

public class TexturePacker : EditorWindow
{
    private Texture2D metallicMap;
    private Texture2D roughnessMap;
    private string savePath = "Assets/Textures/";

    [MenuItem("Tools/Texture Packer (Roughness to Smoothness)")]
    public static void ShowWindow()
    {
        GetWindow<TexturePacker>("Texture Packer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Pack Roughness into Metallic Alpha", EditorStyles.boldLabel);
        GUILayout.Space(10);

        metallicMap = (Texture2D)EditorGUILayout.ObjectField("Metallic Map (Optional)", metallicMap, typeof(Texture2D), false);
        roughnessMap = (Texture2D)EditorGUILayout.ObjectField("Roughness Map (Required)", roughnessMap, typeof(Texture2D), false);

        GUILayout.Space(20);

        if (GUILayout.Button("Pack Textures"))
        {
            if (roughnessMap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Roughness Map.", "OK");
                return;
            }

            PackTextures();
        }
    }

    private void PackTextures()
    {
        string path = AssetDatabase.GetAssetPath(roughnessMap);
        string dir = Path.GetDirectoryName(path);
        string filename = Path.GetFileNameWithoutExtension(path);
        string newPath = Path.Combine(dir, filename + "_MetallicSmoothness.png");

        // Make textures readable
        SetTextureReadable(roughnessMap, true);
        if (metallicMap != null) SetTextureReadable(metallicMap, true);

        int width = roughnessMap.width;
        int height = roughnessMap.height;

        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] roughPixels = roughnessMap.GetPixels();
        Color[] metalPixels = metallicMap != null ? metallicMap.GetPixels() : new Color[roughPixels.Length];
        Color[] finalPixels = new Color[roughPixels.Length];

        for (int i = 0; i < roughPixels.Length; i++)
        {
            // Metallic: Use Red channel of metallic map, or 0 if none
            float metal = metallicMap != null ? metalPixels[i].r : 0f;

            // Smoothness: Invert Roughness (1 - r)
            // Usually roughness is grayscale, so r=g=b. We use r.
            float roughness = roughPixels[i].r;
            float smoothness = 1.0f - roughness;

            // Unity Standard / URP Lit expects:
            // R, G, B = Metallic (usually just R is used, but we can fill RGB)
            // A = Smoothness
            finalPixels[i] = new Color(metal, metal, metal, smoothness);
        }

        output.SetPixels(finalPixels);
        output.Apply();

        byte[] bytes = output.EncodeToPNG();
        File.WriteAllBytes(newPath, bytes);

        // Cleanup: Restore readability settings if desired (skipping for simplicity)
        
        AssetDatabase.Refresh();
        
        // Select the new texture
        Texture2D newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
        EditorGUIUtility.PingObject(newTexture);
        
        Debug.Log($"Created MetallicSmoothness map at: {newPath}");
    }

    private void SetTextureReadable(Texture2D tex, bool isReadable)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null && importer.isReadable != isReadable)
        {
            importer.isReadable = isReadable;
            importer.SaveAndReimport();
        }
    }
}
