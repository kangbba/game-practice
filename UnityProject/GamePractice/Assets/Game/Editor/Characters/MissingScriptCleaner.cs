using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 스크립트가 사라진 컴포넌트를 찾아 걷어낸다.
    ///
    /// 컴포넌트는 스크립트를 GUID 로 가리키는데, .cs 를 지우면 그 GUID 를 아무도 안 가져서 "Missing (Mono Script)" 로 남는다.
    /// 아트팩의 샘플 스크립트(CharacterRig·EffectLifetime·CombatHUD…)를 걷어내고 남은 껍데기가 이 경우다.
    /// 인스펙터에 경고로만 뜨고 빌드는 통과해서, 안 보이면 계속 쌓인다.
    ///
    /// 일회용 정리 도구다 — 콘솔이 깨끗해지면 이 파일째 지워도 된다.
    /// </summary>
    public static class MissingScriptCleaner
    {
        private static readonly string[] Roots = { "Assets/Game", "Assets/DarkFantasy2D", "Assets/Scenes" };

        [MenuItem("★Sayne★/공통/미싱 스크립트 점검", false, 56)]
        public static void Report()
        {
            var total = 0;

            foreach (var path in AssetPaths("t:Prefab"))
            {
                var count = CountInPrefab(path);
                if (count == 0) continue;

                Debug.LogWarning($"[미싱] {path}: 스크립트 없는 컴포넌트 {count}개");
                total += count;
            }

            foreach (var path in AssetPaths("t:Scene"))
            {
                var count = CountInScene(path, out var owners);
                if (count == 0) continue;

                Debug.LogWarning($"[미싱] {path}: 스크립트 없는 컴포넌트 {count}개 — {string.Join(", ", owners)}");
                total += count;
            }

            Debug.Log(total == 0
                ? "[미싱] 스크립트 없는 컴포넌트가 없다."
                : $"[미싱] 모두 {total}개. '미싱 스크립트 걷어내기' 로 지운다.");
        }

        [MenuItem("★Sayne★/공통/미싱 스크립트 걷어내기", false, 57)]
        public static void Clean()
        {
            if (!EditorUtility.DisplayDialog(
                    "미싱 스크립트 걷어내기",
                    "스크립트가 사라진 컴포넌트를 프리팹·씬에서 지운다.\n" +
                    "그 컴포넌트에 들어 있던 값도 같이 사라진다 — 되돌릴 수 없다.\n\n" +
                    "먼저 '미싱 스크립트 점검' 으로 목록을 확인해라.",
                    "지운다", "그만둔다"))
            {
                return;
            }

            var removed = 0;

            foreach (var path in AssetPaths("t:Prefab"))
            {
                var contents = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    var count = contents.GetComponentsInChildren<Transform>(true)
                        .Sum(t => GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject));

                    if (count == 0) continue;

                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Debug.Log($"[미싱] {path}: {count}개 걷어냈다");
                    removed += count;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            // 씬은 열어야 손댈 수 있다. 저장 안 한 변경이 있으면 여기서 물어보고, 끝나면 원래 씬으로 돌려놓는다.
            var opened = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var path in AssetPaths("t:Scene"))
            {
                if (CountInScene(path, out _) == 0) continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var count = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Sum(t => GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject));

                if (count == 0) continue;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[미싱] {path}: {count}개 걷어냈다");
                removed += count;
            }

            if (opened.Length > 0 && EditorSceneManager.GetActiveScene().path != opened)
            {
                EditorSceneManager.OpenScene(opened, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"MissingScriptCleaner: 모두 {removed}개를 걷어냈다.");
        }

        private static IEnumerable<string> AssetPaths(string filter)
        {
            var roots = Roots.Where(AssetDatabase.IsValidFolder).ToArray();
            if (roots.Length == 0) return Enumerable.Empty<string>();

            return AssetDatabase.FindAssets(filter, roots).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        }

        private static int CountInPrefab(string path)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) return 0;

            return root.GetComponentsInChildren<Transform>(true)
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        }

        /// <summary>씬은 열지 않고 YAML 만 읽는다 — 점검 한 번에 씬을 다 열어젖히면 작업 중인 씬이 날아간다.</summary>
        private static int CountInScene(string path, out List<string> owners)
        {
            owners = new List<string>();

            var text = System.IO.File.ReadAllText(path);
            var count = 0;

            foreach (System.Text.RegularExpressions.Match match in
                     System.Text.RegularExpressions.Regex.Matches(
                         text, @"m_Script: \{fileID: \d+, guid: ([a-f0-9]{32}), type: 3\}\s*\n\s*m_Name:.*\n\s*m_EditorClassIdentifier: (.*)"))
            {
                if (ScriptExists(match.Groups[1].Value)) continue;

                count++;
                var id = match.Groups[2].Value.Trim();
                if (id.Length > 0 && !owners.Contains(id)) owners.Add(id);
            }

            return count;
        }

        private static bool ScriptExists(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return path.Length > 0 && AssetDatabase.LoadAssetAtPath<MonoScript>(path) != null;
        }
    }
}
