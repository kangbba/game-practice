using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyAIManager : ManagerBase
    {
        private const float RetargetIntervalMin = 1.5f;
        private const float RetargetIntervalMax = 3f;
        private const float AttackRange = 2.5f;
        private const float SeparationRadius = 3f;
        private const float SeparationWeight = 2.5f;
        private const float AimError = 1.5f;

        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        private readonly Dictionary<Enemy, float> _nextThinkTime = new Dictionary<Enemy, float>();
        private readonly Dictionary<Enemy, Vector3> _aimPoint = new Dictionary<Enemy, Vector3>();

        public EnemyAIManager(HeroManager heroManager, EnemyManager enemyManager)
        {
            _heroManager = heroManager;
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(this, (_, self) => self.UpdateAI())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _nextThinkTime.Clear();
            _aimPoint.Clear();
        }

        private void UpdateAI()
        {
            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                if (!enemy.IsAlive.CurrentValue)
                {
                    continue;
                }

                if (ShouldThink(enemy))
                {
                    Think(enemy);
                }

                enemy.Move(DesiredDirection(enemy));
                TryAttack(enemy);
            }
        }

        private static void TryAttack(Enemy enemy)
        {
            var target = enemy.Target;
            if (target == null || !target.IsAlive.CurrentValue || !enemy.CanAttack)
            {
                return;
            }

            var offset = target.transform.position - enemy.transform.position;
            offset.y = 0f;

            if (offset.magnitude > enemy.AttackRange)
            {
                return;
            }

            enemy.Attack();
            target.TakeDamage(enemy.AttackPower, enemy);
        }

        private bool ShouldThink(Enemy enemy)
        {
            if (!_nextThinkTime.TryGetValue(enemy, out var nextTime))
            {
                return true;
            }

            return Time.time >= nextTime;
        }

        private void Think(Enemy enemy)
        {
            _nextThinkTime[enemy] = Time.time + Random.Range(RetargetIntervalMin, RetargetIntervalMax);

            var target = FindNearestHero(enemy.transform.position);
            enemy.SetTarget(target);

            if (target == null)
            {
                _aimPoint.Remove(enemy);
                return;
            }

            var scatter = Random.insideUnitCircle * AimError;
            _aimPoint[enemy] = target.transform.position + new Vector3(scatter.x, 0f, scatter.y);
        }

        private Hero FindNearestHero(Vector3 from)
        {
            Hero nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (!hero.IsAlive.CurrentValue)
                {
                    continue;
                }

                var distance = (hero.transform.position - from).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = hero;
                }
            }

            return nearest;
        }

        private Vector3 DesiredDirection(Enemy enemy)
        {
            var separation = Separation(enemy);

            var target = enemy.Target;
            if (target == null || !_aimPoint.TryGetValue(enemy, out var aim))
            {
                return separation;
            }

            // 멈출지는 실제 타겟까지의 거리로 정하고, 흩어진 조준점은 경로만 흔든다.
            var toTarget = target.transform.position - enemy.transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= AttackRange)
            {
                return separation;
            }

            var toAim = aim - enemy.transform.position;
            toAim.y = 0f;

            return toAim.normalized + separation;
        }

        private Vector3 Separation(Enemy enemy)
        {
            var push = Vector3.zero;

            foreach (var other in _enemyManager.CurrentEnemies)
            {
                if (other == enemy)
                {
                    continue;
                }

                var away = enemy.transform.position - other.transform.position;
                away.y = 0f;

                var distance = away.magnitude;
                if (distance >= SeparationRadius || distance <= Mathf.Epsilon)
                {
                    continue;
                }

                push += away.normalized * ((SeparationRadius - distance) / SeparationRadius);
            }

            return push * SeparationWeight;
        }
    }
}
