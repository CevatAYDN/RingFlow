using UnityEngine;
using UnityEditor;
using System.IO;

namespace RingFlow.Editor
{
    public static class UiSpriteImporter
    {
        [MenuItem("Nexus/Configure UI Sprites")]
        public static void ConfigureSprites()
        {
            string path = "Assets/Resources/UI/Sprites";
            if (!Directory.Exists(path)) return;

            string[] files = Directory.GetFiles(path, "*.png");
            foreach (string file in files)
            {
                // First remove white background to create clean transparent png
                ProcessAndRemoveWhiteBackground(file);

                // Configure Unity import settings
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                    Debug.Log($"[UiSpriteImporter] Configured {file} as UI Sprite.");
                }
            }
            AssetDatabase.Refresh();
        }

        private static void ProcessAndRemoveWhiteBackground(string filePath)
        {
            if (!File.Exists(filePath)) return;

            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(fileData))
            {
                Debug.LogError($"[UiSpriteImporter] Failed to load image {filePath}");
                return;
            }

            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                byte r = pixels[i].r;
                byte g = pixels[i].g;
                byte b = pixels[i].b;

                // Calculate luminance
                float luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;

                // Thresholds for white background removal
                float minThreshold = 190f; // pixels brighter than this become transparent
                float maxThreshold = 255f; // pure white is fully transparent

                if (luminance >= maxThreshold)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
                else if (luminance > minThreshold)
                {
                    float t = 1f - ((luminance - minThreshold) / (maxThreshold - minThreshold));
                    pixels[i].a = (byte)(t * 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, pngBytes);
            Object.DestroyImmediate(tex);
        }
    }
}
