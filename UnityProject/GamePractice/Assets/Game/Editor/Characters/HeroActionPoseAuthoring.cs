using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sayne
{
    internal static class HeroActionPoseAuthoring
    {
        private const string TorsoPath = "Root/Torso";
        private const string ArmPath = TorsoPath + "/Arm";
        private const string ElbowPath = ArmPath + "/Forearm";
        private const string WeaponPath = ElbowPath + "/Weapon";

        /// <summary>
        /// 관절 지연·꼬리 회수는 클립 길이 대비 비율로 적어 두었다. 이보다 긴 클립(7초 궁극기)에서는
        /// 그 비율이 초 단위로 너무 늘어지므로, 이 길이의 클립이 갖던 초를 넘지 않게 잡는다.
        /// </summary>
        private const float LagReferenceSeconds = 1.8f;

        internal readonly struct Pose
        {
            internal readonly float X, Y, Lean, Arm, Elbow, Blade, Reach, Step, RearStep, Lift, RearLift, Spin;

            /// <param name="spin">Root(골반) 회전. 공중제비용이라 다른 관절과 달리 꼬리 회수를 안 한다 — 360 은 0 과 같다.</param>
            internal Pose(float x, float y, float lean, float arm, float elbow, float blade,
                float reach, float step, float rearStep, float lift = 0f, float rearLift = 0f, float spin = 0f)
            {
                X = x; Y = y; Lean = lean; Arm = arm; Elbow = elbow; Blade = blade;
                Reach = reach; Step = step; RearStep = rearStep; Lift = lift; RearLift = rearLift; Spin = spin;
            }

            internal static Pose Lerp(Pose a, Pose b, float t)
            {
                return new Pose(Mathf.Lerp(a.X, b.X, t), Mathf.Lerp(a.Y, b.Y, t),
                    Mathf.Lerp(a.Lean, b.Lean, t), Mathf.Lerp(a.Arm, b.Arm, t),
                    Mathf.Lerp(a.Elbow, b.Elbow, t), Mathf.Lerp(a.Blade, b.Blade, t),
                    Mathf.Lerp(a.Reach, b.Reach, t), Mathf.Lerp(a.Step, b.Step, t),
                    Mathf.Lerp(a.RearStep, b.RearStep, t), Mathf.Lerp(a.Lift, b.Lift, t),
                    Mathf.Lerp(a.RearLift, b.RearLift, t), Mathf.Lerp(a.Spin, b.Spin, t));
            }
        }

        /// <summary>키프레임 사이를 어떻게 잇나. 베기·낙하처럼 속도를 살릴 구간과 준비·회수처럼 부드럽게 이을 구간을 가른다.</summary>
        internal enum Ease
        {
            Smooth,
            Linear,
            /// <summary>빠르게 시작해 느리게 멈춤 — 도약, 휘두른 뒤 여운.</summary>
            Out,
            /// <summary>느리게 시작해 빠르게 도착 — 낙하.</summary>
            In,
        }

        /// <summary>한 모션의 악보. 포즈와 시각(0~1), 구간별 이음새, 무기를 얼마나 키우고 어디에 꽂을지.</summary>
        internal sealed class Script
        {
            internal Pose[] Poses;
            internal float[] Times;

            /// <summary>Eases[i] 는 Poses[i-1] → Poses[i] 구간. 0 번은 안 쓴다.</summary>
            internal Ease[] Eases;

            /// <summary>영웅 원점 기준 무기 끝 반경. 스킬이 쓴다. 0 이면 확대 없음.</summary>
            internal float Radius;

            /// <summary>무기 길이(유닛). 궁극기가 쓴다 — 몸이 원점에서 멀리 떠나도 크기가 유지된다. 0 이면 안 씀.</summary>
            internal float BladeLength;

            /// <summary>t 에서 무기를 얼마나 키우나(0~1).</summary>
            internal Func<float, float> Expansion = _ => 0f;

            /// <summary>t 에서 칼끝을 땅에 얼마나 맞추나(0~1). 낙하 강타용.</summary>
            internal Func<float, float> GroundAim = _ => 0f;

            /// <summary>맞는 순간(초). 클립에 OnHitFrame 이벤트로 박힌다. 비어 있으면 타격은 런타임 타이머가 낸다.</summary>
            internal float[] HitSeconds = Array.Empty<float>();
        }

        private sealed class Bone
        {
            internal readonly Transform Transform;
            internal readonly string Path;
            internal readonly Vector3 Position, Scale;
            internal readonly float Angle;
            internal readonly List<Keyframe>[] Keys = Enumerable.Range(0, 5).Select(_ => new List<Keyframe>()).ToArray();

            internal Bone(Transform transform, Transform graphic)
            {
                Transform = transform;
                Path = AnimationUtility.CalculateTransformPath(transform, graphic);
                Position = transform.localPosition;
                Scale = transform.localScale;
                Angle = Mathf.DeltaAngle(0f, transform.localEulerAngles.z);
            }

            internal void Reset()
            {
                Transform.localPosition = Position;
                Transform.localRotation = Quaternion.Euler(0f, 0f, Angle);
                Transform.localScale = Scale;
            }

            internal void Rotate(float offset)
            {
                Transform.localRotation = Quaternion.Euler(0f, 0f, Angle + offset);
            }

            internal void Record(float time)
            {
                var angle = Mathf.DeltaAngle(0f, Transform.localEulerAngles.z);
                if (Keys[2].Count > 0)
                {
                    var previous = Keys[2][Keys[2].Count - 1].value;
                    angle = previous + Mathf.DeltaAngle(previous, angle);
                }
                Keys[0].Add(new Keyframe(time, Transform.localPosition.x));
                Keys[1].Add(new Keyframe(time, Transform.localPosition.y));
                Keys[2].Add(new Keyframe(time, angle));
                Keys[3].Add(new Keyframe(time, Transform.localScale.x));
                Keys[4].Add(new Keyframe(time, Transform.localScale.y));
            }
        }

        internal static AnimationClip Create(Transform source, string hero, int action, string name, float duration)
        {
            // 제작용 복사본에서 발 접지와 관절 체인을 계산해 평범한 Transform 커브로 굽는다.
            var instance = UnityEngine.Object.Instantiate(source.gameObject);
            instance.hideFlags = HideFlags.HideAndDontSave;
            var graphic = instance.transform;
            graphic.position = Vector3.zero;
            graphic.rotation = Quaternion.identity;
            graphic.localScale = Vector3.one;
            instance.GetComponent<Animator>().enabled = false;
            try
            {
                var bones = graphic.GetComponentsInChildren<Transform>(true)
                    .Where(b => b != graphic && !AnimationUtility.CalculateTransformPath(b, graphic).Split('/').Contains("Skin"))
                    .Select(b => new Bone(b, graphic)).ToDictionary(b => b.Path);
                var root = bones["Root"];
                var front = bones["Root/FrontLeg"];
                var rear = bones["Root/RearLeg"];
                var frontSole = Sole(front.Transform, graphic);
                var rearSole = Sole(rear.Transform, graphic);
                var frontEnd = front.Transform.InverseTransformPoint(graphic.TransformPoint(frontSole));
                var rearEnd = rear.Transform.InverseTransformPoint(graphic.TransformPoint(rearSole));
                var script = action == 4 ? HeroUltimateChoreography.Build() : ActionScript(hero, action);
                var weapon = bones[WeaponPath];
                var weaponTip = GetWeaponTip(hero);
                var lagScale = Mathf.Min(1f, LagReferenceSeconds / duration);
                var samples = Mathf.CeilToInt(duration * 60f);
                for (var frame = 0; frame <= samples; frame++)
                {
                    var t = (float)frame / samples;
                    foreach (var bone in bones.Values) bone.Reset();
                    var pose = Sample(script, t);
                    var torso = Sample(script, Mathf.Max(0f, t - .012f * lagScale));
                    var arm = Sample(script, Mathf.Max(0f, t - .024f * lagScale));
                    var elbow = Sample(script, Mathf.Max(0f, t - .038f * lagScale));
                    // 지연된 관절도 마지막에는 기본 자세로 돌아온다.
                    var tail = Mathf.Clamp01((1f - t) / (.08f * lagScale));
                    root.Transform.localPosition += new Vector3(pose.X, pose.Y, 0f);
                    root.Rotate(pose.Spin);
                    bones[TorsoPath].Rotate(torso.Lean * tail);
                    bones[TorsoPath].Transform.localScale = new Vector3(1f + pose.Reach * .045f, 1f - pose.Reach * .035f, 1f);
                    bones[ArmPath].Rotate(arm.Arm * tail);
                    bones[ElbowPath].Rotate(elbow.Elbow * tail);
                    // 무기 방향을 어깨/팔꿈치 각도의 합에서 분리해 칼끝이 의도한 호를 지난다.
                    bones[WeaponPath].Rotate((pose.Blade - torso.Lean - arm.Arm - elbow.Elbow) * tail);
                    if (script.BladeLength > 0f)
                    {
                        // 길이를 먼저 정해야 땅에 닿는 지점을 알 수 있다.
                        ExpandWeaponToLength(weapon, graphic, weaponTip, script.BladeLength, script.Expansion(t));
                        AimAtGround(weapon, graphic, weaponTip, script.GroundAim(t));
                    }
                    if (script.Radius > 0f)
                        ExtendWeapon(weapon, graphic, weaponTip, script.Radius, script.Expansion(t));
                    var lag = Sample(script, Mathf.Max(0f, t - .065f * lagScale));
                    Rotate(bones, TorsoPath + "/BackArm", (-lag.Arm * .48f - pose.Lean * .35f) * tail);
                    Rotate(bones, TorsoPath + "/BackArm/Forearm", (22f * pose.Reach - lag.Elbow * .55f) * tail);
                    Rotate(bones, TorsoPath + "/Head", (-torso.Lean * .72f + lag.Lean * .12f) * tail);
                    var drag = Sample(script, Mathf.Max(0f, t - .105f * lagScale));
                    var velocityWindow = .03f * lagScale;
                    var velocity = (pose.X - Sample(script, Mathf.Max(0f, t - velocityWindow)).X) / velocityWindow;
                    var isBig = script.Radius > 0f || script.BladeLength > 0f;
                    var capeLimit = isBig ? 85f : 48f;
                    var scarfLimit = isBig ? 110f : 65f;
                    Rotate(bones, TorsoPath + "/Cape", Mathf.Clamp(-drag.Lean * .9f - velocity * 8f, -capeLimit, capeLimit) * tail);
                    Rotate(bones, TorsoPath + "/Scarf", Mathf.Clamp(-drag.Lean * 1.2f - velocity * 11f, -scarfLimit, scarfLimit) * tail);
                    Rotate(bones, TorsoPath + "/Head/Hair", (-drag.Lean * .35f - velocity * 3f) * tail);
                    Plant(front, graphic, root, frontSole + new Vector3(pose.Step, pose.Lift, 0f), frontEnd, pose.Spin);
                    Plant(rear, graphic, root, rearSole + new Vector3(pose.RearStep, pose.RearLift, 0f), rearEnd, pose.Spin);
                    if (frame == 0 || frame == samples)
                        foreach (var bone in bones.Values) bone.Reset();
                    foreach (var bone in bones.Values) bone.Record(t * duration);
                }
                var clip = new AnimationClip { name = name, frameRate = 60f };
                var properties = new[] { "m_LocalPosition.x", "m_LocalPosition.y", "localEulerAnglesRaw.z", "m_LocalScale.x", "m_LocalScale.y" };
                foreach (var bone in bones.Values)
                {
                    for (var i = 0; i < properties.Length; i++)
                    {
                        var curve = new AnimationCurve(Reduce(bone.Keys[i], i == 2 ? .12f : .0005f));
                        for (var k = 0; k < curve.length; k++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                        }
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bone.Path, typeof(Transform), properties[i]), curve);
                    }
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                // 무기가 뻗는 키프레임에 타격 이벤트를 박는다. 런타임은 이 이벤트로 맞는 순간을 안다.
                AnimationUtility.SetAnimationEvents(clip, script.HitSeconds
                    .Select(seconds => new AnimationEvent { time = seconds, functionName = CharacterAnimations.HitFrameEvent })
                    .ToArray());
                return clip;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static Keyframe[] Reduce(List<Keyframe> keys, float tolerance)
        {
            var keep = new SortedSet<int> { 0, keys.Count - 1 };
            void Split(int start, int end)
            {
                var largest = tolerance;
                var selected = -1;
                for (var i = start + 1; i < end; i++)
                {
                    var t = Mathf.InverseLerp(keys[start].time, keys[end].time, keys[i].time);
                    var error = Mathf.Abs(keys[i].value - Mathf.Lerp(keys[start].value, keys[end].value, t));
                    if (error <= largest) continue;
                    largest = error;
                    selected = i;
                }
                if (selected < 0) return;
                keep.Add(selected);
                Split(start, selected);
                Split(selected, end);
            }
            Split(0, keys.Count - 1);
            return keep.Select(i => keys[i]).ToArray();
        }

        /// <summary>평타·스킬 악보. 포즈 셋(준비·베기·여운)을 과장해 시각표에 얹는다.</summary>
        private static Script ActionScript(string hero, int action)
        {
            var poses = Poses(hero, action).Select(pose => Exaggerate(pose, hero, action)).ToArray();
            var times = action == 3
                ? new[] { 0f, .08f, .18f, CharacterAnimations.SkillImpact, .32f, .40f, .51f, .64f, .67f, .76f, .87f, .95f, 1f }
                : new[] { 0f, .10f, .24f, .36f, .40f, .51f, .69f, .87f, 1f };
            // 베기와 관성 구간은 속도를 유지하고, 준비와 회수만 부드럽게 잇는다.
            var eases = times.Select((_, i) => i == 3 || i == 5 ? Ease.Linear : Ease.Smooth).ToArray();
            return new Script
            {
                Poses = poses,
                Times = times,
                Eases = eases,
                Radius = action == 3 ? 7f : 0f,
                Expansion = t => GetExpansion(t),
            };
        }

        private static Pose Exaggerate(Pose pose, string hero, int action)
        {
            var travel = action switch { 0 => .7f, 1 => 1.1f, 2 => 1.6f, 5 => 2.6f,
                3 => hero == "Nyx" ? 6f : 5f, _ => throw new ArgumentOutOfRangeException(nameof(action)) };
            var height = action == 3 ? 2.5f : action == 5 ? 1.8f : 1f;
            var power = action == 3 ? 1.4f : action == 5 ? 1.25f : 1f;
            var x = pose.X * travel;
            var y = pose.Y >= 0f ? pose.Y * height : pose.Y * 1.2f;
            // 전신 이동을 발에도 더해 큰 보폭에서 다리가 늘어나지 않게 한다.
            return new Pose(x, y, Mathf.Clamp(pose.Lean * power, -70f, 70f),
                Mathf.Clamp(pose.Arm * power, -200f, 200f),
                Mathf.Clamp(pose.Elbow * power, -115f, 115f),
                pose.Blade, pose.Reach * power,
                x + (pose.Step - pose.X) * 1.2f,
                x + (pose.RearStep - pose.X) * 1.2f,
                pose.Lift + y - pose.Y, pose.RearLift + y - pose.Y);
        }

        private static Vector3 GetWeaponTip(string hero)
        {
            var name = hero == "Kage" ? "Sword" : hero == "Aldric" ? "Scythe" : "Staff";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Equipment/Weapon/{name}/{name}.prefab");

            // 무기는 소켓 아래 WeaponMount 에 붙는다. 끝은 무기 규격상 뿌리(쥐는 점)에서 +Y 로 뻗은 곳이고,
            // Mount 가 그 방향을 소켓 기준으로 돌려 놓는다.
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Characters/Heroes/{hero}/{hero}.prefab");
            var mount = heroPrefab.GetComponentsInChildren<Transform>(true).First(t => t.name == "WeaponMount");
            var tip = prefab.transform.InverseTransformPoint(prefab.GetComponent<Weapon>().Tip.position);
            return mount.localRotation * tip;
        }

        /// <summary>스킬에서 무기가 손을 떠나 뻗는 비중. 준비 끝에서 나가기 시작해 타격에 끝까지 뻗고, 회수하며 손으로 돌아온다.</summary>
        private static float GetExpansion(float t)
        {
            var grow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, CharacterAnimations.SkillImpact, t));
            var shrink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.76f, .96f, t));
            return grow * shrink;
        }

        /// <summary>
        /// 칼끝이 발 앞 땅(y=0)에 닿도록 무기를 돌린다. 지금 무기 길이로 닿을 수 있는 가장 먼 땅을 겨눈다.
        /// 몸통이 Reach 로 비균등하게 늘어나 있으면 로컬 회전각과 그림 공간 회전각이 어긋나므로, 몇 번 되풀이해 맞춘다.
        /// </summary>
        private static void AimAtGround(Bone weapon, Transform graphic, Vector3 tip, float weight)
        {
            if (weight <= 0f) return;
            var rest = weapon.Transform.localRotation;
            for (var pass = 0; pass < 6; pass++)
            {
                var grip = (Vector2)graphic.InverseTransformPoint(weapon.Transform.position);
                var blade = (Vector2)graphic.InverseTransformVector(weapon.Transform.TransformVector(tip));
                var reach = Mathf.Sqrt(Mathf.Max(0f, blade.sqrMagnitude - grip.y * grip.y));
                var target = new Vector2(grip.x + reach, 0f) - grip;
                var angle = Vector2.SignedAngle(blade, target);
                weapon.Transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward) * weapon.Transform.localRotation;
            }
            weapon.Transform.localRotation = Quaternion.Slerp(rest, weapon.Transform.localRotation, weight);
        }

        /// <summary>
        /// 스킬: 무기 크기는 그대로 두고, 무기가 뻗는 방향으로 손에서 밀어낸다 — 칼이 손을 떠나 날아가는 느낌이다.
        /// 끝이 영웅 원점에서 반경 radius 에 닿는 만큼만 민다. 판정 반경은 키우던 때와 같다.
        /// </summary>
        private static void ExtendWeapon(Bone weapon, Transform graphic, Vector3 tip, float radius, float weight)
        {
            var grip = (Vector2)graphic.InverseTransformPoint(weapon.Transform.position);
            var blade = (Vector2)graphic.InverseTransformVector(weapon.Transform.TransformVector(tip));
            var end = grip + blade;
            var along = blade.normalized;

            // |end + along * distance| = radius 인 distance. 이미 닿아 있으면 밀지 않는다.
            var projection = Vector2.Dot(end, along);
            var distance = Mathf.Max(0f, -projection + Mathf.Sqrt(Mathf.Max(0f, projection * projection - end.sqrMagnitude + radius * radius)));

            var offset = weapon.Transform.parent.InverseTransformVector(graphic.TransformVector(along * distance * weight));
            weapon.Transform.localPosition += offset;
        }

        /// <summary>손잡이에서 칼끝까지가 length 유닛이 되도록 키운다. 몸이 어디에 있든 크기가 같다.</summary>
        private static void ExpandWeaponToLength(Bone weapon, Transform graphic, Vector3 tip, float length, float weight)
        {
            var blade = (Vector2)graphic.InverseTransformVector(weapon.Transform.TransformVector(tip));
            var scale = Mathf.Max(1f, length / blade.magnitude);
            weapon.Transform.localScale = weapon.Scale * Mathf.Lerp(1f, scale, weight);
        }

        private static Vector3 Sole(Transform leg, Transform graphic)
        {
            var sprite = leg.GetComponentInChildren<SpriteRenderer>();
            var bounds = sprite.bounds;
            return graphic.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y + .015f, leg.position.z));
        }

        /// <summary>
        /// 발바닥을 target(그림 좌표)에 붙인다. 골반이 도는 중(spin)이면 발도 골반을 축으로 같이 돈다 —
        /// 공중제비에서 발이 땅을 향해 버티지 않게.
        /// </summary>
        private static void Plant(Bone leg, Transform graphic, Bone root, Vector3 target, Vector3 restEnd, float spin)
        {
            var hip = root.Transform.localPosition;
            target = hip + Quaternion.Euler(0f, 0f, spin) * (target - hip);
            var delta = leg.Transform.parent.InverseTransformVector(graphic.TransformPoint(target) - leg.Transform.position);
            var angle = Vector2.SignedAngle(restEnd, delta);
            leg.Transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var length = new Vector2(delta.x, delta.y).magnitude / new Vector2(restEnd.x, restEnd.y).magnitude;
            leg.Transform.localScale = new Vector3(length, length, 1f);
        }

        private static void Rotate(Dictionary<string, Bone> bones, string path, float offset)
        {
            if (bones.TryGetValue(path, out var bone)) bone.Rotate(offset);
        }

        private static Pose Sample(Script script, float t)
        {
            var times = script.Times;
            for (var i = 1; i < times.Length; i++)
            {
                if (t > times[i]) continue;
                var u = Mathf.InverseLerp(times[i - 1], times[i], t);
                u = script.Eases[i] switch
                {
                    Ease.Linear => u,
                    Ease.Out => 1f - (1f - u) * (1f - u),
                    Ease.In => u * u,
                    _ => u * u * (3f - 2f * u),
                };
                return Pose.Lerp(script.Poses[i - 1], script.Poses[i], u);
            }
            return default;
        }

        private static Pose[] Poses(string hero, int action)
        {
            Pose windup, strike, follow;
            if (hero == "Kage")
            {
                (windup, strike, follow) = action switch
                {
                    0 => (new Pose(-.10f,-.06f,12,92,-52,65,-.3f,0,-.12f,.10f), new Pose(.25f,-.09f,-28,66,-18,-92,1,.40f,0), new Pose(.32f,-.07f,-34,42,10,-136,.8f,.40f,.04f,0,.08f)),
                    1 => (new Pose(.02f,-.12f,-22,24,-40,-125,-.2f,.12f,-.14f), new Pose(.24f,.06f,14,120,-44,20,1,.32f,-.08f,.05f,.14f), new Pose(.29f,.09f,20,138,-52,65,.7f,.32f,-.02f,.08f,.18f)),
                    2 => (new Pose(-.14f,-.10f,22,140,-64,115,-.5f,0,-.20f,.16f), new Pose(.36f,-.13f,-36,78,-14,-110,1.2f,.54f,.10f), new Pose(.43f,-.10f,-30,32,22,-165,.7f,.54f,.14f,0,.12f)),
                    5 => (new Pose(-.24f,-.22f,38,155,-82,145,-.6f,-.10f,-.32f,.12f), new Pose(.68f,.28f,-48,92,12,-135,1.6f,.82f,.48f,.34f,.42f), new Pose(.82f,.12f,-55,20,38,-235,1,.90f,.62f,.18f,.22f)),
                    3 => (new Pose(-.18f,-.12f,25,100,-62,85,-.5f,0,-.24f,.12f), new Pose(.50f,-.12f,-40,76,-8,-100,1.3f,.65f,.24f), new Pose(.60f,-.08f,-34,28,20,-160,.8f,.68f,.26f,0,.16f)),
                    _ => throw new ArgumentOutOfRangeException(nameof(action))
                };
            }
            else if (hero == "Aldric")
            {
                (windup, strike, follow) = action switch
                {
                    0 => (new Pose(-.08f,-.06f,18,110,-50,70,-.3f,0,-.14f,.06f), new Pose(.18f,-.11f,-25,72,-18,-65,.9f,.32f,-.05f), new Pose(.24f,-.09f,-31,46,12,-100,.8f,.32f,0)),
                    1 => (new Pose(-.12f,-.07f,26,36,-58,105,-.4f,-.03f,-.16f), new Pose(.27f,-.06f,-19,100,-20,-25,1.1f,.42f,0), new Pose(.31f,-.04f,-24,112,-8,-55,.8f,.42f,.05f,0,.05f)),
                    2 => (new Pose(-.10f,-.13f,-20,30,-28,-85,-.4f,.10f,-.18f), new Pose(.22f,.12f,19,140,-42,75,1.2f,.38f,0,.14f,.20f), new Pose(.28f,.17f,24,152,-54,108,.8f,.40f,.05f,.18f,.26f)),
                    5 => (new Pose(-.22f,.26f,30,168,-80,160,-.6f,-.14f,-.30f,.30f,.38f), new Pose(.54f,-.24f,-52,80,12,-105,1.6f,.72f,.34f), new Pose(.64f,-.18f,-58,24,30,-155,1,.78f,.46f)),
                    3 => (new Pose(-.13f,-.10f,26,148,-66,100,-.5f,0,-.20f,.12f), new Pose(.36f,-.14f,-36,90,-20,-80,1.3f,.52f,.10f), new Pose(.42f,-.11f,-40,48,8,-128,1,.52f,.14f)),
                    _ => throw new ArgumentOutOfRangeException(nameof(action))
                };
            }
            else
            {
                (windup, strike, follow) = action switch
                {
                    0 => (new Pose(-.10f,.04f,15,36,-58,35,-.3f,-.04f,-.12f,.05f,.10f), new Pose(.14f,.09f,-18,82,-12,-45,.9f,.22f,-.04f,.12f,.16f), new Pose(.07f,.12f,8,64,-30,-25,-.2f,.18f,-.08f,.15f,.19f)),
                    1 => (new Pose(-.06f,.07f,-12,80,-65,-52,-.3f,.02f,-.12f,.08f,.14f), new Pose(.15f,.15f,16,130,-35,65,1,.24f,-.06f,.18f,.26f), new Pose(.06f,.18f,22,144,-52,90,.5f,.18f,-.10f,.22f,.30f)),
                    2 => (new Pose(-.10f,.18f,22,132,-72,85,-.4f,-.06f,-.16f,.20f,.28f), new Pose(.22f,.05f,-25,70,-12,-72,1.1f,.30f,-.02f,.08f,.16f), new Pose(.10f,.10f,-12,50,-30,-105,.5f,.24f,-.04f,.12f,.20f)),
                    5 => (new Pose(-.24f,.28f,32,158,-82,155,-.6f,-.16f,-.32f,.32f,.40f), new Pose(.40f,.46f,-38,96,10,-125,1.5f,.52f,.22f,.50f,.58f), new Pose(.22f,.32f,-28,30,28,-230,.8f,.34f,.04f,.36f,.44f)),
                    3 => (new Pose(-.12f,.22f,26,140,-70,110,-.5f,-.08f,-.20f,.25f,.34f), new Pose(.24f,.14f,-30,106,-16,-55,1.2f,.34f,.02f,.18f,.28f), new Pose(.08f,.20f,16,80,-45,-15,-.2f,.24f,-.10f,.24f,.34f)),
                    _ => throw new ArgumentOutOfRangeException(nameof(action))
                };
            }
            var preload = Pose.Lerp(default, windup, .48f);
            var recover = Pose.Lerp(follow, default, .62f);
            var settle = Pose.Lerp(follow, default, .94f);
            if (hero != "Nyx")
            {
                recover = new Pose(recover.X, recover.Y, recover.Lean, recover.Arm, recover.Elbow,
                    recover.Blade, recover.Reach, recover.Step, follow.RearStep, .12f, 0f);
                settle = new Pose(settle.X, settle.Y, settle.Lean, settle.Arm, settle.Elbow,
                    settle.Blade, settle.Reach, 0f, settle.RearStep, 0f, .06f);
            }
            if (action == 3)
            {
                var counter = hero switch
                {
                    "Kage" => new Pose(.28f,-.11f,-20,24,-48,-125,-.3f,.42f,.08f),
                    "Aldric" => new Pose(.18f,.10f,18,150,-64,110,-.4f,.40f,.08f,.12f),
                    _ => new Pose(-.08f,.26f,24,144,-62,100,-.5f,.05f,-.16f,.28f,.36f)
                };
                var finish = hero switch
                {
                    "Kage" => new Pose(.56f,.12f,18,138,-38,42,1.1f,.70f,.30f,.18f,.26f),
                    "Aldric" => new Pose(.48f,-.15f,-40,76,-12,-115,1.3f,.62f,.22f),
                    _ => new Pose(.24f,.18f,-24,114,-10,-65,1.3f,.35f,.04f,.22f,.32f)
                };
                var finishFollow = hero == "Kage"
                    ? new Pose(.60f,.08f,24,148,-46,82,.8f,.70f,.30f,.13f,.18f)
                    : Pose.Lerp(finish, follow, .35f);
                return new[] { default(Pose), preload, windup, strike, strike, follow, counter, finish,
                    finish, finishFollow, recover, settle, default };
            }
            return new[] { default(Pose), preload, windup, strike, strike, follow, recover, settle, default };
        }
    }
}
