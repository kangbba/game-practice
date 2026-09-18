using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 근접 무기 — 검·낫·지팡이·몽둥이. 궁극기는 무기가 거대해지는 안무를 한 번 추고,
    /// 클립이 알리는 타격마다 무대 위 적 전부를 벤다. 지금은 모션이 하나뿐이라 근접 무기가 모두 이걸 같이 쓴다 —
    /// 모양은 든 무기 그림이 알아서 달라진다. 제 궁극기가 필요한 무기는 이걸 상속해 PlayUltimateAsync 를 덮는다.
    ///
    /// 궁극기 끝에는 뒤늦게 터지는 광역 도트가 붙는다. 칼을 거두고 가만히 선 뒤, 잠깐의 정적 다음에
    /// 무대 위 적 전부에게 잔상처럼 좌르륵 들어간다 — 사무라이가 등을 돌린 뒤에야 적이 무너지는 그 연출이다.
    /// 모션 끝에 그 시간만큼 가만히 선 자세가 붙어 있고(CharacterAnimations.UltimateDuration), 도트는 그 안에서 끝난다.
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        /// <summary>뒤늦은 도트가 들어가는 시간과 한 틱 간격.</summary>
        private const float AfterstrikeSeconds = 2.5f;
        private const float AfterstrikeInterval = 0.1f;

        /// <summary>도트 전체의 위력(공격력 배수). 틱마다 이걸 틱 수로 나눠 준다.</summary>
        private const float AfterstrikePower = 4f;

        public override string SkillAnimation => CharacterAnimations.SkillMeleeName;

        protected override string UltimateAnimation => CharacterAnimations.UltimateMeleeName;

        public override async UniTask PlayUltimateAsync(UltimateStage stage, CancellationToken token)
        {
            var combat = stage.Hero.Combat;
            var ultimate = combat.Ultimate;
            var seconds = stage.Hero.GetMotionSeconds(ultimate.AnimationHash);

            using (combat.HitMoment
                       .Where(combat, (attack, owner) => attack == owner.Ultimate)
                       .Subscribe(stage, (attack, owner) => owner.HitAll(attack)))
            {
                var motion = combat.PerformUltimateAsync(token);

                // 도트는 모션의 마지막 정지 구간에서 돌아 모션과 같이 끝난다.
                await UniTask.Delay(TimeSpan.FromSeconds(seconds - AfterstrikeSeconds), cancellationToken: token);
                await AfterstrikeAsync(stage, ultimate, token);

                await motion;
            }
        }

        /// <summary>무대 위 적 전부에게 일정 간격으로 작은 타격을 준다. 움찔은 없다 — 이미 끝난 칼질의 잔상이다.</summary>
        private static async UniTask AfterstrikeAsync(UltimateStage stage, CharacterSkill ultimate, CancellationToken token)
        {
            var ticks = Mathf.RoundToInt(AfterstrikeSeconds / AfterstrikeInterval);
            var tick = new BasicAttack(ultimate.Name, ultimate.Animation, AfterstrikePower / ticks, staggerSeconds: 0f);

            for (var i = 0; i < ticks; i++)
            {
                foreach (var target in stage.Targets)
                {
                    stage.Hit(target, tick);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(AfterstrikeInterval), cancellationToken: token);
            }
        }
    }
}
