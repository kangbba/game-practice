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
        private static readonly string[] Actions = { "Attack1", "Attack2", "Attack3", "Skill", "Ultimate" };
        private static readonly float[] Durations = { .55f, .60f, .68f, 1.05f, 1.80f };

        [MenuItem("★Sayne★/영웅/전신 공격 애니메이션 재빌드")]
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
            Debug.Log("HERO_ACTIONS_BUILD_PASS: 3 heroes, 15 independent full-body clips.");
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
                var authored = HeroActionPoseAuthoring.Create(graphic, hero, i, Actions[i], Durations[i]);
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
                        if (Mathf.Abs(curve.Evaluate(0) - curve.Evaluate(clip.length)) > .001f)
                            throw new InvalidOperationException("Action does not recover: " + path + "/" + binding.path);
                    }
                    foreach (var leg in new[] { "Root/FrontLeg", "Root/RearLeg" })
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(leg, typeof(Transform), "localEulerAnglesRaw.z"));
                        if (curve == null || curve.keys.Max(key => key.value) - curve.keys.Min(key => key.value) < 5)
                            throw new InvalidOperationException("Missing footwork: " + path);
                    }
                }
            }
            Debug.Log("HERO_ACTIONS_VALIDATION_PASS: independent assets, bindings, recovery, footwork, base-layer wiring.");
        }

        public static void BuildAndReview()
        {
            Build();
            var reviewFolder = Path.Combine(Path.GetTempPath(), "SayneHeroActionReview");
            Directory.CreateDirectory(reviewFolder);
            foreach (var heroId in Heroes)
            {
                var preview = new PreviewRenderUtility();
                var sheet = new Texture2D(1920, 1600, TextureFormat.RGB24, false);
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
                        var fractions = row == 3 ? new[] { 0f, .18f, .29f, .51f, .64f, .87f } : row == 4 ? new[] { 0f, .30f, .52f, .64f, .78f, .92f } : new[] { 0f, .24f, .36f, .51f, .69f, .87f };
                        for (var column = 0; column < fractions.Length; column++)
                        {
                            clip.SampleAnimation(graphic, fractions[column] * clip.length);
                            preview.BeginStaticPreview(new Rect(0, 0, 320, 320));
                            preview.Render(true);
                            var frame = preview.EndStaticPreview();
                            sheet.SetPixels(column * 320, (4 - row) * 320, 320, 320, frame.GetPixels());
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
            // 장비 이름에 영웅 이름이 없으므로 어떤 한 벌을 입힐지는 여기서 짝지어준다.
            var parts = hero switch
            {
                "Kage" => new List<(BodyPart Part, string Folder, string Name)>
                {
                    (BodyPart.RightHand, "Weapon", "Scythe"),
                    (BodyPart.Head, "Head", "FoxMaskHead"),
                    (BodyPart.Back, "Cape", "NavyCape"),
                    (BodyPart.Neck, "Scarf", "CrimsonScarf"),
                },
                "Aldric" => new List<(BodyPart Part, string Folder, string Name)>
                {
                    (BodyPart.RightHand, "Weapon", "Sword"),
                    (BodyPart.Head, "Head", "SilverHead"),
                    (BodyPart.Hair, "Hair", "SilverHair"),
                    (BodyPart.Back, "Cape", "VioletCape"),
                },
                _ => new List<(BodyPart Part, string Folder, string Name)>
                {
                    (BodyPart.RightHand, "Weapon", "Staff"),
                    (BodyPart.Head, "Head", "WhiteTwinHead"),
                    (BodyPart.Hair, "Hair", "WhiteHair"),
                    (BodyPart.Back, "Cape", "BlackCape"),
                },
            };

            foreach (var part in parts)
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Equipment/{part.Folder}/{part.Name}.prefab");
                graphic.GetComponent<CharacterSkin>().Wear(part.Part, visual);
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
