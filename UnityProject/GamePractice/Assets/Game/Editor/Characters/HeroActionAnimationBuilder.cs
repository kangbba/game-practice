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
                foreach (var action in new[] { "Skill", "Ultimate" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/{action}.anim");
                    var radius = action == "Skill" ? 7f : 10f;
                    var impact = action == "Skill" ? CharacterAnimations.SkillImpact : CharacterAnimations.UltimateImpact;
                    clip.SampleAnimation(graphic, clip.length * impact);
                    var actual = ((Vector2)graphic.transform.InverseTransformPoint(weapon.transform.TransformPoint(tip))).magnitude;
                    if (Mathf.Abs(actual - radius) > .15f)
                        throw new InvalidOperationException($"{hero}/{action}: radius {actual:F2}, expected {radius}.");
                    if (action != "Ultimate") continue;
                    var tipHeight = graphic.transform.InverseTransformPoint(weapon.transform.TransformPoint(tip)).y;
                    if (Mathf.Abs(tipHeight) > .15f)
                        throw new InvalidOperationException($"{hero}: slam misses ground ({tipHeight:F2}).");
                    var root = graphic.transform.Find("Root");
                    var landing = root.localPosition.y;
                    clip.SampleAnimation(graphic, clip.length * .38f);
                    var jump = root.localPosition.y - landing;
                    if (jump < 5f)
                        throw new InvalidOperationException($"{hero}: ultimate jump too small ({jump:F2}).");
                    Debug.Log($"HERO_MOTION_SIZE_PASS: {hero}, skill=7, ultimate={actual:F2}, jump={jump:F2}");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        public static void BuildAndReview()
        {
            Build();
            var reviewFolder = Path.Combine(Path.GetTempPath(), "SayneHeroActionReview");
            Directory.CreateDirectory(reviewFolder);
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
                    camera.orthographicSize = 1.85f;
                    camera.transform.position = new Vector3(.20f, 1.35f, -10);
                    camera.transform.rotation = Quaternion.identity;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.10f, .13f, .18f, 1);
                    camera.nearClipPlane = .1f;
                    camera.farClipPlane = 30;
                    FitReviewCamera(graphic, camera, heroId);
                    for (var row = 0; row < Actions.Length; row++)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{heroId}/{Actions[row]}.anim");
                        var fractions = row == 4 ? new[] { 0f, .18f, .29f, .51f, .64f, .87f } : row == 5 ? new[] { .10f, .20f, .38f, .46f, .52f, .68f } : new[] { 0f, .24f, .36f, .51f, .69f, .87f };
                        for (var column = 0; column < fractions.Length; column++)
                        {
                            clip.SampleAnimation(graphic, fractions[column] * clip.length);
                            preview.BeginStaticPreview(new Rect(0, 0, 320, 320));
                            preview.Render(true);
                            var frame = preview.EndStaticPreview();
                            sheet.SetPixels(column * 320, (Actions.Length - 1 - row) * 320, 320, 320, frame.GetPixels());
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                    }
                    var framesFolder = Path.Combine(reviewFolder, heroId + "-frames");
                    Directory.CreateDirectory(framesFolder);
                    var frameIndex = 0;
                    foreach (var action in Actions)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{heroId}/{action}.anim");
                        for (var frameIndexInClip = 0; frameIndexInClip <= Mathf.CeilToInt(clip.length * 30f); frameIndexInClip++)
                        {
                            clip.SampleAnimation(graphic, Mathf.Min(frameIndexInClip / 30f, clip.length));
                            preview.BeginStaticPreview(new Rect(0, 0, 480, 480));
                            preview.Render(true);
                            var frame = preview.EndStaticPreview();
                            File.WriteAllBytes(Path.Combine(framesFolder, $"{frameIndex++:D4}.png"), frame.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(reviewFolder, heroId + ".png"), sheet.EncodeToPNG());
                }
                finally
                {
                    preview.Cleanup();
                    UnityEngine.Object.DestroyImmediate(sheet);
                }
            }
        }

        internal static void DressPreview(Transform graphic, string hero)
        {
            // 머리·머리카락·망토·스카프는 프리팹에 이미 구워져 있다. 여기서 입히는 건 갈아입는 장비뿐이다.
            var parts = hero switch
            {
                "Kage" => new List<(EquipmentSlot Slot, string Folder, string Name)>
                {
                    (EquipmentSlot.MainHand, "Weapon", "Scythe"),
                },
                "Aldric" => new List<(EquipmentSlot Slot, string Folder, string Name)>
                {
                    (EquipmentSlot.MainHand, "Weapon", "Sword"),
                },
                _ => new List<(EquipmentSlot Slot, string Folder, string Name)>
                {
                    (EquipmentSlot.MainHand, "Weapon", "Staff"),
                },
            };

            foreach (var part in parts)
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Equipment/{part.Folder}/{part.Name}.prefab");
                graphic.GetComponent<CharacterSkin>().Wear(part.Slot, visual);
            }
        }

        private static void FitReviewCamera(GameObject graphic, Camera camera, string hero)
        {
            var bounds = new Bounds();
            var hasBounds = false;
            var renderers = graphic.GetComponentsInChildren<SpriteRenderer>();
            foreach (var action in Actions)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/{hero}/{action}.anim");
                for (var frame = 0; frame <= 60; frame++)
                {
                    clip.SampleAnimation(graphic, clip.length * frame / 60f);
                    foreach (var renderer in renderers)
                    {
                        if (renderer.sprite == null || !renderer.enabled) continue;
                        if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.12f;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        }

    }
}
