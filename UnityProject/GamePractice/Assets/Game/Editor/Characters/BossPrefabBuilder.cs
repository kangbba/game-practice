using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 오우거 리그를 바탕으로 보스 프리팹 변형(Variant)을 만든다. 원본 리그를 고치면 변형도 따라온다.
    /// 만든 뒤에는 [★Sayne★/2. 어드레서블 셋업] 을 한 번 돌려 주소를 등록해야 한다.
    /// </summary>
    public static class BossPrefabBuilder
    {
        private const string SourcePath = "Assets/Game/Characters/Enemies/Ogre/Ogre.prefab";
        private const string OutputFolderParent = "Assets/Game/Characters/Enemies";
        private const string OutputFolderName = "OgreBoss";
        private const string OutputPath = OutputFolderParent + "/" + OutputFolderName + "/OgreBoss.prefab";

        /// <summary>보스는 같은 리그를 키워서 쓴다. 덩치가 보스임을 말하게 한다.</summary>
        private const float Scale = 4.2f;

        /// <summary>살짝 붉은 기운. 같은 오우거라도 한눈에 보스로 읽히게 한다.</summary>
        private static readonly Color Tint = new Color(1f, 0.76f, 0.7f);

        public static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null)
            {
                Debug.LogError($"BossPrefabBuilder: {SourcePath} 를 찾을 수 없다");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = OutputFolderName;
            instance.transform.localScale = Vector3.one * Scale;

            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = Tint;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolderParent + "/" + OutputFolderName))
            {
                AssetDatabase.CreateFolder(OutputFolderParent, OutputFolderName);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, OutputPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();

            Debug.Log($"BossPrefabBuilder: {OutputPath} 생성 완료");
        }
    }
}
