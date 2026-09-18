using TMPro;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>데미지 텍스트 프리팹 빌더. 흰 글자·검은 아웃라인·기울임꼴 UGUI TMP 를 만들어 프리팹으로 저장한다.
    /// 게임이 쓰는 프리팹과, SayneAssets 폴더가 혼자 서도록 같이 두는 예시 프리팹을 각각 만든다.</summary>
    public static class DamageTextBuilder
    {
        private const string PrefabPath = "Assets/Game/UI/DamageText.prefab";
        private const string MaterialPath = "Assets/Game/UI/Art/DamageTextOutline.mat";

        private const string ExamplePrefabPath = "Assets/SayneAssets/UI/DamageText/DamageText.example.prefab";
        private const string ExampleMaterialPath = "Assets/SayneAssets/UI/DamageText/DamageTextOutline.mat";

        private const float FontSize = 48f;
        private const float OutlineWidth = 0.2f;
        private static readonly Vector2 Size = new Vector2(400f, 120f);

        // 끝난 일회성 작업이라 메뉴에서 내렸다. 다시 돌릴 일이 생기면 MenuItem 을 잠깐 붙인다.
        public static void Build()
        {
            BuildOne(PrefabPath, MaterialPath, "DamageText");
            BuildOne(ExamplePrefabPath, ExampleMaterialPath, "DamageText.example");
        }

        private static void BuildOne(string prefabPath, string materialPath, string prefabName)
        {
            var font = TMP_Settings.defaultFontAsset;
            var material = BuildMaterial(font, materialPath);

            var root = new GameObject(prefabName, typeof(RectTransform));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = Size;

                var label = root.AddComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSharedMaterial = material;
                label.fontSize = FontSize;
                label.fontStyle = FontStyles.Bold | FontStyles.Italic;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.raycastTarget = false;
                label.text = "999";

                var damageText = root.AddComponent<DamageText>();
                var serialized = new SerializedObject(damageText);
                serialized.FindProperty("_label").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"DamageTextBuilder: {prefabPath} 저장 완료");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>폰트 기본 머티리얼을 복제해 검은 아웃라인을 얹는다. 기존 에셋이 있으면 값만 갱신한다.</summary>
        private static Material BuildMaterial(TMP_FontAsset font, string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.CopyPropertiesFromMaterial(font.material);
            }

            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, OutlineWidth);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return material;
        }
    }
}
