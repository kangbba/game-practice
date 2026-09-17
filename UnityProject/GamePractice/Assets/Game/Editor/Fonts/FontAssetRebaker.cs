using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// TMP 폰트 에셋을 더 큰 크기로 다시 굽는다. 32pt 로 구운 글자를 54pt 로 늘려 쓰다 보니 뭉개져 보였다.
    ///
    /// 같은 에셋 파일에 덮어쓰되, 아틀라스 텍스처 오브젝트는 그대로 두고 그 안의 픽셀만 갈아 끼운다 —
    /// 텍스처를 새로 만들어 붙이면 파일 안에서의 번호가 바뀌어 머티리얼 6 종이 전부 참조를 잃는다.
    /// </summary>
    public static class FontAssetRebaker
    {
        private const string SourcePath = "Assets/Fonts/SB_Aggro_Bold.otf";
        private const string TargetPath = "Assets/Fonts/TMP/SB_Aggro_Bold SDF.asset";

        /// <summary>굽는 크기. HUD 에서 가장 큰 글자가 54pt 라 그보다 넉넉해야 한다.</summary>
        private const int SamplingPointSize = 64;

        /// <summary>글자 사이 여백. 클수록 외곽선·그림자를 크게 줘도 안 깨진다.</summary>
        private const int AtlasPadding = 8;

        private const int AtlasSize = 4096;

        [MenuItem("★Sayne★/3. 폰트 다시 굽기", false, 3)]
        public static void Rebake()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            var target = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetPath);

            // 새 크기로 빈 폰트를 만들고, 프로젝트에서 실제로 쓰는 글자만 구워 넣는다.
            var baked = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, AtlasSize, AtlasSize,
                AtlasPopulationMode.Dynamic);

            var characters = CollectCharacters();
            baked.TryAddCharacters(characters, out var missing);

            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"아틀라스에 못 들어간 글자가 있다 ({missing.Length}자): {missing}");
            }

            CopyInto(baked, target);
            UpdateMaterials(target);

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"폰트 재굽기 완료 — {SamplingPointSize}pt / {AtlasSize}x{AtlasSize} / 글자 {characters.Length}자");
        }

        /// <summary>
        /// 프로젝트 코드와 프리팹에 실제로 등장하는 글자를 모은다. 전각 한글 전부를 구우면 아틀라스가 모자란다.
        /// 여기 없는 글자는 런타임에 원본 폰트에서 그때그때 구워 채운다(Dynamic) — 네모로 뜨지 않는다.
        /// </summary>
        private static string CollectCharacters()
        {
            var used = new HashSet<char>();

            // 아스키는 통째로. 숫자·영문·기호는 어디서든 튀어나온다.
            for (var c = ' '; c <= '~'; c++)
            {
                used.Add(c);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Script t:Prefab", new[] { "Assets/Game", "Assets/SayneAssets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                foreach (var c in System.IO.File.ReadAllText(path))
                {
                    // 한글 음절·자모와 흔히 섞여 나오는 기호만 담는다.
                    if (c >= 0xAC00 && c <= 0xD7A3 || c >= 0x3131 && c <= 0x318E || c == '×' || c == '·' || c == '…')
                    {
                        used.Add(c);
                    }
                }
            }

            var builder = new StringBuilder(used.Count);

            foreach (var c in used)
            {
                builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 구운 결과를 기존 에셋에 옮겨 담는다. 텍스처와 머티리얼 참조는 건드리지 않고,
        /// 아틀라스 픽셀만 기존 텍스처 위에 덮어쓴다.
        /// </summary>
        private static void CopyInto(TMP_FontAsset baked, TMP_FontAsset target)
        {
            var from = new SerializedObject(baked);
            var to = new SerializedObject(target);

            var property = from.GetIterator();
            property.NextVisible(true);

            do
            {
                // 이 둘만 남긴다 — 텍스처 오브젝트와 머티리얼은 기존 것을 그대로 쓴다.
                if (property.propertyPath == "m_AtlasTextures" || property.propertyPath == "m_Material")
                {
                    continue;
                }

                to.CopyFromSerializedProperty(property);
            }
            while (property.NextVisible(false));

            to.ApplyModifiedPropertiesWithoutUndo();

            CopyAtlasPixels(baked.atlasTextures[0], target.atlasTextures[0]);
        }

        /// <summary>
        /// 이 폰트를 쓰는 머티리얼 전부를 새 아틀라스에 맞춘다.
        /// 셰이더는 여기 적힌 크기·여백으로 아틀라스를 읽는다 — 텍스처만 갈고 두면 엉뚱한 자리를 읽어 흐려진다.
        /// </summary>
        private static void UpdateMaterials(TMP_FontAsset font)
        {
            var folder = System.IO.Path.GetDirectoryName(TargetPath);

            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));

                if (!material.HasProperty(ShaderUtilities.ID_TextureWidth))
                {
                    continue;
                }

                material.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
                material.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
                material.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
                material.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);

                // 외곽선·그림자 두께가 글자 크기를 따라가도록 다시 센다.
                ShaderUtilities.UpdateShaderRatios(material);

                EditorUtility.SetDirty(material);
            }
        }

        /// <summary>
        /// 아틀라스를 기존 텍스처 오브젝트 위에 덮어쓴다. 픽셀을 직접 쓰면 "읽기 불가" 로 막히므로
        /// 에디터에서 직렬화 데이터째 복사한다 — 오브젝트가 그대로라 머티리얼 참조도 그대로다.
        /// </summary>
        private static void CopyAtlasPixels(Texture2D from, Texture2D to)
        {
            var name = to.name;

            EditorUtility.CopySerialized(from, to);
            to.name = name;

            // 구운 텍스처는 읽기 가능 상태다. 그대로 두면 빌드에 사본이 하나 더 들어간다.
            var serialized = new SerializedObject(to);
            serialized.FindProperty("m_IsReadable").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(to);
        }
    }
}
