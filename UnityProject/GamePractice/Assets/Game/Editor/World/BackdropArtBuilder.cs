using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>배경 4겹(하늘·먼 능선·가까운 능선·안개)의 원판을 코드로 만든다. 지면 팔레트(어두운 이끼·흙)에 맞춘 색이다.</summary>
    internal static class BackdropArtBuilder
    {
        internal const string ArtFolder = "Assets/Game/Art/Backdrop";

        /// <summary>능선 원판의 PPU. 1024px / 16 = 월드 64 유닛이라 배치할 때 스케일을 안 건드려도 된다.</summary>
        private const int RidgePPU = 16;

        private static readonly Color SkyTop = new Color(0.045f, 0.055f, 0.105f);
        private static readonly Color SkyMid = new Color(0.115f, 0.145f, 0.225f);
        private static readonly Color SkyHorizon = new Color(0.34f, 0.33f, 0.33f);

        internal static void Build()
        {
            Directory.CreateDirectory(ArtFolder);
            AssetDatabase.Refresh();

            WriteSky();
            WriteRidge("RidgeFar", seed: 3.7f, baseHeight: 22f, peak: 46f, roughness: 1f,
                bottom: new Color(0.20f, 0.23f, 0.31f), top: new Color(0.13f, 0.16f, 0.24f));
            WriteRidge("RidgeNear", seed: 11.3f, baseHeight: 16f, peak: 30f, roughness: 1.9f,
                bottom: new Color(0.10f, 0.12f, 0.17f), top: new Color(0.06f, 0.07f, 0.11f));
            WriteHaze();
        }

        /// <summary>화면 기준 세로 그라디언트. 아래가 지평선, 위가 천정이다.</summary>
        private static void WriteSky()
        {
            const int width = 64;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var t = (float)y / (height - 1);
                var color = t < 0.38f
                    ? Color.Lerp(SkyHorizon, SkyMid, t / 0.38f)
                    : Color.Lerp(SkyMid, SkyTop, (t - 0.38f) / 0.62f);
                for (var x = 0; x < width; x++) pixels[y * width + x] = color;
            }
            Save("Sky", texture, pixels, 100, SpriteAlignment.Center);
        }

        /// <summary>능선 실루엣. 아래쪽은 불투명, 능선 위는 투명이다. 피벗이 아래라 지면 끝에 바로 얹을 수 있다.</summary>
        private static void WriteRidge(string name, float seed, float baseHeight, float peak, float roughness,
            Color bottom, Color top)
        {
            const int width = 1024;
            const int height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var x = 0; x < width; x++)
            {
                var u = (float)x / width;
                var broad = Mathf.PerlinNoise(u * 4f * roughness + seed, seed);
                var fine = Mathf.PerlinNoise(u * 13f * roughness + seed, seed + 7.1f);
                var line = baseHeight + peak * (0.68f * broad + 0.32f * fine);
                for (var y = 0; y < height; y++)
                {
                    var color = Color.Lerp(bottom, top, Mathf.Clamp01(y / line));
                    color.a = Mathf.Clamp01(line - y + 0.5f);
                    pixels[y * width + x] = color;
                }
            }
            Save(name, texture, pixels, RidgePPU, SpriteAlignment.BottomCenter);
        }

        /// <summary>지면 먼 쪽을 하늘색으로 녹이는 띠. 위가 진하고 아래로 갈수록 투명해진다.</summary>
        private static void WriteHaze()
        {
            const int width = 64;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var t = (float)y / (height - 1);
                var color = SkyHorizon;
                color.a = t * t;
                for (var x = 0; x < width; x++) pixels[y * width + x] = color;
            }
            Save("Haze", texture, pixels, RidgePPU, SpriteAlignment.BottomCenter);
        }

        private static void Save(string name, Texture2D texture, Color[] pixels, int pixelsPerUnit, SpriteAlignment alignment)
        {
            texture.SetPixels(pixels);
            texture.Apply();
            var path = $"{ArtFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
