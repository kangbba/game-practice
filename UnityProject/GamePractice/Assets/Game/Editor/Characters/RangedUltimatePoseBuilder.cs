using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 원거리 계열 궁극기의 모션을 굽는다 — 겨눈 채 멈춰 선 자세 하나를 HoldSeconds 동안 그대로 둔다.
    /// 자세는 새로 짓지 않고 평타 1타에서 팔을 앞으로 뻗은 순간을 따온다. 난사는 BarrageUltimate 가 그 사이를 채운다.
    /// 클립 길이가 곧 난사 시간이다 — 늘리려면 HoldSeconds 를 고쳐 다시 굽는다.
    /// </summary>
    public static class RangedUltimatePoseBuilder
    {
        private const string AnimationRoot = "Assets/DarkFantasy2D/Animations/Heroes";
        private const string PrefabRoot = "Assets/Game/Characters/Heroes";
        private static readonly string[] Heroes = { "Kage", "Aldric", "Nyx" };

        private const float HoldSeconds = 3.4f;

        /// <summary>자세를 따올 모션과 그 안의 시점(0~1). 평타 1타가 팔을 뻗어 친 직후다.</summary>
        private const string SourceAction = "Attack1";
        private const float SourceMoment = 0.4f;

        [MenuItem("★Sayne★/공통/원거리 궁극기 자세 빌드", false, 53)]
        public static void Build()
        {
            foreach (var hero in Heroes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{hero}/{hero}.prefab");
                var controller = (AnimatorController)prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
                var source = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/{SourceAction}.anim");

                var clip = SaveClip($"{AnimationRoot}/{hero}/{CharacterAnimations.UltimateRangedName}.anim", Freeze(source));
                Bind(controller, clip);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"RangedUltimatePoseBuilder: 영웅 3명에 {CharacterAnimations.UltimateRangedName} ({HoldSeconds}초 정지 자세) 구움");
        }

        /// <summary>원본 모션의 한 순간을 떠서, 처음부터 끝까지 그 값인 클립을 만든다.</summary>
        private static AnimationClip Freeze(AnimationClip source)
        {
            var moment = source.length * SourceMoment;
            var clip = new AnimationClip { name = CharacterAnimations.UltimateRangedName, frameRate = source.frameRate };

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var value = AnimationUtility.GetEditorCurve(source, binding).Evaluate(moment);
                AnimationUtility.SetEditorCurve(clip, binding,
                    new AnimationCurve(new Keyframe(0f, value), new Keyframe(HoldSeconds, value)));
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                // 그 순간에 걸려 있던 그림. 순간보다 앞선 키가 없으면 첫 키다.
                var value = keys.Where(key => key.time <= moment).DefaultIfEmpty(keys[0]).Last().value;
                AnimationUtility.SetObjectReferenceCurve(clip, binding, new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = value },
                    new ObjectReferenceKeyframe { time = HoldSeconds, value = value },
                });
            }

            return clip;
        }

        /// <summary>있던 클립이면 내용만 갈아 끼운다 — GUID 가 그대로라 상태·참조가 안 끊긴다.</summary>
        private static AnimationClip SaveClip(string path, AnimationClip built)
        {
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
                        ?? machine.AddState(clip.name, new Vector3(560f, 390f));

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
    }
}
