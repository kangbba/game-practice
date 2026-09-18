using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 오우거 족장(보스) 프리팹을 만든다. 오우거를 바탕으로 하되 Variant 가 아니라 자기완결된 일반 프리팹이다 —
    /// 부위 그림·애니메이터·모션 클립을 전부 족장 폴더로 복사해 새 GUID 로 두고, 프리팹은 그 복사본만 가리킨다.
    /// 오우거를 고쳐도 족장은 따라 바뀌지 않는다(CONVENTIONS 의 자기완결성). 모든 캐릭터가 같이 쓰는 재질만 공유한다.
    /// 다시 돌리면 복사본을 오우거 기준으로 새로 떠서 덮어쓴다. 프리팹 GUID 는 그대로라 어드레서블 등록이 안 끊긴다.
    /// </summary>
    public static class BossPrefabBuilder
    {
        private const string SourcePrefabPath = "Assets/Game/Characters/Enemies/Ogre/Ogre.prefab";
        private const string SourcePrefix = "Ogre";

        private const string BossName = "OgreBoss";
        private const string BossFolder = "Assets/Game/Characters/Enemies/" + BossName;
        private const string ArtFolder = BossFolder + "/Art";
        private const string AnimationFolder = BossFolder + "/Animations";
        private const string OutputPath = BossFolder + "/" + BossName + ".prefab";

        /// <summary>보스는 같은 리그를 키워서 쓴다. 덩치가 보스임을 말하게 한다.</summary>
        private const float Scale = 4.2f;

        /// <summary>살짝 붉은 기운. 같은 오우거라도 한눈에 보스로 읽히게 한다.</summary>
        private static readonly Color Tint = new Color(1f, 0.76f, 0.7f);

        public static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            var sourceController = (AnimatorController)source.GetComponentInChildren<Animator>(true).runtimeAnimatorController;

            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(AnimationFolder);
            AssetDatabase.Refresh();

            // 원본 GUID → 복사본 GUID. 복사한 컨트롤러·프리팹 안의 참조를 이 표로 갈아 끼운다.
            var remap = new Dictionary<string, string>();

            var sprites = source.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.sprite != null)
                .Select(renderer => AssetDatabase.GetAssetPath(renderer.sprite))
                .Distinct();
            foreach (var path in sprites)
            {
                Copy(path, $"{ArtFolder}/{RenameOwner(Path.GetFileName(path))}", remap);
            }

            foreach (var clip in sourceController.animationClips.Distinct())
            {
                var path = AssetDatabase.GetAssetPath(clip);
                Copy(path, $"{AnimationFolder}/{Path.GetFileName(path)}", remap);
            }

            var controllerPath = $"{AnimationFolder}/{BossName}.controller";
            Copy(AssetDatabase.GetAssetPath(sourceController), controllerPath, remap);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = BossName;
            instance.transform.localScale = Vector3.one * Scale;

            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = Tint;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, OutputPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();

            // 복사본끼리만 가리키게 한다 — 컨트롤러는 클립을, 프리팹은 그림·컨트롤러를.
            Retarget(controllerPath, remap);
            Retarget(OutputPath, remap);

            Debug.Log($"BossPrefabBuilder: {OutputPath} — 그림 {sprites.Count()}장·클립·컨트롤러를 복사해 자기완결 프리팹으로 만들었다");
        }

        private static void Copy(string from, string to, Dictionary<string, string> remap)
        {
            AssetDatabase.DeleteAsset(to);
            if (!AssetDatabase.CopyAsset(from, to))
            {
                throw new System.InvalidOperationException($"BossPrefabBuilder: {from} → {to} 복사 실패");
            }

            remap[AssetDatabase.AssetPathToGUID(from)] = AssetDatabase.AssetPathToGUID(to);
        }

        /// <summary>파일 속 원본 GUID 를 복사본 GUID 로 바꿔 쓰고 다시 읽힌다. 파일 안의 오브젝트 ID 는 복사본도 같다.</summary>
        private static void Retarget(string path, Dictionary<string, string> remap)
        {
            var text = File.ReadAllText(path);
            foreach (var (from, to) in remap)
            {
                text = text.Replace($"guid: {from}", $"guid: {to}");
            }

            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>Ogre_Head.png → OgreBoss_Head.png. 주인 이름이 앞에 붙은 파일만 이름을 바꾼다.</summary>
        private static string RenameOwner(string fileName)
        {
            return fileName.StartsWith(SourcePrefix + "_") ? BossName + fileName.Substring(SourcePrefix.Length) : fileName;
        }
    }
}
