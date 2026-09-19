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

            // 게이지는 9슬라이스로 폭만 늘어난다. 모서리 사선(6px)이 border(10px) 안에 온전히 들어가야
            // 늘어나는 가운데 영역에 사선이 안 걸린다. 사선 12px + border 8px 조합은 양끝이 길게 뭉개졌다.
            Write("Gauge", 64, 32, false, true, new Vector4(10, 10, 10, 10), 6f);

            WriteVignette();
            WriteGlow();
            WriteDashIcon();
        }

        /// <summary>
        /// 가젯(돌진) 버튼 아이콘 — 겹친 화살 두 개와 뒤로 끌리는 속도선. 다른 아이콘처럼 짙은 외곽선에 색을 채운다.
        /// 그린 아이콘이 생기기 전까지 쓰는 자리 채움이다. 같은 이름(Icons/Dash)으로 덮어 넣으면 그대로 바뀐다.
        /// </summary>
        internal static void WriteDashIcon()
        {
            const int size = 192;
            const float stroke = 16f;
            const float lineStroke = 8f;
            const float outline = 7f;

            var strokes = new[]
            {
                (new Vector2(76f, 170f), new Vector2(128f, 96f), stroke),
                (new Vector2(128f, 96f), new Vector2(76f, 22f), stroke),
                (new Vector2(122f, 170f), new Vector2(174f, 96f), stroke),
                (new Vector2(174f, 96f), new Vector2(122f, 22f), stroke),
                (new Vector2(22f, 140f), new Vector2(62f, 140f), lineStroke),
                (new Vector2(14f, 96f), new Vector2(72f, 96f), lineStroke),
                (new Vector2(22f, 52f), new Vector2(62f, 52f), lineStroke),
            };

            var fillBottom = new Color(0.12f, 0.62f, 0.52f);
            var fillTop = new Color(0.72f, 1f, 0.84f);
            var outlineColor = new Color(0.03f, 0.07f, 0.09f);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);

                    // 모든 획 중 가장 가까운 획의 가장자리까지 거리. 음수면 획 안이다.
                    var distance = float.MaxValue;
                    foreach (var (from, to, width) in strokes)
                    {
                        distance = Mathf.Min(distance, DistanceToSegment(point, from, to) - width);
                    }

                    var fill = Color.Lerp(fillBottom, fillTop, (float)y / size);
                    var color = Color.Lerp(fill, outlineColor, Mathf.Clamp01(distance + 0.5f));
                    color.a = Mathf.Clamp01(outline + 0.5f - distance);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Save("Dash", texture, Vector4.zero, "Icons");
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            var segment = to - from;
            var t = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, from + segment * t);
        }

        /// <summary>
        /// 가운데는 꽉 차고 가장자리로 갈수록 부드럽게 사라지는 네모. 9슬라이스로 늘려 패널 뒤에 깔면 후광,
        /// 앞에 깔면 번쩍이는 섬광이 된다. 흰색이라 쓰는 쪽이 물들인다.
        /// </summary>
        private static void WriteGlow()
        {
            const int size = 128;

            // 가장자리에서 이만큼 안쪽부터 완전히 불투명하다. 9슬라이스 border 와 같아야 늘려도 번짐 폭이 그대로다.
            const float falloff = 40f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y)) + 0.5f;
                    var alpha = Mathf.SmoothStep(0f, 1f, edge / falloff);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Save("Glow", texture, Vector4.one * falloff);
        }

        /// <summary>
        /// 가장자리만 짙고 가운데로 갈수록 투명해지는 막. 저체력 경고가 이걸 붉게 물들여 쓴다.
        /// 화면 비율이 제각각이라 늘여 쓰는 그림이다 — 네모난 테두리가 아니라 부드러운 그라디언트로 둔다.
        /// </summary>
        private static void WriteVignette()
        {
            const int size = 256;

            // 이 비율 안쪽은 완전히 투명하다. 시야를 가리지 않으려면 가운데가 넓게 비어야 한다.
            const float clearRatio = 0.55f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2(size, size) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // 가운데에서 얼마나 멀리 왔나를 0~1 로. 모서리가 1 을 넘으므로 잘라 쓴다.
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size * 0.5f);
                    var edge = Mathf.InverseLerp(clearRatio, 1f, Mathf.Clamp01(distance));

                    // 제곱해서 가장자리에 몰아준다. 선형으로 두면 가운데까지 뿌옇다.
                    pixels[y * size + x] = new Color(1f, 1f, 1f, edge * edge);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Save("Vignette", texture, Vector4.zero);
        }

        /// <param name="corner">사각형 모서리를 깎는 45도 사선의 크기(px). 9슬라이스 border 보다 작아야 한다.</param>
        private static void Write(string name, int width, int height, bool isCircle, bool isOverlay, Vector4 border,
            float corner = 12f)
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
                        : Mathf.Min(Mathf.Min(dx, dy), (dx + dy - corner) * 0.7071f) - 1f;
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
            Save(name, texture, border);
        }

        private static void Save(string name, Texture2D texture, Vector4 border, string folder = "Frames")
        {
            var path = $"{ArtFolder}/{folder}/{name}.png";
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
