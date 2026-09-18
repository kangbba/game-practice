using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sayne
{
    public static class HeroActionAnimationBuilder
    {
        private const string AnimationRoot = "Assets/DarkFantasy2D/Animations/Heroes";
        private const string PrefabRoot = "Assets/Game/Characters/Heroes";
        private static readonly string[] Heroes = { "Kage", "Aldric", "Nyx" };
        private static readonly string[] Actions = { "Attack1", "Attack2", "Attack3", "Attack4", "Skill", "Ultimate" };
        private static readonly float[] Durations = { .55f, .60f, .68f, .68f, CharacterAnimations.SkillDuration, CharacterAnimations.UltimateDuration };

        /// <summary>포즈 원본 인덱스. 5번은 평타 4타 전용 마무리 동작이다.</summary>
        private static readonly int[] PoseIndices = { 0, 1, 2, 5, 3, 4 };

        [MenuItem("★Sayne★/영웅/전신 공격 애니메이션 재빌드", false, 100)]
        public static void Build()
        {
            foreach (var hero in Heroes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{hero}/{hero}.prefab");
                if (prefab == null) throw new InvalidOperationException($"Missing hero: {hero}");
                var animator = prefab.GetComponentInChildren<Animator>(true);
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null) throw new InvalidOperationException($"Missing controller: {hero}");
                WriteActions(hero, animator.transform, controller);
            }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("HERO_ACTIONS_BUILD_PASS: 3 heroes, 18 independent full-body clips.");
        }

        private static void WriteActions(string hero, Transform graphic, AnimatorController controller)
        {
            var folder = $"{AnimationRoot}/{hero}";
            Directory.CreateDirectory(folder);
            var layers = controller.layers;
            layers[0].avatarMask = null;
            // CharacterMotion 이 전신 Base Layer 를 재생하므로 상체 레이어의 덮어쓰기를 끈다.
            for (var i = 1; i < layers.Length; i++) layers[i].defaultWeight = 0f;
            controller.layers = layers;

            for (var i = 0; i < Actions.Length; i++)
            {
                var authored = HeroActionPoseAuthoring.Create(graphic, hero, PoseIndices[i], Actions[i], Durations[i]);
                var path = $"{folder}/{Actions[i]}.anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    AssetDatabase.CreateAsset(authored, path);
                    clip = authored;
                }
                else
                {
                    EditorUtility.CopySerialized(authored, clip);
                    UnityEngine.Object.DestroyImmediate(authored);
                    EditorUtility.SetDirty(clip);
                }

                var machine = controller.layers[0].stateMachine;
                var state = machine.states.Select(child => child.state).FirstOrDefault(item => item.name == Actions[i]);
                if (state == null) state = machine.AddState(Actions[i], new Vector3(330, i * 65));
                state.motion = clip;
                state.speed = 1f;
                state.writeDefaultValues = true;
                // 복귀 시점은 CharacterMotion 이 클립 길이로 관리한다.
                foreach (var transition in state.transitions) state.RemoveTransition(transition);
                EditorUtility.SetDirty(state);
                foreach (var layer in controller.layers)
                {
                    RebindAction(layer.stateMachine, Actions[i], clip);
                }
            }
            EditorUtility.SetDirty(controller);
        }

        private static void RebindAction(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            foreach (var child in machine.states)
            {
                if (child.state.name != name) continue;
                child.state.motion = clip;
                EditorUtility.SetDirty(child.state);
            }
            foreach (var child in machine.stateMachines) RebindAction(child.stateMachine, name, clip);
        }

        public static void Validate()
        {
            var guids = new HashSet<string>();
            foreach (var hero in Heroes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{hero}/{hero}.prefab");
                var graphic = prefab.GetComponentInChildren<Animator>(true).transform;
                var controller = (AnimatorController)graphic.GetComponent<Animator>().runtimeAnimatorController;
                if (controller.layers.Skip(1).Any(layer => layer.defaultWeight != 0))
                    throw new InvalidOperationException(hero + ": upper-body override is still active.");
                for (var i = 0; i < Actions.Length; i++)
                {
                    var path = $"{AnimationRoot}/{hero}/{Actions[i]}.anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip == null || clip.name != Actions[i] || Mathf.Abs(clip.length - Durations[i]) > .001f)
                        throw new InvalidOperationException("Invalid clip: " + path);
                    if (!guids.Add(AssetDatabase.AssetPathToGUID(path))) throw new InvalidOperationException("Shared action asset: " + path);
                    var state = controller.layers[0].stateMachine.states.First(child => child.state.name == Actions[i]).state;
                    if (state.motion != clip) throw new InvalidOperationException("Unwired action: " + path);
                    if (controller.animationClips.Any(other => other.name == clip.name && other != clip))
                        throw new InvalidOperationException("Ambiguous clip duration: " + path);
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (graphic.Find(binding.path) == null) throw new InvalidOperationException("Missing bone: " + binding.path);
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var recovery = binding.propertyName == "localEulerAnglesRaw.z"
                            ? Mathf.DeltaAngle(curve.Evaluate(0), curve.Evaluate(clip.length))
                            : curve.Evaluate(0) - curve.Evaluate(clip.length);
                        if (Mathf.Abs(recovery) > .001f)
                            throw new InvalidOperationException("Action does not recover: " + path + "/" + binding.path);
                    }
                    foreach (var leg in new[] { "Root/FrontLeg", "Root/RearLeg" })
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(leg, typeof(Transform), "localEulerAnglesRaw.z"));
                        if (curve == null || curve.keys.Max(key => key.value) - curve.keys.Min(key => key.value) < 5)
                            throw new InvalidOperationException("Missing footwork: " + path);
                    }
                }
                ValidateMotionSize(prefab, hero);
            }
            Debug.Log("HERO_ACTIONS_VALIDATION_PASS: independent assets, bindings, recovery, footwork, base-layer wiring.");
        }

        private static void ValidateMotionSize(GameObject prefab, string hero)
        {
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var graphic = instance.GetComponentInChildren<Animator>().gameObject;
                graphic.GetComponent<Animator>().enabled = false;
                DressPreview(graphic.transform, hero);
                var step3 = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/Attack3.anim");
                var step4 = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/Attack4.anim");
                var travel = EditorCurveBinding.FloatCurve("Root", typeof(Transform), "m_LocalPosition.x");
                var thirdReach = AnimationUtility.GetEditorCurve(step3, travel).keys.Max(key => key.value);
                var fourthReach = AnimationUtility.GetEditorCurve(step4, travel).keys.Max(key => key.value);
                if (fourthReach - thirdReach < .5f)
                    throw new InvalidOperationException($"{hero}: combo finisher needs a distinct step.");
                var weapon = graphic.transform.Find("Root/Torso/Arm/Forearm/Weapon").GetComponentInChildren<SpriteRenderer>();
                var tip = new Vector3(weapon.sprite.bounds.center.x, weapon.sprite.bounds.max.y, 0f);
                Vector3 TipNow() => graphic.transform.InverseTransformPoint(weapon.transform.TransformPoint(tip));

                var skill = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/Skill.anim");
                skill.SampleAnimation(graphic, skill.length * CharacterAnimations.SkillImpact);
                var skillReach = ((Vector2)TipNow()).magnitude;
                if (Mathf.Abs(skillReach - 7f) > .15f)
                    throw new InvalidOperationException($"{hero}/Skill: radius {skillReach:F2}, expected 7.");

                // 궁극기: 낙하 강타의 칼끝이 땅에 꽂히고 무대 반경(10) 너머까지 닿아야 하며, 그 전에 충분히 높이 떠야 한다.
                var ultimate = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/Ultimate.anim");
                ultimate.SampleAnimation(graphic, HeroUltimateChoreography.SlamSeconds);
                var slamTip = TipNow();
                if (Mathf.Abs(slamTip.y) > .2f)
                    throw new InvalidOperationException($"{hero}: slam misses ground ({slamTip.y:F2}).");
                if (slamTip.x < 10f)
                    throw new InvalidOperationException($"{hero}: slam falls short of the stage edge ({slamTip.x:F2}).");
                var root = graphic.transform.Find("Root");
                var landing = root.localPosition.y;
                ultimate.SampleAnimation(graphic, HeroUltimateChoreography.ApexSeconds);
                var jump = root.localPosition.y - landing;
                if (jump < 5.5f)
                    throw new InvalidOperationException($"{hero}: ultimate jump too small ({jump:F2}).");
                var hits = AnimationUtility.GetAnimationEvents(ultimate)
                    .Count(item => item.functionName == CharacterAnimations.HitFrameEvent);
                if (hits < 2)
                    throw new InvalidOperationException($"{hero}: ultimate has {hits} hit frames.");
                Debug.Log($"HERO_MOTION_SIZE_PASS: {hero}, skill=7, slam=({slamTip.x:F2},{slamTip.y:F2}), jump={jump:F2}, hits={hits}");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        /// <summary>열린 에디터에 재빌드·리뷰 렌더를 시키는 문. 이 파일이 생기면 한 번 돌고 지운다.</summary>
        private const string ReviewRequest = "tmp/heroes/actions.request";

        /// <summary>리뷰 렌더가 떨어지는 곳. 영웅별 포즈 시트(png)와 모션별 30fps 프레임 폴더.</summary>
        private const string ReviewFolder = "tmp/heroes/review";

        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update -= CheckRequest;
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ReviewRequest)) return;
            File.Delete(ReviewRequest);
            BuildAndReview();
        }

        public static void BuildAndReview()
        {
            Build();
            Directory.CreateDirectory(ReviewFolder);
            foreach (var heroId in Heroes)
            {
                var preview = new PreviewRenderUtility();
                var sheet = new Texture2D(1920, 320 * Actions.Length, TextureFormat.RGB24, false);
                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{heroId}/{heroId}.prefab");
                    var hero = preview.InstantiatePrefabInScene(prefab);
                    var graphic = hero.GetComponentInChildren<Animator>(true).gameObject;
                    graphic.GetComponent<Animator>().enabled = false;
                    DressPreview(graphic.transform, heroId);
                    var camera = preview.camera;
                    camera.orthographic = true;
                    camera.transform.rotation = Quaternion.identity;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.10f, .13f, .18f, 1);
                    camera.nearClipPlane = .1f;
                    camera.farClipPlane = 30;
                    // 궁극기는 화면 몇 개 분량을 움직이므로 모션마다 따로 카메라를 맞춘다.
                    for (var row = 0; row < Actions.Length; row++)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{heroId}/{Actions[row]}.anim");
                        FitReviewCamera(graphic, camera, clip);
                        var fractions = row == 4 ? new[] { 0f, .18f, .29f, .51f, .64f, .87f }
                            : row == 5 ? new[] { .15f, .214f, .389f, .60f, .693f, .817f }
                            : new[] { 0f, .24f, .36f, .51f, .69f, .87f };
                        for (var column = 0; column < fractions.Length; column++)
                        {
                            clip.SampleAnimation(graphic, fractions[column] * clip.length);
                            preview.BeginStaticPreview(new Rect(0, 0, 320, 320));
                            preview.Render(true);
                            var frame = preview.EndStaticPreview();
                            sheet.SetPixels(column * 320, (Actions.Length - 1 - row) * 320, 320, 320, frame.GetPixels());
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                        var framesFolder = Path.Combine(ReviewFolder, $"{heroId}-{Actions[row]}");
                        if (Directory.Exists(framesFolder)) Directory.Delete(framesFolder, true);
                        Directory.CreateDirectory(framesFolder);
                        // 궁극기는 시야가 넓어 인물이 작게 나오므로 프레임을 크게 찍는다.
                        var frameSize = row == 5 ? 960 : 480;
                        for (var frameIndex = 0; frameIndex <= Mathf.CeilToInt(clip.length * 30f); frameIndex++)
                        {
                            clip.SampleAnimation(graphic, Mathf.Min(frameIndex / 30f, clip.length));
                            preview.BeginStaticPreview(new Rect(0, 0, frameSize, frameSize));
                            preview.Render(true);
                            var frame = preview.EndStaticPreview();
                            File.WriteAllBytes(Path.Combine(framesFolder, $"{frameIndex:D4}.png"), frame.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(ReviewFolder, heroId + ".png"), sheet.EncodeToPNG());
                }
                finally
                {
                    preview.Cleanup();
                    UnityEngine.Object.DestroyImmediate(sheet);
                }
            }
            Debug.Log($"HERO_ACTIONS_REVIEW_PASS: {Path.GetFullPath(ReviewFolder)}");
        }

        internal static void DressPreview(Transform graphic, string hero)
        {
            // 머리·머리카락·망토·스카프는 프리팹에 이미 구워져 있다. 여기서 입히는 건 갈아입는 장비뿐이다.
            // 프리팹은 벗은 몸이라 시작 차림(무기·몸통·신발)을 전부 입혀야 게임에서 보던 모습이 된다.
            var parts = hero switch
            {
                "Kage" => new List<(EquipmentSlot Slot, string Folder, string Name)>
                {
                    (EquipmentSlot.MainHand, "Weapon", "Sword"),
                    (EquipmentSlot.Chest, "Chest", "KageArmor"),
                    (EquipmentSlot.Boots, "Boots", "KageBoots"),
                },
                "Aldric" => new List<(EquipmentSlot Slot, string Folder, string Name)>
                {
                    (EquipmentSlot.MainHand, "Weapon", "Scythe"),
                    (EquipmentSlot.Chest, "Chest", "AldricCoat"),
                    (EquipmentSlot.Boots, "Boots", "AldricBoots"),
                },
                _ => new List<(EquipmentSlot Slot, string Folder, string Name)>
                {
                    (EquipmentSlot.MainHand, "Weapon", "Staff"),
                    (EquipmentSlot.Chest, "Chest", "NyxDress"),
                    (EquipmentSlot.Boots, "Boots", "NyxBoots"),
                },
            };

            foreach (var part in parts)
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Equipment/{part.Folder}/{part.Name}/{part.Name}.prefab");
                graphic.GetComponent<CharacterSkin>().Wear(part.Slot, visual);
            }
        }

        /// <summary>그 모션이 지나가는 자리 전부가 한 화면에 들어오게 카메라를 맞춘다.</summary>
        private static void FitReviewCamera(GameObject graphic, Camera camera, AnimationClip clip)
        {
            var bounds = new Bounds();
            var hasBounds = false;
            // 거대해진 무기는 빼고 몸이 지나는 자리만 본다 — 무기까지 넣으면 인물이 점이 된다.
            var weaponBone = graphic.transform.Find("Root/Torso/Arm/Forearm/Weapon");
            var renderers = graphic.GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer => !renderer.transform.IsChildOf(weaponBone)).ToArray();
            var steps = Mathf.Max(60, Mathf.CeilToInt(clip.length * 30f));
            for (var frame = 0; frame <= steps; frame++)
            {
                clip.SampleAnimation(graphic, clip.length * frame / steps);
                foreach (var renderer in renderers)
                {
                    if (renderer.sprite == null || !renderer.enabled) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            }
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.12f;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        }

    }
}
