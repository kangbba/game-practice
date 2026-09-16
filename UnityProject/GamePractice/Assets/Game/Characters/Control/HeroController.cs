using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 히어로 반자동 컨트롤러. 입력원이 활성이면 수동 이동, 아니면 자동사냥 이동.
    /// 공격은 항상 사거리 내 자동. 순수 자동 = 입력원이 조용한 상태일 뿐이라 모드 전환 개념이 없다.
    /// 자동일 때 궁극기는 쿨이 돌아온 뒤 사이클 한 바퀴(고유스킬까지)를 마치고 나간다 — 그 시점에 버튼을 누른 것과 같다.
    /// </summary>
    public class HeroController
    {
        private const float StopDistanceFactor = 0.8f;

        /// <summary>타격이 퍼지는 반경. 타겟 주위에 있는 적도 같이 맞는다.</summary>
        private const float SplashRadius = 1.2f;

        private readonly EnemyManager _enemyManager;
        private readonly IMoveInputSource _moveSource;
        private bool _isUltimatePending;

        public Hero Hero { get; }

        public HeroController(Hero hero, EnemyManager enemyManager, IMoveInputSource moveSource)
        {
            Hero = hero;
            _enemyManager = enemyManager;
            _moveSource = moveSource;

            // ② 맞는 순간에 대상을 다시 찾는다 — 그 사이 도망갔거나 죽었으면 헛친다.
            hero.Combat.HitMoment
                .Subscribe(this, (attack, self) => self.ApplyHit(attack))
                .RegisterTo(hero.destroyCancellationToken);
        }

        /// <summary>③ 타겟이 아직 사거리에 있으면 그놈과 주변에 피해를 준다. 벗어났으면 헛친다.</summary>
        private void ApplyHit(BasicAttack attack)
        {
            var target = Hero.Target as Enemy;
            if (target == null || !target.IsAlive || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                if (enemy.IsAlive && SplashDistance(target, enemy) <= SplashRadius)
                {
                    Hero.Combat.Hit(enemy, attack);
                }
            }
        }

        /// <summary>타겟에서 얼마나 떨어져 있나. 깊이는 사거리와 같은 기준으로 좁게 본다.</summary>
        private static float SplashDistance(Enemy target, Enemy other)
        {
            var offset = other.transform.position - target.transform.position;
            offset.y = 0f;

            return CharacterCombat.DistanceOf(offset);
        }

        public void UpdateControl()
        {
            if (Hero == null || !Hero.IsAlive)
            {
                return;
            }

            // 수동 이동이 들어오면 교전을 끊는다. 손을 떼면 타겟 찾기부터 다시 시작한다.
            if (_moveSource.IsActive)
            {
                Hero.SetTarget(null);
                Hero.Move(_moveSource.Direction);
                return;
            }

            if (_isUltimatePending)
            {
                _isUltimatePending = false;
                if (!_moveSource.IsActive && Hero.Combat.CanUseUltimate)
                {
                    UseUltimate();
                    return;
                }
            }

            // ⓪ 싸울 상대를 정한다. 죽었거나 사라졌으면 다시 고른다.
            AcquireTarget();

            AutoMove();
            TryAutoAttack();
        }

        /// <summary>타겟이 없거나 유실됐으면 가장 가까운 적을 새로 잡는다. 살아 있으면 그놈을 계속 쫓는다.</summary>
        private void AcquireTarget()
        {
            if (Hero.Target is Enemy current && current != null && current.IsAlive)
            {
                return;
            }

            Hero.SetTarget(FindNearestEnemy(float.MaxValue));
        }

        private void AutoMove()
        {
            if (Hero.Target is not Enemy target)
            {
                Hero.StopMove();
                return;
            }

            var offset = ToTarget(target);
            if (CharacterCombat.DistanceOf(offset) <= Hero.Combat.AttackRange * StopDistanceFactor)
            {
                Hero.StopMove();
                return;
            }

            Hero.Move(offset.normalized);
        }

        public void ManualAttack()
        {
            if (Hero == null || !Hero.IsAlive)
            {
                return;
            }

            TryAutoAttack();
        }

        /// <summary>궁극기 버튼이 부르는 곳. 자동전투도 사이클을 한 바퀴 돌 때마다 같은 함수를 부른다.</summary>
        public void UseUltimate()
        {
            if (Hero == null)
            {
                return;
            }

            Hero.Combat.UseUltimate();
        }

        private void TryAutoAttack()
        {
            if (!Hero.Combat.CanAttack)
            {
                return;
            }

            // ① 타겟이 사거리에 없으면 휘두르지 않는다.
            if (Hero.Target is not Enemy target || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            Hero.Look(ToTarget(target));

            var isCycleEnd = Hero.Combat.Cycle.IsAtLast;
            Hero.Combat.Attack();

            if (!_moveSource.IsActive && isCycleEnd)
            {
                _isUltimatePending = true;
            }
        }

        private Enemy FindNearestEnemy(float maxDistance)
        {
            Enemy nearest = null;
            var nearestDistance = maxDistance;

            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                var distance = CharacterCombat.DistanceOf(ToTarget(enemy));
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private Vector3 ToTarget(Enemy enemy)
        {
            var offset = enemy.transform.position - Hero.transform.position;
            offset.y = 0f;
            return offset;
        }
    }
}
