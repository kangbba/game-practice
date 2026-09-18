using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 드랍 구슬 프리팹 빌더. 속이 비치는 유리구슬 스프라이트를 굽고,
    /// 그 안에 초상화가 들어가는 프리팹을 만든다. 초상화는 런타임에 주입된다.
    /// </summary>
    internal static class DropItemBuilder
    {
        private const string ArtFolder = "Assets/Game/Drops/Art";
        private const string OrbPath = ArtFolder + "/Orb.png";
        private const string PortraitFolder = ArtFolder + "/Portraits";
        private const string GoldPath = PortraitFolder + "/Gold.png";
        private const string PrefabPath = "Assets/Game/Drops/DropItem.prefab";
        private const string EnemiesRoot = "Assets/Game/Characters/Enemies";

        private const int OrbResolution = 256;
        private const float PixelsPerUnit = 256f;

        /// <summary>구슬 뒤에 깔리는 은은한 빛. 초상화보다 뒤, 유리 표면보다 앞이라 순서가 셋으로 갈린다.</summary>
        private const int GlowOrder = 20;
        private const int PortraitOrder = 21;
        private const int GlassOrder = 22;

        // 끝난 일회성 작업이라 메뉴에서 내렸다. 다시 돌릴 일이 생기면 MenuItem 을 잠깐 붙인다.
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(PortraitFolder))
            {
                Directory.CreateDirectory(PortraitFolder);
                AssetDatabase.Refresh();
            }

            var orb = BakeOrb();
            BakeCoin();

            var root = new GameObject("DropItem");
            try
            {
                var glow = AddSprite(root, "Glow", orb, GlowOrder);
                glow.color = new Color(0.55f, 0.85f, 1f, 0.35f);
                glow.transform.localScale = Vector3.one * 1.25f;

                var portrait = AddSprite(root, "Portrait", null, PortraitOrder);

                var glass = AddSprite(root, "Glass", orb, GlassOrder);
                glass.color = new Color(0.8f, 0.93f, 1f, 0.55f);

                // 구슬은 늘 카메라를 정면으로 본다. 도는 건 카메라 매니저가 활성 빌보드를 모아 한꺼번에 돌린다.
                root.AddComponent<Billboard>();

                var dropItem = root.AddComponent<DropItem>();
                var serialized = new SerializedObject(dropItem);
                serialized.FindProperty("_portrait").objectReferenceValue = portrait;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"DropItemBuilder: {PrefabPath} 저장 완료");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        private static SpriteRenderer AddSprite(GameObject root, string name, Sprite sprite, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);

            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
            renderer.sortingOrder = order;

            return renderer;
        }

        /// <summary>
        /// 가운데가 비고 테두리로 갈수록 뿌옇게 차오르는 유리구슬. 좌상단에 점광 하이라이트를 얹어
        /// 평평한 원이 아니라 구체로 읽히게 한다.
        /// </summary>
        private static Sprite BakeOrb()
        {
            var texture = new Texture2D(OrbResolution, OrbResolution, TextureFormat.RGBA32, false);
            var pixels = new Color[OrbResolution * OrbResolution];
            var center = new Vector2(OrbResolution, OrbResolution) * 0.5f;
            var radius = OrbResolution * 0.5f - 2f;

            for (var y = 0; y < OrbResolution; y++)
            {
                for (var x = 0; x < OrbResolution; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var distance = Vector2.Distance(point, center) / radius;

                    var color = Color.clear;

                    if (distance <= 1f)
                    {
                        // 유리 두께. 가운데는 거의 투명하고 가장자리로 갈수록 두꺼워 보인다.
                        var thickness = Mathf.Pow(distance, 3.5f);
                        color = new Color(0.72f, 0.88f, 1f, Mathf.Lerp(0.07f, 0.55f, thickness));

                        // 테두리 링.
                        if (distance > 0.9f)
                        {
                            var ring = Mathf.InverseLerp(0.9f, 0.97f, distance) * (1f - Mathf.InverseLerp(0.97f, 1f, distance));
                            color = Color.Lerp(color, new Color(0.85f, 0.95f, 1f, 0.85f), ring);
                        }

                        // 좌상단 하이라이트.
                        var highlight = 1f - Mathf.Clamp01(Vector2.Distance(point, center + new Vector2(-radius * 0.32f, radius * 0.34f)) / (radius * 0.26f));
                        color = Color.Lerp(color, new Color(1f, 1f, 1f, 0.75f), Mathf.Pow(highlight, 1.8f));

                        // 가장자리 한 픽셀이 톱니로 보이지 않게 끝에서 알파를 떨어뜨린다.
                        color.a *= Mathf.Clamp01((1f - distance) * radius);
                    }

                    pixels[y * OrbResolution + x] = color;
                }
            }

            return WriteSprite(texture, pixels, OrbPath);
        }

        /// <summary>골드 드랍이 쓰는 동전. 구슬 안에 들어가는 그림이라 테두리와 안쪽 명암만으로 읽히게 한다.</summary>
        private static void BakeCoin()
        {
            var texture = new Texture2D(OrbResolution, OrbResolution, TextureFormat.RGBA32, false);
            var pixels = new Color[OrbResolution * OrbResolution];
            var center = new Vector2(OrbResolution, OrbResolution) * 0.5f;
            var radius = OrbResolution * 0.5f - 2f;

            var deep = new Color(0.55f, 0.36f, 0.06f);
            var bright = new Color(1f, 0.87f, 0.42f);

            for (var y = 0; y < OrbResolution; y++)
            {
                for (var x = 0; x < OrbResolution; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var distance = Vector2.Distance(point, center) / radius;

                    var color = Color.clear;

                    if (distance <= 1f)
                    {
                        // 위에서 빛을 받는 금속. 위가 밝고 아래로 갈수록 어둡다.
                        color = Color.Lerp(deep, bright, (float)y / OrbResolution);

                        // 테두리 띠와 그 안쪽 원판을 나눠 동전처럼 보이게 한다.
                        if (distance > 0.86f)
                        {
                            color = Color.Lerp(color, deep, 0.55f);
                        }
                        else if (distance > 0.74f)
                        {
                            color = Color.Lerp(color, bright, 0.4f);
                        }

                        color.a = Mathf.Clamp01((1f - distance) * radius);
                    }

                    pixels[y * OrbResolution + x] = color;
                }
            }

            WriteSprite(texture, pixels, GoldPath);
        }

        private static Sprite WriteSprite(Texture2D texture, Color[] pixels, string path)
        {
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
