using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 죽는 모션의 몸통 궤적을 다시 굽는다.
    ///
    /// 원래는 Root 를 클립 내내 0 → 88도로 천천히 눕히기만 했다 — Y 는 처음부터 끝까지 0 이라 발이 땅에 붙은 채
    /// 간판처럼 회전했다. 맞고 죽는 몸은 그렇게 안 움직인다: <b>맞은 힘에 떠서</b> 날아가고, 그 사이 이미 다 누웠고,
    /// 땅에 한 번 부딪혀 튀었다가 잦아든다.
    ///
    /// 그래서 셋을 나눠 준다 — 회전은 앞쪽 1/5 안에 끝내고(눕는 건 순식간이다), 높이는 포물선 세 번(큰 도약 →
    /// 착지 반동 → 잔 튕김), 착지 순간에는 회전이 덜컹 흔들린다. 시간은 전부 클립 길이의 비율이라 길이가
    /// 다른 클립(Kage 0.65초, 나머지 0.75초)에도 같은 리듬으로 붙는다.
    ///
    /// Root 의 높이·회전만 갈아 끼운다. 팔다리 포즈는 원래 것을 그대로 둔다.
    /// </summary>
    public static class DeathMotionBuilder
    {
        private const string ClipRoot = "Assets/DarkFantasy2D/Animations";
        private const string ClipName = "Death";
        private const string RootBone = "Root";

        /// <summary>첫 도약의 높이. 뒤 튕김은 이 값의 비율이다.</summary>
        private const float JumpHeight = 1.0f;

        /// <summary>다 누운 각도. 원본이 +z 로 넘어가므로(뒤로 눕는다) 방향은 그대로 따른다.</summary>
        private const float LieAngle = 90f;

        [MenuItem("★Sayne★/공통/죽는 모션 다시 굽기", false, 59)]
        public static void Build()
        {
            var clips = AssetDatabase.FindAssets($"t:AnimationClip {ClipName}", new[] { ClipRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => System.IO.Path.GetFileNameWithoutExtension(path) == ClipName)
                .Where(path => !path.Contains("/Archive/"))
                .ToArray();

            if (clips.Length == 0)
            {
                Debug.LogError($"DeathMotionBuilder: {ClipRoot} 아래에 {ClipName} 클립이 없다");
                return;
            }

            foreach (var path in clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                var length = clip.length;

                SetCurve(clip, "m_LocalPosition.y", Bounce(length));
                SetCurve(clip, "localEulerAnglesRaw.z", Topple(length));

                EditorUtility.SetDirty(clip);
                Debug.Log($"DeathMotionBuilder: {path} — {length:0.00}초 궤적을 다시 구웠다", clip);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"DeathMotionBuilder: 죽는 모션 {clips.Length}개 완료.");
        }

        /// <summary>
        /// 떴다가 떨어지고 두 번 튕긴다. 마루는 0 이고, 각 구간은 포물선이라 접선을 손으로 물린다 —
        /// 기본 접선(부드럽게)을 쓰면 착지에서 둥글게 미끄러져 부딪힌 느낌이 안 난다.
        /// </summary>
        private static AnimationCurve Bounce(float length)
        {
            // (뜨는 시각, 정점 시각, 닿는 시각, 높이) — 전부 클립 길이의 비율이다.
            var arcs = new[]
            {
                (Launch: 0f, Apex: .20f, Land: .40f, Height: JumpHeight),
                (Launch: .40f, Apex: .55f, Land: .72f, Height: JumpHeight * .30f),
                (Launch: .72f, Apex: .82f, Land: .90f, Height: JumpHeight * .08f),
            };

            var keys = new List<Keyframe>();

            foreach (var arc in arcs)
            {
                var launch = arc.Launch * length;
                var apex = arc.Apex * length;
                var land = arc.Land * length;

                // 포물선의 출발·도착 기울기. 높이 h 를 dt 만에 오르면 처음 속도는 2h/dt 다.
                var up = 2f * arc.Height / (apex - launch);
                var down = -2f * arc.Height / (land - apex);

                if (keys.Count == 0)
                {
                    keys.Add(new Keyframe(launch, 0f, 0f, up));
                }
                else
                {
                    // 앞 포물선이 닿은 그 키에서 바로 튕겨 오른다 — 들어오는 기울기는 추락, 나가는 기울기는 반동.
                    var landed = keys[keys.Count - 1];
                    keys[keys.Count - 1] = new Keyframe(landed.time, 0f, landed.inTangent, up);
                }

                keys.Add(new Keyframe(apex, arc.Height, 0f, 0f));
                keys.Add(new Keyframe(land, 0f, down, 0f));
            }

            keys.Add(new Keyframe(length, 0f, 0f, 0f));

            return new AnimationCurve(keys.ToArray());
        }

        /// <summary>
        /// 눕는 회전. 앞쪽에서 거의 다 끝내고 조금 지나쳤다 돌아오며, 첫 착지에서 한 번 덜컹한다.
        /// </summary>
        private static AnimationCurve Topple(float length)
        {
            var spin = LieAngle / (.18f * length);

            var keys = new[]
            {
                new Keyframe(0f, 0f, 0f, spin),
                new Keyframe(.18f * length, LieAngle * .95f, spin * .35f, spin * .35f),
                new Keyframe(.30f * length, LieAngle + 6f, 0f, 0f),
                new Keyframe(.40f * length, LieAngle, 0f, 0f),
                new Keyframe(.46f * length, LieAngle + 3f, 0f, 0f),
                new Keyframe(.72f * length, LieAngle - 1f, 0f, 0f),
                new Keyframe(length, LieAngle, 0f, 0f),
            };

            return new AnimationCurve(keys);
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            var binding = EditorCurveBinding.FloatCurve(RootBone, typeof(Transform), property);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
