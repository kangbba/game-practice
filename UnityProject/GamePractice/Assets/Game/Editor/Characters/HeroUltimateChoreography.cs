using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ease = Sayne.HeroActionPoseAuthoring.Ease;
using Pose = Sayne.HeroActionPoseAuthoring.Pose;
using Script = Sayne.HeroActionPoseAuthoring.Script;

namespace Sayne
{
    /// <summary>
    /// 궁극기 안무 — 7초 동작 뒤에 가만히 선 정지가 붙는다(전체 길이는 CharacterAnimations.UltimateDuration). 한 번 크게 내려찍던 옛 궁극기(Archive/Ultimate_v1)를 대신한다.
    ///
    /// 기 모으기(무기가 하늘 끝까지 자란다) → 베기 셋 → 공중제비 도약 → 낙하 강타
    /// → 마무리 회전 베기 → 공중제비로 내려와 착지해 가만히 선다. 전부 제자리다 — 앞뒤로 움직이지 않는다.
    /// 마지막 정지는 일부러 비워 둔 자리다 — 그 뒤에 도트 데미지가 좌르륵 들어간다.
    ///
    /// 포즈는 과장 없이 최종 유닛으로 적는다(골반 이동 x·y, 각도, 발 위치). 시각은 초.
    /// </summary>
    internal static class HeroUltimateChoreography
    {
        /// <summary>낙하 강타가 땅에 닿는 시각(초). 검증이 이 프레임의 칼끝을 본다.</summary>
        internal const float SlamSeconds = 4.85f;

        /// <summary>공중제비 정점(초). 검증이 이 프레임의 높이를 본다.</summary>
        internal const float ApexSeconds = 4.20f;

        private readonly struct Beat
        {
            internal readonly float Seconds;
            internal readonly Pose Pose;
            internal readonly Ease Ease;
            internal readonly bool Hit;

            internal Beat(float seconds, Pose pose, Ease ease, bool hit = false)
            {
                Seconds = seconds; Pose = pose; Ease = ease; Hit = hit;
            }
        }

        /// <summary>정점에서 골반 높이(유닛). 카메라 궁극기 시점의 위쪽 끝에 머리가 걸리지 않는 높이다.</summary>
        private const float Apex = 6.2f;

        /// <summary>거대해진 무기의 손잡이~끝 길이(유닛). 무기가 뭐든 같다.</summary>
        private const float BladeLength = 7f;

