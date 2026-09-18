using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 캐릭터 애니메이터가 런타임이 기대하는 모양인지 본다.
    ///
    /// 런타임(CharacterMotion)의 약속은 셋이다 — 모션은 <b>베이스 레이어의 상태 이름</b>으로 Play 하고,
    /// 길이는 <b>클립 이름</b>으로 찾고, 타격 시점은 클립에 박힌 <b>OnHitFrame</b> 이벤트가 알린다.
    /// 그래서 상태 이름과 클립 이름이 어긋나거나, 상태가 위 레이어에만 있거나, 이벤트 이름이 틀리면
    /// 컴파일도 되고 인스펙터도 멀쩡한데 그 모션만 조용히 안 나온다. 그 어긋남을 찾는다.
    /// </summary>
    public static class AnimatorAudit
    {
        private static readonly string[] CharacterRoots =
        {
            "Assets/Game/Characters/Heroes",
            "Assets/Game/Characters/Enemies",
        };

        /// <summary>누구든 이 상태로 들어간다 — CharacterMotion 이 상태 스트림을 그대로 Play 한다.</summary>
        private static readonly string[] CommonStates = { "Idle", "Walk", "Hit", "Death" };

        [MenuItem("★Sayne★/공통/애니메이터 점검", false, 58)]
        public static void Audit()
        {
            var errors = 0;
            var warnings = 0;

            foreach (var path in Prefabs())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var animator = prefab.GetComponentInChildren<Animator>(true);
                if (animator == null) continue;

                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null)
                {
                    Debug.LogError($"[애니] {prefab.name}: 컨트롤러가 없거나 AnimatorController 가 아니다 ({path})");
                    errors++;
                    continue;
                }

                var isHero = path.Contains("/Heroes/");
                var lines = new List<string>();

                Inspect(prefab.name, controller, isHero, lines, ref errors, ref warnings);

                if (lines.Count > 0)
                {
                    Debug.LogWarning($"[애니] {prefab.name} ({controller.name})\n  " + string.Join("\n  ", lines), prefab);
                }
                else
                {
                    Debug.Log($"[애니] {prefab.name} ({controller.name}): 이상 없다", prefab);
                }
            }

            if (errors == 0 && warnings == 0)
            {
                Debug.Log("[애니] 모든 캐릭터 애니메이터가 런타임 약속과 맞는다.");
            }
            else
            {
                Debug.LogError($"[애니] 문제 {errors}개, 의심 {warnings}개. 위 로그를 봐라.");
            }
        }

        private static void Inspect(string owner, AnimatorController controller, bool isHero, List<string> lines, ref int errors, ref int warnings)
        {
            if (controller.layers.Length == 0)
            {
                lines.Add("레이어가 하나도 없다");
                errors++;
                return;
            }

            var baseLayer = controller.layers[CharacterAnimations.BaseLayer];
            var states = baseLayer.stateMachine.states.Select(child => child.state).ToArray();

            // 1. 베이스 레이어 상태 이름이 겹치면 Play(hash) 가 어느 쪽으로 갈지 알 수 없다.
            foreach (var group in states.GroupBy(state => state.name).Where(group => group.Count() > 1))
            {
                lines.Add($"'{group.Key}' 상태가 베이스 레이어에 {group.Count()}개 — Play 가 어느 쪽으로 갈지 모른다");
                errors++;
            }

            foreach (var state in states)
            {
                var clip = state.motion as AnimationClip;

                // 2. 모션 없는 상태는 그 자리에서 정지 화면이 된다.
                if (state.motion == null)
                {
                    // Empty 는 위 레이어를 비우려고 일부러 두는 빈 상태다.
                    if (state.name != "Empty")
                    {
                        lines.Add($"'{state.name}' 상태에 모션이 없다");
                        errors++;
                    }

                    continue;
                }

                if (clip == null)
                {
                    lines.Add($"'{state.name}' 상태의 모션이 클립이 아니다(블렌드 트리) — CharacterMotion 은 길이를 클립에서 읽는다");
                    warnings++;
                    continue;
                }

                // 3. 길이는 클립 이름으로 찾고 재생은 상태 이름으로 한다 — 둘이 다르면 PlayOnce 가 길이를 못 찾는다.
                if (clip.name != state.name)
                {
                    lines.Add($"'{state.name}' 상태의 클립 이름이 '{clip.name}' 이다 — 이름이 같아야 길이를 찾는다");
                    errors++;
                }

                // 4. 상태 속도를 건드리면 실제 재생 시간이 클립 길이와 달라지는데, 런타임은 클립 길이로 복귀를 잡는다.
                if (!Mathf.Approximately(state.speed, 1f))
                {
                    lines.Add($"'{state.name}' 상태 speed={state.speed:0.##} — 런타임은 클립 길이 그대로 복귀를 잡아서 시점이 어긋난다");
                    warnings++;
                }

                InspectClip(state.name, clip, lines, ref errors, ref warnings);
            }

            // 5. 런타임이 Play 할 이름이 베이스 레이어에 있어야 한다. 위 레이어에만 있으면 HasState 가 false 다.
            var names = new HashSet<string>(states.Select(state => state.name));
            var required = CommonStates.Concat(new[] { CharacterAnimations.ComboNames[0] });

            if (isHero)
            {
                required = required
                    .Concat(CharacterAnimations.ComboNames)
                    .Concat(new[]
                    {
                        CharacterAnimations.SkillMeleeName, CharacterAnimations.SkillRangedName,
                        CharacterAnimations.UltimateMeleeName, CharacterAnimations.UltimateRangedName
                    });
            }

            foreach (var name in required.Distinct().Where(name => !names.Contains(name)))
            {
                lines.Add($"'{name}' 상태가 베이스 레이어에 없다 — 런타임이 이 이름으로 Play 한다");
                errors++;
            }

            // 6. 길이 표는 클립 이름을 열쇠로 쓴다. 이름이 같은 다른 클립이 섞여 있으면 한쪽 길이가 덮인다.
            foreach (var group in controller.animationClips.Where(clip => clip != null).GroupBy(clip => clip.name))
            {
                var lengths = group.Select(clip => clip.length).Distinct().ToArray();
                if (group.Count() > 1 && lengths.Length > 1)
                {
                    lines.Add($"'{group.Key}' 이름의 클립이 길이가 다른 채로 {group.Count()}개 섞여 있다 — 길이 표가 덮인다");
                    warnings++;
                }
            }
        }

        private static void InspectClip(string state, AnimationClip clip, List<string> lines, ref int errors, ref int warnings)
        {
            // 7. 이벤트는 이름 그대로 메서드를 부른다 — 오타면 재생 순간 예외가 난다.
            foreach (var item in AnimationUtility.GetAnimationEvents(clip))
            {
                if (item.functionName != CharacterAnimations.HitFrameEvent)
                {
                    lines.Add($"{state}: '{item.functionName}' 이벤트를 받을 데가 없다 — CharacterMotion 이 아는 건 {CharacterAnimations.HitFrameEvent} 뿐이다");
                    errors++;
                }

                if (item.time > clip.length + .001f)
                {
                    lines.Add($"{state}: 이벤트가 클립 끝({clip.length:0.00}초) 밖 {item.time:0.00}초에 있다 — 안 불린다");
                    warnings++;
                }
            }

            // 8. 궁극기는 타격 시점을 이벤트로만 잡는다. 없으면 모션만 돌고 아무도 안 맞는다.
            if (state == CharacterAnimations.UltimateMeleeName &&
                !AnimationUtility.GetAnimationEvents(clip).Any(item => item.functionName == CharacterAnimations.HitFrameEvent))
            {
                lines.Add($"{state}: {CharacterAnimations.HitFrameEvent} 이벤트가 하나도 없다 — 때리는 시점이 없다");
                errors++;
            }

            // 9. 제자리 모션은 돌아야 하고, 한 번짜리는 돌면 안 된다.
            var shouldLoop = state == "Idle" || state == "Walk";
            if (shouldLoop && !clip.isLooping)
            {
                lines.Add($"{state}: 루프가 꺼져 있다 — 한 바퀴 돌고 멈춘다");
                warnings++;
            }
            else if (!shouldLoop && clip.isLooping && state != "Empty")
            {
                lines.Add($"{state}: 루프가 켜져 있다 — 한 번짜리 모션이 계속 돈다");
                warnings++;
            }
        }

        private static IEnumerable<string> Prefabs()
        {
            var roots = CharacterRoots.Where(AssetDatabase.IsValidFolder).ToArray();
            if (roots.Length == 0) return Enumerable.Empty<string>();

            return AssetDatabase.FindAssets("t:Prefab", roots).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(path => path);
        }
    }
}
