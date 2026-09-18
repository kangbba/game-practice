using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 리그의 팔다리 본 이름을 좌우로 통일한다.
    ///
    /// 원래 이름은 아트팩이 붙인 것으로, 다리는 `FrontLeg`/`RearLeg`, 팔은 `Arm`/`BackArm` 이었다 —
    /// 어휘가 둘로 갈렸고, 무엇보다 "앞/뒤" 는 정체가 아니라 아이들 포즈다(걷기에서 두 다리가 번갈아 앞으로 나간다).
    /// 실제로 고정된 건 깊이다: sortingOrder 가 높은 쪽이 카메라에 가깝고, 리그가 +x 를 보고 서 있으므로
    /// 가까운 쪽이 캐릭터의 오른쪽이다. 세 히어로 모두 같은 배치였다.
    ///
    /// 클립이 본을 경로 문자열로 가리키고 .anim 안에 그 경로의 해시까지 캐시돼 있어서,
    /// YAML 을 고치는 방식으로는 못 바꾼다 — 해시가 어긋나 에디터에선 맞아 보이고 재생 때 깨진다.
    /// 그래서 AnimationUtility 로 커브를 떼었다 다시 건다.
    /// </summary>
    public static class RigBoneRenamer
    {
        /// <summary>경로를 세그먼트로 쪼개 정확히 일치하는 것만 바꾼다 — 문자열 치환이면 BackArm·Forearm 이 오염된다.</summary>
        private static readonly (string From, string To)[] Renames =
        {
            ("FrontLeg", "RightLeg"),
            ("RearLeg", "LeftLeg"),
            ("Arm", "RightArm"),
            ("BackArm", "LeftArm"),
        };

        private static readonly string[] PrefabRoots =
        {
            "Assets/Game/Characters/Heroes",
            "Assets/Game/Characters/Enemies",
            "Assets/DarkFantasy2D/Prefabs/Characters",
        };

        private const string ClipRoot = "Assets/DarkFantasy2D/Animations";

        [MenuItem("★Sayne★/4. 리그 본 좌우 통일 (Front·Rear·BackArm → Right·Left)", false, 4)]
        public static void Rename()
        {
            var curves = RebindClips();
            var bones = RenameBones();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"RigBoneRenamer: 본 {bones}개, 클립 커브 {curves}개를 갈아끼웠다.");
            Verify();
        }

        /// <summary>
        /// 점검에서 나온 끊긴 바인딩을 실제로 없앤다. 이름만 어긋난 건 Rename 이 고치고,
        /// 그래도 남은 건 리그에서 사라진 본을 가리키는 죽은 커브다(영웅의 Hair — 헤어는 이제 Head 아래 스프라이트다).
        /// 그 클립을 쓰는 리그 중 어디에도 경로가 없을 때만 지운다 — 한 리그에만 있는 본은 건드리지 않는다.
        /// </summary>
        [MenuItem("★Sayne★/공통/리그 바인딩 수리", false, 55)]
        public static void Repair()
        {
            var bones = RenameBones();
            var dropped = DropDeadCurves();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"RigBoneRenamer: 본 {bones}개를 고치고, 죽은 커브 {dropped}개를 걷어냈다.");
            Verify();
        }

        /// <summary>클립별로 그 클립을 재생하는 리그들의 경로 집합을 모은다 — 한 클립을 여러 리그가 나눠 쓴다.</summary>
        private static Dictionary<AnimationClip, HashSet<string>> CollectClipPaths()
        {
            var known = new Dictionary<AnimationClip, HashSet<string>>();

            foreach (var prefabPath in PrefabPaths())
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (root == null) continue;

                var animator = root.GetComponentInChildren<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null) continue;

                var paths = new HashSet<string>();
                CollectPaths(animator.transform, "", paths);

                foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    if (clip == null) continue;

                    if (!known.TryGetValue(clip, out var union))
                    {
                        union = new HashSet<string>();
                        known[clip] = union;
                    }

                    union.UnionWith(paths);
                }
            }

            return known;
        }

        private static int DropDeadCurves()
        {
            var dropped = 0;

            foreach (var pair in CollectClipPaths())
            {
                var clip = pair.Key;
                var paths = pair.Value;
                var touched = false;

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.path.Length == 0 || paths.Contains(binding.path)) continue;

                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    Debug.Log($"[수리] {clip.name}: '{binding.path}' 커브를 걷어냈다 — 리그에 없는 본이다");
                    dropped++;
                    touched = true;
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.path.Length == 0 || paths.Contains(binding.path)) continue;

                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                    Debug.Log($"[수리] {clip.name}: '{binding.path}' 스프라이트 커브를 걷어냈다 — 리그에 없는 본이다");
                    dropped++;
                    touched = true;
                }

                if (touched) EditorUtility.SetDirty(clip);
            }

            return dropped;
        }

        /// <summary>
        /// 돌리기 전·후 아무 때나 눌러 상태를 본다. 클립이 가리키는 경로가 그 리그에 실제로 있는지만 확인한다 —
        /// 이름을 바꾸다 한쪽만 바뀌면 여기서 바로 드러난다.
        /// </summary>
        [MenuItem("★Sayne★/공통/리그 바인딩 점검", false, 54)]
        public static void Verify()
        {
            var broken = 0;
            var checkedClips = 0;
            var seen = new HashSet<AnimationClip>();

            foreach (var prefabPath in PrefabPaths())
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (root == null) continue;

                var animator = root.GetComponentInChildren<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    Debug.Log($"[점검] {root.name}: Animator 나 컨트롤러가 없다 — 건너뜀");
                    continue;
                }

                // 클립 경로는 Animator 가 붙은 오브젝트 기준이다.
                var paths = new HashSet<string>();
                CollectPaths(animator.transform, "", paths);

                foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    if (clip == null) continue;
                    checkedClips++;
                    seen.Add(clip);

                    foreach (var path in ClipPaths(clip))
                    {
                        if (path.Length > 0 && !paths.Contains(path))
                        {
                            Debug.LogError($"[점검] {root.name} / {clip.name}: '{path}' 를 리그에서 못 찾는다");
                            broken++;
                        }
                    }
                }
            }

            ReportOrphanClips(seen);

            if (broken == 0)
            {
                Debug.Log($"[점검] 클립 {checkedClips}개의 경로가 모두 리그와 맞는다.");
            }
            else
            {
                Debug.LogError($"[점검] 끊긴 바인딩 {broken}개. 위 로그의 경로를 확인해라.");
            }
        }

        /// <summary>
        /// 어떤 컨트롤러도 안 거는 클립은 점검도 수리도 못 받는다 — 조용히 썩으니 이름이라도 띄워 둔다.
        /// Archive 는 일부러 남겨 둔 옛 버전이라 셈에서 뺀다.
        /// </summary>
        private static void ReportOrphanClips(HashSet<AnimationClip> used)
        {
            var orphans = AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.Contains("/Archive/"))
                .Where(path => !used.Contains(AssetDatabase.LoadAssetAtPath<AnimationClip>(path)))
                .ToArray();

            if (orphans.Length == 0) return;

            Debug.LogWarning($"[점검] 컨트롤러가 안 쓰는 클립 {orphans.Length}개 — 점검 대상이 아니다:\n{string.Join("\n", orphans)}");
        }

        private static int RebindClips()
        {
            var moved = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                // GetCurveBindings 는 복사본을 준다 — 도는 중에 고쳐도 안전하다.
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var rewritten = Rewrite(binding.path);
                    if (rewritten == binding.path) continue;

                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    AnimationUtility.SetEditorCurve(clip, WithPath(binding, rewritten), curve);
                    moved++;
                }

                // 스프라이트를 갈아 끼우는 커브는 따로 들고 있다.
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var rewritten = Rewrite(binding.path);
                    if (rewritten == binding.path) continue;

                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                    AnimationUtility.SetObjectReferenceCurve(clip, WithPath(binding, rewritten), keys);
                    moved++;
                }

                EditorUtility.SetDirty(clip);
            }

            return moved;
        }

        private static int RenameBones()
        {
            var renamed = 0;

            foreach (var path in PrefabPaths())
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                var touched = 0;

                try
                {
                    foreach (var transform in contents.GetComponentsInChildren<Transform>(true))
                    {
                        foreach (var (from, to) in Renames)
                        {
                            if (transform.name == from)
                            {
                                transform.name = to;
                                touched++;
                                break;
                            }
                        }
                    }

                    if (touched > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        renamed += touched;
                        Debug.Log($"RigBoneRenamer: {path} — 본 {touched}개");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            return renamed;
        }

        private static IEnumerable<string> PrefabPaths()
        {
            var roots = PrefabRoots.Where(AssetDatabase.IsValidFolder).ToArray();
            if (roots.Length == 0)
            {
                yield break;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", roots))
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        private static IEnumerable<string> ClipPaths(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip).Select(b => b.path)
                .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip).Select(b => b.path))
                .Distinct();
        }

        private static void CollectPaths(Transform root, string prefix, HashSet<string> into)
        {
            foreach (Transform child in root)
            {
                var path = prefix.Length == 0 ? child.name : $"{prefix}/{child.name}";
                into.Add(path);
                CollectPaths(child, path, into);
            }
        }

        private static string Rewrite(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            var parts = path.Split('/');
            for (var i = 0; i < parts.Length; i++)
            {
                foreach (var (from, to) in Renames)
                {
                    if (parts[i] == from)
                    {
                        parts[i] = to;
                        break;
                    }
                }
            }

            return string.Join("/", parts);
        }

        private static EditorCurveBinding WithPath(EditorCurveBinding binding, string path)
        {
            return new EditorCurveBinding
            {
                path = path,
                type = binding.type,
                propertyName = binding.propertyName,
            };
        }
    }
}
