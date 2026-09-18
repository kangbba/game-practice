using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sayne
{
    /// <summary>
    /// 활 — 투사체를 쏘는 원거리 무기. 평타는 한 발씩 쏘고, 궁극기는 겨눈 자세로 멈춰 선 채 무대 위 적에게 쏟아붓는다.
    /// 궁극기 모션은 정지 자세 하나이고, 얼마나 버틸지는 그 클립 길이다 — 난사는 그 사이를 이 태스크가 채운다.
    /// 궁극기 위력은 전체 합이다. 한 발은 그걸 발수로 나눈 만큼 때린다.
    /// </summary>
    public class Bow : Weapon
    {
        [Header("활")]
        /// <summary>쏘는 투사체. 화살·총알처럼 무기마다 다른 걸 쏠 수 있게 활이 들고 있다.</summary>
        [SerializeField] private Projectile _projectile;

        private const float ShotsPerSecond = 20f;

        /// <summary>자세를 잡고 첫 발까지, 마지막 발 뒤 자세를 풀기까지의 여유.</summary>
        private const float LeadSeconds = 0.3f;

        /// <summary>화살은 활 쥔 자리에서 겨누는 쪽으로 조금 나가서, 이만큼 흩어져 떠난다.</summary>
        private const float MuzzleForward = 0.3f;
        private const float MuzzleSpread = 0.3f;

        private const float ShotStaggerSeconds = 0.08f;

        public override Projectile Projectile => _projectile;

        protected override string UltimateAnimation => CharacterAnimations.UltimateRangedName;

        public override async UniTask PlayUltimateAsync(UltimateStage stage, CancellationToken token)
        {
            var hero = stage.Hero;
            var ultimate = hero.Combat.Ultimate;
            var motion = hero.Combat.PerformUltimateAsync(token);

            var seconds = hero.GetMotionSeconds(ultimate.AnimationHash) - LeadSeconds * 2f;
            var shots = Mathf.Max(1, Mathf.RoundToInt(seconds * ShotsPerSecond));
            var shot = new BasicAttack(ultimate.Name, ultimate.Animation, ultimate.PowerMultiplier / shots,
                staggerSeconds: ShotStaggerSeconds);

            await UniTask.Delay(TimeSpan.FromSeconds(LeadSeconds), cancellationToken: token);

            for (var i = 0; i < shots; i++)
            {
                var target = stage.PickTarget();
                if (target == null)
                {
                    break;
                }

                Fire(stage, target, shot);
                await UniTask.Delay(TimeSpan.FromSeconds(1f / ShotsPerSecond), cancellationToken: token);
            }

            await motion;
        }

        /// <summary>쏘는 쪽으로 몸을 돌리고 이 활에서 한 발 날린다. 무대 위에 그려야 궁극기 배경에 안 묻힌다.</summary>
        private void Fire(UltimateStage stage, Enemy target, BasicAttack shot)
        {
            stage.Hero.Look(target.transform.position - stage.Hero.transform.position);

            var muzzle = transform.position + transform.up * MuzzleForward + (Vector3)(Random.insideUnitCircle * MuzzleSpread);

            var arrow = Instantiate(_projectile);
            arrow.SetSortingLayer(SortingLayers.UltimateEffect);
            arrow.Launch(muzzle, target.transform, () => stage.Hit(target, shot));
        }
    }
}
