using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyAIManager : ManagerBase
    {
        private const float RetargetIntervalMin = 1.5f;
        private const float RetargetIntervalMax = 3f;
        private const float SeparationRadius = 3f;
        private const float SeparationWeight = 2.5f;
        private const float AimError = 1.5f;

        private readonly PauseManager _pauseManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        private readonly Dictionary<Enemy, float> _nextThinkTime = new Dictionary<Enemy, float>();
        private readonly Dictionary<Enemy, Vector3> _aimPoint = new Dictionary<Enemy, Vector3>();

        public EnemyAIManager(PauseManager pauseManager, HeroManager heroManager, EnemyManager enemyManager)
        {
            _pauseManager = pauseManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            // ② 적도 맞는 순간에 다시 판정한다.
            _enemyManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Enemy enemy)
                    {
                        enemy.Combat.HitMoment
                            .Subscribe((self, enemy), (attack, state) => state.self.ApplyHit(state.enemy, attack))
                            .RegisterTo(enemy.destroyCancellationToken);
                    }
                })
                .RegisterTo(LifeToken);

            // 중단 중에도 Update 는 돈다 — 멈춘 게임에서 적이 판단을 내리면 안 된다.
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Where(_ => !_pauseManager.IsPaused.CurrentValue)
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
                if (ShouldThink(enemy))
                {
                    Think(enemy);
                }

                enemy.Move(DesiredDirection(enemy));
                TryAttack(enemy);
            }
        }

        /// <summary>③ 판정에 걸린 대상에게 피해를 준다. 사이 벌어졌으면 헛친다.</summary>
        private void ApplyHit(Enemy enemy, BasicAttack attack)
        {
            var target = enemy.Target;
            if (target == null || !target.IsAlive)
            {
                return;
            }

            var offset = target.transform.position - enemy.transform.position;
            offset.y = 0f;

            if (enemy.Combat.IsInRange(offset))
            {
                enemy.Combat.Hit(target, attack);
            }
        }

        private static void TryAttack(Enemy enemy)
        {
            var target = enemy.Target;
            if (target == null || !target.IsAlive || !enemy.Combat.CanAttack)
            {
                return;
            }

            var offset = target.transform.position - enemy.transform.position;
            offset.y = 0f;

            if (!enemy.Combat.IsInRange(offset))
            {
                return;
            }

            enemy.Look(offset);
            enemy.Combat.Attack();
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
                if (!hero.IsAlive)
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

            // 사거리 안에서는 멈춘다 — 걸으면서는 못 때린다.
            if (enemy.Combat.IsInRange(toTarget))
            {
                return Vector3.zero;
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
