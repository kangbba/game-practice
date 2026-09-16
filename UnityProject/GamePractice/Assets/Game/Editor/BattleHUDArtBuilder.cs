using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    internal static class BattleHUDArtBuilder
    {
        internal const string ArtFolder = "Assets/Game/UI/Art/BattleHUD";

        internal static void Build()
        {
            Directory.CreateDirectory($"{ArtFolder}/Frames");
            Write("Panel", 256, 128, false, false, new Vector4(20, 20, 20, 20));
            Write("Medallion", 256, 256, true, false, Vector4.zero);
            Write("Ring", 256, 256, true, true, Vector4.zero);
            Write("Gauge", 64, 32, false, true, new Vector4(8, 8, 8, 8));
        }

        private static void Write(string name, int width, int height, bool isCircle, bool isOverlay, Vector4 border)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var dark = new Color(0.025f, 0.038f, 0.075f, 0.96f);
            var light = new Color(0.10f, 0.14f, 0.22f, 0.96f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = Mathf.Min(x, width - 1 - x);
                    var dy = Mathf.Min(y, height - 1 - y);
                    var distance = isCircle
                        ? width * 0.5f - 2f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(width, height) * 0.5f)
                        : Mathf.Min(Mathf.Min(dx, dy), (dx + dy - 12f) * 0.7071f) - 1f;
                    var color = Color.Lerp(dark, light, (float)y / height);
                    if (isOverlay)
                    {
                        color = isCircle ? Color.clear : Color.Lerp(new Color(0.48f, 0.55f, 0.66f), Color.white, (float)y / height);
                    }
                    if (distance < 0f) color = Color.clear;
                    else if ((!isOverlay || isCircle) && distance < 2f)
                        color = Color.Lerp(new Color(0.39f, 0.29f, 0.17f), new Color(0.86f, 0.74f, 0.49f), (float)y / height);
                    else if ((!isOverlay || isCircle) && distance > 5f && distance < 6.5f)
                        color = new Color(0.34f, 0.44f, 0.60f, 0.65f);
                    color.a *= Mathf.Clamp01(distance + 0.5f);
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var path = $"{ArtFolder}/Frames/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
