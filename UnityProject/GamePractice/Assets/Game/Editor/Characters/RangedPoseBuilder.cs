using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 원거리 계열 스킬·궁극기 모션을 굽는다 — 둘 다 겨눈 채 멈춰 선 자세 하나를 정해진 시간 동안 그대로 둔다.
    /// 자세는 새로 짓지 않고 평타 1타에서 팔을 앞으로 뻗은 순간을 따온다.
    /// 궁극기(UltimateRanged)는 HoldSeconds 가 곧 난사 시간이고, 스킬(SkillRanged)은 그 영웅의 근접 스킬(SkillMelee)과 같은 시간만큼 버틴다 —
    /// 무기만 바꿔 들어도 스킬의 호흡이 같게.
    /// </summary>
    public static class RangedPoseBuilder
    {
        private const string AnimationRoot = "Assets/DarkFantasy2D/Animations/Heroes";
        private const string PrefabRoot = "Assets/Game/Characters/Heroes";
        private static readonly string[] Heroes = { "Kage", "Aldric", "Nyx" };

        private const float UltimateHoldSeconds = 3.4f;

        /// <summary>자세를 따올 모션과 그 안의 시점(0~1). 평타 1타가 팔을 뻗어 친 직후다.</summary>
        private const string SourceAction = "Attack1";
        private const float SourceMoment = 0.4f;

        public static void Build()
        {
            foreach (var hero in Heroes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{hero}/{hero}.prefab");
                var controller = (AnimatorController)prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
                var source = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/{SourceAction}.anim");
                var meleeSkill = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    $"{AnimationRoot}/{hero}/{CharacterAnimations.SkillMeleeName}.anim");

                Bind(controller, SaveClip(hero, Freeze(source, CharacterAnimations.UltimateRangedName, UltimateHoldSeconds)));
                Bind(controller, SaveClip(hero, Freeze(source, CharacterAnimations.SkillRangedName, meleeSkill.length)));
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"RangedPoseBuilder: 영웅 3명에 {CharacterAnimations.SkillRangedName}·{CharacterAnimations.UltimateRangedName} 정지 자세 구움");
        }

        /// <summary>원본 모션의 한 순간을 떠서, 처음부터 끝까지 그 값인 클립을 만든다.</summary>
        private static AnimationClip Freeze(AnimationClip source, string name, float holdSeconds)
        {
            var moment = source.length * SourceMoment;
            var clip = new AnimationClip { name = name, frameRate = source.frameRate };

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var value = AnimationUtility.GetEditorCurve(source, binding).Evaluate(moment);
                AnimationUtility.SetEditorCurve(clip, binding,
                    new AnimationCurve(new Keyframe(0f, value), new Keyframe(holdSeconds, value)));
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                // 그 순간에 걸려 있던 그림. 순간보다 앞선 키가 없으면 첫 키다.
                var value = keys.Where(key => key.time <= moment).DefaultIfEmpty(keys[0]).Last().value;
                AnimationUtility.SetObjectReferenceCurve(clip, binding, new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = value },
                    new ObjectReferenceKeyframe { time = holdSeconds, value = value },
                });
            }

            return clip;
        }

        /// <summary>있던 클립이면 내용만 갈아 끼운다 — GUID 가 그대로라 상태·참조가 안 끊긴다.</summary>
        private static AnimationClip SaveClip(string hero, AnimationClip built)
        {
            var path = $"{AnimationRoot}/{hero}/{built.name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(built, path);
                return built;
            }

            EditorUtility.CopySerialized(built, clip);
            Object.DestroyImmediate(built);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>기본 레이어에 같은 이름의 상태를 두고 클립을 건다. 복귀는 CharacterMotion 이 클립 길이로 관리하므로 전이는 없다.</summary>
        private static void Bind(AnimatorController controller, AnimationClip clip)
        {
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(child => child.state).FirstOrDefault(item => item.name == clip.name)
                        ?? machine.AddState(clip.name, new Vector3(560f, 390f + machine.states.Length * 10f));

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;

            foreach (var transition in state.transitions)
            {
                state.RemoveTransition(transition);
            }

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
        }

        /// <summary>
        /// 한 번짜리: 스킬·궁극기 모션 이름을 근접·원거리 짝으로 정리한다. Skill → SkillMelee, Ultimate → UltimateMelee.
        /// 애니메이터 상태 이름과 클립 파일 이름을 같이 바꾸고(클립 GUID 는 그대로), 이어서 원거리 짝을 굽는다.
        /// </summary>
        // 한 번짜리라 돌린 뒤 메뉴에서 내렸다(2026-09-19). 다시 돌리면 옛 이름 클립이 없어 실패한다.
        public static void RenameToPairs()
        {
            foreach (var hero in Heroes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{hero}/{hero}.prefab");
                var controller = (AnimatorController)prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController;

                Rename(hero, controller, "Skill", CharacterAnimations.SkillMeleeName);
                Rename(hero, controller, "Ultimate", CharacterAnimations.UltimateMeleeName);
            }

            AssetDatabase.SaveAssets();
            Build();
        }

        private static void Rename(string hero, AnimatorController controller, string from, string to)
        {
            var error = AssetDatabase.RenameAsset($"{AnimationRoot}/{hero}/{from}.anim", to);
            if (!string.IsNullOrEmpty(error))
            {
                throw new System.InvalidOperationException($"{hero}/{from}.anim → {to}: {error}");
            }

            foreach (var state in controller.layers[0].stateMachine.states.Select(child => child.state).Where(item => item.name == from))
            {
                state.name = to;
                EditorUtility.SetDirty(state);
            }

            EditorUtility.SetDirty(controller);
        }
    }
}
