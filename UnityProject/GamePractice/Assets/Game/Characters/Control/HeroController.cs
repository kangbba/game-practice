using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 히어로 반자동 컨트롤러. 입력원이 활성이면 수동 이동, 아니면 자동사냥 이동.
    /// 공격은 항상 사거리 내 자동. 순수 자동 = 입력원이 조용한 상태일 뿐이라 모드 전환 개념이 없다.
    /// </summary>
    public class HeroController
    {
        private const float StopDistanceFactor = 0.8f;

        private readonly Hero _hero;
        private readonly EnemyManager _enemyManager;
        private readonly IMoveInputSource _moveSource;

        public Hero Hero => _hero;

        public HeroController(Hero hero, EnemyManager enemyManager, IMoveInputSource moveSource)
        {
            _hero = hero;
            _enemyManager = enemyManager;
            _moveSource = moveSource;
        }

        public void UpdateControl()
        {
            if (_hero == null || !_hero.IsAlive.CurrentValue)
            {
                return;
            }

            if (_moveSource.IsActive)
            {
                _hero.Move(_moveSource.Direction);
            }
            else
            {
                AutoMove();
            }

            TryAutoAttack();
        }

        private void AutoMove()
        {
            var target = FindNearestEnemy(float.MaxValue);
            if (target == null)
            {
                _hero.StopMove();
                return;
            }

            var offset = ToTarget(target);
            if (offset.magnitude <= _hero.AttackRange * StopDistanceFactor)
            {
                _hero.StopMove();
                return;
            }

            _hero.Move(offset.normalized);
        }

        public void ManualAttack()
        {
            if (_hero == null || !_hero.IsAlive.CurrentValue)
            {
                return;
            }

            TryAutoAttack();
        }

        public void UseUltimate()
        {
            if (_hero == null)
            {
                return;
            }

            _hero.UseUltimate();
        }

        private void TryAutoAttack()
        {
            if (!_hero.CanAttack)
            {
                return;
            }

            var target = FindNearestEnemy(_hero.AttackRange);
            if (target == null)
            {
                return;
            }

            _hero.Attack();
            target.TakeDamage(_hero.AttackPower, _hero);
        }

        private Enemy FindNearestEnemy(float maxDistance)
        {
            Enemy nearest = null;
            var nearestDistance = maxDistance;

            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                if (!enemy.IsAlive.CurrentValue)
                {
                    continue;
                }

                var distance = ToTarget(enemy).magnitude;
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
            var offset = enemy.transform.position - _hero.transform.position;
            offset.y = 0f;
            return offset;
        }
    }
}