        /// <summary>영웅이 누구든 같은 안무다. 리그(발 위치·무기 원판)에 맞춰 굽기만 영웅별로 한다.</summary>
        internal static Script Build()
        {
            var apex = Apex;
            var duration = CharacterAnimations.UltimateDuration;

            // 궁극기는 제자리에서 한다 — 골반은 x 로 움직이지 않고 위아래(y)로만 오르내린다. 발 x 는 골반 기준 상대 위치다.

            // 땅에 선 포즈. 발은 땅(y=0)에 붙는다.
            Pose Ground(float y, float lean, float arm, float elbow, float blade, float reach,
                float front, float rear, float spin = 0f)
            {
                return new Pose(0f, y, lean, arm, elbow, blade, reach, front, rear, 0f, 0f, spin);
            }

            // 공중 포즈. 발은 골반을 따라 뜨고 tuck 만큼 오므린다. 골반이 spin 만큼 돌면 발도 같이 돈다.
            Pose Air(float y, float lean, float arm, float elbow, float blade, float reach,
                float front, float rear, float tuck, float spin)
            {
                return new Pose(0f, y, lean, arm, elbow, blade, reach, front, rear, y + tuck, y + tuck * 1.2f, spin);
            }

            var beats = new List<Beat>
            {
                new Beat(0f, default, Ease.Smooth),

                // ── 기 모으기: 깊이 웅크렸다가 무기를 하늘로 치켜든다. 이때 무기가 거대해진다.
                new Beat(.30f, Ground(-.38f, 32, 65, -75, 70, -.8f, -.05f, -.45f), Ease.Smooth),
                new Beat(.55f, Ground(-.44f, 36, 60, -80, 82, -.9f, -.05f, -.48f), Ease.Smooth),
                new Beat(.85f, Ground(.06f, 12, 172, -25, 8, -.2f, -.02f, -.40f), Ease.Out),
                new Beat(1.05f, Ground(.10f, 8, 178, -18, 4, -.1f, 0f, -.40f), Ease.Smooth),
                new Beat(1.28f, Ground(-.32f, 42, 150, -90, 125, -1f, -.05f, -.55f), Ease.Smooth),

                // ── 베기 셋: 가로 베기 → 올려 베기 → 머리 위에서 내려찍기. 제자리에서 몸을 싣는다.
                new Beat(1.50f, Ground(-.18f, -36, 82, -12, -95, 1.6f, .38f, -.30f), Ease.Linear, hit: true),
                new Beat(1.62f, Ground(-.16f, -42, 42, 18, -150, 1.1f, .40f, -.28f), Ease.Out),
                new Beat(1.85f, Ground(-.24f, -8, 22, -32, -172, -.3f, .12f, -.36f), Ease.Smooth),
                new Beat(2.08f, Ground(.06f, 18, 120, -30, -60, 1.3f, .32f, -.26f), Ease.Linear, hit: true),
                new Beat(2.22f, Ground(.10f, 28, 170, -60, 60, .6f, .34f, -.24f), Ease.Out),
                new Beat(2.48f, Ground(-.28f, 36, 165, -85, 145, -.9f, .02f, -.50f), Ease.Smooth),
                new Beat(2.72f, Ground(-.30f, -46, 72, 6, -122, 1.7f, .45f, -.36f), Ease.Linear, hit: true),
                new Beat(2.86f, Ground(-.34f, -52, 44, 22, -152, 1.2f, .46f, -.34f), Ease.Out),

                // ── 도약: 웅크렸다가 제자리에서 한 바퀴 공중제비, 정점에서 잠깐 멈춘다.
                new Beat(3.12f, Ground(-.42f, 26, 118, -72, 95, -1f, .10f, -.46f), Ease.Smooth),
                new Beat(3.36f, Air(1.9f, -6, 165, -70, 45, -.3f, .15f, -.20f, .55f, -50), Ease.Out),
                new Beat(3.72f, Air(apex * .74f, 4, 170, -80, 22, -.4f, .12f, -.18f, .60f, -215), Ease.Out),
                new Beat(4.05f, Air(apex, 16, 176, -86, 12, -.5f, .10f, -.16f, .40f, -360), Ease.Out),
                new Beat(4.40f, Air(apex + .1f, 20, 180, -88, 8, -.5f, .10f, -.16f, .35f, -360), Ease.Smooth),

                // ── 낙하 강타: 무기를 땅에 꽂는다. 잠깐 박힌 채 버틴다.
                new Beat(4.58f, Air(apex * .79f, -22, 162, -55, -15, .6f, .12f, -.16f, .35f, -360), Ease.In),
                new Beat(SlamSeconds, Ground(-.32f, -48, 80, 8, -112, 1.8f, .32f, -.32f, -360), Ease.In, hit: true),
                new Beat(5.00f, Ground(-.38f, -52, 76, 12, -116, 1.6f, .33f, -.33f, -360), Ease.Smooth),

                // ── 마무리 회전 베기: 무기를 끌며 일어나 한 바퀴 크게 휘두른다.
                new Beat(5.32f, Ground(-.10f, -12, 40, -24, -162, 0f, .20f, -.30f, -360), Ease.Smooth),
                new Beat(5.56f, Ground(-.22f, 32, 142, -72, 150, -.8f, .10f, -.50f, -360), Ease.Smooth),
                new Beat(5.72f, Ground(-.20f, -44, 78, -4, -95, 1.6f, .36f, -.30f, -360), Ease.Linear, hit: true),
                new Beat(5.92f, Ground(-.26f, -52, 30, 26, -232, 1.1f, .36f, -.30f, -360), Ease.Out),

                // ── 복귀: 공중제비로 내려와 착지하고, 무기가 원래 크기로 줄어든 채 가만히 선다.
                new Beat(6.12f, Ground(-.42f, 22, 62, -42, 32, -.9f, .08f, -.40f, -360), Ease.Smooth),
                new Beat(6.36f, Air(3.0f, 8, 105, -62, 45, -.3f, .12f, -.18f, .5f, -160), Ease.Out),
                new Beat(6.62f, Ground(-.30f, 14, 42, -42, 24, -.5f, .15f, -.30f), Ease.In),
                new Beat(7.00f, default, Ease.Smooth),

                // ── 정지: 칼을 거두고 가만히 선 채 버틴다. 이 사이에 뒤늦은 광역 도트가 좌르륵 들어간다(MeleeWeapon).
                new Beat(duration, default, Ease.Smooth),
            };

            return new Script
            {
                Poses = beats.Select(beat => beat.Pose).ToArray(),
                Times = beats.Select(beat => beat.Seconds / duration).ToArray(),
                Eases = beats.Select(beat => beat.Ease).ToArray(),
                HitSeconds = beats.Where(beat => beat.Hit).Select(beat => beat.Seconds).ToArray(),
                BladeLength = BladeLength,
                Expansion = t => Expansion(t * duration),
                GroundAim = t => GroundAim(t * duration),
            };
        }

        /// <summary>
        /// 무기 크기(0 = 평소, 1 = 거대). 기 모으기에서 자라 끝까지 유지하고, 공중제비 동안만 절반쯤 줄였다가
        /// 낙하하며 다시 뻗는다. 복귀 공중제비에서 평소 크기로 돌아온다.
        /// </summary>
        private static float Expansion(float seconds)
        {
            var grow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1.05f, seconds));
            var flip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.15f, 3.45f, seconds))
                       * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.45f, 4.75f, seconds)));
            var shrink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.05f, 6.50f, seconds));
            return grow * (1f - .55f * flip) * shrink;
        }

        /// <summary>칼끝을 땅에 맞추는 비중. 낙하 중 겨눠서 강타에 꽂고, 일어나며 푼다.</summary>
        private static float GroundAim(float seconds)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.62f, SlamSeconds, seconds))
                   * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.05f, 5.35f, seconds)));
        }
    }
}
