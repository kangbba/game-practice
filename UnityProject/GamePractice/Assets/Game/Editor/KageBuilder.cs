using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sayne
{
    public static class KageBuilder
    {
        internal const string ArtPath = "Assets/DarkFantasy2D/Art/Heroes/Kage";
        internal const string AnimationPath = "Assets/DarkFantasy2D/Animations/Heroes/Kage";
        internal const string PrefabPath = "Assets/Game/Characters/Heroes/Kage/Kage.prefab";

        private const string Torso = "Root/Torso";
        private const string Arm = Torso + "/Arm";
        private const string Forearm = Arm + "/Forearm";
        private const string Weapon = Forearm + "/Weapon";
        private const string BackArm = Torso + "/BackArm";
        private const string BackForearm = BackArm + "/Forearm";
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        [MenuItem("★Sayne★/Kage/리소스 재빌드")]
        public static void Build()
        {
            Directory.CreateDirectory(AnimationPath);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            ImportParts();
            var hero = new GameObject(HeroID.Kage);
            try
            {
                hero.AddComponent<Hero>();
                var collider = hero.AddComponent<CapsuleCollider2D>();
                collider.isTrigger = true;
                collider.offset = new Vector2(0, 1.15f);
                collider.size = new Vector2(.8f, 2.3f);
                var graphic = Bone(hero.transform, "Graphic", Vector2.zero);
                var animator = graphic.gameObject.AddComponent<Animator>();
                var group = graphic.gameObject.AddComponent<SortingGroup>();
                group.sortingLayerName = "Actor";
                graphic.gameObject.AddComponent<Billboard>();
                graphic.gameObject.AddComponent<CharacterFlash>();
                var root = Bone(graphic, "Root", new Vector2(0, .70f));
                Part(root, "RearLeg", "RearLeg", new Vector2(-.16f, 0), 0, -4);
                Part(root, "FrontLeg", "FrontLeg", new Vector2(.17f, 0), 0, -2);
                var torso = Part(root, "Torso", "Torso", new Vector2(0, .64f), 0, 0);
                Part(torso, "Cape", "Cape", new Vector2(-.05f, -.37f), -8, -6);
                Part(torso, "Scarf", "Scarf", new Vector2(-.05f, .02f), -65, -5);
                Part(torso, "Head", "Head", new Vector2(0, .005f), -3, 8);
                var backArm = Part(torso, "BackArm", "UpperArm", new Vector2(.19f, -.16f), -12, -3);
                Part(backArm, "Forearm", "Forearm", new Vector2(-14f / 620, -226f / 620), 18, -3);
                var arm = Part(torso, "Arm", "UpperArm", new Vector2(-.19f, -.16f), 30, 4);
                var forearm = Part(arm, "Forearm", "Forearm", new Vector2(-14f / 620, -226f / 620), 65, 6);
                Part(forearm, "Weapon", "Weapon", new Vector2(21f / 650, -242f / 650), -115, 5);
                Bone(graphic, "EffectSocket", new Vector2(1.05f, 1.05f));

                var clips = CreateClips(graphic);
                animator.runtimeAnimatorController = CreateController(graphic, clips);
                graphic.gameObject.AddComponent<KageGraphic>();
                PrefabUtility.SaveAsPrefabAsset(hero, PrefabPath);
                AssetDatabase.SaveAssets();
                Validate();
                KagePreviewWindow.ExportAttackPreview();
                Debug.Log("Kage: prefab, anatomical pivots, six clips and upper-body mask built and validated.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hero);
            }
        }

        private static void ImportParts()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(ArtPath + "/Kage.png"));
            Sprites.Clear();
            try
            {
                // Rectangles and joint landmarks use source pixels measured from the top left.
                ImportPart(source, "Head", new RectInt(0, 0, 460, 478), new Vector2(260, 414), 390);
                ImportPart(source, "Torso", new RectInt(470, 65, 395, 414), new Vector2(668, 150), 420);
                ImportPart(source, "Scarf", new RectInt(860, 0, 394, 437), new Vector2(960, 100), 610);
                ImportPart(source, "UpperArm", new RectInt(115, 478, 235, 370), new Vector2(239, 530), 620);
                ImportPart(source, "Forearm", new RectInt(530, 480, 205, 368), new Vector2(620, 542), 650);
                ImportPart(source, "Weapon", new RectInt(975, 432, 145, 450), new Vector2(1044, 810), 310);
                ImportPart(source, "RearLeg", new RectInt(100, 852, 264, 402), new Vector2(207, 880), 500);
                ImportPart(source, "FrontLeg", new RectInt(520, 852, 260, 402), new Vector2(615, 880), 500);
                ImportPart(source, "Cape", new RectInt(810, 882, 444, 372), new Vector2(1040, 917), 560);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void ImportPart(Texture2D source, string name, RectInt rect, Vector2 joint, float pixelsPerUnit)
        {
            var path = $"{ArtPath}/Kage_{name}.png";
            var crop = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            crop.SetPixels(source.GetPixels(rect.x, source.height - rect.yMax, rect.width, rect.height));
            crop.Apply();
            File.WriteAllBytes(path, crop.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(crop);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2((joint.x - rect.x) / rect.width, 1f - (joint.y - rect.y) / rect.height);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            Sprites[name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Transform Bone(Transform parent, string name, Vector2 position)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = position;
            return bone;
        }

        private static Transform Part(Transform parent, string name, string sprite, Vector2 position, float rotation, int order)
        {
            var bone = Bone(parent, name, position);
            bone.localRotation = Quaternion.Euler(0, 0, rotation);
            var skin = Bone(bone, "Skin", Vector2.zero).gameObject.AddComponent<SpriteRenderer>();
            skin.sprite = Sprites[sprite];
            skin.sortingOrder = order;
            skin.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFantasy2D/Materials/CharacterUnlit.mat");
            return bone;
        }

        private static Dictionary<string, AnimationClip> CreateClips(Transform graphic)
        {
            var clips = new Dictionary<string, AnimationClip>();
            var idle = Clip(graphic, "Idle", 1.6f, true);
            Curve(idle, "Root", "m_LocalPosition.y", new[] { 0f, .8f, 1.6f }, .70f, .72f, .70f);
            Rotate(idle, Torso + "/Scarf", new[] { 0f, .8f, 1.6f }, -65, -70, -65);
            Rotate(idle, Forearm, new[] { 0f, .8f, 1.6f }, 65, 68, 65);
            clips.Add("Idle", idle);

            var walk = Clip(graphic, "Walk", .64f, true);
            var stride = new[] { 0f, .16f, .32f, .48f, .64f };
            Rotate(walk, "Root/RearLeg", stride, -22, 0, 22, 0, -22);
            Rotate(walk, "Root/FrontLeg", stride, 22, 0, -22, 0, 22);
            Curve(walk, "Root", "m_LocalPosition.y", stride, .70f, .74f, .70f, .74f, .70f);
            Rotate(walk, Torso, stride, -5, -3, -5, -3, -5);
            Rotate(walk, Arm, stride, 27, 33, 27, 22, 27);
            Rotate(walk, BackArm, stride, -20, 0, 25, 0, -20);
            Rotate(walk, Torso + "/Scarf", stride, -82, -90, -85, -76, -82);
            Rotate(walk, Torso + "/Cape", stride, -18, -26, -18, -10, -18);
            clips.Add("Walk", walk);

            var attack = Clip(graphic, "Attack", .50f, false);
            var slash = new[] { 0f, .075f, .115f, .155f, .21f, .34f, .50f };
            Rotate(attack, Arm, slash, 30, 150, 125, 100, 106, 60, 30);
            Rotate(attack, Forearm, slash, 65, -35, -20, -35, -28, 30, 65);
            Rotate(attack, Weapon, slash, -115, -75, -110, -155, -163, -145, -115);
            Rotate(attack, Torso, slash, 0, 9, -3, -12, -10, -4, 0);
            Rotate(attack, Torso + "/Head", slash, -3, -7, 0, 7, 5, 0, -3);
            Rotate(attack, BackArm, slash, -12, -30, -12, 35, 40, 5, -12);
            Rotate(attack, BackForearm, slash, 18, 38, 18, 45, 40, 25, 18);
            Rotate(attack, Torso + "/Scarf", slash, -65, -54, -62, -100, -110, -83, -65);
            Rotate(attack, Torso + "/Cape", slash, -8, 4, -5, -30, -34, -19, -8);
            Rotate(attack, "Root/RearLeg", slash, 0, -6, -9, -16, -16, -8, 0);
            Rotate(attack, "Root/FrontLeg", slash, 0, 8, 10, 18, 18, 7, 0);
            Curve(attack, "Root", "m_LocalPosition.x", slash, 0, -.045f, .045f, .13f, .13f, .06f, 0);
            Curve(attack, "Root", "m_LocalPosition.y", slash, .70f, .67f, .65f, .64f, .64f, .68f, .70f);
            clips.Add("Attack", attack);

            var hit = Clip(graphic, "Hit", .2f, false);
            var recoil = new[] { 0f, .055f, .2f };
            Rotate(hit, Torso, recoil, 0, 14, 0);
            Rotate(hit, Torso + "/Head", recoil, -3, 8, -3);
            Rotate(hit, Arm, recoil, 30, 8, 30);
            clips.Add("Hit", hit);

            var death = Clip(graphic, "Death", .65f, false);
            var fall = new[] { 0f, .16f, .40f, .65f };
            Rotate(death, "Root", fall, 0, 10, 65, 86);
            Curve(death, "Root", "m_LocalPosition.y", fall, .70f, .55f, .31f, .25f);
            Rotate(death, Arm, fall, 30, 15, -10, -20);
            Rotate(death, Forearm, fall, 65, 45, 20, 10);
            clips.Add("Death", death);

            var ultimate = Clip(graphic, "Ultimate", .85f, false);
            var draw = new[] { 0f, .20f, .29f, .35f, .43f, .65f, .85f };
            Rotate(ultimate, Arm, draw, 30, 160, 155, 110, 115, 50, 30);
            Rotate(ultimate, Forearm, draw, 65, -15, -25, -40, -25, 35, 65);
            Rotate(ultimate, Weapon, draw, -115, -70, -85, -175, -185, -140, -115);
            Rotate(ultimate, Torso, draw, 0, 15, 12, -18, -14, -4, 0);
            Rotate(ultimate, Torso + "/Head", draw, -3, -12, -9, 10, 7, 0, -3);
            Rotate(ultimate, Torso + "/Scarf", draw, -65, -40, -45, -110, -120, -90, -65);
            Curve(ultimate, "Root", "m_LocalPosition.x", draw, 0, -.10f, -.08f, .22f, .22f, .08f, 0);
            Curve(ultimate, "Root", "m_LocalPosition.y", draw, .70f, .60f, .60f, .64f, .64f, .68f, .70f);
            clips.Add("Ultimate", ultimate);
            foreach (var clip in clips.Values)
            {
                SaveAsset(clip, $"{AnimationPath}/{clip.name}.anim");
            }
            return clips;
        }

        private static AnimationClip Clip(Transform graphic, string name, float duration, bool loop)
        {
            var clip = new AnimationClip { name = name, frameRate = 60 };
            var times = new[] { 0f, duration };
            foreach (var bone in graphic.GetComponentsInChildren<Transform>())
            {
                if (bone == graphic || bone.name == "Skin" || bone.name == "EffectSocket") continue;
                var path = AnimationUtility.CalculateTransformPath(bone, graphic);
                Curve(clip, path, "m_LocalPosition.x", times, bone.localPosition.x, bone.localPosition.x);
                Curve(clip, path, "m_LocalPosition.y", times, bone.localPosition.y, bone.localPosition.y);
                var angle = Mathf.DeltaAngle(0, bone.localEulerAngles.z);
                Rotate(clip, path, times, angle, angle);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void Rotate(AnimationClip clip, string path, float[] times, params float[] values)
        {
            Curve(clip, path, "localEulerAnglesRaw.z", times, values);
        }

        private static void Curve(AnimationClip clip, string path, string property, float[] times, params float[] values)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(times[i], values[i]);
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static AnimatorController CreateController(Transform graphic, Dictionary<string, AnimationClip> clips)
        {
            var path = AnimationPath + "/Kage.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var oldMachines = new List<AnimatorStateMachine>();
            foreach (var layer in controller.layers) oldMachines.Add(layer.stateMachine);
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            foreach (var machine in oldMachines) UnityEngine.Object.DestroyImmediate(machine, true);
            controller.AddLayer("Base Layer");
            var baseMachine = controller.layers[0].stateMachine;
            foreach (var pair in clips)
            {
                var state = baseMachine.AddState(pair.Key);
                state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationPath}/{pair.Key}.anim");
                state.writeDefaultValues = true;
                if (pair.Key == "Idle") baseMachine.defaultState = state;
            }
            var mask = new AvatarMask { name = "KageUpperBody" };
            var bones = graphic.GetComponentsInChildren<Transform>();
            mask.transformCount = bones.Length;
            for (var i = 0; i < bones.Length; i++)
            {
                var bonePath = AnimationUtility.CalculateTransformPath(bones[i], graphic);
                mask.SetTransformPath(i, bonePath);
                mask.SetTransformActive(i, bonePath == Torso || bonePath.StartsWith(Torso + "/", StringComparison.Ordinal));
            }
            SaveAsset(mask, AnimationPath + "/UpperBody.mask");
            controller.AddLayer("UpperBody");
            var layers = controller.layers;
            layers[1].defaultWeight = 1;
            layers[1].avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(AnimationPath + "/UpperBody.mask");
            var upperMachine = layers[1].stateMachine;
            upperMachine.defaultState = upperMachine.AddState("Empty");
            foreach (var name in new[] { "Attack", "Ultimate" })
            {
                upperMachine.AddState(name).motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationPath}/{name}.anim");
            }
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SaveAsset(UnityEngine.Object asset, string path)
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing == null) AssetDatabase.CreateAsset(asset, path);
            else
            {
                EditorUtility.CopySerialized(asset, existing);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [MenuItem("★Sayne★/Kage/리소스 검증")]
        public static void Validate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<Hero>() == null) throw new InvalidOperationException("Kage Hero prefab missing.");
            var animator = prefab.GetComponentInChildren<Animator>();
            var controller = (AnimatorController)animator.runtimeAnimatorController;
            if (controller.layers.Length != 2 || controller.layers[1].avatarMask == null) throw new InvalidOperationException("Kage upper-body layer missing.");
            foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.sprite == null || renderer.sharedMaterial == null) throw new InvalidOperationException("Missing Kage artwork: " + renderer.name);
                if (renderer.transform.localPosition != Vector3.zero || renderer.transform.localScale != Vector3.one)
                    throw new InvalidOperationException("Kage skin must use its anatomical sprite pivot.");
            }
            foreach (var clip in controller.animationClips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (animator.transform.Find(binding.path) == null) throw new InvalidOperationException("Missing bone: " + binding.path);
                }
            }
            var attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationPath + "/Attack.anim");
            if (Mathf.Abs(attack.length - .50f) > .001f) throw new InvalidOperationException("Kage attack duration mismatch.");
            Debug.Log("Kage validation passed: all sprite/material references, bone paths, two animator layers and 0.50s attack.");
        }
    }
}
