using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GameSetup.Editor
{
    /// <summary>
    /// Assets/Fonts의 Gothic A1 TTF로 TMP 폰트 에셋(Dynamic)을 만들고 TMP 기본 폰트로 지정한다.
    /// Dynamic이라 한글 글리프를 쓰는 만큼만 아틀라스에 채워 넣으므로 에셋이 가볍다.
    /// setup.py가 이 파일을 Assets/Editor에 깔아두고, 에디터가 처음 로드될 때 한 번 돈다.
    /// </summary>
    public static class DefaultFontSetup
    {
        const string FontDir = "Assets/Fonts";
        const string OutDir = FontDir + "/TMP";
        const string DefaultTtf = FontDir + "/GothicA1-Regular.ttf";

        static readonly string[] Ttfs =
        {
            DefaultTtf,
            FontDir + "/GothicA1-Bold.ttf",
        };

        [InitializeOnLoadMethod]
        static void AutoRunOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(DefaultTtf) && LoadFontAsset(DefaultTtf) == null)
                    Run();
            };
        }

        [MenuItem("Tools/Setup/기본 폰트(Gothic A1) 설정")]
        public static void Run()
        {
            if (!File.Exists(DefaultTtf))
            {
                Debug.LogWarning($"[FontSetup] {DefaultTtf} 없음. setup.py를 먼저 실행할 것.");
                return;
            }

            if (TMP_Settings.instance == null)
            {
                Debug.LogWarning("[FontSetup] TMP 필수 리소스가 없다. " +
                                 "Window > TextMeshPro > Import TMP Essential Resources 실행 후 " +
                                 "Tools > Setup > 기본 폰트(Gothic A1) 설정을 다시 누를 것.");
                return;
            }

            Directory.CreateDirectory(OutDir);
            TMP_FontAsset defaultAsset = null;

            foreach (var ttf in Ttfs)
            {
                if (!File.Exists(ttf)) continue;
                var asset = LoadFontAsset(ttf) ?? CreateFontAsset(ttf);
                if (asset != null && ttf == DefaultTtf)
                    defaultAsset = asset;
            }

            if (defaultAsset == null)
            {
                Debug.LogError("[FontSetup] 기본 폰트 에셋 생성 실패.");
                return;
            }

            var settings = new SerializedObject(TMP_Settings.instance);
            var prop = settings.FindProperty("m_defaultFontAsset");
            if (prop == null)
            {
                Debug.LogWarning("[FontSetup] TMP Settings의 기본 폰트 필드를 못 찾았다. 수동 지정 필요.");
                return;
            }

            prop.objectReferenceValue = defaultAsset;
            settings.ApplyModifiedProperties();
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FontSetup] TMP 기본 폰트 → {defaultAsset.name}");
        }

        static string AssetPathFor(string ttf) =>
            $"{OutDir}/{Path.GetFileNameWithoutExtension(ttf)} SDF.asset";

        static TMP_FontAsset LoadFontAsset(string ttf) =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPathFor(ttf));

        static TMP_FontAsset CreateFontAsset(string ttf)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null)
            {
                Debug.LogWarning($"[FontSetup] 폰트 임포트 안 됨: {ttf}");
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (asset == null)
            {
                Debug.LogError($"[FontSetup] TMP 폰트 에셋 생성 실패: {ttf}");
                return null;
            }

            var path = AssetPathFor(ttf);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);

            // 아틀라스 텍스처와 머티리얼은 서브에셋으로 넣어야 에셋이 온전하다.
            if (asset.atlasTextures != null && asset.atlasTextures.Length > 0)
            {
                asset.atlasTextures[0].name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            }
            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FontSetup] 생성: {path}");
            return asset;
        }
    }
}
